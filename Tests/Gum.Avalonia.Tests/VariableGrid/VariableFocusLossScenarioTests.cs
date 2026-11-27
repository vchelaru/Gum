using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.DataTypes;
using Gum.Localization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// An undo while a field still has focus, then leaving the field: leaving must not write the field's
/// text back over the undone value (#5143). These editors wrote on that focus loss in the grid; every
/// field that commits on focus loss is covered on its own in FieldFocusLossTests.
/// </summary>
public class VariableFocusLossScenarioTests
{
    [AvaloniaFact]
    public void Slider_UndoWhileFocused_ThenLeaving_KeepsTheUndo()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        SelectNewInstance(grid, "Sprite");

        AssertLeavingAfterUndoKeepsIt(grid, grid.Editor<SliderDisplay>("Alpha").TextBox, "100", "Item.Alpha");
    }

    [AvaloniaFact]
    public void AngleSelector_UndoWhileFocused_ThenLeaving_KeepsTheUndo()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        SelectNewInstance(grid, "Sprite");

        AssertLeavingAfterUndoKeepsIt(grid, grid.Editor<AngleSelectorDisplay>("Rotation").TextBox, "45", "Item.Rotation");
    }

    [AvaloniaFact]
    public void CornerRadius_UndoWhileFocused_ThenLeaving_KeepsTheUndo()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        SelectNewInstance(grid, "Rectangle");

        AssertLeavingAfterUndoKeepsIt(grid, grid.Editor<CornerRadiusDisplay>("CornerRadius").UniformTextBox, "7", "Item.CornerRadius");
    }

    [AvaloniaFact]
    public void EditableComboBox_PickThenUndoWhileFocused_ThenLeaving_KeepsTheUndo()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave item = grid.Project.AddInstance(button, "Item", "Text");
        // A localization database makes the Text row an editable drop-down of string IDs. Loaded after
        // the project, whose load clears it.
        LocalizationService localization = TestAppBuilder.Services.GetRequiredService<LocalizationService>();
        localization.AddDatabase(new Dictionary<string, string[]>
        {
            ["T_Play"] = new[] { "T_Play", "Play" },
            ["T_Quit"] = new[] { "T_Quit", "Quit" },
        }, new List<string> { "String ID", "English" });
        try
        {
            grid.Select(button);
            grid.Select(item);
            ComboBox combo = grid.Combo("Text");
            combo.IsEditable.ShouldBeTrue();
            TextBox field = combo.GetVisualDescendants().OfType<TextBox>().Single();
            field.Focus();

            grid.PickComboItem("Text", "T_Play");
            VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Item.Text").ShouldBe("T_Play");
            field.IsFocused.ShouldBeTrue("the drop-down keeps focus after a pick");

            grid.Undo();
            grid.Input.Press(Key.Tab, PhysicalKey.Tab);
            grid.ThrowIfPluginFailed();

            VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Item.Text").ShouldBeNull();
        }
        finally
        {
            localization.Clear();
        }
    }

    /// <summary>
    /// For each editor the grid has: edit through it, keep or put focus back in it, undo, then leave by Tab
    /// or by clicking another field. Leaving writes nothing, so the undo stands and history keeps the one
    /// entry with its redo. Then the same for redo.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("TextBox", "Tab")]
    [InlineData("TextBox", "Click")]
    [InlineData("MultiLineText", "Tab")]
    [InlineData("MultiLineText", "Click")]
    [InlineData("EditableCombo", "Tab")]
    [InlineData("EditableCombo", "Click")]
    [InlineData("Combo", "Tab")]
    [InlineData("Combo", "Click")]
    [InlineData("CheckBox", "Tab")]
    [InlineData("CheckBox", "Click")]
    [InlineData("Toggle", "Tab")]
    [InlineData("Toggle", "Click")]
    [InlineData("Slider", "Tab")]
    [InlineData("Slider", "Click")]
    [InlineData("Angle", "Tab")]
    [InlineData("Angle", "Click")]
    [InlineData("ColorHex", "Tab")]
    [InlineData("ColorHex", "Click")]
    [InlineData("FileBox", "Tab")]
    [InlineData("FileBox", "Click")]
    public void UndoAndRedo_WhileTheEditorKeepsFocus_ThenLeaving_WriteNothingBack(string editor, string leaveBy)
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Project.AddInstance(button, "Box", "Container");
        string itemType = editor is "MultiLineText" or "FileBox" ? "Text" : "Sprite";
        InstanceSave item = grid.Project.AddInstance(button, "Item", itemType);
        if (editor == "FileBox")
        {
            button.DefaultState.SetValue("Item.UseCustomFont", true, "bool");
        }
        grid.Select(button);
        grid.Select(item);

        (string Member, Func<Control> FocusTarget, Action Edit) arm = editor switch
        {
            "TextBox" => ("X", () => grid.TextField("X"), () => grid.TypeAndEnter("X", "33")),
            "MultiLineText" => ("Text", () => grid.TextField("Text"), () => grid.TypeAndLeave("Text", "Typed words")),
            "EditableCombo" => ("Parent", () => ComboField(grid, "Parent"), () => grid.PickComboItem("Parent", "Box")),
            "Combo" => ("Blend", () => grid.Combo("Blend"), () => grid.PickComboItem("Blend", "Additive")),
            "CheckBox" => ("Visible", () => grid.Editor<CheckBoxDisplay>("Visible").CheckBox, () => grid.Input.Click(grid.Editor<CheckBoxDisplay>("Visible").CheckBox)),
            "Toggle" => ("XUnits", () => grid.Toggles("XUnits")[0], () => grid.Input.Click(grid.Toggles("XUnits")[1])),
            "Slider" => ("Alpha", () => grid.TextField("Alpha"), () => grid.TypeAndEnter("Alpha", "100")),
            "Angle" => ("Rotation", () => grid.TextField("Rotation"), () => grid.TypeAndEnter("Rotation", "45")),
            "ColorHex" => ("Red", () => grid.TextField("Color"), () => grid.TypeAndEnter("Color", "0A141E")),
            "FileBox" => ("CustomFontFile", () => ShownField(grid, "CustomFontFile"), () => grid.Input.TypeAndEnter(ShownField(grid, "CustomFontFile"), "Typed.fnt")),
            _ => throw new ArgumentOutOfRangeException(nameof(editor)),
        };
        (string member, Func<Control> focusTarget, Action edit) = arm;
        string variableName = "Item." + member;
        object? before = Stored(grid, variableName);
        int historyCount = grid.UndoManager.CurrentElementHistory?.Actions.Count ?? 0;

        edit();
        object? edited = Stored(grid, variableName);
        edited.ShouldNotBe(before, $"the {editor} edit should store a value");
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(historyCount + 1, "the edit records one history entry");

        Focus(grid, focusTarget());
        grid.Undo();
        Leave(grid, leaveBy);
        Stored(grid, variableName).ShouldBe(before, "leaving the field after the undo should not write its text back");
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(historyCount + 1, "leaving records no history entry");
        grid.UndoManager.CanRedo().ShouldBeTrue("leaving should not discard the redo");

        Focus(grid, focusTarget());
        grid.Redo();
        Leave(grid, leaveBy);
        Stored(grid, variableName).ShouldBe(edited, "leaving the field after the redo should not write its text back");
        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(historyCount + 1, "leaving records no history entry");
    }

    private static object? Stored(VariableGridHarness grid, string variableName) =>
        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, variableName);

    private static TextBox ComboField(VariableGridHarness grid, string member) =>
        grid.Combo(member).GetVisualDescendants().OfType<TextBox>().Single();

    private static TextBox ShownField(VariableGridHarness grid, string member)
    {
        SingleDataUiContainer row = grid.Row(member);
        row.BringIntoView();
        grid.Settle();
        return row.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(box => box.IsEffectivelyVisible)
            ?? throw new InvalidOperationException($"The {member} row ({row.Displayer?.GetType().Name}) shows no text field.");
    }

    private static void Focus(VariableGridHarness grid, Control target)
    {
        if (!target.IsFocused)
        {
            target.Focus();
            grid.Settle();
        }
        target.IsFocused.ShouldBeTrue("the editor should hold focus when the undo or redo runs");
    }

    private static void Leave(VariableGridHarness grid, string leaveBy)
    {
        if (leaveBy == "Tab")
        {
            grid.Input.Press(Key.Tab, PhysicalKey.Tab);
        }
        else
        {
            grid.Input.Click(grid.TextField("Y"));
        }
        grid.Settle();
    }

    private static void SelectNewInstance(VariableGridHarness grid, string type)
    {
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave instance = grid.Project.AddInstance(button, "Item", type);
        grid.Select(instance);
    }

    private static void AssertLeavingAfterUndoKeepsIt(VariableGridHarness grid, TextBox field, string typed, string variableName)
    {
        grid.Input.TypeAndEnter(field, typed);
        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, variableName).ShouldNotBeNull();
        field.IsFocused.ShouldBeTrue();

        // Undo rebuilds the rows, which takes focus from the field; a field that keeps it loses it on Tab.
        grid.Undo();
        grid.Input.Press(Key.Tab, PhysicalKey.Tab);
        grid.ThrowIfPluginFailed();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, variableName).ShouldBeNull();
    }
}
