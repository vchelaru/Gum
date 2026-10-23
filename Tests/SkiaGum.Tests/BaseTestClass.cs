using Gum.Tests.Shared;

namespace SkiaGum.Tests;

/// <summary>
/// Base for SkiaGum test classes that change process-wide Gum state. Restores the
/// <see cref="ToolsUtilities.FileManager"/> settings and resets <see cref="Gum.Managers.ObjectFinder.Self"/>
/// when each test ends, so one test's relative directory, stream hook or loaded project does not change
/// what a later test loads (#6502).
/// </summary>
public class BaseTestClass : IDisposable
{
    // Captured before the constructor body runs, so it is the state the previous test left.
    private readonly FileManagerState _fileManagerState = FileManagerState.Capture();

    public virtual void Dispose()
    {
        _fileManagerState.Restore();
        ObjectFinderState.Reset();
    }
}
