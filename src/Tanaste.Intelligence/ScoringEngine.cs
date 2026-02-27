using Tanaste.Intelligence.Contracts;
using Tanaste.Intelligence.Models;

namespace Tanaste.Intelligence;

/// <summary>
/// Provider-agnostic engine that scores all metadata claims for an entity
/// and produces a <see cref="ScoringResult"/> ready for Hub arbitration.
///
/// ──────────────────────────────────────────────────────────────────
/// Processing pipeline (spec: Phase 6 – Claim Arbitration)
/// ──────────────────────────────────────────────────────────────────
///  For each entity:
///  1. Group claims by <c>ClaimKey</c>.
///  2. For each field-group, delegate to <see cref="IConflictResolver.Resolve"/>.
///  3. Map each resolution to a <see cref="FieldScore"/>.
///  4. Compute <see cref="ScoringResult.OverallConfidence"/> = mean of per-field
///     confidences.
///
/// ──────────────────────────────────────────────────────────────────
/// Conflict isolation (spec: Phase 6 – Conflict Isolation invariant)
/// ──────────────────────────────────────────────────────────────────
///  Each field is scored independently; an exception on one field is caught,
///  the field is skipped, and scoring continues for remaining fields.
///
/// ──────────────────────────────────────────────────────────────────
/// Batch scoring
/// ──────────────────────────────────────────────────────────────────
///  <see cref="ScoreBatchAsync"/> fans out to <see cref="ScoreEntityAsync"/>
///  using <c>Task.WhenAll</c> — the caller should supply a reasonable number
///  of contexts to avoid excessive parallelism.
/// </summary>
public sealed class ScoringEngine : IScoringEngine
{
    private readonly IConflictResolver _resolver;

    public ScoringEngine(IConflictResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    // -------------------------------------------------------------------------
    // IScoringEngine
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public Task<ScoringResult> ScoreEntityAsync(ScoringContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);

        var fieldScores = new List<FieldScore>();

        // Group claims by field key, then resolve each group independently.
        var groups = context.Claims
            .GroupBy(c => c.ClaimKey, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            ct.ThrowIfCancellationRequested();

            var claimsForField = group.ToList();
            if (claimsForField.Count == 0) continue;

            // Spec: "A failure to score one specific field MUST NOT prevent
            //        the scoring of other fields within the same entity."
            ClaimResolution resolution;
            try
            {
                resolution = _resolver.Resolve(
                    group.Key,
                    claimsForField,
                    context.ProviderWeights,
                    context.Configuration);
            }
            catch (Exception)
            {
                // Log could be added here; for now we skip this field silently.
                continue;
            }

            fieldScores.Add(new FieldScore
            {
                Key               = group.Key,
                WinningValue      = resolution.WinningClaim.ClaimValue,
                Confidence        = resolution.AdjustedConfidence,
                WinningProviderId = resolution.WinningClaim.ProviderId,
                IsConflicted      = resolution.IsConflicted,
            });
        }

        double overallConfidence = fieldScores.Count > 0
            ? fieldScores.Average(f => f.Confidence)
            : 0.0;

        var result = new ScoringResult
        {
            EntityId          = context.EntityId,
            FieldScores       = fieldScores,
            OverallConfidence = overallConfidence,
            ScoredAt          = DateTimeOffset.UtcNow,
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ScoringResult>> ScoreBatchAsync(
        IEnumerable<ScoringContext> contexts,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        var tasks = contexts
            .Select(ctx => ScoreEntityAsync(ctx, ct));

        ScoringResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }
}
