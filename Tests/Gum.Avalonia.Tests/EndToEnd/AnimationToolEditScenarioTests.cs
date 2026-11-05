using Avalonia.Headless.XUnit;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.Commands;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Messages;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Edits made in the tool outside the Animations tab (renaming or deleting an instance, selecting
/// elements) and what reaches an animation sidecar: the sidecar follows the edit, undo and redo
/// restore it byte for byte, and merely selecting an element writes nothing. These share one rule:
/// the sidecar is saved only for a real edit, and every real edit saves it.
/// </summary>
[Trait("Category", "EndToEnd")]
public class AnimationToolEditScenarioTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    #region Instances that play an animation

    [AvaloniaFact]
    public void RenamingAnInstance_FollowsIntoTheSubAnimationKeyframe_AndUndoAndRedoRestoreTheFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave button, InstanceSave icon) = ButtonPlayingItsIconsBlink(tree);
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Click(tree.NodeFor(icon));
        tree.Dialogs.AnswerNextUserString("Glyph");
        tree.Press(Key.F2, PhysicalKey.F2);

        File.ReadAllText(ButtonAnimations(tree)).ShouldContain("<Name>Glyph.Blink</Name>");
        File.ReadAllText(ButtonAnimations(tree)).ShouldNotContain("IconInstance.Blink");
        ProjectFileSnapshot renamed = tree.SnapshotFiles();

        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the instance rename should restore the keyframe's reference");

        tree.Redo();
        tree.SnapshotFiles().ShouldMatch(renamed, "redoing the instance rename should rename the reference again");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void DeletingAnInstance_LeavesTheKeyframePlayingItsAnimation_ForTheErrorCheck_AndUndoAndRedoRestoreTheFiles()
    {
        // Like deleting a state or an animation a keyframe uses, the keyframe stays and is flagged
        // broken; only moving the instance to another element drops it.
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave button, InstanceSave icon) = ButtonPlayingItsIconsBlink(tree);
        ProjectFileSnapshot start = tree.SnapshotFiles();
        string animationsBefore = File.ReadAllText(ButtonAnimations(tree));

        tree.Click(tree.NodeFor(icon));
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Press(Key.Delete, PhysicalKey.Delete);

        button.Instances.ShouldBeEmpty();
        File.ReadAllText(ButtonAnimations(tree)).ShouldBe(animationsBefore);
        ProjectFileSnapshot deleted = tree.SnapshotFiles();

        tree.Undo();
        button.Instances.Single().Name.ShouldBe("IconInstance");
        tree.SnapshotFiles().ShouldMatch(start, "undoing the delete should bring the instance back");

        tree.Redo();
        tree.SnapshotFiles().ShouldMatch(deleted, "redoing the delete should remove the instance again");

        // While deleted the keyframe is broken and fails the project check by design; undone it is clean.
        tree.Undo();
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void PromotingAContainerToAComponent_DropsTheKeyframesPlayingTheMovedChildrensAnimations_AndUndoRestoresTheFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave button, InstanceSave icon) = ButtonPlayingItsIconsBlink(tree);
        InstanceSave box = tree.Project.AddInstance(button, "Box", "Container");
        tree.Click(tree.NodeFor(icon));
        tree.Grid.PickComboItem("Parent", "Box");
        ReloadAnimations(tree, button);
        string animationsBefore = File.ReadAllText(ButtonAnimations(tree));
        animationsBefore.ShouldContain("IconInstance.Blink");

        // IconInstance moves into the new component with its parent, so Button no longer has it to play.
        tree.Dialogs.AnswerNext<CreateComponentDialogViewModel>(dialog => { dialog.IsCheckboxChecked = true; return true; });
        tree.RightClick(tree.NodeFor(box));
        tree.PickMenu("Create Component");

        File.ReadAllText(ButtonAnimations(tree)).ShouldNotContain("IconInstance.Blink");

        // The new component stays (adding an element records no undo); Button's own files go back.
        tree.Click(tree.NodeFor(button));
        tree.Undo();
        File.ReadAllText(ButtonAnimations(tree)).ShouldBe(animationsBefore, "undoing the promotion should restore the keyframe");

        tree.ThrowIfCrashed();
    }

    [AvaloniaFact]
    public void RenamingTheComponentAnInstanceIsOf_MovesItsSidecar_KeepsTheReferencingKeyframeValid_AndUndoRestoresTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave button, InstanceSave icon) = ButtonPlayingItsIconsBlink(tree);
        ProjectFileSnapshot start = tree.SnapshotFiles();
        string buttonAnimationsBefore = File.ReadAllText(ButtonAnimations(tree));

        tree.Click(tree.NodeFor(Component(tree, "Icon")));
        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "Glyph"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        icon.BaseType.ShouldBe("Glyph");
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "IconAnimations.ganx")).ShouldBeFalse();
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "GlyphAnimations.ganx")).ShouldBeTrue();
        File.ReadAllText(ButtonAnimations(tree)).ShouldBe(buttonAnimationsBefore);

        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename should move the sidecar back");

        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void RenamingAnInstance_OnAScreen_FollowsIntoTheSubAnimationKeyframe_AndUndoRestoresTheFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave icon = IconWithBlink(tree);
        ScreenSave title = tree.Project.AddScreen("Title");
        InstanceSave glyph = tree.Project.AddInstance(title, "IconInstance", icon.Name);
        string screenAnimations = Path.Combine(tree.Project.ProjectFolder, "Screens", "TitleAnimations.ganx");
        AnimationFixtures.WriteSubAnimation(screenAnimations, "Intro", "IconInstance.Blink");
        ReloadAnimations(tree, title);
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Click(tree.NodeFor(glyph));
        tree.Dialogs.AnswerNextUserString("Glyph");
        tree.Press(Key.F2, PhysicalKey.F2);

        File.ReadAllText(screenAnimations).ShouldContain("<Name>Glyph.Blink</Name>");

        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, "undoing the instance rename should restore the screen's keyframe");

        tree.AssertOracles();
    }

    #endregion

    #region Selecting

    [AvaloniaFact]
    public void SelectingAnElement_WithAnAnimationFile_WritesNothing_TakesNoUndoStep_AndRequestsNoFullErrorCheck()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        (ComponentSave button, InstanceSave icon) = ButtonPlayingItsIconsBlink(tree);
        // A file the tool did not write: selecting an element re-validates its keyframes, which must not
        // rewrite it in the tool's own format.
        string path = ButtonAnimations(tree);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\r\n", "\n").Replace("\n", "\r\n"));
        tree.Click(tree.RootNode("Components"));
        tree.Click(tree.NodeFor(button));
        tree.SaveAll();
        ProjectFileSnapshot start = tree.SnapshotFiles();
        int fullErrorChecks = 0;
        IMessenger messenger = Services.GetRequiredService<IMessenger>();
        object recipient = new object();
        messenger.Register<object, RequestErrorRefreshMessage>(recipient, (_, message) =>
        {
            if (message.RequestingPlugin == null)
            {
                fullErrorChecks++;
            }
        });
        try
        {
            tree.Click(tree.NodeFor(Component(tree, "Icon")));
            tree.Click(tree.NodeFor(button));
            tree.Click(tree.NodeFor(icon));
            tree.Click(tree.NodeFor(button));
        }
        finally
        {
            messenger.UnregisterAll(recipient);
        }

        tree.SnapshotFiles().ShouldMatch(start, "selecting elements should not rewrite an animation file");
        fullErrorChecks.ShouldBe(0, "selecting an element is not an edit, so it should not request a full error check");
        tree.ThrowIfCrashed();
    }

    #endregion

    #region States, categories and elements a keyframe refers to

    [AvaloniaFact]
    public void RenamingAState_FollowsIntoTheKeyframe_AndUndoAndRedoRestoreTheFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = ButtonWithAPressedKeyframe(tree);

        ProjectFileSnapshot edited = EditThenUndoAndRedo(tree, "the state rename", () =>
        {
            tree.Dialogs.AnswerNextUserString("Down");
            Services.GetRequiredService<IEditCommands>().AskToRenameState(PressedState(button), button);
        });

        File.ReadAllText(ButtonAnimations(tree)).ShouldContain("<StateName>Looks/Down</StateName>");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void RenamingACategory_FollowsIntoTheKeyframe_AndUndoAndRedoRestoreTheFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = ButtonWithAPressedKeyframe(tree);

        EditThenUndoAndRedo(tree, "the category rename", () =>
        {
            tree.Dialogs.AnswerNextUserString("Shape");
            Services.GetRequiredService<IEditCommands>().AskToRenameStateCategory(button.Categories.Single(), button);
        });

        File.ReadAllText(ButtonAnimations(tree)).ShouldContain("<StateName>Shape/Pressed</StateName>");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void DeletingAState_LeavesTheKeyframeBroken_AndUndoAndRedoRestoreTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = ButtonWithAPressedKeyframe(tree);

        EditThenUndoAndRedo(tree, "the state delete", () =>
        {
            // The animation plugin asks first (keyframes use it), then the general confirmation.
            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
            Services.GetRequiredService<IEditCommands>().AskToDeleteState(PressedState(button), button);
        });

        File.ReadAllText(ButtonAnimations(tree)).ShouldContain("<StateName>Looks/Pressed</StateName>");
    }

    [AvaloniaFact]
    public void DeletingACategory_LeavesTheKeyframeBroken_AndUndoAndRedoRestoreTheFiles()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave button = ButtonWithAPressedKeyframe(tree);

        EditThenUndoAndRedo(tree, "the category delete", () =>
        {
            // The animation plugin asks first (keyframes use it), then the general confirmation.
            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
            Services.GetRequiredService<IEditCommands>().AskToDeleteStateCategory(button.Categories.Single(), button);
        });

        File.ReadAllText(ButtonAnimations(tree)).ShouldContain("<StateName>Looks/Pressed</StateName>");
    }

    [AvaloniaFact]
    public void RenamingTheElement_MovesTheSidecar_AndUndoAndRedoMoveItBackAndForth()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ButtonWithAPressedKeyframe(tree);

        EditThenUndoAndRedo(tree, "the element rename", () =>
        {
            tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "PrimaryButton"; return true; });
            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
            tree.Press(Key.F2, PhysicalKey.F2);
        });

        File.Exists(ButtonAnimations(tree)).ShouldBeFalse();
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "PrimaryButtonAnimations.ganx")).ShouldBeTrue("after the undo and redo the sidecar is under the renamed element");
    }

    /// <summary>
    /// Runs <paramref name="edit"/> on the selected element, then undoes it and redoes it, each time
    /// comparing every project file byte for byte: undo restores the start, redo the edited state.
    /// </summary>
    private static ProjectFileSnapshot EditThenUndoAndRedo(ProjectTreeHarness tree, string what, Action edit)
    {
        ProjectFileSnapshot start = tree.SnapshotFiles();

        edit();
        ProjectFileSnapshot edited = tree.SnapshotFiles();

        tree.Undo();
        tree.SnapshotFiles().ShouldMatch(start, $"undoing {what} should restore the files");

        tree.Redo();
        tree.SnapshotFiles().ShouldMatch(edited, $"redoing {what} should apply it again");
        return edited;
    }

    /// <summary>A Button with a Looks category (Pressed, Released) and a Press animation keying Looks/Pressed; selected, files saved.</summary>
    private static ComponentSave ButtonWithAPressedKeyframe(ProjectTreeHarness tree)
    {
        ComponentSave button = tree.Project.AddComponent("Button");
        StateSaveCategory looks = tree.Project.AddCategory(button, "Looks");
        tree.Project.AddState(button, looks, "Pressed");
        tree.Project.AddState(button, looks, "Released");
        AnimationFixtures.Write(ButtonAnimations(tree), "Press", "Looks/Pressed");
        ReloadAnimations(tree, button);
        return button;
    }

    private static StateSave PressedState(ComponentSave button) =>
        button.Categories.Single().States.Single(state => state.Name == "Pressed");

    #endregion

    #region Fixtures

    /// <summary>
    /// An Icon component whose Blink animation keys its Dim state, and a Button with an IconInstance of
    /// it whose Walk animation plays IconInstance.Blink. Leaves Button selected with its file loaded.
    /// </summary>
    private static (ComponentSave Button, InstanceSave Icon) ButtonPlayingItsIconsBlink(ProjectTreeHarness tree)
    {
        ComponentSave icon = IconWithBlink(tree);
        ComponentSave button = tree.Project.AddComponent("Button");
        InstanceSave glyph = tree.Project.AddInstance(button, "IconInstance", icon.Name);
        AnimationFixtures.WriteSubAnimation(ButtonAnimations(tree), "Walk", "IconInstance.Blink");
        ReloadAnimations(tree, button);
        return (button, glyph);
    }

    private static ComponentSave IconWithBlink(ProjectTreeHarness tree)
    {
        ComponentSave icon = tree.Project.AddComponent("Icon");
        StateSaveCategory looks = tree.Project.AddCategory(icon, "Looks");
        tree.Project.AddState(icon, looks, "Dim");
        AnimationFixtures.Write(Path.Combine(tree.Project.ProjectFolder, "Components", "IconAnimations.ganx"), "Blink", "Looks/Dim");
        return icon;
    }

    /// <summary>
    /// The Animations tab read the element's (then missing) file when the element was added; selecting
    /// another element and back makes it read the file written since. Saves so the files are the start state.
    /// </summary>
    private static void ReloadAnimations(ProjectTreeHarness tree, ElementSave element)
    {
        tree.Click(tree.RootNode(element is ScreenSave ? "Screens" : "Components"));
        tree.Click(tree.NodeFor(element));
        tree.SaveAll();
    }

    private static string ButtonAnimations(ProjectTreeHarness tree) =>
        Path.Combine(tree.Project.ProjectFolder, "Components", "ButtonAnimations.ganx");

    private static ComponentSave Component(ProjectTreeHarness tree, string name) =>
        tree.Project.Project.Components.Single(component => component.Name == name);

    #endregion
}
