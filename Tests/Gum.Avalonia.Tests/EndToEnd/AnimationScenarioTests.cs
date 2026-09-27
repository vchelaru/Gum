using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FlatRedBall.Glue.StateInterpolation;
using Gum.Avalonia.Tests.Animations;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StateAnimationPlugin.ViewModels;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the Animations tab (inventory area ANIM, and the dialogs it opens): the
/// real tab over a saved project, driven with clicks, right-click menus, keys and scripted
/// dialogs, Ctrl+Z and Ctrl+Y through the app-wide hotkeys. Each checks what the tab shows and what
/// the sidecar holds, that undoing back to the start restores the files byte for byte, and ends
/// with the shared oracles (<see cref="AnimationEditorHarness.AssertOracles"/>).
/// </summary>
[Trait("Category", "EndToEnd")]
public class AnimationScenarioTests
{
    private const string Category = "Looks";

    #region Whole animations

    [AvaloniaFact]
    [Trait("Feature", "ANIM-001")]
    [Trait("Feature", "ANIM-007")]
    [Trait("Feature", "DLG-017")]
    [Trait("Feature", "DLG-018")]
    public void AddingAnAnimation_AndStateKeyframes_SavesThem_AndUndoRemovesThem()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        ProjectFileSnapshot start = editor.StartScenario();

        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk" });
        editor.KeyframeList.Items.Count.ShouldBe(2);
        editor.ViewModel.Animations.Single().Keyframes.Select(keyframe => (keyframe.StateName, keyframe.Time))
            .ShouldBe(new[] { ($"{Category}/Pressed", 0f), ($"{Category}/Released", 1f) });
        AnimationSave saved = editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single();
        saved.Name.ShouldBe("Walk");
        saved.States.Select(keyframe => (keyframe.StateName, keyframe.Time))
            .ShouldBe(new[] { ($"{Category}/Pressed", 0f), ($"{Category}/Released", 1f) });

        editor.Undo();
        editor.Undo();
        editor.Undo();
        editor.ViewModel.Animations.ShouldBeEmpty();
        editor.SnapshotFiles().ShouldMatch(start, "undoing the adds should restore the files");

        editor.Redo();
        editor.Redo();
        editor.Redo();
        editor.ViewModel.Animations.Single().Keyframes.Count.ShouldBe(2);

        editor.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "ANIM-002")]
    [Trait("Feature", "ANIM-008")]
    [Trait("Feature", "DLG-002")]
    [Trait("Feature", "DLG-019")]
    public void RenamingAnAnimation_FromItsMenu_FollowsIntoTheSubAnimationKeyframe_AndUndoRestoresBoth()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel blink = editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        ProjectFileSnapshot start = editor.StartScenario();

        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        walk.Keyframes.Single().AnimationName.ShouldBe("Blink");

        editor.RightClick(editor.RowFor(editor.AnimationList, blink));
        editor.Dialogs.AnswerNextUserString("Wink");
        editor.PickContextMenuItem("Rename Animation");

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Wink", "Walk" });
        walk.Keyframes.Single().AnimationName.ShouldBe("Wink");
        ElementAnimationsSave saved = editor.ReadSavedAnimations(button).ShouldNotBeNull();
        saved.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Wink", "Walk" });
        saved.Animations[1].Animations.Single().Name.ShouldBe("Wink");

        editor.Undo();
        editor.Undo();
        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Blink", "Walk" });
        editor.ViewModel.Animations[1].Keyframes.ShouldBeEmpty();
        editor.SnapshotFiles().ShouldMatch(start, "undoing the keyframe and the rename should restore the files");

        editor.Redo();
        editor.Redo();
        editor.ViewModel.Animations[1].Keyframes.Single().AnimationName.ShouldBe("Wink");

        editor.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "ANIM-003")]
    [Trait("Feature", "DLG-001")]
    public void DeletingAnAnimation_FromItsMenu_OnceConfirmed_RemovesIt_AndUndoBringsItBack()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimationViewModel run = editor.AddAnimation("Run");
        editor.AddStateKeyframe($"{Category}/Released");
        ProjectFileSnapshot start = editor.StartScenario();

        editor.RightClick(editor.RowFor(editor.AnimationList, run));
        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.PickContextMenuItem("Delete Animation");

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk" });
        editor.AnimationList.Items.Count.ShouldBe(1);
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk" });

        editor.Undo();
        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk", "Run" });
        editor.SnapshotFiles().ShouldMatch(start, "undoing the delete should restore the files");

        editor.Redo();
        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk" });

        editor.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "ANIM-004")]
    [Trait("Feature", "ANIM-005")]
    [Trait("Feature", "ANIM-006")]
    public void DuplicateLoopAndSquash_FromTheAnimationMenu_Save_AndUndoRestoresTheFiles()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        ProjectFileSnapshot start = editor.StartScenario();

        editor.RightClick(editor.RowFor(editor.AnimationList, walk));
        editor.PickContextMenuItem("Duplicate Animation");
        editor.RightClick(editor.RowFor(editor.AnimationList, walk));
        editor.PickContextMenuItem("Set to Looping");
        editor.RightClick(editor.RowFor(editor.AnimationList, walk));
        editor.Dialogs.AnswerNextUserString("3");
        editor.PickContextMenuItem("Squash/Stretch Frame Times");

        editor.ViewModel.Animations.Select(animation => (animation.Name, animation.Loops))
            .ShouldBe(new[] { ("Walk", true), ("Copy of Walk", false) });
        walk.Keyframes.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0f, 3f });
        editor.ViewModel.Animations[1].Keyframes.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0f, 1f });
        ElementAnimationsSave saved = editor.ReadSavedAnimations(button).ShouldNotBeNull();
        saved.Animations.Select(animation => (animation.Name, animation.Loops)).ShouldBe(new[] { ("Walk", true), ("Copy of Walk", false) });
        saved.Animations[0].States.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0f, 3f });

        editor.Undo();
        editor.Undo();
        editor.Undo();
        editor.ViewModel.Animations.Select(animation => (animation.Name, animation.Loops)).ShouldBe(new[] { ("Walk", false) });
        editor.SnapshotFiles().ShouldMatch(start, "undoing the duplicate, loop and squash should restore the files");

        editor.Redo();
        editor.Redo();
        editor.Redo();
        editor.ViewModel.Animations.Count.ShouldBe(2);

        editor.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "ANIM-016")]
    public void TheAnimationListKeys_Reorder_CopyPaste_AndDelete_AndUndoRestoresTheFiles()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Released");
        ProjectFileSnapshot start = editor.StartScenario();
        editor.Click(editor.RowFor(editor.AnimationList, walk));

        editor.Press(Key.Up, PhysicalKey.ArrowUp, RawInputModifiers.Alt);
        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk", "Blink" });
        editor.AnimationList.IsKeyboardFocusWithin.ShouldBeTrue("the moved animation's list keeps the keyboard focus");
        editor.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        editor.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);
        AnimationViewModel pasted = editor.ViewModel.Animations.Single(animation => animation.Name is not ("Walk" or "Blink"));
        editor.ViewModel.SelectedAnimation.ShouldBeSameAs(pasted);
        pasted.Keyframes.Single().StateName.ShouldBe($"{Category}/Released");
        string[] expectedAfterDelete = editor.ViewModel.Animations.Select(animation => animation.Name).Where(name => name != "Blink").ToArray();
        editor.Click(editor.RowFor(editor.AnimationList, editor.ViewModel.Animations.Single(animation => animation.Name == "Blink")));
        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.Press(Key.Delete, PhysicalKey.Delete);

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(expectedAfterDelete);
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Select(animation => animation.Name).ShouldBe(expectedAfterDelete);

        editor.Undo();
        editor.Undo();
        editor.Undo();
        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Blink", "Walk" });
        editor.SnapshotFiles().ShouldMatch(start, "undoing the reorder, paste and delete should restore the files");

        editor.Redo();
        editor.Redo();
        editor.Redo();
        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(expectedAfterDelete);
        editor.Click(editor.RowFor(editor.AnimationList, editor.ViewModel.Animations[0]));
        editor.Press(Key.Tab, PhysicalKey.Tab);
        editor.AnimationList.IsKeyboardFocusWithin.ShouldBeFalse("Tab still moves the focus out of the list");

        editor.AssertOracles();
    }

    #endregion

    #region Keyframes

    [AvaloniaFact]
    [Trait("Feature", "ANIM-009")]
    [Trait("Feature", "ANIM-011")]
    [Trait("Feature", "DLG-002")]
    public void ANamedEvent_ThenMovingAKeyframe_AndChangingItsInterpolation_Save_AndUndoRestoresTheFiles()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");
        ProjectFileSnapshot start = editor.StartScenario();

        editor.Dialogs.AnswerNextUserString("Footstep");
        editor.PickAddKeyframe("Named Event");
        AnimatedKeyframeViewModel footstep = walk.SelectedKeyframe.ShouldNotBeNull();
        footstep.EventName.ShouldBe("Footstep");
        editor.Click(editor.RowFor(editor.KeyframeList, released));
        editor.TypeAndEnter(editor.DetailTimeBox, "0.5");
        editor.Click(editor.DetailCombos[1]);
        editor.DetailCombos[1].SelectedItem = InterpolationType.Bounce;
        editor.Layout();
        editor.DetailCombos[2].SelectedItem = Easing.InOut;
        editor.Layout();

        released.Time.ShouldBe(0.5f);
        released.InterpolationType.ShouldBe(InterpolationType.Bounce);
        released.Easing.ShouldBe(Easing.InOut);
        editor.Timeline.Rows.Single(row => row.Name == "Default").Items.ShouldContain(footstep, "events are drawn on the Default row");
        AnimationSave saved = editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single();
        saved.Events.Single().Name.ShouldBe("Footstep");
        AnimatedStateSave savedReleased = saved.States.Single(keyframe => keyframe.StateName == $"{Category}/Released");
        (savedReleased.Time, savedReleased.InterpolationType, savedReleased.Easing).ShouldBe((0.5f, InterpolationType.Bounce, Easing.InOut));

        editor.Undo();
        editor.Undo();
        editor.Undo();
        editor.Undo();
        walk = editor.ViewModel.Animations.Single();
        walk.Keyframes.Count.ShouldBe(2);
        editor.SnapshotFiles().ShouldMatch(start, "undoing the event, move, interpolation and easing should restore the files");

        editor.Redo();
        editor.Redo();
        editor.Redo();
        editor.Redo();
        editor.ViewModel.Animations.Single().Keyframes.Count.ShouldBe(3);

        editor.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "ANIM-010")]
    [Trait("Feature", "ANIM-016")]
    public void CopyPasteAndDelete_InTheKeyframeList_Save_AndUndoRestoresTheFiles()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel pressed = editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        ProjectFileSnapshot start = editor.StartScenario();
        editor.Click(editor.RowFor(editor.KeyframeList, pressed));

        editor.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        editor.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);
        walk.Keyframes.Select(keyframe => (keyframe.StateName, keyframe.Time))
            .ShouldBe(new[] { ($"{Category}/Pressed", 0f), ($"{Category}/Pressed", 0.1f), ($"{Category}/Released", 1f) });
        walk.SelectedKeyframe.ShouldBeSameAs(walk.Keyframes[1]);
        editor.DetailCombos[0].SelectedItem.ShouldBe($"{Category}/Pressed", "the pasted keyframe's state box shows its state");
        editor.DetailTimeBox.Text.ShouldBe("0.1");
        editor.RightClick(editor.RowFor(editor.KeyframeList, pressed));
        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.PickContextMenuItem("Delete Keyframe");

        walk.Keyframes.Select(keyframe => (keyframe.StateName, keyframe.Time))
            .ShouldBe(new[] { ($"{Category}/Pressed", 0.1f), ($"{Category}/Released", 1f) });
        editor.KeyframeList.Items.Count.ShouldBe(2);
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0.1f, 1f });

        editor.Undo();
        editor.Undo();
        editor.ViewModel.Animations.Single().Keyframes.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0f, 1f });
        editor.SnapshotFiles().ShouldMatch(start, "undoing the paste and delete should restore the files");

        editor.Redo();
        editor.Redo();
        editor.ViewModel.Animations.Single().Keyframes.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0.1f, 1f });

        editor.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "ANIM-017")]
    [Trait("Feature", "ANIM-008")]
    [Trait("Feature", "DLG-019")]
    public void AScreenPlayingAnInstancesAnimation_SavesTheReference_AndUndoRestoresTheFiles()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave iconType = editor.AddComponent("Icon", Category, "Dim", "Bright");
        editor.Select(iconType);
        editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Dim");
        editor.AddStateKeyframe($"{Category}/Bright");
        ScreenSave menu = editor.AddScreen("MainMenu", Category, "Shown", "Hidden");
        InstanceSave icon = editor.AddInstance(menu, "IconInstance", iconType);
        editor.Select(menu);
        AnimationViewModel intro = editor.AddAnimation("Intro");
        editor.AddStateKeyframe($"{Category}/Shown");
        ProjectFileSnapshot start = editor.StartScenario();

        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single(container => container.InstanceSave == icon);
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");

        AnimatedKeyframeViewModel sub = intro.SelectedKeyframe.ShouldNotBeNull();
        sub.AnimationName.ShouldBe("IconInstance.Blink");
        sub.IsMissingReference.ShouldBeFalse();
        intro.Length.ShouldBe(2f);
        editor.Timeline.Rows.Select(row => row.Name).ShouldContain("IconInstance.Blink");
        AnimationReferenceSave saved = editor.ReadSavedAnimations(menu).ShouldNotBeNull().Animations.Single().Animations.Single();
        (saved.SourceObject, saved.RootName).ShouldBe(("IconInstance", "Blink"));

        editor.Undo();
        editor.ViewModel.Animations.Single().Keyframes.Count.ShouldBe(1);
        editor.SnapshotFiles().ShouldMatch(start, "undoing the sub-animation keyframe should restore the files");

        editor.Redo();
        editor.ViewModel.Animations.Single().Keyframes.Single(keyframe => keyframe.AnimationName == "IconInstance.Blink").IsMissingReference.ShouldBeFalse();

        editor.AssertOracles();
    }

    #endregion

    #region Timeline and playback

    [AvaloniaFact]
    [Trait("Feature", "ANIM-012")]
    [Trait("Feature", "ANIM-013")]
    public void ScrubbingAndPlaying_ShowTheInterpolatedState_AndChangeNoFile()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        ProjectFileSnapshot start = editor.StartScenario();
        int undoActions = editor.UndoManager.CurrentElementHistory.ShouldNotBeNull().Actions.Count;

        editor.TypeAndEnter(editor.TimelineTimeBox, "0.25");
        editor.ViewModel.DisplayedAnimationTime.ShouldBe(0.25, tolerance: 0.0001);
        editor.Scrubber.Value.ShouldBe(0.25, tolerance: 0.0001);
        ((float)editor.SelectedState.CustomCurrentStateSave.ShouldNotBeNull().GetValue("X")!).ShouldBe(25f, tolerance: 0.5f);

        editor.Click(editor.PlayButton);
        editor.ViewModel.IsPlaying.ShouldBeTrue();
        editor.PlayButton.Content.ShouldBe("■ Stop");
        editor.Wait(TimeSpan.FromMilliseconds(150));
        editor.Click(editor.PlayButton);

        editor.ViewModel.IsPlaying.ShouldBeFalse();
        editor.PlayButton.Content.ShouldBe("▶ Play");
        editor.SnapshotFiles().ShouldMatch(start, "scrubbing and playing are not edits");
        editor.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(undoActions, "scrubbing and playing record no undo");

        editor.AssertOracles();
    }

    #endregion

    #region Missing references

    [AvaloniaFact]
    [Trait("Feature", "ANIM-014")]
    [Trait("Feature", "COMBO-012")]
    public void DeletingAStateAKeyframeUses_FlagsTheKeyframe_AndTheErrorCheck_UntilUndoRepairsIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel keyframe = editor.AddStateKeyframe($"{Category}/Released");
        ProjectFileSnapshot start = editor.StartScenario();
        StateSave released = button.Categories[0].States.Single(state => state.Name == "Released");

        // The tree's Delete, once confirmed: the state is removed under an undo lock.
        using (editor.UndoManager.RequestLock())
        {
            TestAppBuilder.Services.GetRequiredService<IDeleteLogic>().Remove(released);
        }
        editor.Layout();

        keyframe.IsMissingReference.ShouldBeTrue();
        walk.HasBrokenKeyframe.ShouldBeTrue();
        editor.RowFor(editor.KeyframeList, keyframe).GetVisualDescendants().OfType<TextBlock>().ShouldContain(text => text.Text == "⚠");
        editor.RowFor(editor.AnimationList, walk).GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "⚠").IsVisible.ShouldBeTrue();
        Should.Throw<ShouldAssertException>(() => ProjectOracles.AssertCheckClean(editor.ProjectFilePath))
            .Message.ShouldContain("Walk");

        editor.Undo();
        editor.ViewModel.Animations.Single().Keyframes.Single().IsMissingReference.ShouldBeFalse();
        editor.ViewModel.Animations.Single().HasBrokenKeyframe.ShouldBeFalse();
        editor.SnapshotFiles().ShouldMatch(start, "undoing the state delete should restore the files");

        editor.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "ANIM-015")]
    public void AKeyframeOnAnUncategorizedState_ShowsTheWarning_AndStillPassesTheCheck()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed");
        button.States.Add(new StateSave { Name = "Hidden", ParentContainer = button });
        editor.Select(button);
        editor.AddAnimation("Walk");
        ProjectFileSnapshot start = editor.StartScenario();

        AnimatedKeyframeViewModel hidden = editor.AddStateKeyframe("Hidden");

        hidden.IsUncategorized.ShouldBeTrue();
        editor.RowFor(editor.KeyframeList, hidden).GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "!").IsVisible.ShouldBeTrue();
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Single().StateName.ShouldBe("Hidden");

        editor.Undo();
        editor.ViewModel.Animations.Single().Keyframes.ShouldBeEmpty();
        editor.SnapshotFiles().ShouldMatch(start, "undoing the keyframe should restore the files");

        editor.Redo();
        editor.AssertOracles();
    }

    #endregion

    #region Changes from outside the tab

    [AvaloniaFact]
    [Trait("Feature", "ANIM-018")]
    public void AnEditToTheSidecarOnDisk_ReloadsTheTab_WhichThenSavesIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.StartScenario();
        string path = editor.AnimationFilePath(button);
        ElementAnimationsSave edited = ElementAnimationsSave.Load(path);
        edited.Animations.Add(new AnimationSave { Name = "Run" });
        edited.Save(path);

        editor.Plugin.CallReactToFileChanged(new FilePath(path));
        editor.Layout();

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk", "Run" });
        editor.AnimationList.Items.Count.ShouldBe(2);

        editor.AssertOracles();
    }

    #endregion
}
