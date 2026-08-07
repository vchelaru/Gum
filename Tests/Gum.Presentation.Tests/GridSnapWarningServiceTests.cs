using System.Collections.Generic;
using Gum.Converters;
using Gum.Services;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

public class GridSnapWarningServiceTests : BaseTestClass
{
    [Fact]
    public void GetInfo_HasNoWarning_WhenSnapToGridIsDisabled()
    {
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.Setup(s => s.SnapToGrid).Returns(false);
        selectionManager.Setup(s => s.HasSelection).Returns(true);
        var service = new GridSnapWarningService(selectionManager.Object);

        var info = service.GetInfo();

        info.HasWarning.ShouldBeFalse();
    }

    [Fact]
    public void GetInfo_HasNoWarning_WhenNothingIsSelected()
    {
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.Setup(s => s.SnapToGrid).Returns(true);
        selectionManager.Setup(s => s.HasSelection).Returns(false);
        var service = new GridSnapWarningService(selectionManager.Object);

        var info = service.GetInfo();

        info.HasWarning.ShouldBeFalse();
    }

    [Fact]
    public void GetInfo_HasNoWarning_WhenSelectionUsesOnlyPixelUnits()
    {
        GraphicalUiElement gue = new(new InvisibleRenderable())
        {
            Name = "MySprite",
            XUnits = GeneralUnitType.PixelsFromSmall,
            YUnits = GeneralUnitType.PixelsFromSmall,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute
        };
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.Setup(s => s.SnapToGrid).Returns(true);
        selectionManager.Setup(s => s.HasSelection).Returns(true);
        selectionManager.Setup(s => s.SelectedGues).Returns(new List<GraphicalUiElement> { gue });
        var service = new GridSnapWarningService(selectionManager.Object);

        var info = service.GetInfo();

        info.HasWarning.ShouldBeFalse();
    }

    [Fact]
    public void GetInfo_ReturnsWarningNamingTheInstance_WhenOneSelectedObjectUsesANonPixelUnit()
    {
        GraphicalUiElement gue = new(new InvisibleRenderable())
        {
            Name = "MySprite",
            XUnits = GeneralUnitType.Percentage,
            YUnits = GeneralUnitType.PixelsFromSmall,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute
        };
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.Setup(s => s.SnapToGrid).Returns(true);
        selectionManager.Setup(s => s.HasSelection).Returns(true);
        selectionManager.Setup(s => s.SelectedGues).Returns(new List<GraphicalUiElement> { gue });
        var service = new GridSnapWarningService(selectionManager.Object);

        var info = service.GetInfo();

        info.HasWarning.ShouldBeTrue();
        info.WarningText.ShouldBe("Snap to Grid: MySprite uses non-pixel units and won't fully snap");
    }

    [Fact]
    public void GetInfo_ReturnsGenericWarning_WhenMultipleObjectsAreSelectedAndAnyUsesANonPixelUnit()
    {
        GraphicalUiElement pixelGue = new(new InvisibleRenderable())
        {
            Name = "PixelObject",
            XUnits = GeneralUnitType.PixelsFromSmall,
            YUnits = GeneralUnitType.PixelsFromSmall,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute
        };
        GraphicalUiElement percentageGue = new(new InvisibleRenderable())
        {
            Name = "PercentageObject",
            XUnits = GeneralUnitType.Percentage,
            YUnits = GeneralUnitType.Percentage,
            WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent,
            HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent
        };
        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.Setup(s => s.SnapToGrid).Returns(true);
        selectionManager.Setup(s => s.HasSelection).Returns(true);
        selectionManager.Setup(s => s.SelectedGues).Returns(new List<GraphicalUiElement> { pixelGue, percentageGue });
        var service = new GridSnapWarningService(selectionManager.Object);

        var info = service.GetInfo();

        info.HasWarning.ShouldBeTrue();
        info.WarningText.ShouldBe("Snap to Grid: one or more selected objects use non-pixel units and won't fully snap");
    }
}
