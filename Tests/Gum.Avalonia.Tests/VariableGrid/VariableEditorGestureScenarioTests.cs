using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// Pointer gestures on the Variables tab's slider, combo and drag editors commit only what the user
/// meant: a finished left-button change, never a right-click, a wheel over a closed combo, or a
/// drag left running after the pointer capture was lost (#5539).
/// </summary>
public class VariableEditorGestureScenarioTests
{
    [AvaloniaFact]
    public void Slider_RightClickOnAMultiSelection_LeavesEveryValueAlone()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave left = grid.Project.AddInstance(button, "Left", "Sprite");
        InstanceSave right = grid.Project.AddInstance(button, "Right", "Sprite");
        button.DefaultState!.SetValue("Left.Alpha", 100, "int");
        button.DefaultState!.SetValue("Right.Alpha", 200, "int");
        grid.Select(left, right);

        grid.Input.RightClick(AlphaSlider(grid));
        grid.Input.OpenContextMenu?.Close();
        grid.Settle();

        VariableGridHarness.StoredValue(button, "Left.Alpha").ShouldBe(100);
        VariableGridHarness.StoredValue(button, "Right.Alpha").ShouldBe(200);
    }

    [AvaloniaFact]
    public void Slider_LeftClickOnTheThumbWithoutMoving_KeepsAnInheritedValueInherited()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave icon = grid.Project.AddInstance(button, "Icon", "Sprite");
        grid.Select(icon);
        Control thumb = AlphaSlider(grid).GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.Thumb>().Single();

        grid.Input.Click(thumb);
        grid.Settle();

        VariableGridHarness.StoredValue(button, "Icon.Alpha").ShouldBeNull();
    }

    [AvaloniaFact]
    public void Slider_LeftClickOnTheTrack_CommitsTheClickedValue()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave icon = grid.Project.AddInstance(button, "Icon", "Sprite");
        grid.Select(icon);
        Slider alpha = AlphaSlider(grid);
        Point center = grid.Input.CenterOf(alpha);

        grid.Input.ClickAt(center);
        grid.Settle();

        object? committed = VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Icon.Alpha");
        committed.ShouldNotBeNull();
        committed.ShouldNotBe(255);
    }

    [AvaloniaFact]
    public void Slider_LosingThePointerCaptureMidThumbDrag_CommitsTheDraggedValue()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave icon = grid.Project.AddInstance(button, "Icon", "Sprite");
        grid.Select(icon);
        Control thumb = AlphaSlider(grid).GetVisualDescendants().OfType<global::Avalonia.Controls.Primitives.Thumb>().Single();
        Point thumbCenter = grid.Input.CenterOf(thumb);

        grid.Input.PressLeftAt(thumbCenter);
        grid.Input.MoveWithLeftHeld(thumbCenter - new Point(40, 0));
        grid.Input.LosePointerCapture();
        grid.Settle();

        object? committed = VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Icon.Alpha");
        committed.ShouldNotBeNull();
        committed.ShouldNotBe(255);
    }

    [AvaloniaFact]
    public void ClosedCombo_Wheel_ChangesNothing_AndRecordsNoUndo()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave icon = grid.Project.AddComponent("Icon");
        StateSaveCategory looks = grid.Project.AddCategory(icon, "Looks");
        grid.Project.AddState(icon, looks, "Big");
        grid.Project.AddState(icon, looks, "Small");
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave iconInstance = grid.Project.AddInstance(button, "IconInstance", "Icon");
        grid.Select(iconInstance);
        grid.PickComboItem("LooksState", "Small");
        grid.Settle();
        grid.Combo("LooksState").Focus();
        int historyBefore = grid.UndoManager.CurrentElementHistory!.Actions.Count;

        grid.Input.Wheel(grid.Input.CenterOf(grid.Combo("LooksState")), -1);
        grid.Input.Wheel(grid.Input.CenterOf(grid.Combo("LooksState")), 1);
        grid.Input.Wheel(grid.Input.CenterOf(grid.Combo("LooksState")), 1);
        grid.Settle();

        VariableGridHarness.StoredValue(button, "IconInstance.LooksState").ShouldBe("Small");
        grid.Combo("LooksState").SelectedItem?.ToString().ShouldBe("Small");
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(historyBefore);
    }

    [AvaloniaFact]
    public void LabelScrub_LosingThePointerCapture_CommitsOnce_AndStopsScrubbing()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        Control label = grid.Editor<TextBoxDisplay>("X").LabelHost;
        Point start = grid.Input.CenterOf(label);
        int historyBefore = grid.UndoManager.CurrentElementHistory!.Actions.Count;

        grid.Input.PressLeftAt(start);
        grid.Input.MoveWithLeftHeld(start + new Point(10, 0));
        grid.Input.LosePointerCapture();
        grid.Settle();
        object? committed = VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "X");
        grid.Input.Hover(start + new Point(30, 0));
        grid.Settle();

        committed.ShouldNotBeNull();
        committed.ShouldNotBe(0f);
        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "X").ShouldBe(committed);
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(historyBefore + 1);
    }

    [AvaloniaFact]
    public void AngleDial_LosingThePointerCapture_CommitsTheDraggedAngle()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave icon = grid.Project.AddInstance(button, "Icon", "Sprite");
        grid.Select(icon);
        Control dial = grid.Editor<AngleSelectorDisplay>("Rotation").Dial;
        Point center = grid.Input.CenterOf(dial);
        int historyBefore = grid.UndoManager.CurrentElementHistory!.Actions.Count;

        grid.Input.PressLeftAt(center + new Point(20, 0));
        grid.Input.MoveWithLeftHeld(center + new Point(0, -20));
        grid.Input.LosePointerCapture();
        grid.Settle();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Icon.Rotation").ShouldBe(90f);
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(historyBefore + 1);
    }

    [AvaloniaFact]
    public void CornerRadiusScrub_LosingThePointerCapture_CommitsOnce_AndStopsScrubbing()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave background = grid.Project.AddInstance(button, "Background", "Rectangle");
        grid.Select(background);
        Control label = grid.Editor<CornerRadiusDisplay>("CornerRadius").UniformLabel;
        Point start = grid.Input.CenterOf(label);
        int historyBefore = grid.UndoManager.CurrentElementHistory!.Actions.Count;

        grid.Input.PressLeftAt(start);
        grid.Input.MoveWithLeftHeld(start + new Point(10, 0));
        grid.Input.LosePointerCapture();
        grid.Settle();
        object? committed = VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Background.CornerRadius");
        grid.Input.Hover(start + new Point(30, 0));
        grid.Settle();

        committed.ShouldNotBeNull();
        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Background.CornerRadius").ShouldBe(committed);
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(historyBefore + 1);
    }

    private static Slider AlphaSlider(VariableGridHarness grid) =>
        grid.Row("Alpha").GetVisualDescendants().OfType<Slider>().Single();
}
