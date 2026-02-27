using System.Reflection;
using Microsoft.Data.Sqlite;
using Tanaste.Storage.Contracts;

namespace Tanaste.Storage;

/// <summary>
/// Manages the lifecycle of the SQLite connection.
/// Implements WAL mode, startup PRAGMAs, and idempotent schema initialisation.
/// ORM-less: all SQL is executed directly via <see cref="SqliteCommand"/>.
/// Spec: Phase 4 – IDatabaseConnection interface.
/// </summary>
public sealed class DatabaseConnection : IDatabaseConnection
{
    private readonly string _databasePath;
    private SqliteConnection? _connection;

    /// <param name="databasePath">
    /// Absolute or relative path to the <c>.db</c> file.
    /// Typically sourced from <c>TanasteMasterManifest.DatabasePath</c>.
    /// </param>
    public DatabaseConnection(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = databasePath;
    }

    // -------------------------------------------------------------------------
    // IDatabaseConnection
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    public SqliteConnection Open()
    {
        if (_connection is not null)
            return _connection;

        _connection = new SqliteConnection($"Data Source={_databasePath}");
        _connection.Open();

        // Spec: "SQLite MUST be configured in Write-Ahead Logging mode."
        // Also enforce foreign keys and keep temp tables in RAM.
        using var pragmaCmd = _connection.CreateCommand();
        pragmaCmd.CommandText =
            "PRAGMA journal_mode = WAL; " +
            "PRAGMA foreign_keys = ON; " +
            "PRAGMA temp_store = MEMORY;";
        pragmaCmd.ExecuteNonQuery();

        return _connection;
    }

    /// <inheritdoc/>
    public void InitializeSchema()
    {
        var conn = Open();
        var ddl = LoadEmbeddedSchema();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = ddl;
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>PRAGMA integrity_check</c> returns anything other than "ok".
    /// </exception>
    public void RunStartupChecks()
    {
        var conn = Open();

        // PRAGMA integrity_check
        using var integrityCmd = conn.CreateCommand();
        integrityCmd.CommandText = "PRAGMA integrity_check;";
        var result = integrityCmd.ExecuteScalar()?.ToString();

        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"SQLite integrity_check failed for '{_databasePath}': {result}");

        // PRAGMA optimize – hints the query planner; safe to run on every start.
        using var optimizeCmd = conn.CreateCommand();
        optimizeCmd.CommandText = "PRAGMA optimize;";
        optimizeCmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Executes a VACUUM to reclaim unused pages.
    /// Spec: "SHOULD perform a VACUUM during low-activity maintenance windows."
    /// Call when <c>MaintenanceSettings.VacuumOnStartup</c> is <c>true</c>.
    /// </summary>
    public void Vacuum()
    {
        var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "VACUUM;";
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads <c>Schema/schema.sql</c> from the assembly's embedded resources.
    /// The resource is registered in the .csproj as an EmbeddedResource so the
    /// DDL ships inside the DLL and requires no file-system deployment.
    /// </summary>
    private static string LoadEmbeddedSchema()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Resource name follows the default convention:
        //   <RootNamespace>.<folder-path-with-dots>.<filename>
        //   → "Tanaste.Storage.Schema.schema.sql"
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("schema.sql", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                "Embedded resource 'schema.sql' was not found in the assembly. " +
                "Ensure Schema\\schema.sql is marked as EmbeddedResource in the .csproj.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
