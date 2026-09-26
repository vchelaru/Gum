using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>The other row kinds and row actions: check boxes, combos, label scrubbing, Add Variable, and renaming through the Name row.</summary>
public class VariableRowScenarioTests
{
    [AvaloniaFact]
    public void ClickingACheckBox_SetsTheValue_AndUndoClearsIt()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);

        CheckBox visible = grid.Editor<CheckBoxDisplay>("Visible").CheckBox;
        grid.Input.Click(visible);
        grid.Settle();

        VariableGridHarness.StoredValue(button, "Label.Visible").ShouldBe(false);

        grid.Undo();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Label.Visible").ShouldBeNull();
        grid.Editor<CheckBoxDisplay>("Visible").CheckBox.IsChecked.ShouldBe(true);
    }

    [AvaloniaFact]
    public void ScrubbingALabel_SetsTheValue_AndRecordsOneUndo()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);
        grid.TypeAndEnter("X", "10");
        TextBoxDisplay x = grid.Editor<TextBoxDisplay>("X");
        TextBlock xLabel = x.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "X");
        global::Avalonia.Point start = grid.Input.CenterOf(xLabel);

        grid.Input.Drag(start, new global::Avalonia.Point(start.X + 20, start.Y), steps: 4);
        grid.Settle();

        VariableGridHarness.StoredValue(button, "Label.X").ShouldBe(30f);
        grid.FieldText("X").ShouldBe("30");

        grid.Undo();

        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, "Label.X").ShouldBe(10f);
    }

    [AvaloniaFact]
    public void PickingAParent_FromTheCombo_SetsIt()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Project.AddInstance(button, "Holder", "Container");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);

        grid.PickComboItem("Parent", "Holder");

        VariableGridHarness.StoredValue(button, "Label.Parent").ShouldBe("Holder");
        grid.Combo("Parent").SelectedItem?.ToString().ShouldBe("Holder");
    }

    [AvaloniaFact]
    public void PickingACategoryState_OnAnInstance_SetsIt()
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

        VariableGridHarness.StoredValue(button, "IconInstance.LooksState").ShouldBe("Small");
        grid.Combo("LooksState").SelectedItem?.ToString().ShouldBe("Small");
    }

    [AvaloniaFact]
    public void AddVariable_AddsARowForTheNewVariable()
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

        button.GetDefaultStateOrThrow().GetVariableSave("Speed").ShouldNotBeNull();
        grid.ShownMemberNames().ShouldContain("Speed");
        grid.TypeAndEnter("Speed", "4");
        VariableGridHarness.StoredValue(button, "Speed").ShouldBe(4f);
    }

    [AvaloniaFact]
    public void TypingANewInstanceName_RenamesTheInstance()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);
        grid.TypeAndEnter("X", "12");

        grid.TypeAndEnter("Name", "Caption");

        label.Name.ShouldBe("Caption");
        VariableGridHarness.StoredValue(button, "Caption.X").ShouldBe(12f);
        grid.ShownMemberNames().ShouldContain("Caption.X");
        grid.Dialogs.Messages.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void MakeDefault_OnAMultiSelection_ClearsEveryInstance()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        InstanceSave left = grid.Project.AddInstance(button, "Left", "Text");
        InstanceSave right = grid.Project.AddInstance(button, "Right", "Text");
        grid.Select(left, right);
        grid.TypeAndEnter("Y", "8");

        grid.PickRowMenuItem("Y", "Make Default");

        VariableGridHarness.StoredValue(button, "Left.Y").ShouldBeNull();
        VariableGridHarness.StoredValue(button, "Right.Y").ShouldBeNull();
    }

    [AvaloniaFact]
    public void EnteringTheSameValueAgain_RecordsNoUndo()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Select(button);
        grid.TypeAndEnter("Height", "44");
        int historyBefore = grid.UndoManager.CurrentElementHistory!.Actions.Count;

        grid.TypeAndEnter("Height", "44");

        grid.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(historyBefore);
    }
}
