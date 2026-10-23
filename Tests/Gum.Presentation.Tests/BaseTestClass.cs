using Gum.Managers;
using Gum.Tests.Shared;
using Gum.Wireframe;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Headless base for Gum.Presentation tests that exercise logic depending on the shared
/// GumCommon singletons. Initializes <see cref="StandardElementsManager"/> (so element/state
/// defaults resolve) and clears <see cref="ObjectFinder"/>'s project and fallback Standard Elements
/// (<see cref="ObjectFinderState.Reset"/>) on both construction and
/// dispose to keep the singleton from leaking state across tests. Resetting only on dispose
/// depends on every prior test's own cleanup succeeding — a test that throws before reaching
/// its dispose, or one that never derives from this class, leaves the static dirty for
/// whichever test runs next. Resetting on construction too means a test's starting state
/// doesn't depend on any other test's history. Both singletons live in GumCommon, so this
/// stays within the headless boundary — no WPF/WinForms required.
/// <para>
/// Also restores the <see cref="FileManager"/> settings (<see cref="FileManager.RelativeDirectory"/>,
/// <see cref="FileManager.UserApplicationDataFolderOverride"/> and the rest of <see cref="FileManagerState"/>)
/// on dispose, so a test may point them anywhere without changing where later tests resolve files.
/// Likewise <see cref="GraphicalUiElementState"/>: the canvas size, which the wireframe layout reads, and
/// <see cref="GraphicalUiElement.MissingFileBehavior"/>.
/// </para>
/// </summary>
public class BaseTestClass : IDisposable
{
    private readonly FileManagerState _fileManagerState = FileManagerState.Capture();
    private readonly GraphicalUiElementState _graphicalUiElementState = GraphicalUiElementState.Capture();

    public BaseTestClass()
    {
        ObjectFinderState.Reset();
        StandardElementsManager.Self.Initialize();
    }

    public virtual void Dispose()
    {
        ObjectFinderState.Reset();
        _fileManagerState.Restore();
        _graphicalUiElementState.Restore();
    }
}
