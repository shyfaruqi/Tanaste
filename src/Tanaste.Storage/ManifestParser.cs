using System.Text.Json;
using Tanaste.Storage.Contracts;
using Tanaste.Storage.Models;

namespace Tanaste.Storage;

/// <summary>
/// Reads and writes <c>tanaste_master.json</c> using <see cref="System.Text.Json"/>.
/// No external serialisation library is required.
///
/// Recovery behaviour (spec: "MUST attempt to load from .bak before halting"):
///   1. Try <c>tanaste_master.json</c>.
///   2. If missing/corrupt, try <c>tanaste_master.json.bak</c> and restore primary.
///   3. If both fail, throw <see cref="InvalidOperationException"/>.
///
/// Save behaviour:
///   • Rotates the current file to <c>.bak</c> before overwriting.
///
/// Spec: Phase 4 – IStorageManifest interface.
/// </summary>
public sealed class ManifestParser : IStorageManifest
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented        = true,
        AllowTrailingCommas  = true,
        ReadCommentHandling  = JsonCommentHandling.Skip,
        // Property names in the JSON use snake_case; handled via [JsonPropertyName].
    };

    private readonly string _manifestPath;
    private readonly string _backupPath;

    /// <param name="manifestPath">
    /// Full path to <c>tanaste_master.json</c>.
    /// The backup is derived by appending <c>.bak</c>.
    /// </param>
    public ManifestParser(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        _manifestPath = manifestPath;
        _backupPath   = manifestPath + ".bak";
    }

    // -------------------------------------------------------------------------
    // IStorageManifest
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public TanasteMasterManifest Load()
    {
        // Attempt 1 – primary file
        if (TryDeserialize(_manifestPath, out var manifest))
            return manifest!;

        // Attempt 2 – backup file; restore primary on success
        if (File.Exists(_backupPath) && TryDeserialize(_backupPath, out manifest))
        {
            // Restore the primary from the backup so subsequent saves work normally.
            File.Copy(_backupPath, _manifestPath, overwrite: true);
            return manifest!;
        }

        throw new InvalidOperationException(
            $"Cannot load '{_manifestPath}' or '{_backupPath}'. " +
            "Both files are missing or contain invalid JSON. " +
            "The system cannot bootstrap without a valid manifest.");
    }

    /// <inheritdoc/>
    public void Save(TanasteMasterManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        // Rotate current primary to backup before overwriting.
        if (File.Exists(_manifestPath))
            File.Copy(_manifestPath, _backupPath, overwrite: true);

        var json = JsonSerializer.Serialize(manifest, SerializerOptions);
        File.WriteAllText(_manifestPath, json);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static bool TryDeserialize(string path, out TanasteMasterManifest? manifest)
    {
        manifest = null;

        if (!File.Exists(path))
            return false;

        try
        {
            var json = File.ReadAllText(path);
            manifest = JsonSerializer.Deserialize<TanasteMasterManifest>(json, SerializerOptions);
            return manifest is not null;
        }
        catch (JsonException)
        {
            // File exists but is corrupt; signal caller to try the backup.
            return false;
        }
        catch (IOException)
        {
            // Transient read failure; treat as unavailable.
            return false;
        }
    }
}
