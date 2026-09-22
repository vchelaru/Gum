using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin;
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

    [Fact]
    public void MoveSelectedAnimationUp_KeepsTheMovedAnimationSelected_WhenTheListDropsTheSelectionDuringTheMove()
    {
        ElementAnimationsViewModel viewModel = CreateViewModel(Mock.Of<IUiTimer>());
        AnimationViewModel blink = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Blink" };
        AnimationViewModel walk = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        viewModel.Animations.Add(blink);
        viewModel.Animations.Add(walk);
        viewModel.SelectedAnimation = walk;
        // A bound list clears its selection when the selected item moves, as the Avalonia ListBox does.
        viewModel.Animations.CollectionChanged += (_, _) => viewModel.SelectedAnimation = null;

        bool moved = viewModel.MoveSelectedAnimationUp();

        moved.ShouldBeTrue();
        viewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Walk", "Blink" });
        viewModel.SelectedAnimation.ShouldBeSameAs(walk);
    }

    [Fact]
    public void LosingTheSelectedAnimation_StopsPlayback_AndItsTimer()
    {
        // Deleting the playing animation hides the Play button, so nothing else could stop the timer.
        Mock<IUiTimer> timer = new Mock<IUiTimer>();
        ElementAnimationsViewModel viewModel = CreateViewModel(timer.Object);
        AnimationViewModel walk = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        viewModel.Animations.Add(walk);
        viewModel.SelectedAnimation = walk;
        viewModel.IsPlaying = true;

        viewModel.SelectedAnimation = null;

        viewModel.IsPlaying.ShouldBeFalse();
        timer.Verify(t => t.Stop(), Times.Once);
    }

    [Fact]
    public void AKeyframeCopiedInOneElementsViewModel_PastesInAnothers_ThroughTheSharedClipboard()
    {
        // Selecting another element replaces the view model, and the copied keyframe must survive that.
        KeyframeClipboard clipboard = new KeyframeClipboard();
        ElementAnimationsViewModel first = CreateViewModel(Mock.Of<IUiTimer>(), clipboard);
        AnimationViewModel walk = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        AnimatedKeyframeViewModel released = new AnimatedKeyframeViewModel { StateName = "Cat/Released", Time = 2, HasValidState = true };
        walk.Keyframes.Add(released);
        first.Animations.Add(walk);
        first.SelectedAnimation = walk;
        walk.SelectedKeyframe = released;
        first.CopySelectedKeyframe();
        ElementAnimationsViewModel second = CreateViewModel(Mock.Of<IUiTimer>(), clipboard);
        AnimationViewModel grow = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Grow" };
        second.Animations.Add(grow);
        second.SelectedAnimation = grow;

        AnimatedKeyframeViewModel? pastedFrom = second.PasteKeyframe();

        pastedFrom.ShouldNotBeNull();
        grow.Keyframes.Single().StateName.ShouldBe("Cat/Released");
        grow.Keyframes.Single().Time.ShouldBe(2.1f);
    }

    [Fact]
    public void RemovingAnAnimation_BreaksTheKeyframesThatPlayIt()
    {
        // The referencing keyframe holds its own copy of the removed animation (the picker loads
        // one from disk), so it is matched by name; it must show as broken now rather than after
        // the element is reselected.
        ElementAnimationsViewModel viewModel = CreateViewModel(Mock.Of<IUiTimer>());
        AnimationViewModel blink = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Blink" };
        AnimationViewModel walk = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        AnimatedKeyframeViewModel reference = new AnimatedKeyframeViewModel { AnimationName = "Blink", SubAnimationViewModel = blink.Clone(), HasValidState = true };
        walk.Keyframes.Add(reference);
        viewModel.Animations.Add(blink);
        viewModel.Animations.Add(walk);

        viewModel.Animations.Remove(blink);

        reference.IsMissingReference.ShouldBeTrue();
        reference.SubAnimationViewModel.ShouldBeNull();
        walk.HasBrokenKeyframe.ShouldBeTrue();
    }

    [Fact]
    public void RenamingAnAnimation_ReportsOneChange_OnceTheNameAndTheReferencesToItAgree()
    {
        // The plugin saves and records an undo per reported change, so the name and the keyframes
        // that play the animation must land in one record, or an undo leaves them disagreeing.
        Mock<IDialogService> dialogs = new Mock<IDialogService>();
        dialogs.Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<GetUserStringOptions?>())).Returns("Wink");
        Mock<IRenameManager> renameManager = new Mock<IRenameManager>();
        ElementAnimationsViewModel viewModel = CreateViewModel(Mock.Of<IUiTimer>(), dialogs: dialogs.Object, renameManager: renameManager.Object);
        AnimationViewModel blink = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Blink" };
        AnimationViewModel walk = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        AnimatedKeyframeViewModel playsBlink = new AnimatedKeyframeViewModel { AnimationName = "Blink", HasValidState = true };
        walk.Keyframes.Add(playsBlink);
        viewModel.Animations.Add(blink);
        viewModel.Animations.Add(walk);
        viewModel.SelectedAnimation = blink;
        renameManager.Setup(r => r.HandleRename(blink, "Blink", It.IsAny<IEnumerable<AnimationViewModel>>(), It.IsAny<ElementSave>()))
            .Callback(() => playsBlink.AnimationName = "Wink");
        List<(string Name, string Reference)> reported = new List<(string, string)>();
        viewModel.AnyChange += (_, _) => reported.Add((blink.Name, playsBlink.AnimationName));

        viewModel.AnimationRightClickItems.Single(item => item.Text == "Rename Animation").Action!();

        reported.ShouldBe(new[] { ("Wink", "Wink") });
    }

    [Fact]
    public void SquashingAnAnimation_ReportsOneChange_OnceEveryKeyframeHasMoved()
    {
        Mock<IDialogService> dialogs = new Mock<IDialogService>();
        dialogs.Setup(d => d.GetUserString(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<GetUserStringOptions?>())).Returns("4");
        ElementAnimationsViewModel viewModel = CreateViewModel(Mock.Of<IUiTimer>(), dialogs: dialogs.Object);
        AnimationViewModel walk = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        walk.Keyframes.Add(new AnimatedKeyframeViewModel { StateName = "Cat/A", Time = 0, HasValidState = true });
        walk.Keyframes.Add(new AnimatedKeyframeViewModel { StateName = "Cat/B", Time = 1, HasValidState = true });
        walk.Keyframes.Add(new AnimatedKeyframeViewModel { StateName = "Cat/C", Time = 2, HasValidState = true });
        viewModel.Animations.Add(walk);
        viewModel.SelectedAnimation = walk;
        List<float[]> reported = new List<float[]>();
        viewModel.AnyChange += (_, _) => reported.Add(walk.Keyframes.Select(keyframe => keyframe.Time).ToArray());

        viewModel.AnimationRightClickItems.Single(item => item.Text == "Squash/Stretch Frame Times").Action!();

        reported.Count.ShouldBe(1);
        reported[0].ShouldBe(new[] { 0f, 2f, 4f });
    }

    [Fact]
    public void DuplicatingAnAnimation_LeavesTheKeyframesThatPlayTheOriginal_PlayingTheOriginal()
    {
        Mock<IRenameManager> renameManager = new Mock<IRenameManager>();
        ElementAnimationsViewModel viewModel = CreateViewModel(Mock.Of<IUiTimer>(), renameManager: renameManager.Object);
        AnimationViewModel blink = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Blink" };
        AnimationViewModel walk = new(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>()) { Name = "Walk" };
        AnimatedKeyframeViewModel playsBlink = new AnimatedKeyframeViewModel { AnimationName = "Blink", HasValidState = true };
        walk.Keyframes.Add(playsBlink);
        viewModel.Animations.Add(blink);
        viewModel.Animations.Add(walk);
        viewModel.SelectedAnimation = blink;

        viewModel.AnimationRightClickItems.Single(item => item.Text == "Duplicate Animation").Action!();

        viewModel.Animations.Select(animation => animation.Name).ShouldBe(new[] { "Blink", "Walk", "Copy of Blink" });
        playsBlink.AnimationName.ShouldBe("Blink");
        renameManager.Verify(r => r.HandleRename(It.IsAny<AnimationViewModel>(), It.IsAny<string>(), It.IsAny<IEnumerable<AnimationViewModel>>(), It.IsAny<ElementSave>()), Times.Never);
    }

    private static ElementAnimationsViewModel CreateViewModel(IUiTimer uiTimer, IKeyframeClipboard? clipboard = null, IDialogService? dialogs = null, IRenameManager? renameManager = null)
    {
        ComponentSave element = new() { Name = "Foo" };
        ISelectedState selectedState = Mock.Of<ISelectedState>(s => s.SelectedElement == element);

        return new ElementAnimationsViewModel(
            Mock.Of<INameVerifier>(),
            dialogs ?? Mock.Of<IDialogService>(),
            Mock.Of<IAnimationCollectionViewModelManager>(),
            renameManager ?? Mock.Of<IRenameManager>(),
            selectedState,
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IOutputManager>(),
            Mock.Of<IAnimationFilePathService>(),
            uiTimer,
            clipboard);
    }
}
