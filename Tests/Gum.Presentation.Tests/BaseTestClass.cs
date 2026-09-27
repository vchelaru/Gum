using Gum.Managers;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Headless base for Gum.Presentation tests that exercise logic depending on the shared
/// GumCommon singletons. Initializes <see cref="StandardElementsManager"/> (so element/state
/// defaults resolve) and clears <see cref="ObjectFinder"/>'s project on both construction and
/// dispose to keep the singleton from leaking state across tests. Resetting only on dispose
/// depends on every prior test's own cleanup succeeding — a test that throws before reaching
/// its dispose, or one that never derives from this class, leaves the static dirty for
/// whichever test runs next. Resetting on construction too means a test's starting state
/// doesn't depend on any other test's history. Both singletons live in GumCommon, so this
/// stays within the headless boundary — no WPF/WinForms required.
/// <para>
/// Also restores <see cref="FileManager.UserApplicationDataFolderOverride"/> on dispose, so a test
/// may point it anywhere (or clear it) without sending later tests out of the process's temp
/// user-data folder.
/// </para>
/// </summary>
public class BaseTestClass : IDisposable
{
    private readonly string? _originalUserDataOverride;

    public BaseTestClass()
    {
        _originalUserDataOverride = FileManager.UserApplicationDataFolderOverride;
        ObjectFinder.Self.GumProjectSave = null;
        StandardElementsManager.Self.Initialize();
    }

    public virtual void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        FileManager.UserApplicationDataFolderOverride = _originalUserDataOverride;
    }
}
