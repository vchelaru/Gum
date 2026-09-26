using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// What the Variables tab edits depends on what is selected: several instances at once, a state
/// other than the default, and values driven by variable references.
/// </summary>
public class VariableContextScenarioTests
{
    [AvaloniaFact]
    public void MultiSelect_ShowsABlankFieldForDifferingValues_AndATypedValueSetsEveryInstance_InOneUndo()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave left = grid.Project.AddInstance(button, "Left", "Text");
        InstanceSave right = grid.Project.AddInstance(button, "Right", "Text");
        grid.Select(left);
        grid.TypeAndEnter("X", "10");
        grid.Select(right);
        grid.TypeAndEnter("X", "20");

        grid.Select(left, right);

        grid.Member("X").IsIndeterminate.ShouldBeTrue();
        grid.FieldText("X").ShouldBe("");

        grid.TypeAndEnter("X", "35");

        VariableGridHarness.StoredValue(button, "Left.X").ShouldBe(35f);
        VariableGridHarness.StoredValue(button, "Right.X").ShouldBe(35f);
        grid.FieldText("X").ShouldBe("35");

        grid.Undo();

        ElementSave undone = grid.SelectedState.SelectedElement!;
        VariableGridHarness.StoredValue(undone, "Left.X").ShouldBe(10f);
        VariableGridHarness.StoredValue(undone, "Right.X").ShouldBe(20f);
    }

    [AvaloniaFact]
    public void MultiSelect_PressingAToggle_SetsEveryInstance()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave left = grid.Project.AddInstance(button, "Left", "Text");
        InstanceSave right = grid.Project.AddInstance(button, "Right", "Text");

        grid.Select(left, right);
        grid.PressToggle("XUnits", Gum.Managers.PositionUnitType.PixelsFromCenterX);

        VariableGridHarness.StoredValue(button, "Left.XUnits").ShouldBe(Gum.Managers.PositionUnitType.PixelsFromCenterX);
        VariableGridHarness.StoredValue(button, "Right.XUnits").ShouldBe(Gum.Managers.PositionUnitType.PixelsFromCenterX);
    }

    [AvaloniaFact]
    public void EditingACategoryState_WritesThatState_AndSwitchingStatesShowsEachStatesValue()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        StateSaveCategory looks = grid.Project.AddCategory(button, "Looks");
        StateSave pressed = grid.Project.AddState(button, looks, "Pressed");
        grid.Select(button);
        string defaultWidthText = grid.FieldText("Width");

        grid.Select(pressed);
        grid.TypeAndEnter("Width", "210");

        VariableGridHarness.StoredValue(button, "Width", pressed).ShouldBe(210f);
        VariableGridHarness.StoredValue(button, "Width").ShouldBeNull();
        (grid.ViewModel.StateInformation ?? "").ShouldContain("Pressed");

        grid.Select(button.GetDefaultStateOrThrow());
        grid.FieldText("Width").ShouldBe(defaultWidthText);

        grid.Select(pressed);
        grid.FieldText("Width").ShouldBe("210");
    }

    [AvaloniaFact]
    public void UndoingAnEditInACategoryState_RestoresThatState()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        StateSaveCategory looks = grid.Project.AddCategory(button, "Looks");
        StateSave pressed = grid.Project.AddState(button, looks, "Pressed");
        grid.Select(button);
        grid.Select(pressed);
        grid.TypeAndEnter("Width", "210");

        grid.Undo();

        ElementSave undone = grid.SelectedState.SelectedElement!;
        StateSave undonePressed = undone.Categories.Single().States.Single();
        VariableGridHarness.StoredValue(undone, "Width", undonePressed).ShouldBeNull();
        grid.FieldText("Width").ShouldNotBe("210");
    }

    [AvaloniaFact]
    public void AVariableReference_DrivesTheValue_AndMakesTheRowReadOnly()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        grid.TypeAndEnter("Height", "40");

        grid.TypeLinesAndApply("VariableReferences", "Width = Height");

        VariableGridHarness.StoredValue(button, "Width").ShouldBe(40f);
        grid.FieldText("Width").ShouldBe("40");
        grid.Member("Width").IsReadOnly.ShouldBeTrue();

        grid.TypeAndEnter("Height", "55");

        VariableGridHarness.StoredValue(button, "Width").ShouldBe(55f);
        grid.FieldText("Width").ShouldBe("55");
    }

    [AvaloniaFact]
    public void TheFilter_StaysAppliedAcrossSelectionChanges()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(button);

        grid.Input.TypeInto(grid.View.FilterTextBox, "Width");
        grid.Select(label);

        grid.ShownMemberNames().ShouldNotBeEmpty();
        grid.ShownMemberNames().ShouldAllBe(name => name.Contains("Width"));
    }

    [AvaloniaFact]
    public void AFirstEditInACategoryState_ShowsSetByTheCategory_AfterSwitchingInstances()
    {
        // Switching between two instances of one type reuses the rows; the reused rows must still
        // be found by the new instance's name, or the "Set by" note waits for a reselect.
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        StateSaveCategory looks = grid.Project.AddCategory(button, "Looks");
        StateSave pressed = grid.Project.AddState(button, looks, "Pressed");
        InstanceSave box = grid.Project.AddInstance(button, "Box", "Container");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Container");
        grid.Select(button);
        grid.Select(pressed);
        grid.Select(box);
        grid.Select(label);

        grid.TypeAndEnter("X", "9");

        VariableGridHarness.StoredValue(button, "Label.X", pressed).ShouldBe(9f);
        grid.Member("Label.X").DetailText.ShouldBe("Set by Looks");
    }

    [AvaloniaFact]
    public void TheFilter_OnAnInstance_MatchesVariableNames_NotTheInstanceName()
    {
        // The rows of instance "Box" are named "Box.Width", "Box.Visible"...; typing "x" must not
        // keep all of them just because the instance name contains an x.
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave box = grid.Project.AddInstance(button, "Box", "Container");
        grid.Select(box);

        grid.Input.TypeInto(grid.View.FilterTextBox, "x");

        List<string> shown = grid.ShownMemberNames();
        shown.ShouldContain("Box.X");
        shown.ShouldNotContain("Box.Visible");
        shown.ShouldAllBe(name => name.Substring(name.IndexOf('.') + 1).Contains("x", StringComparison.OrdinalIgnoreCase));
    }
}
