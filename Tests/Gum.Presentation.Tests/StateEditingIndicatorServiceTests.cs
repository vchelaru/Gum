using System.Drawing;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class StateEditingIndicatorServiceTests : BaseTestClass
{
    [Fact]
    public void GetInfo_HasNoStateInformation_WhenNoElementIsSelected()
    {
        var selectedState = new Mock<ISelectedState>();
        var service = new StateEditingIndicatorService(selectedState.Object);

        var info = service.GetInfo();

        info.HasStateInformation.ShouldBeFalse();
    }

    [Fact]
    public void GetInfo_HasNoStateInformation_WhenSelectedStateIsTheDefaultState()
    {
        var element = new ScreenSave();
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(s => s.SelectedElement).Returns(element);
        selectedState.Setup(s => s.SelectedStateSave).Returns(element.DefaultState);
        var service = new StateEditingIndicatorService(selectedState.Object);

        var info = service.GetInfo();

        info.HasStateInformation.ShouldBeFalse();
    }

    [Fact]
    public void GetInfo_ReturnsPinkCustomStateMessage_WhenACustomCurrentStateSaveIsSet()
    {
        var element = new ScreenSave();
        var nonDefaultState = new StateSave();
        var customState = new StateSave();
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(s => s.SelectedElement).Returns(element);
        selectedState.Setup(s => s.SelectedStateSave).Returns(nonDefaultState);
        selectedState.Setup(s => s.CustomCurrentStateSave).Returns(customState);
        var service = new StateEditingIndicatorService(selectedState.Object);

        var info = service.GetInfo();

        info.HasStateInformation.ShouldBeTrue();
        info.StateInformation.ShouldBe("Displaying custom (animated) state");
        info.StateBackground.ShouldBe(Color.Pink);
    }

    [Fact]
    public void GetInfo_ReturnsYellowEditingStateMessage_WhenANonDefaultStateIsSelected()
    {
        var element = new ScreenSave();
        var nonDefaultState = new StateSave { Name = "MyState" };
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(s => s.SelectedElement).Returns(element);
        selectedState.Setup(s => s.SelectedStateSave).Returns(nonDefaultState);
        var service = new StateEditingIndicatorService(selectedState.Object);

        var info = service.GetInfo();

        info.HasStateInformation.ShouldBeTrue();
        info.StateInformation.ShouldBe("Editing state MyState");
        info.StateBackground.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void GetInfo_PrefixesStateNameWithCategoryName_WhenACategoryIsSelected()
    {
        var element = new ScreenSave();
        var category = new StateSaveCategory { Name = "MyCategory" };
        var nonDefaultState = new StateSave { Name = "MyState" };
        var selectedState = new Mock<ISelectedState>();
        selectedState.Setup(s => s.SelectedElement).Returns(element);
        selectedState.Setup(s => s.SelectedStateSave).Returns(nonDefaultState);
        selectedState.Setup(s => s.SelectedStateCategorySave).Returns(category);
        var service = new StateEditingIndicatorService(selectedState.Object);

        var info = service.GetInfo();

        info.StateInformation.ShouldBe("Editing state MyCategory/MyState");
    }
}
