using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios for the COMBO items where copy, paste, rename and delete meet references
/// between elements: variable references, parents, states, instances of components and animations.
/// Each drives the Project tree (and the Variables tab where a value is set), checks the model and
/// the saved files, undoes where the tool records undo, and ends with the shared oracles.
/// </summary>
[Trait("Category", "EndToEnd")]
public class CopyPasteRenameScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    #region Copy and paste

    [AvaloniaFact]
    [Trait("Feature", "COMBO-001")]
    [Trait("Feature", "TREE-044")]
    public void CopyingAnInstanceWithACrossElementReference_IntoAnotherElement_KeepsTheReferenceLive()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Style");
        tree.Project.AddComponent("Panel");
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(Component(tree, "Style")));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("Width", "90");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single()));
        grid.TypeLinesAndApply("VariableReferences", "Width = Components/Style.Width");

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        ComponentSave panel = Component(tree, "Panel");
        panel.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        ReferenceLines(panel, "Label").ShouldBe(new[] { "Width = Components/Style.Width" });
        VariableGridHarness.StoredValue(panel, "Label.Width").ShouldBe(90f);

        // The pasted reference follows its source like the original does.
        tree.Click(tree.NodeFor(Component(tree, "Style")));
        grid.TypeAndEnter("Width", "140");
        VariableGridHarness.StoredValue(Component(tree, "Panel"), "Label.Width").ShouldBe(140f);
        VariableGridHarness.StoredValue(grid.ReadSaved(Component(tree, "Panel")), "Label.Width").ShouldBe(140f);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-001")]
    [Trait("Feature", "TREE-044")]
    public void CopyingAnInstanceAndTheSiblingItReferences_IntoAnElementWithThatName_RenamesTheReference()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave panel = tree.Project.AddComponent("Panel");
        tree.Project.AddInstance(panel, "Background", "NineSlice");
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave background = tree.Project.AddInstance(button, "Background", "NineSlice");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(background));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("Width", "75");
        tree.Click(tree.NodeFor(label));
        grid.TypeLinesAndApply("VariableReferences", "Width = Background.Width");

        tree.Click(tree.NodeFor(background));
        tree.Click(tree.NodeFor(label), RawInputModifiers.Control);
        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        panel = Component(tree, "Panel");
        panel.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Background", "Background1", "Label" });
        // The copied reference points at the copied sibling, not at the Panel's own Background.
        ReferenceLines(panel, "Label").ShouldBe(new[] { "Width = Background1.Width" });
        VariableGridHarness.StoredValue(panel, "Label.Width").ShouldBe(75f);

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-002")]
    [Trait("Feature", "TREE-044")]
    public void PastingAChildWithoutItsParent_IntoAnotherElement_AttachesItToTheElement()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Panel");
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Box", "Container");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        grid.PickComboItem("Parent", "Box");
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Parent").ShouldBe("Box");
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.Single(instance => instance.Name == "Label")));
        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        ComponentSave panel = Component(tree, "Panel");
        panel.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        VariableGridHarness.StoredValue(panel, "Label.Parent").ShouldBeNull();
        tree.ChildTexts(tree.NodeFor(panel)).ShouldBe(new[] { "Label" });

        tree.Undo();
        Component(tree, "Panel").Instances.ShouldBeEmpty();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the paste should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-003")]
    [Trait("Feature", "TREE-044")]
    public void PastingAnInstanceCopiedInACategoryState_IntoAnElementWithoutThatState_TakesOnlyTheDefaultValues()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Panel");
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        StateSave pressed = tree.Project.AddState(button, looks, "Pressed");
        tree.Click(tree.NodeFor(label));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("Width", "50");
        grid.Select(pressed);
        grid.TypeAndEnter("Height", "210");

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        ComponentSave panel = Component(tree, "Panel");
        panel.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        VariableGridHarness.StoredValue(panel, "Label.Width").ShouldBe(50f);
        // Pressed's value belongs to a state Panel does not have, so it must not become Panel's default.
        VariableGridHarness.StoredValue(panel, "Label.Height").ShouldBeNull();

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-044")]
    public void PastingAComponentInstance_WhereThatTypeIsAlreadyUsed_AddsAUniquelyNamedCopy()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave okButton = tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(okButton));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        Screen(tree, "Title").Instances.Select(instance => instance.Name).ShouldBe(new[] { "OkButton", "OkButton1" });
        Screen(tree, "Title").Instances.ShouldAllBe(instance => instance.BaseType == "Button");
        tree.ChildTexts(tree.NodeFor(Screen(tree, "Title"))).ShouldBe(new[] { "OkButton", "OkButton1" });

        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the paste should restore the files");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-044")]
    public void PastingAComponentInstance_IntoThatComponent_IsRefused()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddComponent("Button");
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave okButton = tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(okButton));
        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        // A Button inside Button would contain itself forever.
        Component(tree, "Button").Instances.ShouldBeEmpty();
        tree.Dialogs.Messages.ShouldHaveSingleItem().ShouldContain("Button");
        tree.SnapshotFiles().ShouldMatch(start, "a refused paste should change no file");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-044")]
    public void CopyingAComponent_AndPastingOnAFolder_AddsACopyInThatFolderWithItsInstancesAndStates()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Label", "Text");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        tree.Project.AddState(button, looks, "Pressed");
        tree.Dialogs.AnswerNext<AddFolderDialogViewModel>(dialog => { dialog.Value = "Controls"; return true; });
        tree.RightClick(tree.RootNode("Components"));
        tree.PickMenu("Add Folder");
        tree.Click(tree.NodeFor(Component(tree, "Button")));

        tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        tree.Click(tree.FolderNode("Components", "Controls"));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        ComponentSave copy = Component(tree, "Controls/Button");
        copy.ShouldNotBeSameAs(Component(tree, "Button"));
        copy.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        copy.Categories.Single().States.Select(state => state.Name).ShouldBe(new[] { "Pressed" });
        tree.ChildTexts(tree.FolderNode("Components", "Controls")).ShouldBe(new[] { "Button" });
        tree.SelectedState.SelectedElement.ShouldBeSameAs(copy);
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Controls", "Button.gucx")).ShouldBeTrue();

        tree.AssertOracles();
    }

    [AvaloniaFact(Skip = "#5249: cutting a component and pasting it on a folder copies it instead of moving it")]
    [Trait("Feature", "TREE-045")]
    public void CuttingAComponent_AndPastingOnAnotherFolder_MovesIt()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Dialogs.AnswerNext<AddFolderDialogViewModel>(dialog => { dialog.Value = "Menus"; return true; });
        tree.RightClick(tree.RootNode("Components"));
        tree.PickMenu("Add Folder");
        ComponentSave toggle = tree.Project.AddComponent("Controls/Toggle");
        tree.Project.AddInstance(toggle, "Label", "Text");
        tree.Click(tree.NodeFor(Component(tree, "Controls/Toggle")));

        tree.Press(Key.X, PhysicalKey.X, RawInputModifiers.Control);
        tree.Click(tree.FolderNode("Components", "Menus"));
        tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        tree.Project.Project.Components.Select(component => component.Name).ShouldBe(new[] { "Menus/Toggle" });
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Controls", "Toggle.gucx")).ShouldBeFalse();
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "Menus", "Toggle.gucx")).ShouldBeTrue();

        tree.AssertOracles();
    }

    #endregion

    #region Renaming what others use

    [AvaloniaFact]
    [Trait("Feature", "COMBO-007")]
    [Trait("Feature", "TREE-043")]
    public void RenamingAnInstanceThatReferencesPointAt_UpdatesTheReferencesInBothElements()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        VariableGridHarness grid = ReferenceButtonBackgroundFromLabelAndPanel(tree);

        tree.Dialogs.AnswerNextUserString("Bg");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        tree.Dialogs.Messages.ShouldHaveSingleItem().ShouldContain("Components/Button.Background.Width in Panel");
        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Bg", "Label" });
        ReferenceLines(Component(tree, "Button"), "Label").ShouldBe(new[] { "Width = Bg.Width" });
        ReferenceLines(Component(tree, "Panel")).ShouldBe(new[] { "Height = Components/Button.Bg.Width" });
        ReferenceLines(grid.ReadSaved(Component(tree, "Button")), "Label").ShouldBe(new[] { "Width = Bg.Width" });
        ReferenceLines(grid.ReadSaved(Component(tree, "Panel"))).ShouldBe(new[] { "Height = Components/Button.Bg.Width" });

        tree.AssertOracles();
    }

    [AvaloniaFact(Skip = "#5248: undoing an instance rename leaves other elements' references on the new name")]
    [Trait("Feature", "COMBO-007")]
    [Trait("Feature", "EDIT-001")]
    public void UndoingAnInstanceRename_RestoresReferencesInOtherElements()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ReferenceButtonBackgroundFromLabelAndPanel(tree);
        ProjectFileSnapshot start = tree.SnapshotFiles();
        tree.Dialogs.AnswerNextUserString("Bg");
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        tree.Undo();

        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Background", "Label" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename should restore every reference to the old name");

        tree.AssertOracles();
    }

    /// <summary>
    /// Button with Background (Width 75) and Label, whose Width references Background.Width; Panel's
    /// Height references Components/Button.Background.Width. Leaves Background selected.
    /// </summary>
    private static VariableGridHarness ReferenceButtonBackgroundFromLabelAndPanel(ProjectTreeHarness tree)
    {
        tree.Project.AddComponent("Panel");
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave background = tree.Project.AddInstance(button, "Background", "NineSlice");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(background));
        VariableGridHarness grid = tree.Grid;
        grid.TypeAndEnter("Width", "75");
        tree.Click(tree.NodeFor(label));
        grid.TypeLinesAndApply("VariableReferences", "Width = Background.Width");
        tree.Click(tree.NodeFor(Component(tree, "Panel")));
        grid.TypeLinesAndApply("VariableReferences", "Height = Components/Button.Background.Width");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.First()));
        return grid;
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-008")]
    [Trait("Feature", "EDIT-001")]
    public void RenamingAStateUsedByAnInstanceAndAnAnimation_UpdatesBoth_AndUndoRestoresThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        tree.Project.AddState(button, looks, "Pressed");
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave okButton = tree.Project.AddInstance(title, "OkButton", "Button");
        string animationFile = Path.Combine(tree.Project.ProjectFolder, "Components", "ButtonAnimations.ganx");
        File.WriteAllText(animationFile, AnimationFileWithKeyframe("Looks/Pressed"));
        tree.Click(tree.NodeFor(okButton));
        VariableGridHarness grid = tree.Grid;
        grid.PickComboItem("LooksState", "Pressed");
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        grid.Select(PressedState(tree));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextUserString("Down");
        Services.GetRequiredService<IEditCommands>().AskToRenameState(PressedState(tree), Component(tree, "Button"));

        Component(tree, "Button").Categories.Single().States.Single().Name.ShouldBe("Down");
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LooksState").ShouldBe("Down");
        VariableGridHarness.StoredValue(grid.ReadSaved(Screen(tree, "Title")), "OkButton.LooksState").ShouldBe("Down");
        File.ReadAllText(animationFile).ShouldContain("<StateName>Looks/Down</StateName>");

        tree.Undo();
        Component(tree, "Button").Categories.Single().States.Single().Name.ShouldBe("Pressed");
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.LooksState").ShouldBe("Pressed");
        tree.SnapshotFiles().ShouldMatch(start, "undoing the state rename should restore the instance value and the keyframe");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-009")]
    [Trait("Feature", "EDIT-001")]
    public void RenamingACategoryWhoseStateVariableIsExposed_KeepsTheExposedVariableWorking()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave icon = tree.Project.AddComponent("Icon");
        StateSaveCategory kind = tree.Project.AddCategory(icon, "Kind");
        tree.Project.AddState(icon, kind, "Round");
        tree.Project.AddState(icon, kind, "Square");
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave glyph = tree.Project.AddInstance(button, "Glyph", "Icon");
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave okButton = tree.Project.AddInstance(title, "OkButton", "Button");
        tree.Click(tree.NodeFor(glyph));
        VariableGridHarness grid = tree.Grid;
        tree.Dialogs.AnswerNextUserString("GlyphKind");
        grid.PickRowMenuItem("KindState", "Expose Variable");
        tree.Click(tree.NodeFor(okButton));
        grid.PickComboItem("GlyphKind", "Square");
        tree.Click(tree.NodeFor(Component(tree, "Icon")));
        grid.Select(Component(tree, "Icon").Categories.Single().States.First());

        tree.Dialogs.AnswerNextUserString("Shape");
        Services.GetRequiredService<IEditCommands>().AskToRenameStateCategory(Component(tree, "Icon").Categories.Single(), Component(tree, "Icon"));

        Component(tree, "Icon").Categories.Single().Name.ShouldBe("Shape");
        VariableSave exposed = Component(tree, "Button").GetDefaultStateOrThrow().Variables.Single(variable => variable.ExposedAsName == "GlyphKind");
        exposed.Name.ShouldBe("Glyph.ShapeState");
        VariableGridHarness.StoredValue(Screen(tree, "Title"), "OkButton.GlyphKind").ShouldBe("Square");
        tree.Click(tree.NodeFor(Screen(tree, "Title").Instances.Single()));
        grid.Combo("GlyphKind").SelectedItem?.ToString().ShouldBe("Square");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "COMBO-006")]
    [Trait("Feature", "TREE-043")]
    [Trait("Feature", "EDIT-001")]
    public void RenamingAComponentWithAnimations_MovesItsAnimationFile_AndUndoMovesItBack()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        tree.Project.AddState(button, looks, "Pressed");
        string components = Path.Combine(tree.Project.ProjectFolder, "Components");
        File.WriteAllText(Path.Combine(components, "ButtonAnimations.ganx"), AnimationFileWithKeyframe("Looks/Pressed"));
        // The Animations tab read Button's (then missing) file when Button was added; selecting
        // another element and back makes it read the file written above.
        tree.Click(tree.RootNode("Components"));
        tree.Click(tree.NodeFor(Component(tree, "Button")));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "PrimaryButton"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        File.Exists(Path.Combine(components, "ButtonAnimations.ganx")).ShouldBeFalse();
        File.ReadAllText(Path.Combine(components, "PrimaryButtonAnimations.ganx")).ShouldContain("<StateName>Looks/Pressed</StateName>");

        tree.Undo();
        Component(tree, "Button").ShouldNotBeNull();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename should move the animation file back");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "TREE-027")]
    public void DuplicatingAComponentWithStatesAndAnimations_CopiesBoth()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        tree.Project.AddState(button, looks, "Pressed");
        string components = Path.Combine(tree.Project.ProjectFolder, "Components");
        File.WriteAllText(Path.Combine(components, "ButtonAnimations.ganx"), AnimationFileWithKeyframe("Looks/Pressed"));
        // The Animations tab read Button's (then missing) file when Button was added; selecting
        // another element and back makes it read the file written above.
        tree.Click(tree.RootNode("Components"));
        tree.Click(tree.NodeFor(Component(tree, "Button")));

        tree.Dialogs.AnswerNextUserString("ButtonCopy");
        tree.RightClick(tree.NodeFor(Component(tree, "Button")));
        tree.PickMenu("Duplicate Button");

        ComponentSave copy = Component(tree, "ButtonCopy");
        copy.Categories.Single().States.Select(state => state.Name).ShouldBe(new[] { "Pressed" });
        File.ReadAllText(Path.Combine(components, "ButtonCopyAnimations.ganx")).ShouldContain("<StateName>Looks/Pressed</StateName>");
        File.Exists(Path.Combine(components, "ButtonAnimations.ganx")).ShouldBeTrue();

        tree.AssertOracles();
    }

    #endregion

    #region Deleting a parent

    [AvaloniaFact]
    [Trait("Feature", "COMBO-013")]
    [Trait("Feature", "TREE-042")]
    [Trait("Feature", "EDIT-001")]
    public void DeletingAParentInstance_KeepsOrDeletesItsChildrenAsChosen_AndUndoRestoresThem()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = tree.Project.AddComponent("Button");
        tree.Project.AddInstance(button, "Box", "Container");
        InstanceSave label = tree.Project.AddInstance(button, "Label", "Text");
        tree.Click(tree.NodeFor(label));
        tree.Grid.PickComboItem("Parent", "Box");
        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.First()));
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(dialog => ChooseDeleteChildren(dialog, deleteChildren: false));
        tree.Press(Key.Delete, PhysicalKey.Delete);

        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Label" });
        VariableGridHarness.StoredValue(Component(tree, "Button"), "Label.Parent").ShouldBeNull();
        tree.ChildTexts(tree.NodeFor(Component(tree, "Button"))).ShouldBe(new[] { "Label" });

        tree.Undo();
        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Box", "Label" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing the parent-only delete should restore the files");

        tree.Click(tree.NodeFor(Component(tree, "Button").Instances.First()));
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(dialog => ChooseDeleteChildren(dialog, deleteChildren: true));
        tree.Press(Key.Delete, PhysicalKey.Delete);

        Component(tree, "Button").Instances.ShouldBeEmpty();
        tree.ChildTexts(tree.NodeFor(Component(tree, "Button"))).ShouldBeEmpty();

        tree.Undo();
        Component(tree, "Button").Instances.Select(instance => instance.Name).ShouldBe(new[] { "Box", "Label" });
        tree.SnapshotFiles().ShouldMatch(start, "undoing the delete with children should restore the files");

        tree.AssertOracles();
    }

    private static bool ChooseDeleteChildren(DeleteOptionsDialogViewModel dialog, bool deleteChildren)
    {
        DeleteOptionChoiceViewModel choice = dialog.Choices.ShouldHaveSingleItem();
        choice.Options[0].IsChecked = !deleteChildren;
        choice.Options[1].IsChecked = deleteChildren;
        return true;
    }

    #endregion

    private static string AnimationFileWithKeyframe(string stateName) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
        "<ElementAnimationsSave xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n" +
        "  <Animations>\r\n" +
        "    <AnimationSave>\r\n" +
        "      <Loops>false</Loops>\r\n" +
        "      <Name>Press</Name>\r\n" +
        "      <States>\r\n" +
        "        <AnimatedStateSave>\r\n" +
        $"          <StateName>{stateName}</StateName>\r\n" +
        "          <Time>0</Time>\r\n" +
        "          <InterpolationType>Linear</InterpolationType>\r\n" +
        "          <Easing>Out</Easing>\r\n" +
        "        </AnimatedStateSave>\r\n" +
        "      </States>\r\n" +
        "      <Animations />\r\n" +
        "      <Events />\r\n" +
        "    </AnimationSave>\r\n" +
        "  </Animations>\r\n" +
        "</ElementAnimationsSave>";

    private static List<string> ReferenceLines(ElementSave element, string? instanceName = null)
    {
        string listName = instanceName == null ? "VariableReferences" : instanceName + ".VariableReferences";
        // The Variables tab stores an instance's lines without the spaces around "=".
        return element.GetDefaultStateOrThrow().GetVariableListSave(listName)?.ValueAsIList?.Cast<string>()
            .Select(line => string.Join(" = ", line.Split('=').Select(side => side.Trim())))
            .ToList() ?? new List<string>();
    }

    private static ScreenSave Screen(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Screens.Single(screen => screen.Name == name);

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    private static StateSave PressedState(ProjectTreeHarness tree) =>
        Component(tree, "Button").Categories.Single().States.Single(state => state.Name == "Pressed");
}
