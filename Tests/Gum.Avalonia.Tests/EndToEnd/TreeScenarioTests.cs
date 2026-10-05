using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the Project tree (inventory area TREE): real clicks, menus, keys and
/// scripted dialogs on the head's own tree. Each one undoes and redoes what it did where the tool
/// records undo, checks that undoing back to the start restores the files byte for byte, and ends
/// with the shared oracles (<see cref="ProjectTreeHarness.AssertOracles"/>).
/// </summary>
[Trait("Category", "EndToEnd")]
public class TreeScenarioTests
{
    #region Adding

    [AvaloniaFact]
    [Trait("Feature", "TREE-050")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    [Trait("Feature", "KEY-001")]
    [Trait("Feature", "KEY-002")]
    public void AddObjectMenu_AddsAnInstance_AndUndoRedoFollowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(button));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.RightClick(tree.NodeFor(button));
        tree.PickMenu("Add object to Button", "Sprite");

        InstanceSave sprite = button.Instances.ShouldHaveSingleItem();
        sprite.BaseType.ShouldBe("Sprite");
        tree.SelectedState.SelectedInstance.ShouldBeSameAs(sprite);
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { sprite.Name });

        tree.Undo();
        button.Instances.ShouldBeEmpty();
        tree.ChildTexts(tree.NodeFor(button)).ShouldBeEmpty();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the add should restore the files");

        tree.Redo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { sprite.Name });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { sprite.Name });

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-015")]
    [Trait("Feature", "TREE-017")]
    [Trait("Feature", "TREE-018")]
    [Trait("Feature", "DLG-005")]
    public void AddScreenComponentAndFolder_FromTheRootMenus_ShowInTheTreeAndOnDisk()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();

        tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog => { dialog.Value = "TitleScreen"; return true; });
        tree.RightClick(tree.RootNode("Screens"));
        tree.PickMenu("Add Screen");

        tree.Dialogs.AnswerNext<AddFolderDialogViewModel>(dialog => { dialog.Value = "Controls"; return true; });
        tree.RightClick(tree.RootNode("Components"));
        tree.PickMenu("Add Folder");

        tree.Dialogs.AnswerNext<AddComponentDialogViewModel>(dialog => { dialog.Value = "Toggle"; return true; });
        tree.RightClick(tree.FolderNode("Components", "Controls"));
        tree.PickMenu("Add Component");

        tree.Project.Project.Screens.Select(screen => screen.Name).ShouldBe(new[] { "TitleScreen" });
        ComponentSave toggle = tree.Project.Project.Components.ShouldHaveSingleItem();
        toggle.Name.ShouldBe("Controls/Toggle");
        tree.ChildTexts(tree.RootNode("Screens")).ShouldBe(new[] { "TitleScreen" });
        tree.ChildTexts(tree.FolderNode("Components", "Controls")).ShouldBe(new[] { "Toggle" });
        tree.SelectedState.SelectedElement.ShouldBeSameAs(toggle);
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Controls", "Toggle.gucx")).ShouldBeTrue();

        // Adding an element records no undo (per-element history starts with the element), so
        // Ctrl+Z and Ctrl+Y must leave everything as it is.
        ProjectFileSnapshot added = tree.SnapshotFiles();
        tree.Undo();
        tree.Redo();
        tree.SnapshotFiles().ShouldMatch(added, "an add with no undo history should survive Ctrl+Z");
        tree.Project.Project.Components.ShouldHaveSingleItem().ShouldBeSameAs(toggle);

        tree.AssertOracles();
    }

    #endregion

    #region Duplicating

    [AvaloniaFact]
    [Trait("Feature", "TREE-046")]
    [Trait("Feature", "TREE-034")]
    [Trait("Feature", "KEY-006")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void DuplicatingAnInstance_WithCtrlDAndTheMenu_AddsCopies_AndUndoRemovesThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Press(Key.D, PhysicalKey.D, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(label));
        tree.RightClick(tree.NodeFor(label));
        tree.PickMenu("Duplicate Label");

        button.Instances.Count.ShouldBe(3);
        button.Instances.ShouldAllBe(instance => instance.BaseType == "Text");
        button.Instances.Select(instance => instance.Name).Distinct().Count().ShouldBe(3);
        tree.ChildTexts(tree.NodeFor(button)).Count.ShouldBe(3);

        tree.Undo();
        tree.Undo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing both duplicates should restore the files");

        tree.Redo();
        tree.Redo();
        button.Instances.Count.ShouldBe(3);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-027")]
    [Trait("Feature", "DLG-002")]
    public void DuplicatingAComponent_FromItsMenu_AddsACopyWithItsInstances()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(button));

        tree.Dialogs.AnswerNextUserString("ButtonCopy");
        tree.RightClick(tree.NodeFor(button));
        tree.PickMenu("Duplicate Button");

        ComponentSave copy = tree.Project.Project.Components.Single(component => component.Name == "ButtonCopy");
        copy.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        tree.ChildTexts(tree.NodeFor(copy)).ShouldBe(new[] { "Label" });
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "ButtonCopy.gucx")).ShouldBeTrue();

        tree.AssertOracles();
    }

    #endregion

    #region Deleting

    [AvaloniaFact]
    [Trait("Feature", "TREE-042")]
    [Trait("Feature", "TREE-035")]
    [Trait("Feature", "KEY-007")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void DeleteKeyOnAnInstance_RemovesIt_OnceConfirmed_AndUndoRestoresIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Background", "NineSlice");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Press(Key.Delete, PhysicalKey.Delete);

        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Background" });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "Background" });

        tree.Undo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Background", "Label" });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "Background", "Label" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing the delete should restore the files");

        tree.Redo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Background" });

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-028")]
    public void DeletingAComponent_FromItsMenu_RemovesItsNodeAndFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddComponent("Panel");
        tree.Click(tree.NodeFor(button));

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.RightClick(tree.NodeFor(button));
        tree.PickMenu("Delete");

        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Panel" });
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Panel" });
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx")).ShouldBeFalse();

        // Deleting an element is not undoable (its history goes with it); Ctrl+Z must not bring
        // back a half-restored element.
        ProjectFileSnapshot deleted = tree.SnapshotFiles();
        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(deleted, "an element delete has no undo");

        tree.AssertOracles();
    }

    #endregion

    #region Renaming

    [AvaloniaFact]
    [Trait("Feature", "TREE-043")]
    [Trait("Feature", "KEY-008")]
    [Trait("Feature", "DLG-006")]
    [Trait("Feature", "EDIT-001")]
    public void F2OnAComponent_RenamesItsNodeAndFile_AndUndoRenamesItBack()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(button));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "PrimaryButton"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        tree.Dialogs.Messages.ShouldHaveSingleItem().ShouldStartWith("Are you sure you want to rename Button?");
        button.Name.ShouldBe("PrimaryButton");
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "PrimaryButton" });
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "PrimaryButton.gucx")).ShouldBeTrue();
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx")).ShouldBeFalse();

        tree.Undo();
        button.Name.ShouldBe("Button");
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Button" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-043")]
    [Trait("Feature", "KEY-008")]
    [Trait("Feature", "DLG-002")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void F2OnAnInstance_RenamesIt_AndUndoRestoresTheName()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextUserString("Caption");
        tree.Press(Key.F2, PhysicalKey.F2);

        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Caption" });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "Caption" });

        tree.Undo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "Label" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename should restore the files");

        tree.Redo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Caption" });

        tree.AssertOracles();
    }

    #endregion

    #region Copy, cut and paste

    [AvaloniaFact]
    [Trait("Feature", "TREE-044")]
    [Trait("Feature", "KEY-003")]
    [Trait("Feature", "KEY-005")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void CtrlCThenCtrlV_WithinAnElement_AddsACopy_AndUndoRemovesIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        button.Instances.Count.ShouldBe(2);
        button.Instances.ShouldAllBe(instance => instance.BaseType == "Text");
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(button.Instances.Select(instance => instance.Name));

        tree.Undo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing the paste should restore the files");

        tree.Redo();
        button.Instances.Count.ShouldBe(2);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-044")]
    [Trait("Feature", "TREE-045")]
    [Trait("Feature", "KEY-004")]
    [Trait("Feature", "KEY-005")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "COMBO-018")]
    public void CtrlXThenCtrlV_IntoAnotherElement_MovesTheInstance_AndUndoInEachRestoresBoth()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave panel = tree.Project.AddComponent("Panel");
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Background", "NineSlice");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Press(Key.X, PhysicalKey.X, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(panel));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Background" });
        panel.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        panel.Instances.Single().BaseType.ShouldBe("Text");
        tree.ChildTexts(tree.NodeFor(panel)).ShouldBe(new[] { "Label" });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "Background" });

        // Undo history is per element: the paste undoes in Panel, the cut in Button.
        tree.Click(tree.NodeFor(panel));
        tree.Undo();
        tree.Click(tree.NodeFor(button));
        tree.Undo();
        panel.Instances.ShouldBeEmpty();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Background", "Label" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing the paste and the cut should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-044")]
    [Trait("Feature", "KEY-003")]
    [Trait("Feature", "KEY-005")]
    public void CtrlCThenCtrlV_IntoAnotherElement_CopiesTheInstanceAndItsValues()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave panel = tree.Project.AddComponent("Panel");
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        button.DefaultState!.SetValue("Label.Text", "Click me", "string");
        tree.SaveAll();
        tree.Click(tree.NodeFor(label));

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(panel));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        panel.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        panel.DefaultState!.GetValue("Label.Text").ShouldBe("Click me");
        tree.ChildTexts(tree.NodeFor(panel)).ShouldBe(new[] { "Label" });

        tree.AssertOracles();
    }

    #endregion

    #region Reordering

    [AvaloniaFact]
    [Trait("Feature", "TREE-047")]
    [Trait("Feature", "KEY-016")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void AltUpAndAltDown_ReorderTheSelectedInstance_AndUndoRestoresTheOrder()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "A", "Sprite");
        tree.Project.AddInstance(button, "B", "Sprite");
        InstanceSave c = tree.Project.AddInstance(button, "C", "Sprite");
        tree.Click(tree.NodeFor(c));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Press(Key.Up, PhysicalKey.ArrowUp, RawInputModifiers.Alt);
        tree.Press(Key.Up, PhysicalKey.ArrowUp, RawInputModifiers.Alt);

        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "C", "A", "B" });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "C", "A", "B" });
        tree.SelectedState.SelectedInstance!.Name.ShouldBe("C");

        tree.Press(Key.Down, PhysicalKey.ArrowDown, RawInputModifiers.Alt);
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "A", "C", "B" });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "A", "C", "B" });

        tree.Undo();
        tree.Undo();
        tree.Undo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "A", "B", "C" });
        tree.ChildTexts(tree.NodeFor(button)).ShouldBe(new[] { "A", "B", "C" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing every move should restore the files");

        tree.Redo();
        button.Instances.Select(instance => instance.Name).ShouldBe(new[] { "A", "C", "B" });

        tree.AssertOracles();
    }

    #endregion

    #region Instance menu

    [AvaloniaFact]
    [Trait("Feature", "TREE-033")]
    [Trait("Feature", "EDIT-001")]
    [Trait("Feature", "EDIT-002")]
    public void LockMenu_LocksTheInstance_OffersUnlock_AndUndoRedoFollowIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.RightClick(tree.NodeFor(label));
        tree.PickMenu("Lock Label");

        Component(tree, "Button").Instances.Single().Locked.ShouldBeTrue();
        File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx")).ShouldContain("Locked=\"true\"");
        tree.RightClick(tree.NodeFor(Component(tree, "Button").Instances.Single()));
        tree.MenuHeaders().ShouldContain("Unlock Label");
        tree.PickMenu("Unlock Label");
        Component(tree, "Button").Instances.Single().Locked.ShouldBeFalse();

        tree.Undo();
        Component(tree, "Button").Instances.Single().Locked.ShouldBeTrue();
        tree.Undo();
        Component(tree, "Button").Instances.Single().Locked.ShouldBeFalse();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the lock and unlock should restore the files");
        tree.Redo();
        Component(tree, "Button").Instances.Single().Locked.ShouldBeTrue();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-031")]
    public void GoToDefinition_SelectsTheInstancesComponent()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave okButton = tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(okButton));

        tree.RightClick(tree.NodeFor(okButton));
        tree.PickMenu("Go to definition");

        tree.SelectedState.SelectedElement.ShouldBeSameAs(Component(tree, "Button"));
        tree.SelectedState.SelectedInstance.ShouldBeNull();
        tree.View.Selection.SelectedNode.ShouldBeSameAs(tree.NodeFor(Component(tree, "Button")));

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-036")]
    [Trait("Feature", "EDIT-001")]
    public void AddParentObject_WrapsTheInstanceInANewContainer_AndUndoRemovesIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNext<AddInstanceDialogViewModel>(dialog => { dialog.Value = "Frame"; return true; });
        tree.RightClick(tree.NodeFor(label));
        tree.PickMenu("Add parent object to 'Label'");

        ComponentSave edited = Component(tree, "Button");
        edited.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label", "Frame" }, ignoreOrder: true);
        edited.Instances.Single(instance => instance.Name == "Frame").BaseType.ShouldBe("Container");
        edited.GetDefaultStateOrThrow().GetValue("Label.Parent").ShouldBe("Frame");
        tree.ChildTexts(tree.NodeFor(edited)).ShouldBe(new[] { "Frame" });
        tree.ChildTexts(tree.NodeFor(edited.Instances.Single(instance => instance.Name == "Frame"))).ShouldBe(new[] { "Label" });

        tree.Undo();
        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing the wrap should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-037")]
    [Trait("Feature", "EDIT-001")]
    public void AddToBase_MovesTheInstanceIntoTheBaseComponent_AndUndoInEachRestoresBoth()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Project.AddComponent("FancyButton");
        tree.Click(tree.NodeFor(Component(tree, "FancyButton")));
        tree.Grid.PickComboItem("BaseType", "Button");
        InstanceSave glow = tree.Project.AddInstance(Component(tree, "FancyButton"), "Glow", "Sprite");
        tree.Click(tree.NodeFor(glow));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.RightClick(tree.NodeFor(glow));
        tree.PickMenu("Add Glow to base Button");

        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Glow" });
        Component(tree, "Button").Instances.Single().BaseType.ShouldBe("Sprite");
        Component(tree, "FancyButton").Instances.ShouldHaveSingleItem().DefinedByBase.ShouldBeTrue();
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Glow.Parent").ShouldBeNull();
        tree.ChildTexts(tree.NodeFor(Component(tree, "Button"))).ShouldBe(new[] { "Glow" });

        tree.Click(tree.NodeFor(Component(tree, "Button")));
        tree.Undo();
        tree.Click(tree.NodeFor(Component(tree, "FancyButton")));
        tree.Undo();
        Component(tree, "Button").Instances.ShouldBeEmpty();
        Component(tree, "FancyButton").Instances.ShouldHaveSingleItem().DefinedByBase.ShouldBeFalse();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the move in each element should restore the files");

        tree.AssertOracles();
    }

    #endregion

    #region Element menu

    [AvaloniaFact]
    [Trait("Feature", "TREE-029")]
    public void ForceSaveObject_WritesTheElementsFileAgain()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Label", "Text");
        string file = Path.Combine(tree.Project.ProjectFolder, "Components", "Button.gucx");
        string saved = File.ReadAllText(file);
        File.Delete(file);
        tree.Click(tree.NodeFor(button));

        tree.RightClick(tree.NodeFor(button));
        tree.PickMenu("Force Save Object");

        File.ReadAllText(file).ShouldBe(saved);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-041")]
    [Trait("Feature", "TREE-002")]
    public void DeletingAMixedSelection_OfAComponentAndAnInstance_RemovesBoth()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave panel = tree.Project.AddComponent("Panel");
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Background", "NineSlice");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(panel));
        tree.Click(tree.NodeFor(label), RawInputModifiers.Control);

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.RightClick(tree.NodeFor(label));
        tree.MenuHeaders().ShouldBe(new[] { "Delete 2 items" });
        tree.PickMenu("Delete 2 items");

        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Button" });
        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Background" });
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Button" });
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Panel.gucx")).ShouldBeFalse();

        tree.AssertOracles();
    }

    #endregion

    #region Folders and behaviors

    [AvaloniaFact]
    [Trait("Feature", "TREE-024")]
    public void DeleteFolder_RemovesAnEmptyFolderOnceConfirmed_AndRefusesOneWithComponents()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNext<AddFolderDialogViewModel>(dialog => { dialog.Value = "Empty"; return true; });
        tree.RightClick(tree.RootNode("Components"));
        tree.PickMenu("Add Folder");
        tree.Project.AddComponent("Controls/Toggle");
        string emptyFolder = Path.Combine(tree.Project.ProjectFolder, "Components", "Empty");
        Directory.Exists(emptyFolder).ShouldBeTrue();

        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.RightClick(tree.FolderNode("Components", "Empty"));
        tree.PickMenu("Delete Folder");

        Directory.Exists(emptyFolder).ShouldBeFalse();
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Controls" });

        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.RightClick(tree.FolderNode("Components", "Controls"));
        tree.PickMenu("Delete Folder");

        tree.Dialogs.Messages.Count.ShouldBe(2);
        tree.Dialogs.Messages[1].ShouldBe("Cannot delete this folder, it currently contains a file.");
        Directory.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Controls")).ShouldBeTrue();
        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Controls/Toggle" });

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-020")]
    public void AddBehavior_FromTheBehaviorsMenu_AddsItToTheProjectTreeAndDisk()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();

        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");

        tree.Project.Project.Behaviors.Select(behavior => behavior.Name).ShouldBe(new[] { "Clickable" });
        tree.ChildTexts(tree.RootNode("Behaviors")).ShouldBe(new[] { "Clickable" });
        tree.SelectedState.SelectedBehavior.ShouldBeSameAs(tree.Project.Project.Behaviors.Single());
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Behaviors", "Clickable.behx")).ShouldBeTrue();

        tree.AssertOracles();
    }

    #endregion

    #region Searching

    [AvaloniaFact]
    [Trait("Feature", "TREE-009")]
    [Trait("Feature", "TREE-008")]
    [Trait("Feature", "COMBO-040")]
    public void SearchingByNameAndByVariable_FindsElementsAndInstances_AndAFoundElementCanBeRenamed()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        ScreenSave title = tree.Project.AddScreen("Title");
        tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(label));
        tree.Grid.TypeAndLeave("Text", "Press start");

        tree.Search("button");
        tree.SearchResultTexts().ShouldBe(new[] { "Title/OkButton (Button)", "Button (Container)" }, ignoreOrder: true);

        tree.Search("start");
        tree.SearchResultTexts().ShouldBeEmpty();
        tree.ClickIncludeVariables();
        tree.SearchResultTexts().ShouldBe(new[] { "Label.Text=Press start on Button/Label" });

        tree.Search("butt");
        tree.ClickSearchResult("Button (Container)");
        tree.SelectedState.SelectedElement.ShouldBeSameAs(Component(tree, "Button"));

        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "PrimaryButton"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        Component(tree, "PrimaryButton").ShouldNotBeNull();
        tree.Project.Project.Screens.Single().Instances.Single().BaseType.ShouldBe("PrimaryButton");

        tree.View.ClearSearchText();
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-010")]
    public void ClearSearchButton_EmptiesTheSearch_AndShowsTheTreeAgain()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");

        tree.Search("butt");
        tree.View.SearchResults.IsVisible.ShouldBeTrue();
        tree.View.Tree.IsVisible.ShouldBeFalse();

        tree.Input.Click(tree.View.SearchClearButton);

        tree.View.SearchBox.Text.ShouldBeNullOrEmpty();
        tree.View.SearchResults.IsVisible.ShouldBeFalse();
        tree.View.Tree.IsVisible.ShouldBeTrue();
        tree.ChildTexts(tree.RootNode("Components")).ShouldBe(new[] { "Button" });
    }

    #endregion

    #region More element and instance menus

    [AvaloniaFact]
    [Trait("Feature", "TREE-023")]
    public void RenameFolder_MovesItsComponents_AndUpdatesInstancesOfThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Controls/Toggle");
        ScreenSave title = tree.Project.AddScreen("Title");
        tree.Project.AddInstance(title, "SoundToggle", "Controls/Toggle");

        tree.Dialogs.AnswerNext<RenameFolderDialogViewModel>(dialog => { dialog.Value = "Widgets"; return true; });
        tree.RightClick(tree.FolderNode("Components", "Controls"));
        tree.PickMenu("Rename Folder");

        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Widgets/Toggle" });
        tree.Project.Project.Screens.Single().Instances.Single().BaseType.ShouldBe("Widgets/Toggle");
        tree.ChildTexts(tree.FolderNode("Components", "Widgets")).ShouldBe(new[] { "Toggle" });
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Widgets", "Toggle.gucx")).ShouldBeTrue();
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Controls", "Toggle.gucx")).ShouldBeFalse();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-030")]
    [Trait("Feature", "TREE-050")]
    public void AFavoriteComponent_IsOfferedFirstInAddObject_AndAddsAnInstance()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Panel");
        tree.Project.AddComponent("Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));

        tree.RightClick(tree.NodeFor(Component(tree, "Button")));
        tree.PickMenu("Add to Favorites");
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        tree.RightClick(tree.NodeFor(Component(tree, "Panel")));
        tree.PickMenu("Add object to Panel", "Button");

        Component(tree, "Panel").Instances.ShouldHaveSingleItem().BaseType.ShouldBe("Button");

        tree.RightClick(tree.NodeFor(Component(tree, "Button")));
        tree.PickMenu("Remove from Favorites");
        tree.RightClick(tree.NodeFor(Component(tree, "Button")));
        tree.MenuHeaders().ShouldContain("Add to Favorites");
        tree.View.ContextMenu.Close();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-032")]
    public void CreateComponent_FromAParentInstance_MovesItsChildrenIntoTheNewComponent()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave box = tree.Project.AddInstance(button, "Box", "Container");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Project.AddInstance(button, "Caption", "Text");
        tree.Click(tree.NodeFor(label));
        tree.Grid.PickComboItem("Parent", "Box");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.First()));

        tree.Dialogs.AnswerNext<CreateComponentDialogViewModel>(dialog => { dialog.IsCheckboxChecked = true; return true; });
        tree.RightClick(tree.NodeFor(Component(tree, "Button").Instances.First()));
        tree.PickMenu("Create Component");

        ComponentSave created = Component(tree, "BoxComponent");
        created.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        // The replacement keeps Box's place in the order, which is the draw order.
        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Box", "Caption" });
        InstanceSave replacement = Component(tree, "Button").Instances.First();
        replacement.Name.ShouldBe("Box");
        replacement.BaseType.ShouldBe("BoxComponent");
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "BoxComponent.gucx")).ShouldBeTrue();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-032")]
    public void CreateComponent_DropsAReferenceFromOutsideToAMovedChild_KeepingItsValue_AndOneUndoRestoresIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Box", "Container");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        InstanceSave caption = tree.Project.AddInstance(button, "Caption", "Text");
        tree.Click(tree.NodeFor(label));
        tree.Grid.PickComboItem("Parent", "Box");
        tree.Grid.TypeAndEnter("X", "12");
        tree.Click(tree.NodeFor(caption));
        tree.Grid.TypeLinesAndApply("VariableReferences", "X = Label.X");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.First(instance => instance.Name == "Box")));

        tree.Dialogs.AnswerNext<CreateComponentDialogViewModel>(dialog => { dialog.IsCheckboxChecked = true; return true; });
        tree.RightClick(tree.NodeFor(Component(tree, "Button").Instances.First(instance => instance.Name == "Box")));
        tree.PickMenu("Create Component");

        tree.OutputWritten.ShouldContain("Label moved into BoxComponent, so these references were dropped:");
        tree.OutputWritten.ShouldContain("Button (Default): Caption.VariableReferences line \"X = Label.X\", kept Caption.X = 12");
        Component(tree, "Button").GetDefaultStateOrThrow().VariableLists.ShouldNotContain(list => list.Name == "Caption.VariableReferences");
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Caption.X").ShouldBe(12f);

        tree.Undo();

        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Box", "Label", "Caption" }, ignoreOrder: true);
        Component(tree, "Button").Instances.Single(instance => instance.Name == "Box").BaseType.ShouldBe("Container");
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Parent").ShouldBe("Box");
        Component(tree, "Button").GetDefaultStateOrThrow().VariableLists.Single(list => list.Name == "Caption.VariableReferences")
            .ValueAsIList.Cast<string>().ShouldBe(new[] { "X = Label.X" });
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-032")]
    public void CreateComponent_KeepsWhatOtherStatesSetOnThePromotedInstance()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Box", "Container");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        tree.Grid.PickComboItem("Parent", "Box");
        StateSaveCategory look = tree.Project.AddCategory(button, "Look");
        StateSave hidden = tree.Project.AddState(button, look, "Hidden");
        hidden.SetValue("Box.Visible", false, "bool");
        hidden.SetValue("Label.Red", 10, "int");

        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.First(instance => instance.Name == "Box")));
        tree.Dialogs.AnswerNext<CreateComponentDialogViewModel>(dialog => { dialog.IsCheckboxChecked = true; return true; });
        tree.RightClick(tree.NodeFor(Component(tree, "Button").Instances.First(instance => instance.Name == "Box")));
        tree.PickMenu("Create Component");

        StateSave hiddenAfter = Component(tree, "Button").Categories.Single().States.Single();
        hiddenAfter.GetValue("Box.Visible").ShouldBe(false);
        hiddenAfter.GetValue("Label.Red").ShouldBeNull();
        tree.OutputWritten.ShouldContain("Button (Hidden): Label.Red = 10");

        tree.Undo();

        Component(tree, "Button").Categories.Single().States.Single().GetValue("Label.Red").ShouldBe(10);
        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-025")]
    public void ViewReferences_ListsTheElementsThatUseTheComponent()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        tree.Project.AddComponent("Panel");
        ScreenSave title = tree.Project.AddScreen("Title");
        tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        List<string> references = new List<string>();
        string message = "";

        tree.Dialogs.AnswerNext<DisplayReferencesDialog>(dialog =>
        {
            references.AddRange(dialog.References.Select(reference => reference.ToString() ?? ""));
            message = dialog.Message;
            return false;
        });
        tree.RightClick(tree.NodeFor(Component(tree, "Button")));
        tree.PickMenu("View References");

        message.ShouldBe("The following files reference Button (Container)");
        references.ShouldHaveSingleItem().ShouldContain("Title");
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-038")]
    [Trait("Feature", "TREE-039")]
    public void RenamingAndDeletingABehavior_FromItsMenu_UpdatesTheProjectTreeAndDisk()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNextUserString("Clickable");
        tree.RightClick(tree.RootNode("Behaviors"));
        tree.PickMenu("Add Behavior");
        string behaviors = Path.Combine(tree.Project.ProjectFolder, "Behaviors");

        tree.Dialogs.AnswerNextUserString("Pressable");
        tree.RightClick(tree.RootNode("Behaviors").Nodes.Single());
        tree.PickMenu("Rename");

        tree.Project.Project.Behaviors.Single().Name.ShouldBe("Pressable");
        tree.ChildTexts(tree.RootNode("Behaviors")).ShouldBe(new[] { "Pressable" });
        File.Exists(Path.Combine(behaviors, "Pressable.behx")).ShouldBeTrue();
        File.Exists(Path.Combine(behaviors, "Clickable.behx")).ShouldBeFalse();

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.RightClick(tree.RootNode("Behaviors").Nodes.Single());
        tree.PickMenu("Delete");

        tree.Project.Project.Behaviors.ShouldBeEmpty();
        tree.ChildTexts(tree.RootNode("Behaviors")).ShouldBeEmpty();

        tree.AssertOracles();
    }

    #endregion

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);
}
