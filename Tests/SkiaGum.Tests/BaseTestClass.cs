using Gum.Tests.Shared;
using RenderingLibrary.Content;

namespace SkiaGum.Tests;

/// <summary>
/// Base for SkiaGum test classes that change process-wide Gum state. Restores the
/// <see cref="ToolsUtilities.FileManager"/> settings and <see cref="LoaderManager.ContentLoader"/>, and resets
/// <see cref="Gum.Managers.ObjectFinder.Self"/>, when each test ends, so one test's relative directory, stream
/// hook, content loader or loaded project does not change what a later test loads (#6502, #6585).
/// </summary>
public class BaseTestClass : IDisposable
{
    // Captured before the constructor body runs, so it is the state the previous test left.
    private readonly FileManagerState _fileManagerState = FileManagerState.Capture();
    private readonly GraphicalUiElementState _graphicalUiElementState = GraphicalUiElementState.Capture();
    private readonly IContentLoader _contentLoader = CaptureContentLoader();

    public virtual void Dispose()
    {
        _fileManagerState.Restore();
        _graphicalUiElementState.Restore();
        ObjectFinderState.Reset();
        LoaderManager.Self.ContentLoader = _contentLoader;
    }

    // SystemManagers.Initialize assigns the default loader only on its first call in the process, so a
    // null captured before that call must not be restored: no later Initialize would assign it again.
    // Assigning the same default here first makes the captured loader the one Initialize would keep.
    private static IContentLoader CaptureContentLoader()
    {
        LoaderManager.Self.ContentLoader ??= new ContentLoader();
        return LoaderManager.Self.ContentLoader;
    }
}
