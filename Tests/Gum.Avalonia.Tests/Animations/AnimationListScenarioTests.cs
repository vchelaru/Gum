using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.DataTypes;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// What a user does to whole animations in the left column: the right-click menu's rename,
/// duplicate, looping and squash/stretch, and the list's reorder, copy, paste and delete hotkeys.
/// </summary>
public class AnimationListScenarioTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void RenamingAnAnimation_FromTheContextMenu_FollowsIntoSubAnimationKeyframes_AndSaves()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel blink = editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        editor.Click(editor.RowFor(editor.AnimationList, blink));

        editor.Dialogs.AnswerNextUserString("Wink");
        editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Rename Animation").Action!();
        editor.Layout();

        blink.Name.ShouldBe("Wink");
        walk.Keyframes.Single().AnimationName.ShouldBe("Wink");
        editor.RowFor(editor.AnimationList, blink).GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ShouldContain("Wink");
        ElementAnimationsSave saved = editor.ReadSavedAnimations(component).ShouldNotBeNull();
        saved.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Wink", "Walk" });
        saved.Animations[1].Animations.Single().Name.ShouldBe("Wink");
    }

    [AvaloniaFact]
    public void RenamingAnAnimation_ThenUndo_RestoresTheNameAndTheReferencesToIt_Together()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel blink = editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddAnimation("Walk");
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        editor.Click(editor.RowFor(editor.AnimationList, blink));
        editor.Dialogs.AnswerNextUserString("Wink");
        editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Rename Animation").Action!();
        editor.Layout();

        editor.UndoManager.PerformUndo();
        editor.Layout();

        editor.ThrowIfPluginFailed();
        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Blink", "Walk" });
        AnimationViewModel walk = editor.ViewModel.Animations[1];
        walk.Keyframes.Single().AnimationName.ShouldBe("Blink");
        walk.HasBrokenKeyframe.ShouldBeFalse();
        ElementAnimationsSave saved = editor.ReadSavedAnimations(component).ShouldNotBeNull();
        saved.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Blink", "Walk" });
        saved.Animations[1].Animations.Single().Name.ShouldBe("Blink");

        editor.UndoManager.PerformRedo();
        editor.Layout();

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Wink", "Walk" });
        editor.ViewModel.Animations[1].Keyframes.Single().AnimationName.ShouldBe("Wink");
    }

    [AvaloniaFact]
    public void DuplicatingAnAnimation_FromTheContextMenu_AddsACopyWithItsKeyframes_AndSaves()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");

        editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Duplicate Animation").Action!();
        editor.Layout();

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk", "Copy of Walk" });
        editor.ViewModel.Animations[1].Keyframes.Select(keyframe => (keyframe.StateName, keyframe.Time))
            .ShouldBe(new[] { ($"{Category}/Pressed", 0f), ($"{Category}/Released", 1f) });
        editor.AnimationList.Items.Count.ShouldBe(2);
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk", "Copy of Walk" });
    }

    [AvaloniaFact]
    public void DuplicatingAnAnimation_LeavesTheKeyframesThatPlayIt_PlayingTheOriginal()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel blink = editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        editor.Click(editor.RowFor(editor.AnimationList, blink));

        editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Duplicate Animation").Action!();
        editor.Layout();

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Blink", "Walk", "Copy of Blink" });
        walk.Keyframes.Single().AnimationName.ShouldBe("Blink");
        walk.HasBrokenKeyframe.ShouldBeFalse();
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single(animation => animation.Name == "Walk").Animations.Single().Name.ShouldBe("Blink");
    }

    [AvaloniaFact]
    public void SetToLooping_FromTheContextMenu_ShowsTheLoopGlyph_AndSaves()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");

        editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Set to Looping").Action!();
        editor.Layout();

        walk.Loops.ShouldBeTrue();
        ToggleButton loopToggle = editor.RowFor(editor.AnimationList, walk).GetVisualDescendants().OfType<ToggleButton>().Single();
        loopToggle.IsChecked.ShouldBe(true);
        loopToggle.Content.ShouldBe("∞");
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().Loops.ShouldBeTrue();

        // The row's toggle is the other way to change it.
        editor.Click(loopToggle);
        walk.Loops.ShouldBeFalse();
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().Loops.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void SquashStretch_FromTheContextMenu_ScalesEveryKeyframeTime_AndSaves()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        editor.AddStateKeyframe($"{Category}/Pressed");

        editor.Dialogs.AnswerNextUserString("4");
        editor.ViewModel.AnimationRightClickItems.Single(item => item.Text == "Squash/Stretch Frame Times").Action!();
        editor.Layout();

        walk.Keyframes.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0f, 2f, 4f });
        walk.Length.ShouldBe(4f);
        editor.Scrubber.Maximum.ShouldBe(4);
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().States.Select(state => state.Time).ShouldBe(new[] { 0f, 2f, 4f });
    }

    [AvaloniaFact]
    public void AltUp_InTheAnimationList_MovesTheAnimationUp_AndSaves()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Blink");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.Click(editor.RowFor(editor.AnimationList, walk));

        editor.Press(Key.Up, PhysicalKey.ArrowUp, RawInputModifiers.Alt);

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk", "Blink" });
        editor.ViewModel.SelectedAnimation.ShouldBeSameAs(walk);
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk", "Blink" });
    }

    [AvaloniaFact]
    public void CopyAndPaste_InTheAnimationList_AddsACopyWithAUniqueName_AndSaves()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Click(editor.RowFor(editor.AnimationList, walk));

        editor.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        editor.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        editor.ViewModel.Animations.Count.ShouldBe(2);
        AnimationViewModel pasted = editor.ViewModel.Animations[1];
        pasted.Name.ShouldNotBe("Walk");
        pasted.Keyframes.Single().StateName.ShouldBe($"{Category}/Pressed");
        editor.ViewModel.SelectedAnimation.ShouldBeSameAs(pasted);
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk", pasted.Name });
    }

    [AvaloniaFact]
    public void GameSpeedButtons_StepThroughTheSpeeds()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        Button slower = editor.Window.GetVisualDescendants().OfType<Button>().Single(button => button.Content is "−");
        Button faster = editor.Window.GetVisualDescendants().OfType<Button>().Single(button => button.Content is "+" && ToolTip.GetTip(button) is string tip && tip.StartsWith("Speed Up"));

        editor.Click(faster);
        editor.ViewModel.CurrentGameSpeed.ShouldBe("200%");
        editor.Click(slower);
        editor.Click(slower);
        editor.ViewModel.CurrentGameSpeed.ShouldBe("50%");
        editor.Window.GetVisualDescendants().OfType<TextBlock>().ShouldContain(text => text.Text == "50%");
    }
}
