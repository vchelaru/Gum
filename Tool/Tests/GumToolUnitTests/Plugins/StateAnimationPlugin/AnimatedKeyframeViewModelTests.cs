using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Pins <see cref="AnimatedKeyframeViewModel.IsMissingReference"/>, the flag the keyframe-list
/// icon uses to mark a keyframe that references a state/animation which no longer exists (issue
/// #3386). Mirrors the broken-keyframe logic in <c>AnimationViewModel.GetErrors</c>: a keyframe is
/// broken only when it points at a state/animation (not a named event) and its reference is invalid.
/// </summary>
public class AnimatedKeyframeViewModelTests : BaseTestClass
{
    private readonly IBitmapLoader _bitmapLoader = Mock.Of<IBitmapLoader>();

    [Fact]
    public void ChangingHasValidState_RaisesPropertyChanged_ForIsMissingReference()
    {
        // The keyframe-list icon reacts to error recomputation only because HasValidState now
        // notifies and IsMissingReference depends on it; this pins that reactive contract.
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel(_bitmapLoader)
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
    public void IsMissingReference_IsFalse_ForEventKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel(_bitmapLoader)
        {
            EventName = "OnSomething",
            HasValidState = false,
        };

        keyframe.IsMissingReference.ShouldBeFalse();
    }

    [Fact]
    public void IsMissingReference_IsFalse_ForValidStateKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel(_bitmapLoader)
        {
            StateName = "Cat/Idle",
            HasValidState = true,
        };

        keyframe.IsMissingReference.ShouldBeFalse();
    }

    [Fact]
    public void IsMissingReference_IsTrue_ForInvalidAnimationKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel(_bitmapLoader)
        {
            AnimationName = "MissingAnim",
            HasValidState = false,
        };

        keyframe.IsMissingReference.ShouldBeTrue();
    }

    [Fact]
    public void IsMissingReference_IsTrue_ForInvalidStateKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel(_bitmapLoader)
        {
            StateName = "Cat/Missing",
            HasValidState = false,
        };

        keyframe.IsMissingReference.ShouldBeTrue();
    }

    [Fact]
    public void MissingReferenceMessage_NamesAnimation_ForAnimationKeyframe()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel(_bitmapLoader)
        {
            AnimationName = "Spin",
        };

        keyframe.MissingReferenceMessage.ShouldBe("Could not find animation \"Spin\"");
    }

    [Fact]
    public void MissingReferenceMessage_NamesCategoryAndState_ForCategorizedState()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel(_bitmapLoader)
        {
            StateName = "ButtonStates/Highlighted",
        };

        keyframe.MissingReferenceMessage.ShouldBe("Could not find state \"Highlighted\" in category \"ButtonStates\"");
    }

    [Fact]
    public void MissingReferenceMessage_NamesStateOnly_ForUncategorizedState()
    {
        AnimatedKeyframeViewModel keyframe = new AnimatedKeyframeViewModel(_bitmapLoader)
        {
            StateName = "Highlighted",
        };

        keyframe.MissingReferenceMessage.ShouldBe("Could not find state \"Highlighted\"");
    }
}
