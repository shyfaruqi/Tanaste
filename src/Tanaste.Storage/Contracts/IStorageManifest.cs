using Tanaste.Storage.Models;

namespace Tanaste.Storage.Contracts;

/// <summary>
/// Defines access methods for the tanaste_master.json configuration file.
/// Spec: Phase 4 – Interfaces § IStorageManifest
/// </summary>
public interface IStorageManifest
{
    /// <summary>
    /// Loads the manifest from <c>tanaste_master.json</c>.
    /// Falls back to <c>tanaste_master.json.bak</c> if the primary is missing
    /// or corrupt, restoring the primary in the process.
    /// Throws <see cref="InvalidOperationException"/> if both files are unavailable.
    /// Spec: "MUST attempt to load from .bak before halting."
    /// </summary>
    TanasteMasterManifest Load();

    /// <summary>
    /// Serialises <paramref name="manifest"/> to <c>tanaste_master.json</c>.
    /// Rotates the previous file to <c>tanaste_master.json.bak</c> first.
    /// </summary>
    void Save(TanasteMasterManifest manifest);
}
