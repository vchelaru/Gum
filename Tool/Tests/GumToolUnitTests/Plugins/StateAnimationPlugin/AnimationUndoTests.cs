using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.Undos;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System;
using System.Threading;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Covers the user-facing halves of animation undo (#3406): the keyframe-delete confirmation prompt
/// on <see cref="ElementAnimationsViewModel.DeleteSelectedKeyframe"/> (the issue's minimum bar) and the
/// History-tab wording for an animation change (<see cref="UndosViewModel.DescribeAnimationChange"/>).
/// The headless capture/diff/apply and the desync guard live in Gum.Presentation.Tests' UndoManagerTests.
/// </summary>
public class AnimationUndoTests : BaseTestClass
{
    private readonly ISelectedState _selectedState = Mock.Of<ISelectedState>();
    private readonly IWireframeObjectManager _wireframeObjectManager = Mock.Of<IWireframeObjectManager>();

    [Fact]
    public void DeleteSelectedKeyframe_DoesNotRemoveKeyframe_WhenPromptDeclined()
    {
        RunOnSta(() =>
        {
            Mock<IDialogService> dialogService = new();
            // ShowYesNoMessage is an extension over ShowMessage(...); a non-affirmative result is "No".
            dialogService
                .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
                .Returns(MessageDialogResult.Negative);
            ElementAnimationsViewModel viewModel = CreateViewModel(dialogService.Object, out AnimationViewModel animation);

            viewModel.DeleteSelectedKeyframe();

            animation.Keyframes.Count.ShouldBe(1);
        });
    }

    [Fact]
    public void DeleteSelectedKeyframe_RemovesKeyframe_WhenPromptConfirmed()
    {
        RunOnSta(() =>
        {
            Mock<IDialogService> dialogService = new();
            // ShowYesNoMessage is an extension over ShowMessage(...); an affirmative result is "Yes".
            dialogService
                .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
                .Returns(MessageDialogResult.Affirmative);
            ElementAnimationsViewModel viewModel = CreateViewModel(dialogService.Object, out AnimationViewModel animation);

            viewModel.DeleteSelectedKeyframe();

            animation.Keyframes.Count.ShouldBe(0);
        });
    }

    [Fact]
    public void DescribeAnimationChange_DescribesAddedRemovedAndModifiedAnimations()
    {
        // before: "Idle" with one keyframe, "Walk" present. after: "Idle" with two keyframes (modified),
        // "Walk" gone (removed), "Run" new (added).
        ElementAnimationsSave before = new();
        before.Animations.Add(new AnimationSave { Name = "Idle", States = { new AnimatedStateSave { StateName = "Default", Time = 0 } } });
        before.Animations.Add(new AnimationSave { Name = "Walk" });

        ElementAnimationsSave after = new();
        after.Animations.Add(new AnimationSave { Name = "Idle", States = { new AnimatedStateSave { StateName = "Default", Time = 0 }, new AnimatedStateSave { StateName = "Highlighted", Time = 1 } } });
        after.Animations.Add(new AnimationSave { Name = "Run" });

        string description = UndosViewModel.DescribeAnimationChange(before, after);

        description.ShouldContain("Add animation: Run");
        description.ShouldContain("Remove animation: Walk");
        description.ShouldContain("Modify animation: Idle");
    }

    [Fact]
    public void DescribeAnimationChange_ReturnsEmpty_WhenNeitherSnapshotHasAnimations()
    {
        UndosViewModel.DescribeAnimationChange(null, null).ShouldBeEmpty();
    }

    [Fact]
    public void ShouldReloadViewModel_ReturnsTrue_WhenForcedEvenThoughElementUnchanged()
    {
        // The after-undo path passes forceReload: true so the tab repaints from the just-restored
        // .ganx even when the selected element is the same one already loaded (issue #3406 follow-up).
        ComponentSave element = new();

        bool shouldReload = MainStateAnimationPlugin.ShouldReloadViewModel(
            currentlyReferencedElement: element,
            selectedElement: element,
            forceReload: true);

        shouldReload.ShouldBeTrue();
    }

    [Fact]
    public void ShouldReloadViewModel_ReturnsFalse_WhenElementUnchangedAndNotForced()
    {
        // The stale-VM early-out the normal refresh path relies on: same element, no reload.
        ComponentSave element = new();

        bool shouldReload = MainStateAnimationPlugin.ShouldReloadViewModel(
            currentlyReferencedElement: element,
            selectedElement: element,
            forceReload: false);

        shouldReload.ShouldBeFalse();
    }

    [Fact]
    public void ShouldReloadViewModel_ReturnsTrue_WhenElementChanged()
    {
        ComponentSave currentElement = new();
        ComponentSave selectedElement = new();

        bool shouldReload = MainStateAnimationPlugin.ShouldReloadViewModel(
            currentlyReferencedElement: currentElement,
            selectedElement: selectedElement,
            forceReload: false);

        shouldReload.ShouldBeTrue();
    }

    [Fact]
    public void RestoreAnimationSelection_ReselectsAnimationAndReturnsMatchingKeyframe_ByContent()
    {
        // After an undo forces the view model to rebuild, the previously-selected animation is
        // reselected and the keyframe matching by content (state/sub-animation/event name + time) is
        // selected on it. This is what keeps the user's selection — and the right-side property panel —
        // across the reload when the selected keyframe itself was unchanged by the undo.
        RunOnSta(() =>
        {
            ElementAnimationsViewModel rebuilt = ViewModelWithAnimation("Anim", out AnimationViewModel animation,
                Keyframe("Default", 0f), Keyframe("Highlighted", 1f));
            var selection = new MainStateAnimationPlugin.AnimationSelectionState(
                "Anim", Keyframe("Highlighted", 1f), KeyframeIndex: 1, KeyframeCount: 2);

            AnimatedKeyframeViewModel? matched =
                MainStateAnimationPlugin.RestoreAnimationSelection(rebuilt, selection);

            rebuilt.SelectedAnimation.ShouldBe(animation);
            matched.ShouldNotBeNull();
            matched!.StateName.ShouldBe("Highlighted");
            matched.Time.ShouldBe(1f);
            // Selected on the animation, and pre-set before it became the active animation.
            animation.SelectedKeyframe.ShouldBe(matched);
        });
    }

    [Fact]
    public void RestoreAnimationSelection_FallsBackToIndex_WhenSelectedKeyframesOwnValueWasReverted()
    {
        // The user's repro: select a keyframe, change its time 5 -> 4, undo. The reverted keyframe is
        // back at time 5, so the captured keyframe (time 4) matches nothing by content; since the count
        // is unchanged, fall back to the captured index so the keyframe the user was editing stays
        // selected (with its time showing 5 again).
        RunOnSta(() =>
        {
            ElementAnimationsViewModel rebuilt = ViewModelWithAnimation("Anim", out AnimationViewModel animation,
                Keyframe("Default", 0f), Keyframe("Highlighted", 5f));
            var selection = new MainStateAnimationPlugin.AnimationSelectionState(
                "Anim", Keyframe("Highlighted", 4f), KeyframeIndex: 1, KeyframeCount: 2);

            AnimatedKeyframeViewModel? matched =
                MainStateAnimationPlugin.RestoreAnimationSelection(rebuilt, selection);

            matched.ShouldNotBeNull();
            matched!.Time.ShouldBe(5f);
            animation.SelectedKeyframe.ShouldBe(matched);
        });
    }

    [Fact]
    public void RestoreAnimationSelection_DropsKeyframeSelection_WhenCountChanged()
    {
        // Undo of an add removes the selected keyframe and changes the count. Content match fails and
        // the index fallback is suppressed (count differs), so the keyframe selection drops cleanly
        // rather than grabbing a neighbor. The animation is still reselected.
        RunOnSta(() =>
        {
            ElementAnimationsViewModel rebuilt = ViewModelWithAnimation("Anim", out AnimationViewModel animation,
                Keyframe("Default", 0f));
            var selection = new MainStateAnimationPlugin.AnimationSelectionState(
                "Anim", Keyframe("Highlighted", 1f), KeyframeIndex: 1, KeyframeCount: 2);

            AnimatedKeyframeViewModel? matched =
                MainStateAnimationPlugin.RestoreAnimationSelection(rebuilt, selection);

            rebuilt.SelectedAnimation.ShouldBe(animation);
            matched.ShouldBeNull();
            animation.SelectedKeyframe.ShouldBeNull();
        });
    }

    [Fact]
    public void RestoreAnimationSelection_FallsBackToAnimationIndex_WhenSelectedAnimationWasRenamed()
    {
        // An external .ganx edit renamed the selected animation, so the captured name ("OldName")
        // matches nothing in the rebuilt view model. Because the animation count is unchanged, fall
        // back to the captured slot so the same row stays selected through the rename (#3410), and the
        // keyframe is re-matched on it by content.
        RunOnSta(() =>
        {
            ElementAnimationsViewModel rebuilt = ViewModelWithAnimation("NewName", out AnimationViewModel animation,
                Keyframe("Default", 0f), Keyframe("Highlighted", 1f));
            var selection = new MainStateAnimationPlugin.AnimationSelectionState(
                "OldName", Keyframe("Highlighted", 1f), KeyframeIndex: 1, KeyframeCount: 2,
                AnimationIndex: 0, AnimationCount: 1);

            AnimatedKeyframeViewModel? matched =
                MainStateAnimationPlugin.RestoreAnimationSelection(rebuilt, selection);

            rebuilt.SelectedAnimation.ShouldBe(animation);
            matched.ShouldNotBeNull();
            matched!.StateName.ShouldBe("Highlighted");
            animation.SelectedKeyframe.ShouldBe(matched);
        });
    }

    [Fact]
    public void RestoreAnimationSelection_DropsAnimationSelection_WhenRenamedAndAnimationCountChanged()
    {
        // The selected animation's name doesn't match AND the animation count changed (an animation was
        // added/deleted in the external edit), so reselecting by index would grab an unrelated row.
        // Drop the selection cleanly instead.
        RunOnSta(() =>
        {
            ElementAnimationsViewModel rebuilt = ViewModelWithAnimation("NewName", out AnimationViewModel _,
                Keyframe("Default", 0f));
            var selection = new MainStateAnimationPlugin.AnimationSelectionState(
                "OldName", Keyframe("Default", 0f), KeyframeIndex: 0, KeyframeCount: 1,
                AnimationIndex: 1, AnimationCount: 2);

            AnimatedKeyframeViewModel? matched =
                MainStateAnimationPlugin.RestoreAnimationSelection(rebuilt, selection);

            rebuilt.SelectedAnimation.ShouldBeNull();
            matched.ShouldBeNull();
        });
    }

    private AnimatedKeyframeViewModel Keyframe(string stateName, float time)
        => new() { StateName = stateName, Time = time };

    private ElementAnimationsViewModel ViewModelWithAnimation(string animationName, out AnimationViewModel animation,
        params AnimatedKeyframeViewModel[] keyframes)
    {
        animation = new AnimationViewModel(_selectedState, _wireframeObjectManager) { Name = animationName };
        foreach (AnimatedKeyframeViewModel keyframe in keyframes)
        {
            animation.Keyframes.Add(keyframe);
        }

        ElementAnimationsViewModel viewModel = new(
            Mock.Of<INameVerifier>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(),
            _selectedState,
            _wireframeObjectManager,
            Mock.Of<IOutputManager>());
        viewModel.Animations.Add(animation);
        return viewModel;
    }

    private ElementAnimationsViewModel CreateViewModel(IDialogService dialogService, out AnimationViewModel animation)
    {
        animation = new AnimationViewModel(_selectedState, _wireframeObjectManager) { Name = "Anim" };
        AnimatedKeyframeViewModel keyframe = new() { StateName = "Default" };
        animation.Keyframes.Add(keyframe);
        animation.SelectedKeyframe = keyframe;

        ElementAnimationsViewModel viewModel = new(
            Mock.Of<INameVerifier>(),
            dialogService,
            Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(),
            _selectedState,
            _wireframeObjectManager,
            Mock.Of<IOutputManager>());
        viewModel.Animations.Add(animation);
        viewModel.SelectedAnimation = animation;
        return viewModel;
    }

    /// <summary>
    /// WPF controls (the right-click MenuItems built in ElementAnimationsViewModel's constructor)
    /// require an STA thread; xUnit's default runner is MTA.
    /// </summary>
    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        Thread thread = new(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (caught != null)
        {
            throw new Exception("Test body threw on the STA thread.", caught);
        }
    }
}
