using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins.Behaviors;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.ProjectServices.FontGeneration;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the Variables tab and states (inventory areas VAR, DISP, STATE and the
/// COMBO items that cross them): selection through the Project tree, edits typed, clicked and
/// picked in the head's own Variables tab, Ctrl+Z and Ctrl+Y through the app-wide hotkeys. Each
/// checks what the tab shows and what was saved, that undoing back to the start restores the
/// files byte for byte, and ends with the shared oracles.
/// </summary>
[Trait("Category", "EndToEnd")]
public class VariableScenarioTests
{
    #region Editing each kind of value

    [AvaloniaFact]
    [Trait("Feature", "VAR-001")]
    [Trait("Feature", "DISP-001")]
    [Trait("Feature", "FILE-009")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void TypingANumber_SavesIt_AndUndoRedoFollowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        VariableGridHarness grid = tree.Grid;
        string inheritedWidth = grid.FieldText("Width");
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.TypeAndEnter("Width", "175");

        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(175f);
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "Width").ShouldBe(175f);
        grid.ShowsSetMarker("Width").ShouldBeTrue();

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBeNull();
        grid.FieldText("Width").ShouldBe(inheritedWidth);
        tree.SnapshotFiles().ShouldMatch(start, "undoing the edit should restore the files");

        tree.Redo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(175f);
        grid.FieldText("Width").ShouldBe("175");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VAR-001")]
    [Trait("Feature", "VAR-002")]
    [Trait("Feature", "DISP-002")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void TypingText_IntoAnInstance_SavesIt_AndUndoRedoFollowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.TypeAndLeave("Text", "Click me");

        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Text").ShouldBe("Click me");
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "Label.Text").ShouldBe("Click me");

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Text").ShouldBeNull();
        grid.FieldText("Text").ShouldBe("Hello");
        tree.SnapshotFiles().ShouldMatch(start, "undoing the edit should restore the files");

        tree.Redo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Text").ShouldBe("Click me");
        grid.FieldText("Text").ShouldBe("Click me");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-003")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void ClickingACheckBox_SavesTheBool_AndUndoRedoFollowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.Input.Click(grid.Editor<CheckBoxDisplay>("Visible").CheckBox);
        grid.Settle();

        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Visible").ShouldBe(false);
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "Label.Visible").ShouldBe(false);

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Visible").ShouldBeNull();
        grid.Editor<CheckBoxDisplay>("Visible").CheckBox.IsChecked.ShouldBe(true);
        tree.SnapshotFiles().ShouldMatch(start, "undoing the edit should restore the files");

        tree.Redo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Visible").ShouldBe(false);
        grid.Editor<CheckBoxDisplay>("Visible").CheckBox.IsChecked.ShouldBe(false);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-005")]
    [Trait("Feature", "DISP-019")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void PickingEnums_FromAComboAndAToggle_SavesThem_AndUndoRedoFollowThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave icon = tree.Project.AddInstance(button, "Icon", "Sprite");
        tree.Click(tree.NodeFor(icon));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.PickComboItem("TextureAddress", nameof(TextureAddress.Custom));
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        grid.PressToggle("ChildrenLayout", ChildrenLayout.TopToBottomStack);

        ComponentSave edited = Component(tree, "Button");
        VariableGridHarness.StoredValue(edited, "Icon.TextureAddress").ShouldBe(TextureAddress.Custom);
        VariableGridHarness.StoredValue(edited, "ChildrenLayout").ShouldBe(ChildrenLayout.TopToBottomStack);
        ComponentSave saved = grid.ReadSaved(edited);
        // The file stores the enum as its number; loading the project types it again.
        Convert.ToInt32(VariableGridHarness.StoredValue(saved, "Icon.TextureAddress")).ShouldBe((int)TextureAddress.Custom);
        Convert.ToInt32(VariableGridHarness.StoredValue(saved, "ChildrenLayout")).ShouldBe((int)ChildrenLayout.TopToBottomStack);

        tree.Undo();
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Icon.TextureAddress").ShouldBeNull();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "ChildrenLayout").ShouldBeNull();
        tree.SnapshotFiles().ShouldMatch(start, "undoing both edits should restore the files");

        tree.Redo();
        tree.Redo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "ChildrenLayout").ShouldBe(ChildrenLayout.TopToBottomStack);
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single()));
        grid.Combo("TextureAddress").SelectedItem?.ToString().ShouldBe(nameof(TextureAddress.Custom));

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "DISP-016")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void TypingAHexColor_SavesTheChannels_AndUndoRedoFollowThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.Input.TypeAndEnter(grid.Editor<ColorDisplay>("Color").HexTextBox, "40FF80");
        grid.Settle();

        ComponentSave saved = grid.ReadSaved(Component(tree, "Button"));
        VariableGridHarness.StoredValue(saved, "Label.Red").ShouldBe(64);
        VariableGridHarness.StoredValue(saved, "Label.Blue").ShouldBe(128);

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Red").ShouldBeNull();
        grid.Editor<ColorDisplay>("Color").HexTextBox.Text.ShouldBe("FFFFFF");
        tree.SnapshotFiles().ShouldMatch(start, "undoing the color should restore the files");

        tree.Redo();
        grid.Editor<ColorDisplay>("Color").HexTextBox.Text.ShouldBe("40FF80");

        tree.AssertOracles();
    }

    #endregion

    #region States

    [AvaloniaFact]
    [Trait("Feature", "STATE-001")]
    [Trait("Feature", "VAR-008")]
    [Trait("Feature", "VAR-009")]
    [Trait("Feature", "COMBO-019")]
    [Trait("Feature", "EDIT-001")]
    public void EditingACategoryState_KeepsTheDefault_AndMakeDefaultThereTakesTheDefaultsValue()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        tree.Project.AddState(button, looks, "Pressed");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("Width", "120");
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.Select(PressedState(tree));
        grid.TypeAndEnter("Width", "210");

        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", PressedState(tree)).ShouldBe(210f);
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(120f);
        (grid.ViewModel.StateInformation ?? "").ShouldContain("Pressed");
        grid.Select(Component(tree, "Button").GetDefaultStateOrThrow());
        grid.FieldText("Width").ShouldBe("120");
        grid.Select(PressedState(tree));
        grid.FieldText("Width").ShouldBe("210");

        grid.PickRowMenuItem("Width", "Make Default");

        // Every state of a category sets the same variables, so Make Default there sets the value
        // the default state gives rather than removing it.
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", PressedState(tree)).ShouldBe(120f);
        grid.FieldText("Width").ShouldBe("120");
        ComponentSave saved = grid.ReadSaved(Component(tree, "Button"));
        VariableGridHarness.StoredValue(saved, "Width", saved.Categories.Single().States.Single()).ShouldBe(120f);

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", PressedState(tree)).ShouldBe(210f);
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the state edit and Make Default should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-014")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void UndoAcrossAStateSwitch_UndoesEachStatesEdit()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        tree.Project.AddState(button, looks, "Pressed");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.TypeAndEnter("Width", "120");
        grid.Select(PressedState(tree));
        grid.TypeAndEnter("Width", "210");
        grid.Select(Component(tree, "Button").GetDefaultStateOrThrow());

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", PressedState(tree)).ShouldBeNull();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(120f);
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBeNull();
        tree.SnapshotFiles().ShouldMatch(start, "undoing both states' edits should restore the files");

        tree.Redo();
        tree.Redo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(120f);
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width", PressedState(tree)).ShouldBe(210f);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-015")]
    [Trait("Feature", "EDIT-001")]
    public void UndoAcrossAnElementSwitch_UndoesOnlyTheSelectedElementsEdit()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Panel");
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();

        grid.TypeAndEnter("Width", "120");
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        grid.TypeAndEnter("Width", "310");
        tree.Click(tree.NodeFor(Component(tree, "Button")));

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBeNull();
        VariableGridHarness.StoredValue(Component(tree, "Panel"), "Width").ShouldBe(310f);
        grid.FieldText("Width").ShouldNotBe("120");

        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        grid.FieldText("Width").ShouldBe("310");
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Panel"), "Width").ShouldBeNull();
        tree.SnapshotFiles().ShouldMatch(start, "undoing in each element should restore the files");

        tree.AssertOracles();
    }

    #endregion

    #region References and exposed variables

    [AvaloniaFact]
    [Trait("Feature", "VAR-024")]
    [Trait("Feature", "DISP-013")]
    [Trait("Feature", "EDIT-001")]
    public void AReferenceToAnotherElement_FollowsItsEdits_AndItsUndo()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Panel");
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("Width", "90");
        tree.Click(tree.NodeFor(Component(tree, "Panel")));

        grid.TypeLinesAndApply("VariableReferences", "Width = Components/Button.Width");

        VariableGridHarness.StoredValue(Component(tree, "Panel"), "Width").ShouldBe(90f);
        grid.Member("Width").IsReadOnly.ShouldBeTrue();
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        ProjectFileSnapshot beforeEdit = tree.SnapshotFiles();

        grid.TypeAndEnter("Width", "140");

        VariableGridHarness.StoredValue(Component(tree, "Panel"), "Width").ShouldBe(140f);
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Panel")), "Width").ShouldBe(140f);

        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Width").ShouldBe(90f);
        VariableGridHarness.StoredValue(Component(tree, "Panel"), "Width").ShouldBe(90f);
        tree.SnapshotFiles().ShouldMatch(beforeEdit, "undoing the referenced edit should restore both elements' files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VAR-011")]
    [Trait("Feature", "COMBO-020")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void AnExposedVariable_IsSetOnAnInstanceElsewhere_AndUndoRedoFollowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        VariableGridHarness grid = ExposeLabelTextAndUseButtonInTitle(tree);
        ProjectFileSnapshot beforeSet = tree.SnapshotFiles();

        grid.TypeAndLeave("LabelText", "OK");

        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBe("OK");
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.LabelText").ShouldBe("OK");

        tree.Undo();
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBeNull();
        tree.SnapshotFiles().ShouldMatch(beforeSet, "undoing the instance value should restore the files");
        tree.Redo();
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBe("OK");
        grid.FieldText("LabelText").ShouldBe("OK");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VAR-012")]
    [Trait("Feature", "COMBO-021")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void UnexposingAVariableSetOnInstances_AndChoosingClear_ClearsThoseValues_AndUndoRedoFollowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        VariableGridHarness grid = ExposeLabelTextAndUseButtonInTitle(tree);
        grid.TypeAndLeave("LabelText", "OK");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single()));
        ProjectFileSnapshot beforeUnexpose = tree.SnapshotFiles();
        string? listedInstances = null;
        tree.Dialogs.AnswerNext<ChoiceDialogViewModel>(dialog =>
        {
            listedInstances = dialog.Message;
            dialog.SelectedValue = "Un-expose and clear values";
            return true;
        });

        grid.PickRowMenuItem("Text", "Un-expose Variable LabelText (Label.Text)");

        listedInstances.ShouldNotBeNull();
        listedInstances.ShouldContain("OkButton in Title");
        Component(tree, "Button").GetDefaultStateOrThrow().Variables.ShouldNotContain(variable => variable.ExposedAsName == "LabelText");
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBeNull();
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.LabelText").ShouldBeNull();

        tree.Undo();
        Component(tree, "Button").GetDefaultStateOrThrow().Variables.ShouldContain(variable => variable.ExposedAsName == "LabelText");
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBe("OK");
        tree.SnapshotFiles().ShouldMatch(beforeUnexpose, "one undo should restore the exposure and the cleared instance value");

        tree.Redo();
        Component(tree, "Button").GetDefaultStateOrThrow().Variables.ShouldNotContain(variable => variable.ExposedAsName == "LabelText");
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBeNull();
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.LabelText").ShouldBeNull();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VAR-012")]
    [Trait("Feature", "COMBO-021")]
    public void UnexposingAVariableSetOnInstances_AndChoosingKeep_LeavesThoseValues()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        VariableGridHarness grid = ExposeLabelTextAndUseButtonInTitle(tree);
        grid.TypeAndLeave("LabelText", "OK");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single()));
        tree.Dialogs.AnswerNext<ChoiceDialogViewModel>(dialog =>
        {
            dialog.SelectedValue = "Un-expose, keep values";
            return true;
        });

        grid.PickRowMenuItem("Text", "Un-expose Variable LabelText (Label.Text)");

        Component(tree, "Button").GetDefaultStateOrThrow().Variables.ShouldNotContain(variable => variable.ExposedAsName == "LabelText");
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBe("OK");
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.LabelText").ShouldBe("OK");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VAR-012")]
    [Trait("Feature", "COMBO-021")]
    public void UnexposingAVariableSetOnInstances_AndCancelling_ChangesNothing()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        VariableGridHarness grid = ExposeLabelTextAndUseButtonInTitle(tree);
        grid.TypeAndLeave("LabelText", "OK");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single()));
        ProjectFileSnapshot beforeUnexpose = tree.SnapshotFiles();
        tree.Dialogs.AnswerNext<ChoiceDialogViewModel>(_ => false);

        grid.PickRowMenuItem("Text", "Un-expose Variable LabelText (Label.Text)");

        Component(tree, "Button").GetDefaultStateOrThrow().Variables.ShouldContain(variable => variable.ExposedAsName == "LabelText");
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBe("OK");
        tree.SnapshotFiles().ShouldMatch(beforeUnexpose, "cancelling should leave every file as it was");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VAR-012")]
    [Trait("Feature", "COMBO-021")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void UnexposingAVariableADerivedElementSets_AndChoosingClear_ClearsItsValue_AndUndoRedoFollowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        ComponentSave fancyButton = tree.Project.AddComponent("FancyButton");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        tree.Dialogs.AnswerNextUserString("LabelText");
        grid.PickRowMenuItem("Text", "Expose Variable");
        tree.Click(tree.NodeFor(fancyButton));
        grid.PickComboItem("BaseType", "Button");
        grid.TypeAndLeave("LabelText", "Fancy");
        fancyButton.GetDefaultStateOrThrow().Variables.ShouldContain(variable => variable.Name == "Label.Text" && variable.ExposedAsName == "LabelText");
        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot beforeUnexpose = tree.SnapshotFiles();
        string? listedUsers = null;
        tree.Dialogs.AnswerNext<ChoiceDialogViewModel>(dialog =>
        {
            listedUsers = dialog.Message;
            dialog.SelectedValue = "Un-expose and clear values";
            return true;
        });

        grid.PickRowMenuItem("Text", "Un-expose Variable LabelText (Label.Text)");

        listedUsers.ShouldNotBeNull().ShouldContain("FancyButton (derived)");
        VariableGridHarness.StoredValue(Component(tree, "FancyButton"), "LabelText").ShouldBeNull();
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "FancyButton")), "LabelText").ShouldBeNull();

        tree.Undo();
        Component(tree, "Button").GetDefaultStateOrThrow().Variables.ShouldContain(variable => variable.ExposedAsName == "LabelText");
        VariableGridHarness.StoredValue(Component(tree, "FancyButton"), "LabelText").ShouldBe("Fancy");
        tree.SnapshotFiles().ShouldMatch(beforeUnexpose, "one undo should restore the exposure and the derived element's value");

        tree.Redo();
        VariableGridHarness.StoredValue(Component(tree, "FancyButton"), "LabelText").ShouldBeNull();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VAR-015")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void DeletingAVariableADerivedElementSets_ClearsItsValue_AndUndoRedoFollowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        ComponentSave fancyButton = tree.Project.AddComponent("FancyButton");
        tree.Click(tree.NodeFor(button));
        VariableGridHarness grid = tree.Grid;
        tree.Dialogs.AnswerNext<AddVariableViewModel>(dialog =>
        {
            dialog.SelectedItem = "float";
            dialog.EnteredName = "Speed";
            return true;
        });
        grid.Input.Click(grid.View.AddVariableButton);
        grid.Settle();
        tree.Click(tree.NodeFor(fancyButton));
        grid.PickComboItem("BaseType", "Button");
        grid.TypeAndEnter("Speed", "5");
        VariableGridHarness.StoredValue(Component(tree, "FancyButton"), "Speed").ShouldBe(5f);
        tree.Click(tree.NodeFor(button));
        ProjectFileSnapshot beforeDelete = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        grid.PickRowMenuItem("Speed", "Delete Variable [Speed]");

        Component(tree, "Button").GetDefaultStateOrThrow().GetVariableSave("Speed").ShouldBeNull();
        VariableGridHarness.StoredValue(Component(tree, "FancyButton"), "Speed").ShouldBeNull();
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "FancyButton")), "Speed").ShouldBeNull();

        tree.Undo();
        Component(tree, "Button").GetDefaultStateOrThrow().GetVariableSave("Speed").ShouldNotBeNull();
        VariableGridHarness.StoredValue(Component(tree, "FancyButton"), "Speed").ShouldBe(5f);
        tree.SnapshotFiles().ShouldMatch(beforeDelete, "one undo should restore the variable and the derived element's value");

        tree.Redo();
        VariableGridHarness.StoredValue(Component(tree, "FancyButton"), "Speed").ShouldBeNull();

        tree.AssertOracles();
    }

    /// <summary>
    /// Button with a Label whose Text is exposed as LabelText, used as OkButton in screen Title,
    /// with OkButton selected.
    /// </summary>
    private static VariableGridHarness ExposeLabelTextAndUseButtonInTitle(ProjectTreeHarness tree)
    {
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave okButton = tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        tree.Dialogs.AnswerNextUserString("LabelText");
        grid.PickRowMenuItem("Text", "Expose Variable");
        tree.Click(tree.NodeFor(okButton));
        return grid;
    }

    #endregion

    #region Renaming and deleting a used component

    [AvaloniaFact]
    [Trait("Feature", "COMBO-004")]
    [Trait("Feature", "COMBO-005")]
    [Trait("Feature", "COMBO-016")]
    [Trait("Feature", "TREE-043")]
    [Trait("Feature", "EDIT-001")]
    public void RenamingAUsedComponent_UpdatesInstancesDerivedElementsAndReferences_AndUndoRestoresThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Panel");
        tree.Project.AddComponent("FancyButton");
        ComponentSave button = tree.Project.AddComponent("Button");
        ScreenSave title = tree.Project.AddScreen("Title");
        tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(Component(tree, "FancyButton")));
        VariableGridHarness grid = tree.Grid;
        grid.PickComboItem("BaseType", "Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        grid.TypeAndEnter("Width", "90");
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        grid.TypeLinesAndApply("VariableReferences", "Width = Components/Button.Width");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "PrimaryButton"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        Screen(tree, "Title").Instances.Single().BaseType.ShouldBe("PrimaryButton");
        Component(tree, "FancyButton").BaseType.ShouldBe("PrimaryButton");
        ReferenceLines(Component(tree, "Panel")).ShouldBe(new[] { "Width = Components/PrimaryButton.Width" });
        grid.ReadSaved(Screen(tree, "Title")).Instances.Single().BaseType.ShouldBe("PrimaryButton");
        grid.ReadSaved(Component(tree, "FancyButton")).BaseType.ShouldBe("PrimaryButton");
        ReferenceLines(grid.ReadSaved(Component(tree, "Panel"))).ShouldBe(new[] { "Width = Components/PrimaryButton.Width" });

        tree.Undo();
        Screen(tree, "Title").Instances.Single().BaseType.ShouldBe("Button");
        Component(tree, "FancyButton").BaseType.ShouldBe("Button");
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename should restore every file that used the old name");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-010")]
    [Trait("Feature", "TREE-011")]
    public void DeletingAUsedComponent_ListsItsUsers_AndLeavesTheirInstancesInError()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        ScreenSave title = tree.Project.AddScreen("Title");
        tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        string? deleteMessage = null;

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(dialog => { deleteMessage = dialog.Message; return true; });
        tree.RightClick(tree.NodeFor(Component(tree, "Button")));
        tree.PickMenu("Delete");

        deleteMessage.ShouldNotBeNull().ShouldContain("OkButton");
        tree.Project.Project.Components.ShouldBeEmpty();
        Screen(tree, "Title").Instances.Single().BaseType.ShouldBe("Button");
        Should.Throw<ShouldAssertException>(() => ProjectOracles.AssertCheckClean(tree.Project.ProjectFilePath))
            .Message.ShouldContain("Title");

        // The user deletes the orphaned instance; the project is clean again.
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Click(tree.NodeFor(Screen(tree, "Title").Instances.Single()));
        tree.Press(Key.Delete, PhysicalKey.Delete);
        Screen(tree, "Title").Instances.ShouldBeEmpty();

        tree.AssertOracles();
    }

    #endregion

    #region What the tab shows

    [AvaloniaFact]
    [Trait("Feature", "VAR-003")]
    [Trait("Feature", "VAR-004")]
    [Trait("Feature", "VAR-005")]
    [Trait("Feature", "VAR-034")]
    [Trait("Feature", "STATE-018")]
    public void FilteringAndCollapsing_ChangeWhichRowsShow_AndWriteNoFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        ProjectFileSnapshot start = tree.SnapshotFiles();
        int allRows = grid.ShownMemberNames().Count;

        // Categories come in a fixed order, each with its own header color.
        List<string> categories = grid.ShownCategoryNames();
        categories.IndexOf("Position").ShouldBeLessThan(categories.IndexOf("Dimensions"));
        categories.IndexOf("Dimensions").ShouldBeLessThan(categories.IndexOf("Text"));
        grid.Grid.Categories.Single(category => category.Name == "Position").HeaderColor
            .ShouldNotBe(grid.Grid.Categories.Single(category => category.Name == "Dimensions").HeaderColor);

        tree.Press(Key.E, PhysicalKey.E, RawInputModifiers.Control);
        grid.Settle();
        grid.View.FilterTextBox.IsFocused.ShouldBeTrue();
        grid.Input.Window.KeyTextInput("wid");
        grid.Settle();
        grid.ShownMemberNames().ShouldNotBeEmpty();
        grid.ShownMemberNames().ShouldAllBe(name => name.Contains("Wid", StringComparison.OrdinalIgnoreCase));

        grid.Input.Press(Key.Escape, PhysicalKey.Escape);
        grid.View.FilterTextBox.Text.ShouldBeNullOrEmpty();
        grid.ShownMemberNames().Count.ShouldBe(allRows);

        grid.Input.Click(grid.CategoryHeader("Position"));
        try
        {
            grid.Grid.Categories.Single(category => category.Name == "Position").IsExpanded.ShouldBeFalse();
            // The collapse is remembered by name across selections.
            tree.Click(tree.NodeFor(Component(tree, "Button")));
            tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single()));
            grid.Grid.Categories.Single(category => category.Name == "Position").IsExpanded.ShouldBeFalse();
        }
        finally
        {
            // The expansion memory is shared by every grid for the rest of the run.
            grid.Input.Click(grid.CategoryHeader("Position"));
        }
        grid.Grid.Categories.Single(category => category.Name == "Position").IsExpanded.ShouldBeTrue();

        tree.SnapshotFiles().ShouldMatch(start, "filtering and collapsing should write nothing");
        tree.AssertOracles();
    }

    #endregion

    #region Instance rows

    [AvaloniaFact]
    [Trait("Feature", "VAR-006")]
    [Trait("Feature", "VAR-007")]
    [Trait("Feature", "VAR-026")]
    [Trait("Feature", "VAR-027")]
    [Trait("Feature", "VAR-028")]
    [Trait("Feature", "VAR-029")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void ScrubbingMultiSelectingAndPickingParentStateAndType_OnInstances_SaveEachEdit_AndUndoRedoFollowThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave icon = tree.Project.AddComponent("Icon");
        StateSaveCategory looks = tree.Project.AddCategory(icon, "Looks");
        tree.Project.AddState(icon, looks, "Big");
        tree.Project.AddState(icon, looks, "Small");
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Holder", "Container");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        InstanceSave caption = tree.Project.AddInstance(button, "Caption", "Text");
        InstanceSave iconInstance = tree.Project.AddInstance(button, "IconInstance", "Icon");
        InstanceSave thing = tree.Project.AddInstance(button, "Thing", "Container");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("X", "10");
        ProjectFileSnapshot start = tree.SnapshotFiles();

        TextBlock xLabel = grid.Editor<TextBoxDisplay>("X").GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "X");
        global::Avalonia.Point from = grid.Input.CenterOf(xLabel);
        grid.Input.Drag(from, new global::Avalonia.Point(from.X + 20, from.Y), steps: 4);
        grid.Settle();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.X").ShouldBe(30f);

        tree.Click(tree.NodeFor(caption), RawInputModifiers.Control);
        tree.SelectedState.SelectedInstances.Count().ShouldBe(2);
        grid.TypeAndEnter("Y", "8");
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Y").ShouldBe(8f);
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Caption.Y").ShouldBe(8f);

        tree.Click(tree.NodeFor(label));
        grid.PickComboItem("Parent", "Holder");
        tree.Click(tree.NodeFor(iconInstance));
        grid.PickComboItem("LooksState", "Small");
        tree.Click(tree.NodeFor(thing));
        grid.ShownMemberNames().ShouldNotContain("Thing.Text");
        grid.PickComboItem("BaseType", "Text");
        grid.ShownMemberNames().ShouldContain("Thing.Text");

        ComponentSave saved = grid.ReadSaved(Component(tree, "Button"));
        VariableGridHarness.StoredValue(saved, "Label.X").ShouldBe(30f);
        VariableGridHarness.StoredValue(saved, "Caption.Y").ShouldBe(8f);
        VariableGridHarness.StoredValue(saved, "Label.Parent").ShouldBe("Holder");
        VariableGridHarness.StoredValue(saved, "IconInstance.LooksState").ShouldBe("Small");
        saved.Instances.Single(instance => instance.Name == "Thing").BaseType.ShouldBe("Text");

        tree.Undo();
        Component(tree, "Button").Instances.Single(instance => instance.Name == "Thing").BaseType.ShouldBe("Container");
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "IconInstance.LooksState").ShouldBeNull();
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Parent").ShouldBeNull();
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Y").ShouldBeNull();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Caption.Y").ShouldBeNull();
        tree.Undo();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.X").ShouldBe(10f);
        tree.SnapshotFiles().ShouldMatch(start, "undoing every edit should restore the files");

        for (int i = 0; i < 5; i++)
        {
            tree.Redo();
        }
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.X").ShouldBe(30f);
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Caption.Y").ShouldBe(8f);
        VariableGridHarness.StoredValue(Component(tree, "Button"), "IconInstance.LooksState").ShouldBe("Small");
        Component(tree, "Button").Instances.Single(instance => instance.Name == "Thing").BaseType.ShouldBe("Text");

        tree.AssertOracles();
    }

    #endregion

    #region Row and category menus

    [AvaloniaFact]
    [Trait("Feature", "VAR-013")]
    [Trait("Feature", "VAR-014")]
    [Trait("Feature", "VAR-016")]
    [Trait("Feature", "VAR-018")]
    [Trait("Feature", "VAR-019")]
    [Trait("Feature", "VAR-020")]
    [Trait("Feature", "VAR-021")]
    [Trait("Feature", "VAR-032")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void RenamingHidingExposingAndPastingVariables_FollowIntoInstances_AndUndoRedoFollowThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        VariableGridHarness grid = ExposeLabelTextAndUseButtonInTitle(tree);
        grid.TypeAndLeave("LabelText", "OK");
        InstanceSave caption = tree.Project.AddInstance(Component(tree, "Button"), "Caption", "Text");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single(instance => instance.Name == "Label")));
        grid.TypeAndEnter("X", "10");
        grid.TypeAndEnter("Y", "20");
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextUserString("Caption");
        grid.PickRowMenuItem("Text", "Rename Variable [LabelText]");
        Component(tree, "Button").GetDefaultStateOrThrow().Variables.ShouldContain(variable => variable.ExposedAsName == "Caption");
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.Caption").ShouldBe("OK");

        tree.Click(tree.NodeFor(Component(tree, "Button")));
        tree.Dialogs.AnswerNext<AddVariableViewModel>(dialog => { dialog.SelectedItem = "float"; dialog.EnteredName = "Speed"; return true; });
        grid.Input.Click(grid.View.AddVariableButton);
        grid.Settle();
        string? duplicateError = null;
        tree.Dialogs.AnswerNext<AddVariableViewModel>(dialog =>
        {
            dialog.SelectedItem = "float";
            dialog.EnteredName = "Speed";
            duplicateError = dialog.ErrorMessage;
            return false;
        });
        grid.Input.Click(grid.View.AddVariableButton);
        grid.Settle();
        duplicateError.ShouldNotBeNullOrEmpty();
        Component(tree, "Button").GetDefaultStateOrThrow().Variables.Count(variable => variable.Name == "Speed").ShouldBe(1);

        grid.PickRowMenuItem("Speed", "Hide from Instances");
        tree.Click(tree.NodeFor(Screen(tree, "Title").Instances.Single()));
        grid.ShownMemberNames().ShouldNotContain(name => name.EndsWith("Speed", StringComparison.Ordinal));
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        grid.PickRowMenuItem("Speed", "Show on Instances");
        tree.Click(tree.NodeFor(Screen(tree, "Title").Instances.Single()));
        grid.ShownMemberNames().ShouldContain(name => name.EndsWith("Speed", StringComparison.Ordinal));

        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single(instance => instance.Name == "Label")));
        tree.Dialogs.AnswerNext<ExposeColorDialogViewModel>(_ => true);
        grid.PickRowMenuItem("Color", "Expose Color");
        Component(tree, "Button").GetDefaultStateOrThrow().Variables
            .Where(variable => variable.Name is "Label.Red" or "Label.Green" or "Label.Blue")
            .ShouldAllBe(variable => !string.IsNullOrEmpty(variable.ExposedAsName));

        grid.PickCategoryMenuItem("Position", "Copy Values");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single(instance => instance.Name == "Caption")));
        grid.PickCategoryMenuItem("Position", "Paste Values");
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Caption.X").ShouldBe(10f);
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Button")), "Caption.Y").ShouldBe(20f);

        // Button's history: paste, expose, show, hide, add, rename (which also rewrote Title).
        for (int i = 0; i < 6; i++)
        {
            tree.Undo();
        }
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBe("OK");
        tree.SnapshotFiles().ShouldMatch(start, "undoing every step should restore the files");

        for (int i = 0; i < 6; i++)
        {
            tree.Redo();
        }
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.Caption").ShouldBe("OK");
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Caption.X").ShouldBe(10f);
        Component(tree, "Button").GetDefaultStateOrThrow().GetVariableSave("Speed").ShouldNotBeNull();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "VAR-021")]
    [Trait("Feature", "VAR-022")]
    [Trait("Feature", "VAR-023")]
    [Trait("Feature", "VAR-031")]
    [Trait("Feature", "VAR-033")]
    public void ABehaviorsVariables_AreAddedEditedRequiredAndDeleted()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        VariableGridHarness grid = tree.Grid;
        ((DataUiGrid)grid.View.BehaviorGrid).Categories.SelectMany(category => category.Members).Select(member => member.Name)
            .ShouldContain("DefaultImplementation");

        tree.Dialogs.AnswerNext<AddVariableViewModel>(dialog => { dialog.SelectedItem = "bool"; dialog.EnteredName = "IsEnabled"; return true; });
        grid.Input.Click(grid.View.AddVariableButton);
        grid.Settle();
        Behavior(tree).RequiredVariables.Variables.Select(variable => variable.Name).ShouldBe(new[] { "IsEnabled" });

        tree.Dialogs.AnswerNext<AddVariableViewModel>(dialog => { dialog.EnteredName = "IsActive"; return true; });
        PickBehaviorVariableMenuItem(grid, "IsEnabled", "Edit Variable");
        Behavior(tree).RequiredVariables.Variables.Select(variable => variable.Name).ShouldBe(new[] { "IsActive" });

        // A component using the behavior reports the variable it lacks until it adds it.
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        BehaviorsViewModel behaviors = BehaviorsTab();
        behaviors.EditCommand.Execute(null);
        behaviors.AllBehaviors.Single(item => item.Name == "Clickable").IsChecked = true;
        behaviors.ConfirmEditCommand.Execute(null);
        tree.ThrowIfCrashed();
        Component(tree, "Button").Behaviors.Select(reference => reference.BehaviorName).ShouldBe(new[] { "Clickable" });
        grid.Settle();
        grid.ViewModel.HasErrors.ShouldBeTrue();
        grid.ViewModel.ErrorInformation.ShouldNotBeNull().ShouldContain("IsActive");
        tree.Dialogs.AnswerNext<AddVariableViewModel>(dialog => { dialog.SelectedItem = "bool"; dialog.EnteredName = "IsActive"; return true; });
        grid.Input.Click(grid.View.AddVariableButton);
        grid.Settle();
        grid.ViewModel.HasErrors.ShouldBeFalse();

        tree.Click(tree.RootNode("Behaviors").Nodes.Single());
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        PickBehaviorVariableMenuItem(grid, "IsActive", "Delete Variable");
        Behavior(tree).RequiredVariables.Variables.ShouldBeEmpty();
        File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Behaviors", "Clickable.behx")).ShouldNotContain("IsActive");

        tree.AssertOracles();
    }

    #endregion

    #region References and fonts

    [AvaloniaFact]
    [Trait("Feature", "VAR-010")]
    [Trait("Feature", "VAR-017")]
    [Trait("Feature", "VAR-025")]
    [Trait("Feature", "VAR-030")]
    [Trait("Feature", "EDIT-001")]
    public void ACopiedVariableName_BecomesAReference_F12OnItSelectsItsSource_AndAFontSizeChange_AsksForTheFont()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Project.AddComponent("Panel");
        // Fonts are made for the elements that use the edited one.
        tree.Project.AddInstance(tree.Project.AddScreen("Title"), "OkButton", "Button");
        RecordingClipboardService clipboard = (RecordingClipboardService)TestAppBuilder.Services.GetRequiredService<IClipboardService>();
        clipboard.Clear();
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        grid.PickRowMenuItem("X", "Copy Qualified Variable Name");
        clipboard.LastText.ShouldBe("Components/Button.Label.X");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        grid.PickRowMenuItem("Width", "Copy Qualified Variable Name");
        clipboard.LastText.ShouldBe("Components/Button.Width");
        grid.TypeAndEnter("Width", "90");

        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        grid.TypeLinesAndApply("VariableReferences", "Width = " + clipboard.LastText);
        VariableGridHarness.StoredValue(Component(tree, "Panel"), "Width").ShouldBe(90f);

        TextBox references = grid.Editor<StringListTextBoxDisplay>("VariableReferences").EditorTextBox;
        grid.Input.Click(references);
        references.CaretIndex = 0;
        grid.Input.Press(Key.F12, PhysicalKey.F12);
        grid.Settle();
        tree.SelectedState.SelectedElement.ShouldBeSameAs(Component(tree, "Button"));

        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot beforeFont = tree.SnapshotFiles();
        NoOpFontFileGenerator fonts = (NoOpFontFileGenerator)TestAppBuilder.Services.GetRequiredService<IFontFileGenerator>();
        lock (fonts.RequestedFntPaths)
        {
            fonts.RequestedFntPaths.Clear();
        }
        grid.TypeAndEnter("FontSize", "37");
        tree.WaitUntil(() => { lock (fonts.RequestedFntPaths) { return fonts.RequestedFntPaths.Any(path => path.Contains("37")); } },
            TimeSpan.FromSeconds(10), "a font of size 37 to be asked for");

        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(beforeFont, "undoing the font size should restore the files");
        tree.AssertOracles();
    }

    #endregion

    private static void PickBehaviorVariableMenuItem(VariableGridHarness grid, string variableName, string header)
    {
        grid.Settle();
        ListBoxItem row = grid.BehaviorVariables.GetVisualDescendants().OfType<ListBoxItem>()
            .Single(item => (item.DataContext as VariableSave)?.Name == variableName);
        grid.Input.Click(row);
        grid.Input.RightClick(row);
        grid.Input.PickContextMenuItem(header);
        grid.Settle();
    }

    private static BehaviorSave Behavior(ProjectTreeHarness tree) => tree.Project.Project.Behaviors.Single();

    /// <summary>The view model behind the head's Behaviors tab.</summary>
    private static BehaviorsViewModel BehaviorsTab() =>
        ((AvaloniaTabManager)TestAppBuilder.Services.GetRequiredService<ITabManager>()).AllTabs
            .Select(tab => tab.Content is Control view ? view.DataContext : tab.Content)
            .OfType<BehaviorsViewModel>().Single();

    private static ScreenSave Screen(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Screens.Single(screen => screen.Name == name);

    private static List<string> ReferenceLines(ElementSave element) =>
        element.GetDefaultStateOrThrow().GetVariableListSave("VariableReferences")?.ValueAsIList?.Cast<string>().ToList() ?? new List<string>();

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static StateSave PressedState(ProjectTreeHarness tree) =>
        Component(tree, "Button").Categories.Single().States.Single(state => state.Name == "Pressed");
}
