using Tanaste.Domain.Entities;

namespace Tanaste.Intelligence.Models;

/// <summary>
/// All inputs required by <see cref="Contracts.IScoringEngine.ScoreEntityAsync"/>.
///
/// Bundling inputs into a context object keeps the engine interface stable as
/// new tuning parameters are added, and makes unit testing straightforward.
///
/// Spec: Phase 6 – Provider Agnosticism (the engine sees only claims and weights,
/// never provider-specific logic).
/// </summary>
public sealed class ScoringContext
{
    /// <summary>
    /// The entity being scored — either a <c>Work.Id</c> or an <c>Edition.Id</c>.
    /// Copied verbatim into the returned <see cref="ScoringResult.EntityId"/>.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// All <see cref="MetadataClaim"/> records for <see cref="EntityId"/>.
    /// The engine groups these by <c>ClaimKey</c> internally.
    /// Must not be null; may be empty (yields an empty result with zero confidence).
    /// </summary>
    public required IReadOnlyList<MetadataClaim> Claims { get; init; }

    /// <summary>
    /// Map of <c>ProviderId → weight</c> for every provider whose claims appear
    /// in <see cref="Claims"/>.  Providers absent from this map default to weight 1.0.
    ///
    /// Typically populated from <see cref="Tanaste.Storage.Models.ProviderBootstrap.Weight"/>
    /// values loaded at startup.
    /// </summary>
    public required IReadOnlyDictionary<Guid, double> ProviderWeights { get; init; }

    /// <summary>
    /// Scoring thresholds and decay parameters for this evaluation.
    /// Use <see cref="ScoringConfiguration"/> defaults when no explicit config is available.
    /// </summary>
    public required ScoringConfiguration Configuration { get; init; }
}
