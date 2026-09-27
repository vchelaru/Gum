using Avalonia.Headless.XUnit;
using Avalonia.Input;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Services.Dialogs;
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

    [AvaloniaFact(Skip = "#5246: un-exposing a variable leaves the instance values set through it")]
    [Trait("Feature", "VAR-012")]
    [Trait("Feature", "COMBO-021")]
    public void UnexposingAVariableSetOnInstances_ClearsThoseValues()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        VariableGridHarness grid = ExposeLabelTextAndUseButtonInTitle(tree);
        grid.TypeAndLeave("LabelText", "OK");

        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single()));
        grid.PickRowMenuItem("Text", "Un-expose Variable LabelText (Label.Text)");

        Component(tree, "Button").GetDefaultStateOrThrow().Variables.ShouldNotContain(variable => variable.ExposedAsName == "LabelText");
        // The instance value set the exposed variable, which no longer exists.
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LabelText").ShouldBeNull();
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.LabelText").ShouldBeNull();

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

    private static ScreenSave Screen(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Screens.Single(screen => screen.Name == name);

    private static List<string> ReferenceLines(ElementSave element) =>
        element.GetDefaultStateOrThrow().GetVariableListSave("VariableReferences")?.ValueAsIList?.Cast<string>().ToList() ?? new List<string>();

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static StateSave PressedState(ProjectTreeHarness tree) =>
        Component(tree, "Button").Categories.Single().States.Single(state => state.Name == "Pressed");
}
