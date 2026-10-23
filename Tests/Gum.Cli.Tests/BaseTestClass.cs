using Gum.Tests.Shared;

namespace Gum.Cli.Tests;

/// <summary>
/// Base for tests that run CLI commands in this process. Commands load projects, and the screenshot
/// and svg commands initialize a runtime, which sets process-wide statics (ObjectFinder's project,
/// FileManager settings, GraphicalUiElement.MissingFileBehavior). Dispose restores them so a later
/// test does not depend on which command ran first (#6556). Also owns a temp directory per test.
/// </summary>
public class BaseTestClass : IDisposable
{
    // Captured before the constructor body runs, so it is the state the previous test left.
    private readonly FileManagerState _fileManagerState = FileManagerState.Capture();
    private readonly GraphicalUiElementState _graphicalUiElementState = GraphicalUiElementState.Capture();

    protected string TempDirectory { get; }

    public BaseTestClass()
    {
        TempDirectory = Path.Combine(Path.GetTempPath(), "GumCliTests", GetType().Name + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempDirectory);
    }

    public virtual void Dispose()
    {
        ObjectFinderState.Reset();
        _fileManagerState.Restore();
        _graphicalUiElementState.Restore();

        try
        {
            Directory.Delete(TempDirectory, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
