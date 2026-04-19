using RenderingLibrary.Content;

namespace SokolGum.Tests;

/// <summary>
/// Resets process-wide statics between tests so each test starts from a
/// clean slate. SokolGum's backend is simpler than RaylibGum's (no input
/// subsystem, no Forms state) so the teardown is minimal — just the
/// texture cache, which many tests never touch but one poisoning it
/// would break every test downstream.
/// </summary>
public abstract class BaseTestClass : IDisposable
{
    public virtual void Dispose()
    {
        // Toggling CacheTextures off-then-on is the idiomatic way to flush
        // LoaderManager's disposable dictionary between tests (matches
        // RaylibGum's pattern).
        LoaderManager.Self.CacheTextures = false;
        LoaderManager.Self.CacheTextures = true;
    }
}
