using System.IO;

namespace Gum.Logic.FileWatch;

/// <summary>
/// Creates the <see cref="FileSystemWatcher"/>s that <see cref="FileWatchManager"/> configures and
/// subscribes to, so tests can supply watchers that raise events on demand.
/// </summary>
public interface IFileSystemWatcherFactory
{
    /// <summary>
    /// Returns a new, unconfigured watcher.
    /// </summary>
    FileSystemWatcher Create();
}

/// <inheritdoc/>
public class FileSystemWatcherFactory : IFileSystemWatcherFactory
{
    /// <inheritdoc/>
    public FileSystemWatcher Create() => new FileSystemWatcher();
}
