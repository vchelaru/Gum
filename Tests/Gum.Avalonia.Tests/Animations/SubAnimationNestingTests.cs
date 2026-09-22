using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// Animations that play other animations of the same element several levels deep, and the loops
/// that must not be possible: the picker leaves out an animation that already plays the one being
/// edited, and a loop written into the sidecar by hand loads flagged instead of hanging the tool.
/// </summary>
public class SubAnimationNestingTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void ThePicker_LeavesOutAnAnimation_ThatAlreadyPlaysTheOneBeingEdited()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(button);
        AnimationViewModel blink = editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddAnimation("Walk");
        AddOwnSubAnimation(editor, "Blink", offered: new[] { "Blink" });
        editor.Click(editor.RowFor(editor.AnimationList, blink));

        string[]? offeredToBlink = null;
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            offeredToBlink = dialog.Animations.Select(animation => animation.Name).ToArray();
            return false;
        });
        editor.PickAddKeyframe("Sub-Animation");

        offeredToBlink.ShouldNotBeNull().ShouldBeEmpty("Walk plays Blink, so Blink playing Walk would loop");
        blink.Keyframes.Count.ShouldBe(1);
    }

    [AvaloniaFact]
    public void ALoopWrittenIntoTheSidecar_LoadsFlagged_AndTheTabKeepsWorking()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        ComponentSave other = editor.AddComponent("Other", Category, "A");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddAnimation("Run");
        editor.AddStateKeyframe($"{Category}/Released");
        string path = editor.AnimationFilePath(button);
        ElementAnimationsSave onDisk = ElementAnimationsSave.Load(path);
        onDisk.Animations.Single(animation => animation.Name == "Walk").Animations.Add(new AnimationReferenceSave { Name = "Run", Time = 1 });
        onDisk.Animations.Single(animation => animation.Name == "Run").Animations.Add(new AnimationReferenceSave { Name = "Walk", Time = 1 });
        onDisk.Save(path);

        editor.Select(other);
        editor.Select(button);

        AnimationViewModel walk = editor.ViewModel.Animations.Single(animation => animation.Name == "Walk");
        AnimatedKeyframeViewModel playsRun = walk.Keyframes.Single(keyframe => keyframe.AnimationName == "Run");
        playsRun.IsMissingReference.ShouldBeFalse();
        AnimatedKeyframeViewModel playsWalkAgain = playsRun.SubAnimationViewModel.ShouldNotBeNull().Keyframes.Single(keyframe => keyframe.AnimationName == "Walk");
        playsWalkAgain.SubAnimationViewModel.ShouldBeNull();
        playsWalkAgain.IsMissingReference.ShouldBeTrue();
        walk.Length.ShouldBe(2f);

        editor.Click(editor.RowFor(editor.AnimationList, walk));
        editor.TypeAndEnter(editor.TimelineTimeBox, "1.5");
        editor.ThrowIfPluginFailed();
        editor.ViewModel.DisplayedAnimationTime.ShouldBe(1.5);
        editor.Timeline.Rows.Select(row => row.Name).ShouldBe(new[] { Category, "Run" });
        editor.SaveFrame("sidecar-loop");
    }

    [AvaloniaFact]
    public void AnAnimationThreeDeep_ReportsItsFullLength_AndReloadsWithIt()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", Category, "Pressed", "Released");
        ComponentSave other = editor.AddComponent("Other", Category, "A");
        editor.Select(button);
        editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        AnimationViewModel wave = editor.AddAnimation("Wave");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AddOwnSubAnimation(editor, "Blink", offered: new[] { "Blink" });
        wave.Length.ShouldBe(2f);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel playsWave = AddOwnSubAnimation(editor, "Wave", offered: new[] { "Blink", "Wave" });

        playsWave.Length.ShouldBe(2f);
        walk.Length.ShouldBe(3f);
        editor.Scrubber.Maximum.ShouldBe(3);
        editor.Timeline.Rows.Select(row => row.Name).ShouldBe(new[] { Category, "Wave" });

        editor.Select(other);
        editor.Select(button);

        AnimationViewModel reloaded = editor.ViewModel.Animations.Single(animation => animation.Name == "Walk");
        reloaded.Length.ShouldBe(3f);
        AnimatedKeyframeViewModel reloadedWave = reloaded.Keyframes.Single(keyframe => keyframe.AnimationName == "Wave");
        reloadedWave.SubAnimationViewModel.ShouldNotBeNull().Keyframes.Single(keyframe => keyframe.AnimationName == "Blink").SubAnimationViewModel.ShouldNotBeNull().Keyframes.Count.ShouldBe(2);
        editor.Click(editor.RowFor(editor.AnimationList, reloaded));
        editor.TypeAndEnter(editor.TimelineTimeBox, "2.5");
        editor.ThrowIfPluginFailed();
        editor.SelectedState.CustomCurrentStateSave.ShouldNotBeNull();
    }

    /// <summary>
    /// Adds a keyframe playing this element's own <paramref name="name"/> animation to the selected
    /// animation, checking the picker offered exactly <paramref name="offered"/>.
    /// </summary>
    private static AnimatedKeyframeViewModel AddOwnSubAnimation(AnimationEditorHarness editor, string name, string[] offered)
    {
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.Animations.Select(animation => animation.Name).ShouldBe(offered);
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == name);
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        return editor.ViewModel.SelectedAnimation!.SelectedKeyframe.ShouldNotBeNull();
    }
}
