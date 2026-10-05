using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaloniaDataUi.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>The gradient unit rows (#5654): radius units are a button strip of only the units the renderers honor.</summary>
public class GradientUnitsScenarioTests
{
    [AvaloniaFact]
    public void GradientRadiusUnits_AreAThreeButtonStrip_ThatSetsTheValue()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave circle = grid.Project.AddInstance(button, "Ring", "Circle");
        grid.Select(circle);
        grid.Input.Click(grid.Editor<CheckBoxDisplay>("UseGradient").CheckBox);
        grid.Settle();
        grid.PickComboItem("GradientType", "Radial");

        ToggleButtonOptionDisplay strip = grid.Editor<ToggleButtonOptionDisplay>("GradientOuterRadiusUnits");
        strip.Buttons.Count().ShouldBe(3);

        grid.PressToggle("GradientOuterRadiusUnits", DimensionUnitType.PercentageOfParent);

        VariableGridHarness.StoredValue(button, "Ring.GradientOuterRadiusUnits").ShouldBe(DimensionUnitType.PercentageOfParent);
    }

    [AvaloniaFact]
    public void GradientYUnits_OfferNoBaselineButton_ButPositionYUnitsStillDo()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave circle = grid.Project.AddInstance(button, "Ring", "Circle");
        grid.Select(circle);
        grid.Input.Click(grid.Editor<CheckBoxDisplay>("UseGradient").CheckBox);
        grid.Settle();

        grid.Editor<ToggleButtonOptionDisplay>("GradientY1Units").Buttons.Count().ShouldBe(4);
        grid.Editor<ToggleButtonOptionDisplay>("YUnits").Buttons.Count().ShouldBe(5);
    }
}
