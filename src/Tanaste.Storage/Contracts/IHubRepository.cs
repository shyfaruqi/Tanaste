using Tanaste.Domain.Aggregates;

namespace Tanaste.Storage.Contracts;

/// <summary>
/// Persistence contract for loading <see cref="Hub"/> aggregates with their
/// child Works and associated CanonicalValues.
/// Used exclusively by GET /hubs in Tanaste.Api.
/// </summary>
public interface IHubRepository
{
    /// <summary>
    /// Returns all hubs, each populated with their Works and each Work's
    /// CanonicalValues. Editions and MediaAssets are NOT loaded (not needed
    /// by the list endpoint; add a FindByIdAsync overload later if required).
    /// </summary>
    Task<IReadOnlyList<Hub>> GetAllAsync(CancellationToken ct = default);
}
