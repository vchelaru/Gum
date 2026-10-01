using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>Row menus (expose, delete, hide from instances), editable combos, and undo across state switches.</summary>
public class VariableMenuScenarioTests
{
    [AvaloniaFact]
    public void ExposingAnInstanceVariable_AddsItToTheComponent_AndInstancesOfItShowIt()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);

        grid.Dialogs.AnswerNextUserString("LabelText");
        grid.PickRowMenuItem("Text", "Expose Variable");

        button.GetDefaultStateOrThrow().Variables.ShouldContain(variable => variable.ExposedAsName == "LabelText");
        ComponentSave screenOwner = grid.Project.AddComponent("Panel");
        InstanceSave buttonInstance = grid.Project.AddInstance(screenOwner, "ButtonInstance", "Button");
        grid.Select(buttonInstance);
        grid.ShownMemberNames().ShouldContain("ButtonInstance.LabelText");

        // Text is a multi-line field: Enter adds a line, leaving the field commits.
        grid.TypeAndLeave("LabelText", "Howdy");

        VariableGridHarness.StoredValue(screenOwner, "ButtonInstance.LabelText").ShouldBe("Howdy");
    }

    [AvaloniaFact]
    public void UnexposingAVariable_RemovesTheExposedName()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);
        grid.Dialogs.AnswerNextUserString("LabelText");
        grid.PickRowMenuItem("Text", "Expose Variable");

        grid.PickRowMenuItem("Text", "Un-expose Variable LabelText (Label.Text)");

        button.GetDefaultStateOrThrow().Variables.ShouldNotContain(variable => variable.ExposedAsName == "LabelText");
        grid.RowMenu("Text").ShouldContain("Expose Variable");
    }

    [AvaloniaFact]
    public void DeletingACustomVariable_RemovesItsRow()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        grid.Dialogs.AnswerNext<AddVariableViewModel>(dialog =>
        {
            dialog.SelectedItem = "float";
            dialog.EnteredName = "Speed";
            return true;
        });
        grid.Input.Click(grid.View.AddVariableButton);
        grid.Settle();

        grid.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        grid.PickRowMenuItem("Speed", "Delete Variable [Speed]");

        button.GetDefaultStateOrThrow().GetVariableSave("Speed").ShouldBeNull();
        grid.ShownMemberNames().ShouldNotContain("Speed");
    }

    [AvaloniaFact]
    public void HidingAVariableFromInstances_RemovesItsRowOnInstances()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        ComponentSave panel = grid.Project.AddComponent("Panel");
        InstanceSave buttonInstance = grid.Project.AddInstance(panel, "ButtonInstance", "Button");
        grid.Select(button);

        grid.PickRowMenuItem("Rotation", "Hide from Instances");

        button.VariablesHiddenFromInstances.ShouldContain("Rotation");
        grid.Select(buttonInstance);
        grid.ShownMemberNames().ShouldNotContain("ButtonInstance.Rotation");
    }

    [AvaloniaFact]
    public void TypingAParentName_IntoTheEditableCombo_AndPressingEnter_SetsIt()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Project.AddInstance(button, "Holder", "Container");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);

        global::Avalonia.Controls.ComboBox combo = grid.Combo("Parent");
        grid.Input.Click(combo);
        grid.Input.Window.KeyTextInput("Holder");
        grid.Input.Press(Key.Enter, PhysicalKey.Enter);
        grid.Settle();

        VariableGridHarness.StoredValue(button, "Label.Parent").ShouldBe("Holder");
        grid.ComboText("Parent").ShouldBe("Holder");
    }

    [AvaloniaFact]
    public void UndoingADefaultStateEdit_AfterSelectingAnotherState_RestoresTheDefault()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        StateSaveCategory looks = grid.Project.AddCategory(button, "Looks");
        StateSave pressed = grid.Project.AddState(button, looks, "Pressed");
        grid.Select(button);
        grid.TypeAndEnter("Height", "61");
        grid.Select(pressed);

        grid.Undo();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Height").ShouldBeNull();
        grid.SelectedState.SelectedStateSave!.Name.ShouldBe("Default");
        grid.FieldText("Height").ShouldNotBe("61");
    }

    [AvaloniaFact]
    public void MakeDefault_InACategoryState_SetsTheDefaultStatesValue()
    {
        // A category's states all keep the variable, so Make Default copies the default state's
        // value rather than removing it.
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        StateSaveCategory looks = grid.Project.AddCategory(button, "Looks");
        StateSave pressed = grid.Project.AddState(button, looks, "Pressed");
        grid.Select(button);
        grid.TypeAndEnter("Width", "120");
        grid.Select(pressed);
        grid.TypeAndEnter("Width", "210");
        grid.RowMenu("Width").ShouldContain("Make Default (120)");

        grid.PickRowMenuItem("Width", "Make Default");

        VariableGridHarness.StoredValue(button, "Width", pressed).ShouldBe(120f);
        grid.FieldText("Width").ShouldBe("120");
    }

    [AvaloniaFact]
    public void MakeDefault_InACategoryState_UsesTheInheritedValue_WhenTheDefaultStateDoesNotSetIt()
    {
        // The default state doesn't set Width, so Make Default falls back to the value the default
        // state inherits from the base type, the same value the category gave its other states.
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        button.GetDefaultStateOrThrow().Variables.RemoveAll(item => item.Name == "Width");
        StateSaveCategory looks = grid.Project.AddCategory(button, "Looks");
        StateSave pressed = grid.Project.AddState(button, looks, "Pressed");
        object? inheritedWidth = button.GetDefaultStateOrThrow().GetValueRecursive("Width");
        inheritedWidth.ShouldNotBeNull();
        grid.Select(pressed);
        grid.TypeAndEnter("Width", "210");
        grid.RowMenu("Width").ShouldContain($"Make Default ({inheritedWidth})");

        grid.PickRowMenuItem("Width", "Make Default");

        ElementSave current = grid.SelectedState.SelectedElement!;
        StateSave currentPressed = current.Categories.Single().States.Single();
        VariableGridHarness.StoredValue(current, "Width", currentPressed).ShouldBe(inheritedWidth);
        grid.FieldText("Width").ShouldBe(inheritedWidth.ToString());

        grid.Undo();

        ElementSave afterUndo = grid.SelectedState.SelectedElement!;
        VariableGridHarness.StoredValue(afterUndo, "Width", afterUndo.Categories.Single().States.Single())
            .ShouldBe(210f);
    }

    [AvaloniaFact]
    public void MakeDefault_OnAStateVariableInACategoryState_ClearsIt_AndItsLabelShowsNoValue()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave icon = grid.Project.AddComponent("Icon");
        StateSaveCategory looks = grid.Project.AddCategory(icon, "Looks");
        grid.Project.AddState(icon, looks, "Big");
        grid.Project.AddState(icon, looks, "Small");
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave iconInstance = grid.Project.AddInstance(button, "IconInstance", "Icon");
        StateSaveCategory sizes = grid.Project.AddCategory(button, "Sizes");
        StateSave wide = grid.Project.AddState(button, sizes, "Wide");
        grid.Select(iconInstance);
        grid.Select(wide);
        grid.SelectedState.SelectedInstance.ShouldBe(iconInstance);
        grid.PickComboItem("LooksState", "Small");

        grid.RowMenu("LooksState").ShouldContain("Make Default");

        grid.PickRowMenuItem("LooksState", "Make Default");

        VariableGridHarness.StoredValue(button, "IconInstance.LooksState", wide).ShouldBeNull();
    }

    [AvaloniaFact]
    public void MakeDefaultLabel_OnAnInstanceInTheDefaultState_ShowsTheInheritedValue()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);
        grid.TypeAndEnter("X", "42");

        grid.RowMenu("X").ShouldContain("Make Default (0)");
    }
}
