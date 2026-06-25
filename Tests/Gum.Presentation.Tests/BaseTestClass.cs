using Gum.Managers;

namespace Gum.Presentation.Tests;

/// <summary>
/// Headless base for Gum.Presentation tests that exercise logic depending on the shared
/// GumCommon singletons. Initializes <see cref="StandardElementsManager"/> (so element/state
/// defaults resolve) and clears <see cref="ObjectFinder"/>'s project on dispose to keep the
/// singletons from leaking state across tests. Both singletons live in GumCommon, so this
/// stays within the headless boundary — no WPF/WinForms required.
/// </summary>
public class BaseTestClass : IDisposable
{
    public BaseTestClass()
    {
        StandardElementsManager.Self.Initialize();
    }

    public virtual void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
