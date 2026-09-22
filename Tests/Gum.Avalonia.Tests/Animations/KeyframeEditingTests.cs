using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// Editing keyframes at the edges: bad and odd times typed into the detail box, the clipboard across
/// animations and elements, and undo of keyframe edits.
/// </summary>
public class KeyframeEditingTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void TypingTextThatIsNotATime_LeavesTheKeyframeAndTheSidecarAlone()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");
        DateTime savedAt = File.GetLastWriteTimeUtc(editor.AnimationFilePath(button));

        editor.TypeAndEnter(editor.DetailTimeBox, "abc");

        released.Time.ShouldBe(1f);
        editor.ThrowIfPluginFailed();
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Select(state => state.Time).ShouldBe(new[] { 0f, 1f });
        File.GetLastWriteTimeUtc(editor.AnimationFilePath(button)).ShouldBe(savedAt);
    }

    [AvaloniaFact]
    public void TypingANegativeTime_DoesNotBreakTheTab()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");

        editor.TypeAndEnter(editor.DetailTimeBox, "-1");

        editor.ThrowIfPluginFailed();
        released.Time.ShouldBe(-1f);
        editor.Timeline.Rows[0].Items.ShouldBe(new[] { released });
        // The marker stays at the start of the track rather than off to the left of it.
        editor.KeyframeMarkerCenter(released).X.ShouldBeGreaterThan(0);
        walk.SelectedKeyframe = null;
        editor.ClickAt(editor.KeyframeMarkerCenter(released));
        walk.SelectedKeyframe.ShouldBeSameAs(released);
        editor.SaveFrame("negative-time");
    }

    [AvaloniaFact]
    public void MovingASubAnimationKeyframeLater_ExtendsTheAnimation_ByItsLength()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        AnimatedKeyframeViewModel blink = walk.SelectedKeyframe.ShouldNotBeNull();
        walk.Length.ShouldBe(2f);

        editor.TypeAndEnter(editor.DetailTimeBox, "3");

        blink.Time.ShouldBe(3f);
        walk.Length.ShouldBe(4f);
        editor.Scrubber.Maximum.ShouldBe(4);
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single(animation => animation.Name == "Walk").Animations.Single().Time.ShouldBe(3f);
    }

    [AvaloniaFact]
    public void SquashStretch_ScalesASubAnimationKeyframesTime_ButNotItsLength()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        AnimatedKeyframeViewModel blink = walk.SelectedKeyframe.ShouldNotBeNull();

        editor.Dialogs.AnswerNextUserString("4");
        editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Squash/Stretch Frame Times").Action!();
        editor.Layout();

        blink.Time.ShouldBe(2f);
        blink.Length.ShouldBe(1f);
        walk.Length.ShouldBe(3f);
    }

    [AvaloniaFact]
    public void SquashStretch_RejectsAnInvalidLength()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");

        editor.Dialogs.AnswerNextUserString("0");
        Should.Throw<InvalidOperationException>(() => editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Squash/Stretch Frame Times").Action!())
            .Message.ShouldContain("greater than 0");
        walk.Keyframes.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0f, 1f });
    }

    [AvaloniaFact]
    public void ACopiedKeyframe_PastesIntoAnotherAnimation_OfTheSameElement()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");
        editor.TypeAndEnter(editor.DetailTimeBox, "2");
        editor.Click(editor.RowFor(editor.KeyframeList, released));
        editor.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        AnimationViewModel run = editor.AddAnimation("Run");
        AnimatedKeyframeViewModel pressed = editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Click(editor.RowFor(editor.KeyframeList, pressed));

        editor.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        run.Keyframes.Select(keyframe => (keyframe.StateName, keyframe.Time)).ShouldBe(new[] { ($"{Category}/Pressed", 0f), ($"{Category}/Released", 2.1f) });
        walk.Keyframes.Count.ShouldBe(1);
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single(animation => animation.Name == "Run").States.Count.ShouldBe(2);
    }

    [AvaloniaFact]
    public void ACopiedKeyframe_PastedIntoAnotherElement_IsFlaggedWhenThatElementLacksTheState()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        ComponentSave label = editor.AddComponent("Label", "Looks", "Big");
        editor.Select(button);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");
        editor.Click(editor.RowFor(editor.KeyframeList, released));
        editor.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        editor.Select(label);
        AnimationViewModel grow = editor.AddAnimation("Grow");
        AnimatedKeyframeViewModel big = editor.AddStateKeyframe("Looks/Big");
        editor.Click(editor.RowFor(editor.KeyframeList, big));

        editor.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        editor.ThrowIfPluginFailed();
        grow.Keyframes.Count.ShouldBe(2);
        AnimatedKeyframeViewModel pasted = grow.Keyframes.Single(keyframe => keyframe.StateName == $"{Category}/Released");
        pasted.IsMissingReference.ShouldBeTrue("Label has no AnimationCategory/Released state");
        grow.HasBrokenKeyframe.ShouldBeTrue();
        editor.ReadSavedAnimations(label).ShouldNotBeNull().Animations.Single().States.Count.ShouldBe(2);
    }

    [AvaloniaFact]
    public void CopyingANamedEventOrSubAnimationKeyframe_PastesACopy()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Dialogs.AnswerNextUserString("Footstep");
        editor.PickAddKeyframe("Named Event");
        AnimatedKeyframeViewModel footstep = walk.SelectedKeyframe.ShouldNotBeNull();
        editor.Click(editor.RowFor(editor.KeyframeList, footstep));

        editor.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        editor.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        editor.ThrowIfPluginFailed();
        walk.Keyframes.Count(keyframe => keyframe.EventName == "Footstep").ShouldBe(2);
        walk.Keyframes.Single(keyframe => keyframe.EventName == "Footstep" && keyframe != footstep).Time.ShouldBe(1.1f);

        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        AnimatedKeyframeViewModel blink = walk.SelectedKeyframe.ShouldNotBeNull();
        editor.Click(editor.RowFor(editor.KeyframeList, blink));

        editor.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        editor.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        editor.ThrowIfPluginFailed();
        walk.Keyframes.Count(keyframe => keyframe.AnimationName == "Blink").ShouldBe(2);
        walk.Keyframes.Where(keyframe => keyframe.AnimationName == "Blink").ShouldAllBe(keyframe => keyframe.Length == 1f);
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single(animation => animation.Name == "Walk").Animations.Count.ShouldBe(2);
    }

    [AvaloniaFact]
    public void PasteWithNothingCopied_DoesNothing()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel pressed = editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Click(editor.RowFor(editor.KeyframeList, pressed));

        editor.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        walk.Keyframes.Count.ShouldBe(1);
    }

    [AvaloniaFact]
    public void ChangingTheInterpolation_ThenUndo_RestoresIt_InTheTabAndOnDisk()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.DetailCombos[1].SelectedItem = FlatRedBall.Glue.StateInterpolation.InterpolationType.Bounce;
        editor.Layout();
        editor.DetailCombos[2].SelectedItem = FlatRedBall.Glue.StateInterpolation.Easing.InOut;
        editor.Layout();

        editor.UndoManager.PerformUndo();
        editor.Layout();

        editor.ThrowIfPluginFailed();
        AnimatedKeyframeViewModel keyframe = editor.ViewModel.Animations.Single().Keyframes.Single();
        keyframe.InterpolationType.ShouldBe(FlatRedBall.Glue.StateInterpolation.InterpolationType.Bounce);
        keyframe.Easing.ShouldBe(FlatRedBall.Glue.StateInterpolation.Easing.Out);
        editor.ViewModel.SelectedAnimation!.SelectedKeyframe.ShouldBeSameAs(keyframe);
        editor.DetailCombos[2].SelectedItem.ShouldBe(FlatRedBall.Glue.StateInterpolation.Easing.Out);
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Single().Easing.ShouldBe(FlatRedBall.Glue.StateInterpolation.Easing.Out);

        editor.UndoManager.PerformUndo();
        editor.Layout();

        keyframe = editor.ViewModel.Animations.Single().Keyframes.Single();
        keyframe.InterpolationType.ShouldBe(FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear);
        editor.DetailCombos[1].SelectedItem.ShouldBe(FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear);

        editor.UndoManager.PerformRedo();
        editor.Layout();

        editor.ViewModel.Animations.Single().Keyframes.Single().InterpolationType.ShouldBe(FlatRedBall.Glue.StateInterpolation.InterpolationType.Bounce);
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Single().InterpolationType.ShouldBe(FlatRedBall.Glue.StateInterpolation.InterpolationType.Bounce);
    }

    [AvaloniaFact]
    public void RightClickingAKeyframe_OpensItsMenu_WhoseDeleteRemovesIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");
        walk.SelectedKeyframe = null;
        editor.Layout();

        editor.RightClick(editor.RowFor(editor.KeyframeList, released));

        walk.SelectedKeyframe.ShouldBeSameAs(released, "a right-click selects the row it lands on");
        editor.OpenContextMenu.ShouldNotBeNull().Items.OfType<global::Avalonia.Controls.MenuItem>().Select(item => item.Header).ShouldBe(new object?[] { "Add Keyframe", "Delete Keyframe" });

        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.PickContextMenuItem("Delete Keyframe");

        editor.ThrowIfPluginFailed();
        walk.Keyframes.Select(keyframe => keyframe.StateName).ShouldBe(new[] { $"{Category}/Pressed" });
        editor.KeyframeList.Items.Count.ShouldBe(1);
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Count.ShouldBe(1);
    }

    [AvaloniaFact]
    public void DeletingAKeyframe_ThenUndo_BringsItBack_AndRedoRemovesItAgain()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");
        editor.Click(editor.RowFor(editor.KeyframeList, released));
        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.Press(Key.Delete, PhysicalKey.Delete);
        editor.ViewModel.Animations.Single().Keyframes.Count.ShouldBe(1);

        editor.UndoManager.PerformUndo();
        editor.Layout();

        editor.ViewModel.Animations.Single().Keyframes.Select(keyframe => keyframe.StateName).ShouldBe(new[] { $"{Category}/Pressed", $"{Category}/Released" });
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Count.ShouldBe(2);

        editor.UndoManager.PerformRedo();
        editor.Layout();

        editor.ViewModel.Animations.Single().Keyframes.Select(keyframe => keyframe.StateName).ShouldBe(new[] { $"{Category}/Pressed" });
        editor.ReadSavedAnimations(button).ShouldNotBeNull().Animations.Single().States.Count.ShouldBe(1);
    }
}
