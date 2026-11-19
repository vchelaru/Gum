using System.Threading;

namespace Gum.Managers;

/// <summary>
/// A number that moves whenever the loaded project may have changed, so a result computed from the
/// project can be reused while it stays the same. <see cref="Gum.Plugins.PluginManager"/> moves it
/// around every plugin notification that can follow an edit (variable set, add, remove, rename, undo,
/// reload, project load), so an edit that tells plugins about itself moves it.
/// </summary>
public interface IProjectChangeVersion
{
    /// <summary>The current version; equal values mean no change was reported in between.</summary>
    long Version { get; }

    /// <summary>Records that the project may have changed.</summary>
    void MarkChanged();
}

/// <inheritdoc cref="IProjectChangeVersion"/>
public sealed class ProjectChangeVersion : IProjectChangeVersion
{
    private long _version;

    /// <inheritdoc/>
    public long Version => Interlocked.Read(ref _version);

    /// <inheritdoc/>
    public void MarkChanged() => Interlocked.Increment(ref _version);
}
