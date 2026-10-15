using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics.Animation;

// Ported from FlatRedBall2's KeyframeEvaluatorTests and EffectiveFrameColorTests (#1478). Every
// frame is a keyframe, a keyframe's curve runs to the next keyframe of the same group, and snap
// (the default) reproduces the behavior from before interpolation existed.
public class KeyframeEvaluatorTests
{
    private static AnimationFrame Frame(float x = 0, float y = 0, float length = 1f,
        string? offsetType = null, string? offsetEasing = null, bool skipOffset = false)
        => new()
        {
            FrameLength = length,
            RelativeX = x,
            RelativeY = y,
            OffsetInterpolation = offsetType,
            OffsetEasing = offsetEasing,
            SkipOffsetKeyframe = skipOffset,
        };

    private static AnimationChain Chain(params AnimationFrame[] frames)
    {
        AnimationChain chain = new() { Name = "c" };
        chain.AddRange(frames);
        return chain;
    }

    // ---- Offset: snap is today's behavior ----

    [Fact]
    public void EvaluateOffset_DefaultFrames_ReturnsFrameOwnOffsetAtAnyElapsed()
    {
        AnimationChain chain = Chain(Frame(10, 20), Frame(50, 60), Frame(-5, 7));

        for (int i = 0; i < 3; i++)
        {
            foreach (double elapsed in new[] { 0.0, 0.3, 0.999 })
            {
                (float x, float y) = KeyframeEvaluator.EvaluateOffset(chain, loop: true, i, elapsed);
                x.ShouldBe(chain[i].RelativeX);
                y.ShouldBe(chain[i].RelativeY);
            }
        }
    }

    [Fact]
    public void EvaluateOffset_ExplicitInstant_BehavesAsSnap()
    {
        AnimationChain chain = Chain(Frame(0, offsetType: "Instant"), Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.99).X.ShouldBe(0f);
    }

    [Fact]
    public void EvaluateOffset_SnapAtSegmentEnd_StillHoldsStartValue()
    {
        AnimationChain chain = Chain(Frame(10), Frame(90));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 1.0).X.ShouldBe(10f);
    }

    // ---- Offset: glide ----

    [Fact]
    public void EvaluateOffset_LinearToNextFrame_ReturnsMidpointAtHalfway()
    {
        AnimationChain chain = Chain(Frame(0, 10, offsetType: "Linear"), Frame(100, 30));

        (float x, float y) = KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.5);

        x.ShouldBe(50f, 0.001f);
        y.ShouldBe(20f, 0.001f);
    }

    [Fact]
    public void EvaluateOffset_ArrivesExactlyAtNextKeyframeValue()
    {
        AnimationChain chain = Chain(Frame(0, offsetType: "Linear"), Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 1, 0).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_UnequalFrameLengths_UsesOwnFrameLength()
    {
        AnimationChain chain = Chain(Frame(0, length: 4f, offsetType: "Linear"), Frame(100, length: 1f));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 1.0).X.ShouldBe(25f, 0.001f);
    }

    // ---- Offset: skipped frames ----

    [Fact]
    public void EvaluateOffset_SkippedFrameBetween_GlideSpansSummedFrameLengths()
    {
        AnimationChain chain = Chain(
            Frame(0, offsetType: "Linear"),
            Frame(skipOffset: true),
            Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.5).X.ShouldBe(25f, 0.001f);
        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 1, 0.5).X.ShouldBe(75f, 0.001f);
        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 2, 0).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_SkippedFrame_OwnValueAndCurveAreIgnored()
    {
        AnimationChain chain = Chain(
            Frame(0, offsetType: "Linear"),
            Frame(999, 999, offsetType: "Cubic", offsetEasing: "In", skipOffset: true),
            Frame(100));

        (float x, float y) = KeyframeEvaluator.EvaluateOffset(chain, loop: true, 1, 0.5);

        x.ShouldBe(75f, 0.001f);
        y.ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void EvaluateOffset_SkippedFramesAfterSnapKeyframe_HoldStartThenSnapAtNextKeyframe()
    {
        AnimationChain chain = Chain(
            Frame(10),
            Frame(skipOffset: true),
            Frame(skipOffset: true),
            Frame(90));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 1, 0.9).X.ShouldBe(10f);
        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 2, 0.9).X.ShouldBe(10f);
        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 3, 0).X.ShouldBe(90f);
    }

    [Fact]
    public void EvaluateOffset_FirstFrameMarkedSkipped_StillActsAsKeyframe()
    {
        AnimationChain chain = Chain(Frame(0, offsetType: "Linear", skipOffset: true), Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.5).X.ShouldBe(50f, 0.001f);
    }

    // ---- Offset: ends of the chain ----

    [Fact]
    public void EvaluateOffset_LoopingChain_LastKeyframeGlidesBackToFirst()
    {
        AnimationChain chain = Chain(Frame(-10), Frame(10, offsetType: "Linear"));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 1, 0.5).X.ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void EvaluateOffset_LoopingChain_TrailingSkippedFramesAreCoveredByWrapGlide()
    {
        AnimationChain chain = Chain(
            Frame(0),
            Frame(100, offsetType: "Linear"),
            Frame(skipOffset: true));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 1, 0.5).X.ShouldBe(75f, 0.001f);
        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 2, 0.5).X.ShouldBe(25f, 0.001f);
    }

    [Fact]
    public void EvaluateOffset_NonLoopingChain_LastFrameHolds()
    {
        AnimationChain chain = Chain(Frame(0, offsetType: "Linear"), Frame(100, offsetType: "Linear"));

        KeyframeEvaluator.EvaluateOffset(chain, loop: false, 1, 0.5).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_NonLoopingChain_TrailingSkippedFramesHoldLastKeyframe()
    {
        AnimationChain chain = Chain(
            Frame(0),
            Frame(100, offsetType: "Linear"),
            Frame(skipOffset: true));

        KeyframeEvaluator.EvaluateOffset(chain, loop: false, 2, 0.5).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_LoopingSingleKeyframeChain_Holds()
    {
        AnimationChain chain = Chain(Frame(7, 9, offsetType: "Linear"));

        (float x, float y) = KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.5);

        x.ShouldBe(7f);
        y.ShouldBe(9f);
    }

    [Fact]
    public void EvaluateOffset_LoopingChainWithOneKeyframeAndSkippedFrames_Holds()
    {
        AnimationChain chain = Chain(Frame(7, offsetType: "Linear"), Frame(skipOffset: true));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 1, 0.5).X.ShouldBe(7f);
    }

    // ---- Offset: easing ----

    [Theory]
    [InlineData("In", 25f)]
    [InlineData("Out", 75f)]
    [InlineData("InOut", 50f)]
    [InlineData(null, 75f)]
    [InlineData("NotAnEasing", 75f)]
    public void EvaluateOffset_QuadraticAtHalfway_FollowsEasing(string? easing, float expected)
    {
        AnimationChain chain = Chain(Frame(0, offsetType: "Quadratic", offsetEasing: easing), Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.5).X.ShouldBe(expected, 0.001f);
    }

    [Theory]
    [InlineData("In")]
    [InlineData("Out")]
    [InlineData("InOut")]
    public void EvaluateOffset_LinearIgnoresEasing(string easing)
    {
        AnimationChain chain = Chain(Frame(0, offsetType: "Linear", offsetEasing: easing), Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.25).X.ShouldBe(25f, 0.001f);
    }

    [Theory]
    [InlineData("In")]
    [InlineData("Out")]
    [InlineData("InOut")]
    public void EvaluateOffset_InstantIgnoresEasing_HoldsStartUntilSegmentEnd(string easing)
    {
        AnimationChain chain = Chain(Frame(0, offsetType: "Instant", offsetEasing: easing), Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.999).X.ShouldBe(0f);
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("")]
    [InlineData("3")]
    public void EvaluateOffset_UnknownCurveName_TreatedAsSnap(string type)
    {
        AnimationChain chain = Chain(Frame(0, offsetType: type), Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.5).X.ShouldBe(0f);
    }

    [Fact]
    public void EvaluateOffset_EasingWithoutType_StaysSnap()
    {
        AnimationChain chain = Chain(Frame(0, offsetEasing: "In"), Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.5).X.ShouldBe(0f);
    }

    [Fact]
    public void EvaluateOffset_OvershootingCurve_IsExactAtEndpoints()
    {
        AnimationChain chain = Chain(Frame(0, offsetType: "Back", offsetEasing: "Out"), Frame(100));

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0).X.ShouldBe(0f);
        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 1.0).X.ShouldBe(100f);
    }

    [Fact]
    public void EvaluateOffset_EmptyChain_ReturnsZero()
    {
        KeyframeEvaluator.EvaluateOffset(Chain(), loop: true, 0, 0).ShouldBe((0f, 0f));
    }

    // ---- Color ----

    private static AnimationFrame ColorFrame(int? red = null, int? green = null, int? alpha = null,
        string? type = null, string? easing = null, bool skip = false,
        AnimationFrameColorOperation? op = null)
        => new()
        {
            FrameLength = 1f,
            Red = red,
            Green = green,
            Alpha = alpha,
            ColorInterpolation = type,
            ColorEasing = easing,
            SkipColorKeyframe = skip,
            ColorOperation = op,
        };

    [Fact]
    public void EvaluateColor_DefaultFrames_ReturnsResolvedFrameColor()
    {
        AnimationChain chain = Chain(
            ColorFrame(red: 10, alpha: 200),
            ColorFrame(green: 40));

        KeyframeColor c0 = KeyframeEvaluator.EvaluateColor(chain, loop: true, 0, 0.7);
        c0.Red.ShouldBe(10);
        c0.Green.ShouldBeNull();
        c0.Alpha.ShouldBe(200);

        KeyframeColor c1 = KeyframeEvaluator.EvaluateColor(chain, loop: true, 1, 0.7);
        c1.Red.ShouldBe(10);
        c1.Green.ShouldBe(40);
        c1.Alpha.ShouldBe(200);
    }

    [Fact]
    public void EvaluateColor_ChannelsInterpolateIndependently()
    {
        AnimationChain chain = Chain(
            ColorFrame(red: 0, green: 0, type: "Linear"),
            ColorFrame(red: 100, green: 200));

        KeyframeColor c = KeyframeEvaluator.EvaluateColor(chain, loop: true, 0, 0.5);

        c.Red.ShouldBe(50);
        c.Green.ShouldBe(100);
    }

    [Fact]
    public void EvaluateColor_NullChannelOnKeyframeInheritsBeforeInterpolating()
    {
        AnimationChain chain = Chain(
            ColorFrame(red: 0),
            ColorFrame(type: "Linear"),
            ColorFrame(red: 100));

        KeyframeEvaluator.EvaluateColor(chain, loop: true, 1, 0.5).Red.ShouldBe(50);
    }

    [Fact]
    public void EvaluateColor_ChannelNullAtBothEnds_StaysNull()
    {
        AnimationChain chain = Chain(ColorFrame(red: 0, type: "Linear"), ColorFrame(red: 100));

        KeyframeEvaluator.EvaluateColor(chain, loop: true, 0, 0.5).Green.ShouldBeNull();
    }

    [Fact]
    public void EvaluateColor_ChannelNullAtOneEnd_UsesOperationIdentityAndRoundsAwayFromZero()
    {
        // Unset Alpha reads as 255, so 255 -> 100 at the midpoint is 177.5, which rounds to 178.
        AnimationChain chain = Chain(ColorFrame(type: "Linear"), ColorFrame(alpha: 100));

        KeyframeEvaluator.EvaluateColor(chain, loop: true, 0, 0.5).Alpha.ShouldBe(178);
    }

    [Fact]
    public void EvaluateColor_AddOperationNullChannel_UsesZeroIdentity()
    {
        AnimationChain chain = Chain(
            ColorFrame(op: AnimationFrameColorOperation.Add, type: "Linear"),
            ColorFrame(red: 100));

        KeyframeEvaluator.EvaluateColor(chain, loop: true, 0, 0.5).Red.ShouldBe(50);
    }

    [Fact]
    public void EvaluateColor_SkippedFrame_GlideSpansItAndItsOwnValueIsIgnored()
    {
        AnimationChain chain = Chain(
            ColorFrame(red: 0, type: "Linear"),
            ColorFrame(red: 255, skip: true),
            ColorFrame(red: 100));

        KeyframeEvaluator.EvaluateColor(chain, loop: true, 1, 0.5).Red.ShouldBe(75);
    }

    [Fact]
    public void EvaluateColor_SkippedFrameCurve_IsIgnored()
    {
        AnimationChain chain = Chain(
            ColorFrame(red: 0, type: "Linear"),
            ColorFrame(type: "Cubic", easing: "In", skip: true),
            ColorFrame(red: 100));

        KeyframeEvaluator.EvaluateColor(chain, loop: true, 1, 0.5).Red.ShouldBe(75);
    }

    [Fact]
    public void EvaluateColor_KeyframeAfterSkippedFrame_DoesNotInheritSkippedFramesChannel()
    {
        AnimationChain chain = Chain(
            ColorFrame(red: 20),
            ColorFrame(red: 255, skip: true),
            ColorFrame());

        KeyframeEvaluator.EvaluateColor(chain, loop: false, 2, 0).Red.ShouldBe(20);
    }

    [Fact]
    public void EvaluateColor_FirstFrameMarkedSkipped_StillContributesChannels()
    {
        AnimationChain chain = Chain(ColorFrame(red: 20, skip: true), ColorFrame());

        KeyframeEvaluator.EvaluateColor(chain, loop: false, 1, 0).Red.ShouldBe(20);
    }

    [Fact]
    public void EvaluateColor_SkippedFrameColorOperation_StillInherits()
    {
        AnimationChain chain = Chain(
            ColorFrame(op: AnimationFrameColorOperation.Multiply),
            ColorFrame(op: AnimationFrameColorOperation.Add, skip: true),
            ColorFrame());

        KeyframeEvaluator.EvaluateColor(chain, loop: false, 1, 0).Operation.ShouldBe(AnimationFrameColorOperation.Add);
        KeyframeEvaluator.EvaluateColor(chain, loop: false, 2, 0).Operation.ShouldBe(AnimationFrameColorOperation.Add);
    }

    [Fact]
    public void EvaluateColor_OffsetAndColorGroupsAreIndependent()
    {
        AnimationChain chain = Chain(
            new AnimationFrame { FrameLength = 1f, OffsetInterpolation = "Linear", ColorInterpolation = "Linear", Red = 0 },
            new AnimationFrame { FrameLength = 1f, RelativeX = 100, Red = 100, SkipColorKeyframe = true },
            new AnimationFrame { FrameLength = 1f, RelativeX = 200, Red = 200 });

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 1, 0.5).X.ShouldBe(100f);
        KeyframeEvaluator.EvaluateColor(chain, loop: true, 1, 0.5).Red.ShouldBe(150);
    }

    [Fact]
    public void EvaluateColor_LoopingLastKeyframeGlidesToFirst()
    {
        AnimationChain chain = Chain(ColorFrame(alpha: 0), ColorFrame(alpha: 200, type: "Linear"));

        KeyframeEvaluator.EvaluateColor(chain, loop: true, 1, 0.5).Alpha.ShouldBe(100);
    }

    [Fact]
    public void EvaluateColor_NonLoopingLastFrameHolds()
    {
        AnimationChain chain = Chain(ColorFrame(alpha: 0, type: "Linear"), ColorFrame(alpha: 200, type: "Linear"));

        KeyframeEvaluator.EvaluateColor(chain, loop: false, 1, 0.5).Alpha.ShouldBe(200);
    }

    [Fact]
    public void EvaluateColor_OvershootingCurve_IsExactAtEndpoints()
    {
        AnimationChain chain = Chain(
            ColorFrame(alpha: 0, type: "Back", easing: "Out"),
            ColorFrame(alpha: 255));

        KeyframeEvaluator.EvaluateColor(chain, loop: true, 0, 0).Alpha.ShouldBe(0);
        KeyframeEvaluator.EvaluateColor(chain, loop: true, 0, 1.0).Alpha.ShouldBe(255);
    }

    // ---- Detecting keyframe data ----

    [Fact]
    public void HasOffsetKeyframes_PlainChain_IsFalse()
    {
        KeyframeEvaluator.HasOffsetKeyframes(Chain(Frame(1), Frame(2))).ShouldBeFalse();
    }

    [Fact]
    public void HasOffsetKeyframes_AnyCurveOrSkip_IsTrue()
    {
        KeyframeEvaluator.HasOffsetKeyframes(Chain(Frame(1, offsetType: "Linear"), Frame(2))).ShouldBeTrue();
        KeyframeEvaluator.HasOffsetKeyframes(Chain(Frame(1), Frame(2, skipOffset: true))).ShouldBeTrue();
    }

    [Fact]
    public void HasOffsetKeyframes_IgnoresUnknownCurveName()
    {
        KeyframeEvaluator.HasOffsetKeyframes(Chain(Frame(1, offsetType: "Bogus"), Frame(2))).ShouldBeFalse();
    }

    [Fact]
    public void HasColorKeyframes_AnyCurveOrSkip_IsTrueAndIgnoresOffsetFields()
    {
        KeyframeEvaluator.HasColorKeyframes(Chain(Frame(1, offsetType: "Linear", skipOffset: true))).ShouldBeFalse();
        KeyframeEvaluator.HasColorKeyframes(Chain(ColorFrame(red: 1, type: "Linear"), ColorFrame())).ShouldBeTrue();
        KeyframeEvaluator.HasColorKeyframes(Chain(ColorFrame(red: 1), ColorFrame(skip: true))).ShouldBeTrue();
    }

    [Fact]
    public void HasColorKeyframes_FirstFrameSkipFlagAlone_IsFalse()
    {
        // The first frame is always a keyframe, so its skip flag changes nothing.
        KeyframeEvaluator.HasColorKeyframes(Chain(ColorFrame(red: 1, skip: true), ColorFrame())).ShouldBeFalse();
    }
}
