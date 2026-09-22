using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// The animation tutorials' workflows, driven through the tab with real input: each scenario is one
/// thing a user does in the Animations tab, checked against what the tab shows and what it saves.
/// </summary>
public class AnimationEditorScenarioTests
{
    private const string Category = "AnimationCategory";

    [AvaloniaFact]
    public void AddingAnAnimation_ThroughTheDialog_ListsItSelected_AndWritesTheSidecar()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);

        AnimationViewModel walk = editor.AddAnimation("Walk", loops: true);

        editor.ViewModel.SelectedAnimation.ShouldBeSameAs(walk);
        editor.AnimationList.SelectedItem.ShouldBeSameAs(walk);
        editor.RowFor(editor.AnimationList, walk).GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ShouldContain("Walk");
        editor.PlayButton.IsEffectivelyVisible.ShouldBeTrue();
        ElementAnimationsSave saved = editor.ReadSavedAnimations(component).ShouldNotBeNull();
        saved.Animations.Select(animation => (animation.Name, animation.Loops)).ShouldBe(new[] { ("Walk", true) });
        editor.SaveFrame("add-animation");
    }

    [AvaloniaFact]
    public void AddingStateKeyframes_ThroughTheMenu_PlacesThemASecondApart_OnTheirCategoryRow()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");

        AnimatedKeyframeViewModel first = editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel second = editor.AddStateKeyframe($"{Category}/Released");
        AnimatedKeyframeViewModel third = editor.AddStateKeyframe($"{Category}/Pressed");

        editor.ViewModel.SelectedAnimation!.Keyframes.ShouldBe(new[] { first, second, third });
        editor.ViewModel.SelectedAnimation.Keyframes.Select(keyframe => keyframe.Time).ShouldBe(new[] { 0f, 1f, 2f });
        editor.KeyframeList.Items.Count.ShouldBe(3);
        editor.Timeline.Rows.Select(row => row.Name).ShouldBe(new[] { Category });
        editor.Timeline.Rows[0].Items.ShouldBe(new[] { first, second, third });
        ElementAnimationsSave saved = editor.ReadSavedAnimations(component).ShouldNotBeNull();
        saved.Animations.Single().States.Select(state => (state.StateName, state.Time))
            .ShouldBe(new[] { ($"{Category}/Pressed", 0f), ($"{Category}/Released", 1f), ($"{Category}/Pressed", 2f) });
        editor.SaveFrame("add-keyframes");
    }

    [AvaloniaFact]
    public void ClickingAMarkerOnTheTimeline_SelectsItsKeyframe_AndShowsItsDetails()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");
        editor.AddStateKeyframe($"{Category}/Pressed");

        editor.ClickAt(editor.KeyframeMarkerCenter(released));

        editor.ViewModel.SelectedAnimation!.SelectedKeyframe.ShouldBeSameAs(released);
        editor.KeyframeList.SelectedItem.ShouldBeSameAs(released);
        editor.Detail.IsEffectivelyVisible.ShouldBeTrue();
        editor.DetailCombos[0].Text.ShouldBe($"{Category}/Released");
        editor.DetailTimeBox.Text.ShouldBe("1");
        // Selecting a state keyframe selects its state in the tool, so the canvas shows it.
        editor.SelectedState.SelectedStateSave.ShouldBeSameAs(component.Categories[0].States.Single(state => state.Name == "Released"));
        editor.SaveFrame("select-marker");
    }

    [AvaloniaFact]
    public void TypingANewTime_MovesTheKeyframe_ResortsTheList_AndSaves()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel first = editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel second = editor.AddStateKeyframe($"{Category}/Released");
        AnimatedKeyframeViewModel third = editor.AddStateKeyframe($"{Category}/Pressed");
        editor.ClickAt(editor.KeyframeMarkerCenter(third));

        editor.TypeAndEnter(editor.DetailTimeBox, "0.5");

        third.Time.ShouldBe(0.5f);
        editor.ViewModel.SelectedAnimation!.Keyframes.ShouldBe(new[] { first, third, second });
        editor.Timeline.Rows[0].Items.ShouldBe(new[] { first, third, second });
        ElementAnimationsSave saved = editor.ReadSavedAnimations(component).ShouldNotBeNull();
        saved.Animations.Single().States.Select(state => state.Time).ShouldBe(new[] { 0f, 0.5f, 1f });
    }

    [AvaloniaFact]
    public void ChangingInterpolationAndEasing_InTheDetailColumn_Saves()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel keyframe = editor.AddStateKeyframe($"{Category}/Pressed");

        editor.DetailCombos[1].SelectedItem = FlatRedBall.Glue.StateInterpolation.InterpolationType.Bounce;
        editor.DetailCombos[2].SelectedItem = FlatRedBall.Glue.StateInterpolation.Easing.InOut;
        editor.Layout();

        keyframe.InterpolationType.ShouldBe(FlatRedBall.Glue.StateInterpolation.InterpolationType.Bounce);
        keyframe.Easing.ShouldBe(FlatRedBall.Glue.StateInterpolation.Easing.InOut);
        AnimatedStateSave saved = editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().States.Single();
        saved.InterpolationType.ShouldBe(FlatRedBall.Glue.StateInterpolation.InterpolationType.Bounce);
        saved.Easing.ShouldBe(FlatRedBall.Glue.StateInterpolation.Easing.InOut);
    }

    [AvaloniaFact]
    public void PressingDelete_InTheKeyframeList_RemovesTheKeyframe_OnceConfirmed()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel first = editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel second = editor.AddStateKeyframe($"{Category}/Released");
        editor.Click(editor.RowFor(editor.KeyframeList, second));

        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Negative);
        editor.Press(Key.Delete, PhysicalKey.Delete);
        editor.ViewModel.SelectedAnimation!.Keyframes.ShouldBe(new[] { first, second });

        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.Press(Key.Delete, PhysicalKey.Delete);

        editor.ViewModel.SelectedAnimation.Keyframes.ShouldBe(new[] { first });
        editor.KeyframeList.Items.Count.ShouldBe(1);
        editor.Timeline.Rows[0].Items.ShouldBe(new[] { first });
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().States.Count.ShouldBe(1);
    }

    [AvaloniaFact]
    public void CopyAndPaste_InTheKeyframeList_AddsACopy_ATenthOfASecondLater()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel first = editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Click(editor.RowFor(editor.KeyframeList, first));

        editor.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
        editor.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);

        AnimationViewModel walk = editor.ViewModel.SelectedAnimation!;
        walk.Keyframes.Count.ShouldBe(2);
        walk.Keyframes[1].StateName.ShouldBe($"{Category}/Pressed");
        walk.Keyframes[1].Time.ShouldBe(0.1f);
        walk.SelectedKeyframe.ShouldBeSameAs(walk.Keyframes[1]);
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().States.Select(state => state.Time).ShouldBe(new[] { 0f, 0.1f });
    }

    [AvaloniaFact]
    public void TypingATimeAboveTheTimeline_ScrubsTheCanvas_ToTheInterpolatedState()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");

        editor.TypeAndEnter(editor.TimelineTimeBox, "0.5");

        editor.ViewModel.DisplayedAnimationTime.ShouldBe(0.5);
        editor.Scrubber.Value.ShouldBe(0.5);
        StateSave shown = editor.SelectedState.CustomCurrentStateSave.ShouldNotBeNull();
        // Pressed sets X to 0 and Released to 100; halfway between them is 50.
        shown.GetValue("X").ShouldBe(50f);
        editor.SaveFrame("scrub");
    }

    [AvaloniaFact]
    public void PressingPlay_AdvancesTheTime_AndStopsAtTheEnd()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        editor.ViewModel.SelectedAnimation!.Length.ShouldBe(1f);

        editor.Click(editor.PlayButton);

        editor.ViewModel.IsPlaying.ShouldBeTrue();
        editor.PlayButton.Content.ShouldBe("■ Stop");
        editor.Wait(TimeSpan.FromMilliseconds(250));
        editor.ViewModel.DisplayedAnimationTime.ShouldBeGreaterThan(0);
        editor.WaitUntil(() => !editor.ViewModel.IsPlaying, TimeSpan.FromSeconds(4)).ShouldBeTrue("a one-second animation stops on its own");
        editor.PlayButton.Content.ShouldBe("▶ Play");
    }

    [AvaloniaFact]
    public void ANamedEventAndASubAnimation_GetTheirOwnRows_WithACircleAndABar()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel blink = editor.AddAnimation("Blink");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");

        editor.Dialogs.AnswerNextUserString("Footstep");
        editor.PickAddKeyframe("Named Event");
        AnimatedKeyframeViewModel footstep = walk.SelectedKeyframe.ShouldNotBeNull();
        footstep.EventName.ShouldBe("Footstep");
        footstep.Time.ShouldBe(1f);

        editor.Dialogs.AnswerNext<SubAnimationSelectionDialogViewModel>(dialog =>
        {
            dialog.SelectedContainer = dialog.AnimationContainers!.Single();
            dialog.SelectedAnimation = dialog.Animations.Single(animation => animation.Name == "Blink");
            return true;
        });
        editor.PickAddKeyframe("Sub-Animation");
        AnimatedKeyframeViewModel sub = walk.SelectedKeyframe.ShouldNotBeNull();
        sub.AnimationName.ShouldBe("Blink");
        sub.Time.ShouldBe(2f);
        sub.Length.ShouldBe(1f);

        editor.Timeline.Rows.Select(row => row.Name).ShouldBe(new[] { "Default", Category, "Blink" });
        walk.Length.ShouldBe(3f);
        AnimationSave saved = editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single(animation => animation.Name == "Walk");
        saved.Events.Single().Name.ShouldBe("Footstep");
        saved.Animations.Single().Name.ShouldBe("Blink");
        editor.SaveFrame("event-and-sub-animation");
    }

    [AvaloniaFact]
    public void HoveringAKeyframeRow_HighlightsItsMarker_AndTheReverse()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel first = editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel second = editor.AddStateKeyframe($"{Category}/Released");

        editor.Hover(editor.CenterOf(editor.RowFor(editor.KeyframeList, second)));
        second.IsTimelineVisualHovered.ShouldBeTrue();
        first.IsTimelineVisualHovered.ShouldBeFalse();

        editor.Hover(editor.KeyframeMarkerCenter(first));
        first.IsTimelineVisualHovered.ShouldBeTrue();
        second.IsTimelineVisualHovered.ShouldBeFalse();

        editor.Hover(new Point(5, 5));
        first.IsTimelineVisualHovered.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void DeletingAnAnimation_ThenUndo_BringsItBack_InTheTabAndOnDisk()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.Click(editor.RowFor(editor.AnimationList, walk));

        editor.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        editor.Press(Key.Delete, PhysicalKey.Delete);
        editor.ViewModel.Animations.ShouldBeEmpty();
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.ShouldBeEmpty();

        editor.UndoManager.PerformUndo();
        editor.Layout();

        editor.ViewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk" });
        editor.ViewModel.Animations[0].Keyframes.Single().StateName.ShouldBe($"{Category}/Pressed");
        editor.AnimationList.Items.Count.ShouldBe(1);
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().Name.ShouldBe("Walk");
    }

    [AvaloniaFact]
    public void RenamingAState_InTheTool_FollowsIntoTheKeyframes()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel keyframe = editor.AddStateKeyframe($"{Category}/Pressed");
        StateSave pressed = component.Categories[0].States.Single(state => state.Name == "Pressed");

        TestAppBuilder.Services.GetRequiredService<IRenameLogic>().RenameState(pressed, component.Categories[0], "Down");
        editor.Layout();

        keyframe.StateName.ShouldBe($"{Category}/Down");
        editor.ViewModel.Animations.Single().Keyframes.Single().StateName.ShouldBe($"{Category}/Down");
        editor.ViewModel.Animations.Single().HasBrokenKeyframe.ShouldBeFalse();
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().States.Single().StateName.ShouldBe($"{Category}/Down");
    }

    [AvaloniaFact]
    public void DraggingTheScrubber_MovesTheTime_AndTheCanvasState()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        Point thumb = editor.CenterOf(editor.ScrubberThumb);
        double trackWidth = editor.Scrubber.Bounds.Width - editor.ScrubberThumb.Bounds.Width;

        editor.Drag(thumb, new Point(thumb.X + trackWidth / 2, thumb.Y));

        editor.ViewModel.DisplayedAnimationTime.ShouldBe(0.5, tolerance: 0.05);
        editor.TimelineTimeBox.Text.ShouldStartWith("0.5");
        StateSave shown = editor.SelectedState.CustomCurrentStateSave.ShouldNotBeNull();
        ((float)shown.GetValue("X")).ShouldBe(50f, tolerance: 5f);
    }

    [AvaloniaFact]
    public void TypingAMissingStateName_FlagsTheKeyframeAndItsAnimation_UntilAStateIsPicked()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        AnimationViewModel walk = editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel keyframe = editor.AddStateKeyframe($"{Category}/Pressed");

        editor.TypeAndEnter(editor.StateComboTextBox, $"{Category}/Gone");

        keyframe.StateName.ShouldBe($"{Category}/Gone");
        keyframe.IsMissingReference.ShouldBeTrue();
        walk.HasBrokenKeyframe.ShouldBeTrue();
        editor.RowFor(editor.KeyframeList, keyframe).GetVisualDescendants().OfType<TextBlock>().ShouldContain(text => text.Text == "⚠");
        editor.Detail.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == keyframe.MissingReferenceMessage).IsVisible.ShouldBeTrue();
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().States.Single().StateName.ShouldBe($"{Category}/Gone");
        editor.SaveFrame("missing-state");

        editor.DetailCombos[0].SelectedItem = $"{Category}/Released";
        editor.Layout();

        keyframe.StateName.ShouldBe($"{Category}/Released");
        keyframe.IsMissingReference.ShouldBeFalse();
        walk.HasBrokenKeyframe.ShouldBeFalse();
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().States.Single().StateName.ShouldBe($"{Category}/Released");
    }

    [AvaloniaFact]
    public void AKeyframeOnAnUncategorizedState_ShowsTheWarningMark()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed");
        component.States.Add(new StateSave { Name = "Hidden", ParentContainer = component });
        editor.Select(component);
        editor.AddAnimation("Walk");

        AnimatedKeyframeViewModel hidden = editor.AddStateKeyframe("Hidden");

        hidden.IsUncategorized.ShouldBeTrue();
        editor.RowFor(editor.KeyframeList, hidden).GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == "!").IsVisible.ShouldBeTrue();
        editor.Timeline.Rows.Select(row => row.Name).ShouldBe(new[] { "Default" });
    }

    [AvaloniaFact]
    public void PlayingALoopingAnimation_WrapsAroundInsteadOfStopping()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk", loops: true);
        editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel last = editor.AddStateKeyframe($"{Category}/Released");
        editor.TypeAndEnter(editor.DetailTimeBox, "0.3");
        last.Time.ShouldBe(0.3f);

        editor.Click(editor.PlayButton);
        editor.Wait(TimeSpan.FromMilliseconds(700));

        editor.ViewModel.IsPlaying.ShouldBeTrue();
        editor.ViewModel.DisplayedAnimationTime.ShouldBeLessThanOrEqualTo(0.3 + 0.05);
        editor.Click(editor.PlayButton);
        editor.ViewModel.IsPlaying.ShouldBeFalse();
    }
}
