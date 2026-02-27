using System.Text.Json.Serialization;

namespace Tanaste.Storage.Models;

/// <summary>
/// Root model for <c>tanaste_master.json</c>.
/// Contains environment-level bootstrap settings for the Tanaste platform.
/// Spec: Phase 4 – Configuration Management responsibility.
/// </summary>
public sealed class TanasteMasterManifest
{
    /// <summary>Manifest format version. Increment when the shape changes.</summary>
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// Path to the SQLite database file.
    /// Relative paths are resolved from the manifest's own directory.
    /// Spec: "database file and tanaste_master.json MUST reside in the same
    /// root directory or a designated application data folder."
    /// </summary>
    [JsonPropertyName("database_path")]
    public string DatabasePath { get; set; } = "tanaste.db";

    /// <summary>
    /// Root directory for media file storage.
    /// No BLOBs are stored in the database; all binaries live here.
    /// </summary>
    [JsonPropertyName("data_root")]
    public string DataRoot { get; set; } = "./media";

    /// <summary>Provider bootstrap entries loaded before the provider_registry table is queried.</summary>
    [JsonPropertyName("providers")]
    public List<ProviderBootstrap> Providers { get; set; } = [];

    /// <summary>Settings governing background maintenance tasks.</summary>
    [JsonPropertyName("maintenance")]
    public MaintenanceSettings Maintenance { get; set; } = new();

    /// <summary>
    /// Thresholds and tuning parameters for the Intelligence &amp; Scoring Engine.
    /// Spec: Phase 6 – Threshold Enforcement; Weight Management.
    /// </summary>
    [JsonPropertyName("scoring")]
    public ScoringSettings Scoring { get; set; } = new();
}

/// <summary>
/// Lightweight descriptor for a provider that should be registered on first run.
/// Full configuration lives in <c>provider_config</c> (database).
/// </summary>
public sealed class ProviderBootstrap
{
    /// <summary>Must match <c>provider_registry.name</c>.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>Maps to <c>provider_registry.is_enabled</c>.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default scoring weight for this provider.
    /// Consumed by the scoring engine; not stored in this table.
    /// </summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; } = 1.0;
}

/// <summary>
/// Threshold and decay parameters for the Phase 6 Intelligence &amp; Scoring Engine.
/// All thresholds are in the [0.0, 1.0] probability range.
/// </summary>
public sealed class ScoringSettings
{
    /// <summary>
    /// Minimum confidence score required for the arbiter to automatically link
    /// a Work to an existing Hub without human review.
    /// Spec: Phase 6 – Hub Integrity invariant.
    /// </summary>
    [JsonPropertyName("auto_link_threshold")]
    public double AutoLinkThreshold { get; set; } = 0.85;

    /// <summary>
    /// Scores at or above this value but below <see cref="AutoLinkThreshold"/>
    /// are flagged as NeedsReview rather than auto-linked or rejected.
    /// Spec: Phase 6 – Low Confidence Flags.
    /// </summary>
    [JsonPropertyName("conflict_threshold")]
    public double ConflictThreshold { get; set; } = 0.60;

    /// <summary>
    /// When the runner-up value's normalised weight is within this margin of the
    /// winner's weight, the field is flagged as conflicted.
    /// Smaller values = stricter conflict detection.
    /// </summary>
    [JsonPropertyName("conflict_epsilon")]
    public double ConflictEpsilon { get; set; } = 0.05;

    /// <summary>
    /// Claims older than this many days receive a time-decay multiplier.
    /// Set to 0 to disable stale-claim decay entirely.
    /// Spec: Phase 6 – Stale Claim Handling.
    /// </summary>
    [JsonPropertyName("stale_claim_decay_days")]
    public int StaleClaimDecayDays { get; set; } = 90;

    /// <summary>
    /// Weight multiplier applied to claims older than <see cref="StaleClaimDecayDays"/>.
    /// Must be in (0.0, 1.0]; default 0.8 reduces stale-claim influence by 20 %.
    /// </summary>
    [JsonPropertyName("stale_claim_decay_factor")]
    public double StaleClaimDecayFactor { get; set; } = 0.8;
}

/// <summary>Parameters for background housekeeping tasks.</summary>
public sealed class MaintenanceSettings
{
    /// <summary>
    /// <c>transaction_log</c> rows beyond this threshold are pruned.
    /// Spec: "SHOULD be archived or truncated after reaching 100,000 entries."
    /// </summary>
    [JsonPropertyName("max_transaction_log_entries")]
    public int MaxTransactionLogEntries { get; set; } = 100_000;

    /// <summary>
    /// When <c>true</c>, a VACUUM is issued during the startup sequence.
    /// Spec: "SHOULD perform a VACUUM during low-activity maintenance windows."
    /// </summary>
    [JsonPropertyName("vacuum_on_startup")]
    public bool VacuumOnStartup { get; set; } = false;
}
