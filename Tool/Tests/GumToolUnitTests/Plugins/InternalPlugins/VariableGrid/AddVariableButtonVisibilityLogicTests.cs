using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Moq;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.VariableGrid;

public class AddVariableButtonVisibilityLogicTests : BaseTestClass
{
    [Fact]
    public void ShouldShow_ReturnsFalse_WhenAnInstanceIsSelectedOnAScreen()
    {
        // Bug repro for issue #4067: right-clicking an already-selected Screen and adding an
        // object auto-selects the new instance. The Screen-only "Add Variable" button must hide.
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(s => s.SelectedScreen).Returns(new ScreenSave());
        selectedState.Setup(s => s.SelectedInstance).Returns(new InstanceSave());

        AddVariableButtonVisibilityLogic.ShouldShow(selectedState.Object).ShouldBeFalse();
    }

    [Fact]
    public void ShouldShow_ReturnsFalse_WhenNothingIsSelected()
    {
        var selectedState = new Mock<ISelectedState>();

        AddVariableButtonVisibilityLogic.ShouldShow(selectedState.Object).ShouldBeFalse();
    }

    [Fact]
    public void ShouldShow_ReturnsTrue_WhenScreenIsSelectedWithNoInstance()
    {
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(s => s.SelectedScreen).Returns(new ScreenSave());

        AddVariableButtonVisibilityLogic.ShouldShow(selectedState.Object).ShouldBeTrue();
    }
}
