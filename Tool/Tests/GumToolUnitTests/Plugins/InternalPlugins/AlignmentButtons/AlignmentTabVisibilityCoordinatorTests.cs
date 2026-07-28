using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.Plugins.AlignmentButtons;
using Gum.ToolStates;
using Moq;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.AlignmentButtons;

public class AlignmentTabVisibilityCoordinatorTests : BaseTestClass
{
    [Fact]
    public void Refresh_HidesTab_WhenNothingIsSelected()
    {
        var selectedState = new Mock<ISelectedState>();
        var tab = new Mock<IPluginTab>();
        var coordinator = new AlignmentTabVisibilityCoordinator(selectedState.Object, tab.Object);

        coordinator.Refresh();

        tab.Verify(t => t.Hide(), Times.Once);
        tab.Verify(t => t.Show(), Times.Never);
    }

    [Fact]
    public void Refresh_HidesTab_WhenScreenIsSelectedWithNoInstance()
    {
        var screen = new ScreenSave();
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(s => s.SelectedElement).Returns(screen);
        selectedState.Setup(s => s.SelectedScreen).Returns(screen);
        selectedState.Setup(s => s.SelectedStateSave).Returns(new StateSave());
        var tab = new Mock<IPluginTab>();
        var coordinator = new AlignmentTabVisibilityCoordinator(selectedState.Object, tab.Object);

        coordinator.Refresh();

        tab.Verify(t => t.Hide(), Times.Once);
        tab.Verify(t => t.Show(), Times.Never);
    }

    [Fact]
    public void Refresh_ShowsTab_WhenInstanceIsAutoSelectedAfterAddOnAnAlreadySelectedScreen()
    {
        // Bug repro for issue #4067: right-clicking an already-selected Screen and adding an
        // object auto-selects the new instance. The coordinator's Refresh() must be called (by
        // the plugin's InstanceSelected handler) and must show the tab for the new instance.
        var screen = new ScreenSave();
        var instance = new InstanceSave();
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(s => s.SelectedElement).Returns(screen);
        selectedState.Setup(s => s.SelectedScreen).Returns(screen);
        selectedState.Setup(s => s.SelectedStateSave).Returns(new StateSave());
        selectedState.Setup(s => s.SelectedInstance).Returns(instance);
        var tab = new Mock<IPluginTab>();
        var coordinator = new AlignmentTabVisibilityCoordinator(selectedState.Object, tab.Object);

        coordinator.Refresh();

        tab.Verify(t => t.Show(), Times.Once);
        tab.Verify(t => t.Hide(), Times.Never);
    }
}
