using Tanaste.Ingestion.Models;

namespace Tanaste.Ingestion.Contracts;

/// <summary>
/// Interface for the OS-specific file system observation service.
/// Spec: Phase 7 – Interfaces § IFileWatcher.
///
/// Raises a <see cref="FileDetected"/> event for every raw OS notification.
/// Consumers (typically <see cref="DebounceQueue"/>) are responsible for
/// coalescing and gating the raw events before processing.
/// </summary>
public interface IFileWatcher : IDisposable
{
    /// <summary>
    /// Fired on the watcher's internal thread whenever the OS reports a
    /// file system change.  Handlers MUST be fast and non-blocking.
    /// </summary>
    event EventHandler<FileEvent> FileDetected;

    /// <summary>
    /// Registers a directory to be observed.
    /// May be called multiple times to watch several root paths.
    /// </summary>
    void AddDirectory(string path, bool includeSubdirectories = true);

    /// <summary>Begins raising <see cref="FileDetected"/> events.</summary>
    void Start();

    /// <summary>Pauses event raising without releasing OS resources.</summary>
    void Stop();
}
