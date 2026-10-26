using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// A drag writes intermediate values and the reference cascade waits for the full commit, so a drag
/// whose row goes away mid-drag (another instance selected with the mouse still held) must still end
/// committed: saved, one undo entry, and the elements reading the value up to date (#6888).
/// </summary>
public class DragEndsOnSelectionChangeScenarioTests
{
    private sealed class Scene
    {
        public required VariableGridHarness Grid { get; init; }
        public required ComponentSave Button { get; init; }
        public required InstanceSave Source { get; init; }
        public required InstanceSave Other { get; init; }
        public int HistoryBeforeDrag { get; set; }
    }

    private static Scene Build(VariableGridHarness grid, string type, string variable)
    {
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave source = grid.Project.AddInstance(button, "Source", type);
        InstanceSave other = grid.Project.AddInstance(button, "Other", type);
        InstanceSave follower = grid.Project.AddInstance(button, "Follower", type);
        grid.Select(follower);
        grid.TypeLinesAndApply("VariableReferences", $"{variable} = Source.{variable}");
        grid.Select(source);
        grid.Settle();
        return new Scene { Grid = grid, Button = button, Source = source, Other = other, HistoryBeforeDrag = grid.UndoManager.CurrentElementHistory!.Actions.Count };
    }

    private static void AssertEndedCommitted(Scene scene, string variable, object? valueBefore)
    {
        VariableGridHarness grid = scene.Grid;
        ElementSave element = grid.SelectedState.SelectedElement!;
        object? source = VariableGridHarness.StoredValue(element, $"Source.{variable}");
        source.ShouldNotBeNull();
        source.ShouldNotBe(valueBefore);
        VariableGridHarness.StoredValue(element, $"Follower.{variable}").ShouldBe(source);
        VariableGridHarness.StoredValue(grid.ReadSaved(scene.Button), $"Source.{variable}").ShouldBe(source);
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(scene.HistoryBeforeDrag + 1);
    }

    private static void ReleaseAndAssertNothingMoreHappens(Scene scene, Point point, string variable)
    {
        VariableGridHarness grid = scene.Grid;
        object? committed = VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, $"Source.{variable}");
        grid.Input.Window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        grid.Settle();
        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, $"Source.{variable}").ShouldBe(committed);
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(scene.HistoryBeforeDrag + 1);
    }

    [AvaloniaFact]
    public void LabelScrub_SelectingAnotherInstanceMidDrag_CommitsTheDrag()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        Scene scene = Build(grid, "Sprite", "X");
        object? before = VariableGridHarness.StoredValue(scene.Button, "Source.X");
        Point start = grid.Input.CenterOf(grid.Editor<TextBoxDisplay>("X").LabelHost);

        grid.Input.PressLeftAt(start);
        grid.Input.MoveWithLeftHeld(start + new Point(10, 0));
        grid.Select(scene.Other);
        grid.Settle();

        AssertEndedCommitted(scene, "X", before);
        ReleaseAndAssertNothingMoreHappens(scene, start + new Point(10, 0), "X");
    }

    [AvaloniaFact]
    public void AngleDial_SelectingAnotherInstanceMidDrag_CommitsTheDrag()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        Scene scene = Build(grid, "Sprite", "Rotation");
        object? before = VariableGridHarness.StoredValue(scene.Button, "Source.Rotation");
        Point center = grid.Input.CenterOf(grid.Editor<AngleSelectorDisplay>("Rotation").Dial);

        grid.Input.PressLeftAt(center + new Point(20, 0));
        grid.Input.MoveWithLeftHeld(center + new Point(0, -20));
        grid.Select(scene.Other);
        grid.Settle();

        AssertEndedCommitted(scene, "Rotation", before);
        ReleaseAndAssertNothingMoreHappens(scene, center + new Point(0, -20), "Rotation");
    }

    [AvaloniaFact]
    public void Slider_SelectingAnotherInstanceMidDrag_CommitsTheDrag()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        Scene scene = Build(grid, "Sprite", "Alpha");
        object? before = VariableGridHarness.StoredValue(scene.Button, "Source.Alpha");
        Control thumb = grid.Row("Alpha").GetVisualDescendants().OfType<Slider>().Single()
            .GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.Thumb>().Single();
        Point thumbCenter = grid.Input.CenterOf(thumb);

        grid.Input.PressLeftAt(thumbCenter);
        grid.Input.MoveWithLeftHeld(thumbCenter - new Point(40, 0));
        grid.Select(scene.Other);
        grid.Settle();

        AssertEndedCommitted(scene, "Alpha", before);
        ReleaseAndAssertNothingMoreHappens(scene, thumbCenter - new Point(40, 0), "Alpha");
    }

    [AvaloniaFact]
    public void ColorPicker_SelectingAnotherInstanceMidDrag_CommitsTheDrag()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        Scene scene = Build(grid, "Rectangle", "StrokeRed");
        object? before = VariableGridHarness.StoredValue(scene.Button, "Source.StrokeRed");
        ColorDisplay display = grid.Editor<ColorDisplay>("StrokeColor");

        display.HandleColorPicked(global::Avalonia.Media.Color.FromRgb(200, 20, 30));
        grid.Select(scene.Other);
        grid.Settle();

        AssertEndedCommitted(scene, "StrokeRed", before);
    }
}
