using Microsoft.Data.Sqlite;
using Tanaste.Domain.Aggregates;
using Tanaste.Domain.Entities;
using Tanaste.Domain.Enums;
using Tanaste.Storage.Contracts;

namespace Tanaste.Storage;

/// <summary>
/// ORM-less SQLite implementation of <see cref="IHubRepository"/>.
/// Loads all hubs with their child Works and each Work's CanonicalValues
/// using two sequential queries (no N+1) — same pattern as
/// <see cref="MediaAssetRepository"/>.
/// </summary>
public sealed class HubRepository : IHubRepository
{
    private readonly IDatabaseConnection _db;

    public HubRepository(IDatabaseConnection db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Hub>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var conn  = _db.Open();
        var hubs  = new Dictionary<Guid, Hub>();
        var works = new Dictionary<Guid, Work>();

        // ── Query A: all hubs LEFT JOIN their works ───────────────────────────
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT h.id, h.universe_id, h.created_at,
                       w.id, w.media_type, w.sequence_index
                FROM   hubs h
                LEFT JOIN works w ON w.hub_id = h.id
                ORDER  BY h.created_at, w.id;
                """;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var hubId = Guid.Parse(reader.GetString(0));
                if (!hubs.TryGetValue(hubId, out var hub))
                {
                    hub = new Hub
                    {
                        Id         = hubId,
                        UniverseId = reader.IsDBNull(1) ? null : Guid.Parse(reader.GetString(1)),
                        CreatedAt  = DateTimeOffset.Parse(reader.GetString(2)),
                    };
                    hubs[hubId] = hub;
                }

                // LEFT JOIN: work columns are NULL when the hub has no works.
                if (!reader.IsDBNull(3))
                {
                    var workId = Guid.Parse(reader.GetString(3));
                    if (!works.ContainsKey(workId))
                    {
                        var work = new Work
                        {
                            Id            = workId,
                            HubId         = hubId,
                            MediaType     = Enum.Parse<MediaType>(reader.GetString(4), ignoreCase: true),
                            SequenceIndex = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        };
                        works[workId] = work;
                        hub.Works.Add(work);
                    }
                }
            }
        }

        // ── Query B: canonical values for all loaded works ────────────────────
        if (works.Count > 0)
        {
            var workIds    = works.Keys.ToList();
            var paramNames = workIds.Select((_, i) => $"@p{i}").ToList();

            using var cmd2 = conn.CreateCommand();
            cmd2.CommandText = $"""
                SELECT entity_id, key, value, last_scored_at
                FROM   canonical_values
                WHERE  entity_id IN ({string.Join(", ", paramNames)});
                """;

            for (int i = 0; i < workIds.Count; i++)
                cmd2.Parameters.AddWithValue($"@p{i}", workIds[i].ToString());

            using var reader2 = cmd2.ExecuteReader();
            while (reader2.Read())
            {
                var entityId = Guid.Parse(reader2.GetString(0));
                if (works.TryGetValue(entityId, out var work))
                {
                    work.CanonicalValues.Add(new CanonicalValue
                    {
                        EntityId     = entityId,
                        Key          = reader2.GetString(1),
                        Value        = reader2.GetString(2),
                        LastScoredAt = DateTimeOffset.Parse(reader2.GetString(3)),
                    });
                }
            }
        }

        IReadOnlyList<Hub> result = hubs.Values.ToList();
        return Task.FromResult(result);
    }
}
