using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Moq;
using Shouldly;
using System;

namespace GumToolUnitTests.ToolStates;

public class SelectedStateTests : BaseTestClass
{
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly Mock<IMessenger> _messenger;
    private readonly SelectedState _selectedState;

    public SelectedStateTests()
    {
        _guiCommands = new Mock<IGuiCommands>();
        _pluginManager = new Mock<IPluginManager>();
        _messenger = new Mock<IMessenger>();
        _selectedState = new SelectedState(
            _guiCommands.Object,
            _pluginManager.Object,
            _messenger.Object,
            // SelectedBehaviorVariable (the only PropertyGridManager use) is not exercised here,
            // so the factory is never invoked.
            new Lazy<PropertyGridManager>(() => null!));
    }

    [Fact]
    public void SelectedElement_SetWhileInstanceSelected_FiresInstanceSelectedWithNull()
    {
        // Regression for issue #2946: when an instance is selected and the SelectedElement
        // setter runs (e.g. DeleteLogic re-asserts the owning element after removing the
        // instance), HandleElementsSelected silently clears snapshot.SelectedInstance
        // without notifying listeners. The variable grid subscribes to InstanceSelected to
        // refresh and so keeps displaying the deleted instance's variables.
        ScreenSave screen = CreateScreen("TestScreen");
        InstanceSave instance = AddInstance(screen, "Inst");

        _selectedState.SelectedInstance = instance;
        _pluginManager.Invocations.Clear();

        // Re-asserting the same element silently clears SelectedInstance — the exact
        // path DeleteLogic.PerformConfirmedSingleInstanceDelete hits.
        _selectedState.SelectedElement = screen;

        _pluginManager.Verify(
            p => p.InstanceSelected(screen, null!),
            Times.Once);
    }

    [Fact]
    public void SelectedElement_SetToDifferentElementWhileInstanceSelected_FiresInstanceSelectedWithNull()
    {
        // Same gap applies when switching to a different element: the previously-selected
        // instance is silently cleared from the snapshot. Variable-grid refresh on a
        // different element runs via the state cascade, but listeners that need to know
        // the *instance* deselection (independent of state) still rely on the event.
        ScreenSave screenA = CreateScreen("ScreenA");
        InstanceSave instance = AddInstance(screenA, "Inst");
        ScreenSave screenB = CreateScreen("ScreenB");

        _selectedState.SelectedInstance = instance;
        _pluginManager.Invocations.Clear();

        _selectedState.SelectedElement = screenB;

        _pluginManager.Verify(
            p => p.InstanceSelected(screenB, null!),
            Times.Once);
    }

    [Fact]
    public void SelectedElement_FiresStateSelectedBeforeElementSelected()
    {
        // WireframeRefreshCoordinator (issue #3212) relies on the state event firing BEFORE the
        // element event when an element is selected: HandleStateSelected rebuilds the wireframe and
        // records the element, then HandleElementSelected skips its redundant rebuild for that same
        // element. If this cascade order is ever reversed, the element rebuild would no longer be
        // suppressed and the double-rebuild (and duplicate missing-file errors) would silently return.
        ScreenSave screen = CreateScreen("TestScreen");
        var invocationOrder = new List<string>();
        _pluginManager
            .Setup(p => p.ReactToStateSaveSelected(It.IsAny<StateSave>()))
            .Callback(() => invocationOrder.Add(nameof(IPluginManager.ReactToStateSaveSelected)));
        _pluginManager
            .Setup(p => p.ElementSelected(It.IsAny<ElementSave>()))
            .Callback(() => invocationOrder.Add(nameof(IPluginManager.ElementSelected)));

        _selectedState.SelectedElement = screen;

        invocationOrder.ShouldContain(nameof(IPluginManager.ReactToStateSaveSelected));
        invocationOrder.ShouldContain(nameof(IPluginManager.ElementSelected));
        invocationOrder.IndexOf(nameof(IPluginManager.ReactToStateSaveSelected))
            .ShouldBeLessThan(invocationOrder.IndexOf(nameof(IPluginManager.ElementSelected)));
    }

    [Fact]
    public void SelectedElement_SetWhileNoInstanceSelected_DoesNotFireInstanceSelected()
    {
        // The new InstanceSelected fire-on-silent-clear should only trigger when an
        // instance was actually selected. Setting SelectedElement with no prior instance
        // selection must not emit a spurious InstanceSelected event.
        ScreenSave screen = CreateScreen("TestScreen");

        _selectedState.SelectedElement = screen;

        _pluginManager.Verify(
            p => p.InstanceSelected(It.IsAny<ElementSave>(), It.IsAny<InstanceSave>()),
            Times.Never);
    }

    private static ScreenSave CreateScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (project == null)
        {
            project = new GumProjectSave();
            ObjectFinder.Self.GumProjectSave = project;
        }
        project.Screens.Add(screen);
        return screen;
    }

    private static InstanceSave AddInstance(ScreenSave screen, string name)
    {
        InstanceSave instance = new InstanceSave { Name = name, ParentContainer = screen };
        screen.Instances.Add(instance);
        return instance;
    }
}
