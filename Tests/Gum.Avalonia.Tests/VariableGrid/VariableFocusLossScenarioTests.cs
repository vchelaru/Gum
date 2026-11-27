using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
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
