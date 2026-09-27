using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
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
}
