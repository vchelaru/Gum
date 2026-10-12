using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The Variables tab's color picker driven by the mouse: clicks and drags on the saturation/value
/// square and the hue bar, checked against the picker's color, the saved Red/Green/Blue, and undo.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ColorPickerScenarioTests
{
    // Pixels in from a control's edge, so a click lands inside it rather than on its border.
    private const double Inset = 2;

    [AvaloniaFact]
    [Trait("Feature", "DISP-016")]
    public void ClickingTheSpectrum_PicksSaturationAndValue_AtTheCornersAndCenter()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (VariableGridHarness grid, CompactColorPicker picker) = OpenPicker(tree);
        Control square = Square(picker);

        // The square is the red hue's: white has no hue, and a click leaves the hue alone.
        Click(grid, square, 1.0, 0.0);
        ShouldBeNear(picker.Color, Color.FromRgb(255, 0, 0), tolerance: 8);

        Click(grid, square, 0.0, 0.0);
        ShouldBeNear(picker.Color, Color.FromRgb(255, 255, 255), tolerance: 8);

        Click(grid, square, 0.0, 1.0);
        ShouldBeNear(picker.Color, Color.FromRgb(0, 0, 0), tolerance: 8);

        Click(grid, square, 1.0, 1.0);
        ShouldBeNear(picker.Color, Color.FromRgb(0, 0, 0), tolerance: 8);

        Click(grid, square, 0.5, 0.5);
        ShouldBeNear(picker.Color, Color.FromRgb(128, 64, 64), tolerance: 8);
        picker.Spectrum.HsvColor.H.ShouldBe(0, tolerance: 0.5);
        grid.Editor<ColorDisplay>("Color").HexTextBox.Text.ShouldBe(
            $"{picker.Color.R:X2}{picker.Color.G:X2}{picker.Color.B:X2}");
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-016")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void DraggingTheSpectrum_SavesTheColorOnRelease_AndOneUndoRestoresIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (VariableGridHarness grid, CompactColorPicker picker) = OpenPicker(tree);
        Control square = Square(picker);
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.Input.Drag(PointIn(grid, square, 0.25, 0.25), PointIn(grid, square, 1.0, 0.0), steps: 6);

        ShouldBeNear(picker.Color, Color.FromRgb(255, 0, 0), tolerance: 8);
        ComponentSave saved = grid.ReadSaved(Component(tree));
        int red = (int)VariableGridHarness.StoredValue(saved, "Label.Red")!;
        int green = (int)VariableGridHarness.StoredValue(saved, "Label.Green")!;
        int blue = (int)VariableGridHarness.StoredValue(saved, "Label.Blue")!;
        red.ShouldBeGreaterThanOrEqualTo(251);
        green.ShouldBeLessThanOrEqualTo(4);
        blue.ShouldBeLessThanOrEqualTo(4);
        (red, green, blue).ShouldBe((picker.Color.R, picker.Color.G, picker.Color.B));

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree), "Label.Red").ShouldBeNull();
        grid.Editor<ColorDisplay>("Color").HexTextBox.Text.ShouldBe("FFFFFF");
        tree.SnapshotFiles().ShouldMatch(start, "one undo should undo the whole drag");

        tree.Redo();
        grid.Editor<ColorDisplay>("Color").HexTextBox.Text.ShouldBe($"{red:X2}{green:X2}{blue:X2}");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-016")]
    public void ClickingTheHueBar_PicksTheHue_AtBothEndsAndInTheMiddle()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (VariableGridHarness grid, CompactColorPicker picker) = OpenPicker(tree);
        // White and black have no hue to show, so start from a saturated pick.
        Click(grid, Square(picker), 1.0, 0.0);
        ShouldBeNear(picker.Color, Color.FromRgb(255, 0, 0), tolerance: 8);

        Click(grid, picker.HueSlider, 0.5, 0.5);
        picker.Spectrum.HsvColor.H.ShouldBe(180, tolerance: 3);
        ShouldBeNear(picker.Color, Color.FromRgb(0, 255, 255), tolerance: 12);

        Click(grid, picker.HueSlider, 0.0, 0.5);
        picker.Spectrum.HsvColor.H.ShouldBe(0, tolerance: 3);
        ShouldBeNear(picker.Color, Color.FromRgb(255, 0, 0), tolerance: 12);

        Click(grid, picker.HueSlider, 1.0, 0.5);
        double rightEnd = picker.Spectrum.HsvColor.H;
        (rightEnd >= 357 || rightEnd <= 3).ShouldBeTrue($"the right end is the red hue again, was {rightEnd}");
        ShouldBeNear(picker.Color, Color.FromRgb(255, 0, 0), tolerance: 12);
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-016")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void DraggingTheHueBar_SavesTheColorOnRelease_AndOneUndoRestoresIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        grid.Input.TypeAndEnter(grid.Editor<ColorDisplay>("Color").HexTextBox, "FF0000");
        grid.Settle();
        ProjectFileSnapshot start = tree.SnapshotFiles();
        CompactColorPicker picker = OpenPicker(grid);

        grid.Input.Drag(PointIn(grid, picker.HueSlider, 0.0, 0.5), PointIn(grid, picker.HueSlider, 1.0 / 3.0, 0.5), steps: 6);

        // A third of the way along is hue 120, pure green.
        picker.Spectrum.HsvColor.H.ShouldBe(120, tolerance: 8);
        ComponentSave saved = grid.ReadSaved(Component(tree));
        int red = (int)VariableGridHarness.StoredValue(saved, "Label.Red")!;
        int green = (int)VariableGridHarness.StoredValue(saved, "Label.Green")!;
        int blue = (int)VariableGridHarness.StoredValue(saved, "Label.Blue")!;
        red.ShouldBeLessThanOrEqualTo(40);
        green.ShouldBe(255);
        blue.ShouldBeLessThanOrEqualTo(40);
        (red, green, blue).ShouldBe((picker.Color.R, picker.Color.G, picker.Color.B));

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree), "Label.Green").ShouldBe(0);
        VariableGridHarness.StoredValue(Component(tree), "Label.Blue").ShouldBe(0);
        grid.Editor<ColorDisplay>("Color").HexTextBox.Text.ShouldBe("FF0000");
        tree.SnapshotFiles().ShouldMatch(start, "one undo should undo the whole drag");

        tree.AssertOracles();
    }

    private static (VariableGridHarness Grid, CompactColorPicker Picker) OpenPicker(ProjectTreeHarness tree)
    {
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        return (tree.Grid, OpenPicker(tree.Grid));
    }

    /// <summary>Clicks the Color row's swatch, which opens the picker in a flyout.</summary>
    private static CompactColorPicker OpenPicker(VariableGridHarness grid)
    {
        ColorDisplay display = grid.Editor<ColorDisplay>("Color");
        Button swatchButton = display.GetVisualDescendants().OfType<Button>().Single();
        grid.Input.Click(swatchButton);
        // Hit testing reads the last rendered frame, which does not have the flyout yet.
        grid.Input.RenderFrame();
        display.ColorPicker.IsEffectivelyVisible.ShouldBeTrue("clicking the swatch should open the picker");
        return display.ColorPicker;
    }

    /// <summary>The part of the spectrum that takes the pointer; the control itself is wider than the square it draws.</summary>
    private static Control Square(CompactColorPicker picker) =>
        picker.Spectrum.GetVisualDescendants().OfType<Control>().Single(control => control.Name == "PART_InputTarget");

    /// <summary>Clicks the point at the given fraction of the control's width and height, kept inside its edges.</summary>
    private static void Click(VariableGridHarness grid, Control control, double fractionX, double fractionY) =>
        grid.Input.ClickAt(PointIn(grid, control, fractionX, fractionY));

    private static Point PointIn(VariableGridHarness grid, Control control, double fractionX, double fractionY)
    {
        grid.Input.Layout();
        double x = Math.Clamp(control.Bounds.Width * fractionX, Inset, control.Bounds.Width - Inset);
        double y = Math.Clamp(control.Bounds.Height * fractionY, Inset, control.Bounds.Height - Inset);
        return control.TranslatePoint(new Point(x, y), grid.Input.Window)
            ?? throw new InvalidOperationException($"{control.GetType().Name} is not in the window.");
    }

    private static void ShouldBeNear(Color actual, Color expected, int tolerance)
    {
        Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(tolerance, $"red of {actual}");
        Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(tolerance, $"green of {actual}");
        Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(tolerance, $"blue of {actual}");
    }

    private static ComponentSave Component(ProjectTreeHarness tree) =>
        tree.Project.Project.Components.Single(component => component.Name == "Button");
}
