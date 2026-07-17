using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="AnimatedKeyframeViewModel.IsMissingReference"/>, the flag the keyframe-list
/// icon uses to mark a keyframe that references a state/animation which no longer exists (issue
/// #3386). Mirrors the broken-keyframe logic in <c>AnimationViewModel.GetErrors</c>: a keyframe is
/// broken only when it points at a state/animation (not a named event) and its reference is invalid.
/// Relocated out of GumToolUnitTests into headless Gum.Presentation.Tests once
/// AnimatedKeyframeViewModel's dead WPF BitmapFrame plumbing was removed (ADR-0005, issue #3754).
/// </summary>
public class AnimatedKeyframeViewModelTests
{
    [Fact]
    public void ChangingHasValidState_RaisesPropertyChanged_ForIsMissingReference()
    {
        // The keyframe-list icon reacts to error recomputation only because HasValidState now
        // notifies and IsMissingReference depends on it; this pins that reactive contract.
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            StateName = "Cat/Idle",
            HasValidState = true,
        };
        List<string> changed = new List<string>();
        keyframe.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? "");

        keyframe.HasValidState = false;

        changed.ShouldContain(nameof(AnimatedKeyframeViewModel.IsMissingReference));
    }

    [Fact]
    public void IsInterpolationElementVisible_IsFalse_WhenStateNameIsEmpty()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            AnimationName = "Spin",
        };

        keyframe.IsInterpolationElementVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsInterpolationElementVisible_IsTrue_WhenStateNameIsSet()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            StateName = "Cat/Idle",
        };

        keyframe.IsInterpolationElementVisible.ShouldBeTrue();
    }

    [Fact]
    public void IsMissingReference_IsFalse_ForEventKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            EventName = "OnSomething",
            HasValidState = false,
        };

        keyframe.IsMissingReference.ShouldBeFalse();
    }

    [Fact]
    public void IsMissingReference_IsFalse_ForValidStateKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            StateName = "Cat/Idle",
            HasValidState = true,
        };

        keyframe.IsMissingReference.ShouldBeFalse();
    }

    [Fact]
    public void IsMissingReference_IsTrue_ForInvalidAnimationKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            AnimationName = "MissingAnim",
            HasValidState = false,
        };

        keyframe.IsMissingReference.ShouldBeTrue();
    }

    [Fact]
    public void IsMissingReference_IsTrue_ForInvalidStateKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            StateName = "Cat/Missing",
            HasValidState = false,
        };

        keyframe.IsMissingReference.ShouldBeTrue();
    }

    [Fact]
    public void MissingReferenceMessage_NamesAnimation_ForAnimationKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            AnimationName = "Spin",
        };

        keyframe.MissingReferenceMessage.ShouldBe("Could not find animation \"Spin\"");
    }

    [Fact]
    public void MissingReferenceMessage_NamesCategoryAndState_ForCategorizedState()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            StateName = "ButtonStates/Highlighted",
        };

        keyframe.MissingReferenceMessage.ShouldBe("Could not find state \"Highlighted\" in category \"ButtonStates\"");
    }

    [Fact]
    public void MissingReferenceMessage_NamesStateOnly_ForUncategorizedState()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            StateName = "Highlighted",
        };

        keyframe.MissingReferenceMessage.ShouldBe("Could not find state \"Highlighted\"");
    }

    [Fact]
    public void ShowInvalidStateWarning_IsFalse_ForEventKeyframe()
    {
        // A named event is never considered broken, even with HasValidState false (its default).
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            EventName = "OnSomething",
        };

        keyframe.ShowInvalidStateWarning.ShouldBeFalse();
    }

    [Fact]
    public void ShowInvalidStateWarning_IsTrue_ForInvalidStateKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel
        {
            StateName = "Cat/Missing",
            HasValidState = false,
        };

        keyframe.ShowInvalidStateWarning.ShouldBeTrue();
    }
}
