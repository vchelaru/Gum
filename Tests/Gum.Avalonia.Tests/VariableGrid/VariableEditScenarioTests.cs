using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>Editing values in the Variables tab: typing, toggles, Make Default, and undo/redo of each.</summary>
public class VariableEditScenarioTests
{
    [AvaloniaFact]
    public void TypingAWidth_SetsTheDefaultState_SavesIt_AndMarksTheRow()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);

        grid.TypeAndEnter("Width", "175");

        VariableGridHarness.StoredValue(button, "Width").ShouldBe(175f);
        VariableGridHarness.StoredValue(grid.ReadSaved(button), "Width").ShouldBe(175f);
        grid.FieldText("Width").ShouldBe("175");
        grid.ShowsSetMarker("Width").ShouldBeTrue();
    }

    [AvaloniaFact]
    public void TabbingOutOfAField_CommitsTheValue()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);

        grid.TypeAndLeave("Height", "33");

        VariableGridHarness.StoredValue(button, "Height").ShouldBe(33f);
    }

    [AvaloniaFact]
    public void UndoAndRedo_OfATypedValue_UpdateTheElementAndTheField()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        string inheritedWidthText = grid.FieldText("Width");
        grid.TypeAndEnter("Width", "175");

        grid.Undo();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Width").ShouldBeNull();
        grid.FieldText("Width").ShouldBe(inheritedWidthText);
        grid.ShowsSetMarker("Width").ShouldBeFalse();

        grid.Redo();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Width").ShouldBe(175f);
        grid.FieldText("Width").ShouldBe("175");
        grid.ShowsSetMarker("Width").ShouldBeTrue();
    }

    [AvaloniaFact]
    public void TwoEdits_UndoOneAtATime()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        grid.TypeAndEnter("Width", "160");
        grid.TypeAndEnter("Width", "175");

        grid.Undo();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Width").ShouldBe(160f);
        grid.FieldText("Width").ShouldBe("160");
    }

    [AvaloniaFact]
    public void PressingAUnitsToggle_SetsTheUnits_AndUndoRestoresThem()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);

        grid.PressToggle("WidthUnits", DimensionUnitType.RelativeToParent);

        VariableGridHarness.StoredValue(button, "WidthUnits").ShouldBe(DimensionUnitType.RelativeToParent);
        grid.Toggles("WidthUnits").Single(toggle => toggle.IsChecked == true).Tag
            .ShouldBeOfType<WpfDataUi.Controls.ToggleButtonOption>().Value.ShouldBe(DimensionUnitType.RelativeToParent);

        grid.Undo();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "WidthUnits").ShouldNotBe(DimensionUnitType.RelativeToParent);
        grid.Toggles("WidthUnits").Single(toggle => toggle.IsChecked == true).Tag
            .ShouldBeOfType<WpfDataUi.Controls.ToggleButtonOption>().Value.ShouldNotBe(DimensionUnitType.RelativeToParent);
    }

    [AvaloniaFact]
    public void MakeDefault_OnAnInstanceValue_RemovesIt_AndShowsTheInheritedValue()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);
        grid.TypeAndEnter("X", "42");
        VariableGridHarness.StoredValue(button, "Label.X").ShouldBe(42f);

        grid.PickRowMenuItem("X", "Make Default");

        VariableGridHarness.StoredValue(button, "Label.X").ShouldBeNull();
        grid.ShowsSetMarker("X").ShouldBeFalse();
        grid.FieldText("X").ShouldBe("0");
    }

    [AvaloniaFact]
    public void TypingInvalidText_LeavesTheValue_AndRestoresTheField()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        grid.TypeAndEnter("Width", "175");

        grid.TypeAndLeave("Width", "abc");

        VariableGridHarness.StoredValue(button, "Width").ShouldBe(175f);
    }
}
