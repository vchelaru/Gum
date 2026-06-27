using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using StateAnimationPlugin.ViewModels;
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
}
