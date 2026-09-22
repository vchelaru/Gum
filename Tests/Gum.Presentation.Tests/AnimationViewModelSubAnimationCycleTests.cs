using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// Sub-animation references that loop back on themselves: the sidecar can hold them (a hand edit,
/// or two animations that each play the other), and loading must stop at the repeat instead of
/// recursing forever.
/// </summary>
public class AnimationViewModelSubAnimationCycleTests
{
    private readonly ISelectedState _selectedState = Mock.Of<ISelectedState>();
    private readonly IWireframeObjectManager _wireframeObjectManager = Mock.Of<IWireframeObjectManager>();

    [Fact]
    public void FromSave_StopsAtAnAnimationAlreadyBeingLoaded_AndFlagsThatKeyframe()
    {
        ComponentSave element = new ComponentSave { Name = "Button" };
        ElementAnimationsSave all = new ElementAnimationsSave();
        AnimationSave walk = new AnimationSave { Name = "Walk" };
        walk.Animations.Add(new AnimationReferenceSave { Name = "Run", Time = 1 });
        AnimationSave run = new AnimationSave { Name = "Run" };
        run.Animations.Add(new AnimationReferenceSave { Name = "Walk", Time = 2 });
        all.Animations.Add(walk);
        all.Animations.Add(run);
        IAnimationSaveRepository repository = Mock.Of<IAnimationSaveRepository>(r => r.GetElementAnimationsSave(element) == all);

        AnimationViewModel loaded = AnimationViewModel.FromSave(walk, element, repository, _selectedState, _wireframeObjectManager, all);

        AnimatedKeyframeViewModel playsRun = loaded.Keyframes.Single();
        playsRun.IsMissingReference.ShouldBeFalse();
        AnimationViewModel runViewModel = playsRun.SubAnimationViewModel.ShouldNotBeNull();
        AnimatedKeyframeViewModel playsWalkAgain = runViewModel.Keyframes.Single();
        playsWalkAgain.SubAnimationViewModel.ShouldBeNull("Walk is already being loaded, so the reference back to it must not be followed");
        playsWalkAgain.IsMissingReference.ShouldBeTrue();
        loaded.Length.ShouldBe(3f);
    }

    [Fact]
    public void FromSave_FollowsTheSameAnimation_WhenItIsPlayedTwiceWithoutACycle()
    {
        ComponentSave element = new ComponentSave { Name = "Button" };
        ElementAnimationsSave all = new ElementAnimationsSave();
        AnimationSave walk = new AnimationSave { Name = "Walk" };
        walk.Animations.Add(new AnimationReferenceSave { Name = "Blink", Time = 0 });
        walk.Animations.Add(new AnimationReferenceSave { Name = "Blink", Time = 2 });
        AnimationSave blink = new AnimationSave { Name = "Blink" };
        blink.States.Add(new AnimatedStateSave { StateName = "Looks/Dim", Time = 1 });
        all.Animations.Add(walk);
        all.Animations.Add(blink);
        IAnimationSaveRepository repository = Mock.Of<IAnimationSaveRepository>(r => r.GetElementAnimationsSave(element) == all);

        AnimationViewModel loaded = AnimationViewModel.FromSave(walk, element, repository, _selectedState, _wireframeObjectManager, all);

        loaded.Keyframes.Count.ShouldBe(2);
        loaded.Keyframes.ShouldAllBe(keyframe => keyframe.SubAnimationViewModel != null);
        loaded.Length.ShouldBe(3f);
    }
}
