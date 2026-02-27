using Tanaste.Api.Models;
using Tanaste.Storage.Contracts;

namespace Tanaste.Api.Endpoints;

public static class MetadataEndpoints
{
    public static IEndpointRouteBuilder MapMetadataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/metadata")
                       .WithTags("Metadata");

        group.MapMethods("/resolve", ["PATCH"], async (
            ResolveRequest request,
            IDatabaseConnection db,
            ITransactionJournal journal,
            CancellationToken ct) =>
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request.ClaimKey))
                return Results.BadRequest("claim_key must not be empty.");

            if (string.IsNullOrWhiteSpace(request.ChosenValue))
                return Results.BadRequest("chosen_value must not be empty.");

            var resolvedAt = DateTimeOffset.UtcNow;

            var conn = db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO canonical_values (entity_id, key, value, last_scored_at)
                VALUES (@entity_id, @key, @value, @last_scored_at)
                ON CONFLICT(entity_id, key) DO UPDATE SET
                    value          = excluded.value,
                    last_scored_at = excluded.last_scored_at;
                """;
            cmd.Parameters.AddWithValue("@entity_id",      request.EntityId.ToString());
            cmd.Parameters.AddWithValue("@key",            request.ClaimKey);
            cmd.Parameters.AddWithValue("@value",          request.ChosenValue);
            cmd.Parameters.AddWithValue("@last_scored_at", resolvedAt.ToString("O"));
            cmd.ExecuteNonQuery();

            journal.Log(
                "CANONICAL_VALUE_MANUAL_RESOLVE",
                "CanonicalValue",
                request.EntityId.ToString());

            return Results.Ok(new ResolveResponse
            {
                EntityId    = request.EntityId,
                ClaimKey    = request.ClaimKey,
                ChosenValue = request.ChosenValue,
                ResolvedAt  = resolvedAt,
            });
        })
        .WithName("ResolveMetadataConflict")
        .WithSummary("Manually override a metadata canonical value, locking in the chosen value.")
        .Produces<ResolveResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}
