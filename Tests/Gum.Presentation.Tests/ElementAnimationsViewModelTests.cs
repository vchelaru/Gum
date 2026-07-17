using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins the two seams that used to require WPF (and an STA thread) to construct
/// <see cref="ElementAnimationsViewModel"/>: the right-click menus, now framework-neutral
/// <c>ContextMenuItemViewModel</c> data instead of WPF <c>MenuItem</c> instances, and animation
/// playback, now driven through the injected <see cref="IUiTimer"/> instead of a WPF
/// <c>DispatcherTimer</c> built inside the constructor (ADR-0005, issue #3754).
/// </summary>
public class ElementAnimationsViewModelTests
{
    [Fact]
    public void AnimationRightClickItems_ContainsOnlyAddAnimation_WhenNoAnimationSelected()
    {
        ElementAnimationsViewModel viewModel = CreateViewModel(Mock.Of<IUiTimer>());

        viewModel.AnimationRightClickItems.Select(x => x.Text).ShouldBe(new[] { "Add Animation" });
    }

    [Fact]
    public void AnimationRightClickItems_IncludesAnimationCommands_WhenAnimationSelected()
    {
        ElementAnimationsViewModel viewModel = CreateViewModel(Mock.Of<IUiTimer>());
        AnimationViewModel animation = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        viewModel.Animations.Add(animation);

        viewModel.SelectedAnimation = animation;

        viewModel.AnimationRightClickItems.Select(x => x.Text).ShouldBe(new[]
        {
            "Add Animation", "Rename Animation", "Squash/Stretch Frame Times", "Delete Animation",
            "Duplicate Animation", "Set to Looping"
        });
    }

    [Fact]
    public void AnimationStateRightClickItems_HasAddKeyframeSubmenuOnly_WhenNoKeyframeSelected()
    {
        ElementAnimationsViewModel viewModel = CreateViewModel(Mock.Of<IUiTimer>());
        AnimationViewModel animation = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        viewModel.Animations.Add(animation);

        viewModel.SelectedAnimation = animation;

        viewModel.AnimationStateRightClickItems.Select(x => x.Text).ShouldBe(new[] { "Add Keyframe" });
        viewModel.AnimationStateRightClickItems[0].Children.Select(x => x.Text)
            .ShouldBe(new[] { "State", "Sub-Animation", "Named Event" });
    }

    [Fact]
    public void AnimationStateRightClickItems_IncludesDeleteKeyframe_WhenKeyframeSelected()
    {
        ElementAnimationsViewModel viewModel = CreateViewModel(Mock.Of<IUiTimer>());
        AnimatedKeyframeViewModel keyframe = new() { StateName = "Idle" };
        AnimationViewModel animation = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        animation.Keyframes.Add(keyframe);
        viewModel.Animations.Add(animation);
        viewModel.SelectedAnimation = animation;

        animation.SelectedKeyframe = keyframe;

        viewModel.AnimationStateRightClickItems.Select(x => x.Text).ShouldBe(new[] { "Add Keyframe", "Delete Keyframe" });
    }

    [Fact]
    public void IsPlaying_True_StartsTimerAndResetsDisplayedTime()
    {
        Mock<IUiTimer> uiTimer = new();
        ElementAnimationsViewModel viewModel = CreateViewModel(uiTimer.Object);
        viewModel.DisplayedAnimationTime = 5;

        viewModel.IsPlaying = true;

        uiTimer.Verify(x => x.Start(TimeSpan.FromMilliseconds(20)), Times.Once);
        viewModel.DisplayedAnimationTime.ShouldBe(0);
    }

    [Fact]
    public void IsPlaying_False_StopsTimer()
    {
        Mock<IUiTimer> uiTimer = new();
        ElementAnimationsViewModel viewModel = CreateViewModel(uiTimer.Object);
        viewModel.IsPlaying = true;

        viewModel.IsPlaying = false;

        uiTimer.Verify(x => x.Stop(), Times.Once);
    }

    [Fact]
    public void TimerTick_AdvancesDisplayedAnimationTime_ByOneFrameOnFirstTick()
    {
        Mock<IUiTimer> uiTimer = new();
        ElementAnimationsViewModel viewModel = CreateViewModel(uiTimer.Object);
        viewModel.IsPlaying = true;

        uiTimer.Raise(x => x.Tick += null);

        // First tick after IsPlaying=true has no prior tick timestamp, so it advances by exactly one
        // fixed-frequency frame (20ms) rather than a wall-clock delta.
        viewModel.DisplayedAnimationTime.ShouldBe(0.02, 0.0001);
    }

    private static ElementAnimationsViewModel CreateViewModel(IUiTimer uiTimer)
    {
        ComponentSave element = new() { Name = "Foo" };
        ISelectedState selectedState = Mock.Of<ISelectedState>(s => s.SelectedElement == element);

        return new ElementAnimationsViewModel(
            Mock.Of<INameVerifier>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            Mock.Of<IRenameManager>(),
            selectedState,
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IOutputManager>(),
            Mock.Of<IAnimationFilePathService>(),
            uiTimer);
    }
}
