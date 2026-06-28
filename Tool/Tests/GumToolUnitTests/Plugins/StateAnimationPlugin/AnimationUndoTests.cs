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
    private readonly IBitmapLoader _bitmapLoader = Mock.Of<IBitmapLoader>();
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
    public void RestoreAnimationSelection_ReselectsAnimationAndReturnsMatchingKeyframe_ByIdentity()
    {
        // After an undo forces the view model to rebuild, the previously-selected animation is
        // reselected and the keyframe matching by content (state/sub-animation/event name + time) is
        // returned, so the caller can reapply it once the keyframes ListBox has rebound. This is what
        // keeps the user's selection — and the right-side property panel — across the reload.
        RunOnSta(() =>
        {
            ElementAnimationsViewModel rebuilt = ViewModelWithAnimation("Anim", out AnimationViewModel animation,
                Keyframe("Default", 0f), Keyframe("Highlighted", 1f));
            AnimatedKeyframeViewModel previouslySelected = Keyframe("Highlighted", 1f);

            AnimatedKeyframeViewModel? matched =
                MainStateAnimationPlugin.RestoreAnimationSelection(rebuilt, "Anim", previouslySelected);

            rebuilt.SelectedAnimation.ShouldBe(animation);
            matched.ShouldNotBeNull();
            matched!.StateName.ShouldBe("Highlighted");
            matched.Time.ShouldBe(1f);
            // Selected on the animation, and pre-set before it became the active animation.
            animation.SelectedKeyframe.ShouldBe(matched);
        });
    }

    [Fact]
    public void RestoreAnimationSelection_ReselectsAnimationButReturnsNull_WhenKeyframeNoLongerExists()
    {
        // If the undo removed the keyframe that was selected, the match fails (returns null) while the
        // animation is still reselected. Mirrors element-undo's silent selection drop when the selected
        // object no longer exists.
        RunOnSta(() =>
        {
            ElementAnimationsViewModel rebuilt = ViewModelWithAnimation("Anim", out AnimationViewModel animation,
                Keyframe("Default", 0f));
            AnimatedKeyframeViewModel removed = Keyframe("Highlighted", 1f);

            AnimatedKeyframeViewModel? matched =
                MainStateAnimationPlugin.RestoreAnimationSelection(rebuilt, "Anim", removed);

            rebuilt.SelectedAnimation.ShouldBe(animation);
            matched.ShouldBeNull();
            animation.SelectedKeyframe.ShouldBeNull();
        });
    }

    private AnimatedKeyframeViewModel Keyframe(string stateName, float time)
        => new(_bitmapLoader) { StateName = stateName, Time = time };

    private ElementAnimationsViewModel ViewModelWithAnimation(string animationName, out AnimationViewModel animation,
        params AnimatedKeyframeViewModel[] keyframes)
    {
        animation = new AnimationViewModel(_bitmapLoader, _selectedState, _wireframeObjectManager) { Name = animationName };
        foreach (AnimatedKeyframeViewModel keyframe in keyframes)
        {
            animation.Keyframes.Add(keyframe);
        }

        ElementAnimationsViewModel viewModel = new(
            Mock.Of<INameVerifier>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(),
            _bitmapLoader,
            _selectedState,
            _wireframeObjectManager,
            Mock.Of<IOutputManager>());
        viewModel.Animations.Add(animation);
        return viewModel;
    }

    private ElementAnimationsViewModel CreateViewModel(IDialogService dialogService, out AnimationViewModel animation)
    {
        animation = new AnimationViewModel(_bitmapLoader, _selectedState, _wireframeObjectManager) { Name = "Anim" };
        AnimatedKeyframeViewModel keyframe = new(_bitmapLoader) { StateName = "Default" };
        animation.Keyframes.Add(keyframe);
        animation.SelectedKeyframe = keyframe;

        ElementAnimationsViewModel viewModel = new(
            Mock.Of<INameVerifier>(),
            dialogService,
            Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(),
            _bitmapLoader,
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
