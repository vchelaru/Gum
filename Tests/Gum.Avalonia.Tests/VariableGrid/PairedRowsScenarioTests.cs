using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.DataTypes;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// The Variables tab shows X with Y and Width with Height in one row each, the units rows below them.
/// Each cell keeps its own set-value marker and detail text.
/// </summary>
public class PairedRowsScenarioTests
{
    private static double TopOf(VariableGridHarness grid, string memberName) =>
        grid.Row(memberName).TranslatePoint(new Point(0, 0), grid.Input.Window)!.Value.Y;

    private static double LeftOf(VariableGridHarness grid, string memberName) =>
        grid.Row(memberName).TranslatePoint(new Point(0, 0), grid.Input.Window)!.Value.X;

    private static double MarkerOpacity(VariableGridHarness grid, string memberName)
    {
        Grid frame = grid.Row(memberName).GetVisualAncestors().OfType<Grid>().First();
        return ((Border)frame.Children[0]).Child!.Opacity;
    }

    [AvaloniaFact]
    public void XWithY_AndWidthWithHeight_ShareARow_WithTheUnitsRowsBelow()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Container");
        grid.Select(label);
        grid.Input.Layout();

        TopOf(grid, "Y").ShouldBe(TopOf(grid, "X"), 0.5);
        LeftOf(grid, "X").ShouldBeLessThan(LeftOf(grid, "Y"));
        TopOf(grid, "Height").ShouldBe(TopOf(grid, "Width"), 0.5);
        LeftOf(grid, "Width").ShouldBeLessThan(LeftOf(grid, "Height"));
        TopOf(grid, "XUnits").ShouldBeGreaterThan(TopOf(grid, "X"));
        TopOf(grid, "YUnits").ShouldBeGreaterThan(TopOf(grid, "XUnits"));
        TopOf(grid, "WidthUnits").ShouldBeGreaterThan(TopOf(grid, "Width"));
    }

    [AvaloniaFact]
    public void TextureLeftWithTop_AndTextureWidthWithHeight_ShareARow()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave sprite = grid.Project.AddInstance(button, "Icon", "Sprite");
        grid.Select(sprite);
        grid.PickComboItem("TextureAddress", "Custom");
        grid.Input.Layout();

        TopOf(grid, "TextureTop").ShouldBe(TopOf(grid, "TextureLeft"), 0.5);
        LeftOf(grid, "TextureLeft").ShouldBeLessThan(LeftOf(grid, "TextureTop"));
        TopOf(grid, "TextureHeight").ShouldBe(TopOf(grid, "TextureWidth"), 0.5);
        LeftOf(grid, "TextureWidth").ShouldBeLessThan(LeftOf(grid, "TextureHeight"));
        TopOf(grid, "TextureWidth").ShouldBeGreaterThan(TopOf(grid, "TextureLeft"));
    }

    [AvaloniaTheory]
    [InlineData("Circle")]
    [InlineData("Rectangle")]
    public void GradientPoints_ShareARowPerPoint_WithTheUnitsRowsBelow(string baseType)
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave shape = grid.Project.AddInstance(button, "Shape", baseType);
        grid.Select(shape);
        grid.Input.Click(grid.Editor<CheckBoxDisplay>("UseGradient").CheckBox);
        grid.Settle();

        TopOf(grid, "GradientY1").ShouldBe(TopOf(grid, "GradientX1"), 0.5);
        LeftOf(grid, "GradientX1").ShouldBeLessThan(LeftOf(grid, "GradientY1"));
        TopOf(grid, "GradientY2").ShouldBe(TopOf(grid, "GradientX2"), 0.5);
        LeftOf(grid, "GradientX2").ShouldBeLessThan(LeftOf(grid, "GradientY2"));
        TopOf(grid, "GradientX2").ShouldBeGreaterThan(TopOf(grid, "GradientX1"));
        TopOf(grid, "GradientX1Units").ShouldBeGreaterThan(TopOf(grid, "GradientX1"));
        TopOf(grid, "GradientY1Units").ShouldBeGreaterThan(TopOf(grid, "GradientX1Units"));
    }

    /// <summary>
    /// Arc and the legacy Skia shapes come from the Skia plugin, which the harness does not load, so
    /// their rows are checked from their default states: pairs match by name, so both gradient
    /// members being present is what makes them share a row.
    /// </summary>
    [Theory]
    [InlineData("Arc")]
    [InlineData("ColoredCircle")]
    [InlineData("RoundedRectangle")]
    public void GradientPairs_AreInTheListOfPairs_AndBothMembersExistOnTheShape(string shape)
    {
        StateSave state = shape switch
        {
            "Arc" => StandardElementsManager.GetArcState(),
            "ColoredCircle" => StandardElementsManager.GetColoredCircleState(),
            _ => StandardElementsManager.GetRoundedRectangleState(),
        };
        List<string> names = state.Variables.Select(variable => variable.Name).ToList();

        foreach ((string first, string second) in new[] { ("GradientX1", "GradientY1"), ("GradientX2", "GradientY2") })
        {
            VariableGridRows.Pairs.ShouldContain(new RowPair(first, second));
            names.ShouldContain(first);
            names.ShouldContain(second);
        }
    }

    [AvaloniaFact]
    public void EditingX_ShowsTheSetMarkerOnXAlone_AndTabReachesY()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Container");
        grid.Select(label);
        MarkerOpacity(grid, "X").ShouldBe(0);
        MarkerOpacity(grid, "Y").ShouldBe(0);

        grid.TypeAndEnter("X", "42");

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Label.X").ShouldBe(42f);
        MarkerOpacity(grid, "X").ShouldBe(1);
        MarkerOpacity(grid, "Y").ShouldBe(0);

        grid.TextField("X").Focus();
        grid.Input.Press(Key.Tab, PhysicalKey.Tab);

        grid.TextField("Y").IsFocused.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void ReferenceOnY_ShowsItsDetailUnderYsCellOnly()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Project.AddInstance(button, "Leader", "Container");
        InstanceSave follower = grid.Project.AddInstance(button, "Follower", "Container");
        grid.Select(follower);

        grid.TypeLinesAndApply("VariableReferences", "Y = Leader.Y");

        grid.Member("Y").DetailText.ShouldBe("Set by Leader.Y");
        bool ShowsHint(string memberName) => grid.Row(memberName).GetVisualDescendants().OfType<TextBlock>()
            .Any(text => text.IsVisible && text.Inlines is { Count: > 0 });
        ShowsHint("Y").ShouldBeTrue();
        ShowsHint("X").ShouldBeFalse();
        MarkerOpacity(grid, "Y").ShouldBe(1);
        MarkerOpacity(grid, "X").ShouldBe(0);
        grid.ClickDetailLink("Y");
        grid.SelectedState.SelectedInstance!.Name.ShouldBe("Leader");
    }
}
