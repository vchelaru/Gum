using System.Collections.Generic;
using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics.Animation;

// Playback side of keyframe interpolation: while a glide is active the offset and color change
// every update, not only when the frame changes.
public class AnimationChainLogicKeyframeTests
{
    private static AnimationChainList ListOf(bool loop, params AnimationFrame[] frames)
    {
        AnimationChain chain = new() { Name = "c", Loop = loop };
        chain.AddRange(frames);
        return new AnimationChainList { chain };
    }

    private static AnimationFrame OffsetFrame(float x, string? type = null, bool skip = false, float length = 1f)
        => new() { FrameLength = length, RelativeX = x, OffsetInterpolation = type, SkipOffsetKeyframe = skip };

    private static AnimationFrame ColorFrame(int? alpha = null, int? red = null, string? type = null,
        bool skip = false, AnimationFrameColorOperation? op = null, float length = 1f)
        => new()
        {
            FrameLength = length,
            Alpha = alpha,
            Red = red,
            ColorInterpolation = type,
            SkipColorKeyframe = skip,
            ColorOperation = op,
        };

    private static AnimationChainLogic Play(AnimationChainList chains)
    {
        AnimationChainLogic logic = new() { AnimationChains = chains, Animate = true };
        logic.CurrentChainName = "c";
        return logic;
    }

    // ---- Offset ----

    [Fact]
    public void CurrentOffset_PlainChain_IsCurrentFrameRelativeOffsetMidFrame()
    {
        AnimationChainLogic logic = Play(ListOf(true, OffsetFrame(10), OffsetFrame(50)));

        logic.AnimateSelf(1.5);

        logic.CurrentOffset.X.ShouldBe(50f);
    }

    [Fact]
    public void CurrentOffset_LiveEditOfPlainFrame_IsReflectedWithoutReapplyingTheFrame()
    {
        AnimationChainList chains = ListOf(true, OffsetFrame(10), OffsetFrame(50));
        AnimationChainLogic logic = Play(chains);

        chains[0][0].RelativeX = 77;

        logic.CurrentOffset.X.ShouldBe(77f);
    }

    [Fact]
    public void CurrentOffset_LinearGlide_ChangesEveryUpdateMidFrame()
    {
        AnimationChainLogic logic = Play(ListOf(true, OffsetFrame(0, "Linear"), OffsetFrame(100)));

        logic.AnimateSelf(0.25);
        logic.CurrentOffset.X.ShouldBe(25f, 0.001f);

        logic.AnimateSelf(0.25);
        logic.CurrentOffset.X.ShouldBe(50f, 0.001f);
    }

    [Fact]
    public void CurrentOffset_GlideAcrossSkippedFrame_SpansBothFrames()
    {
        AnimationChainLogic logic = Play(ListOf(true,
            OffsetFrame(0, "Linear"), OffsetFrame(999, skip: true), OffsetFrame(100)));

        logic.AnimateSelf(1.5);

        logic.CurrentOffset.X.ShouldBe(75f, 0.001f);
    }

    [Fact]
    public void CurrentOffset_LoopingLastKeyframe_GlidesBackToFirst()
    {
        AnimationChainLogic logic = Play(ListOf(true, OffsetFrame(-10), OffsetFrame(10, "Linear")));

        logic.AnimateSelf(1.5);

        logic.CurrentOffset.X.ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void CurrentOffset_NonLoopingChainFinished_HoldsLastKeyframe()
    {
        AnimationChainLogic logic = Play(ListOf(false, OffsetFrame(0, "Linear"), OffsetFrame(100, "Linear")));

        logic.AnimateSelf(5);

        logic.CurrentOffset.X.ShouldBe(100f);
    }

    [Fact]
    public void CurrentOffset_MillisecondChain_UsesSecondsForBothLengthsAndElapsed()
    {
        AnimationChainSave save = new() { Name = "c" };
        save.Frames.Add(new AnimationFrameSave { FrameLength = 1000, OffsetInterpolation = "Linear" });
        save.Frames.Add(new AnimationFrameSave { FrameLength = 1000, RelativeX = 100 });
        AnimationChainLogic logic = Play(new AnimationChainList { save.ToAnimationChain(TimeMeasurementUnit.Millisecond) });

        logic.AnimateSelf(0.5);

        logic.CurrentOffset.X.ShouldBe(50f, 0.001f);
    }

    [Fact]
    public void CurrentOffset_NoChain_IsZero()
    {
        new AnimationChainLogic().CurrentOffset.ShouldBe((0f, 0f));
    }

    // ---- Color ----

    [Fact]
    public void ApplyKeyframeColor_PlainChain_IsNeverInvoked()
    {
        AnimationChainLogic logic = Play(ListOf(true, ColorFrame(alpha: 0), ColorFrame(alpha: 255)));
        int calls = 0;
        logic.ApplyKeyframeColor = _ => calls++;

        logic.AnimateSelf(0.5);
        logic.AnimateSelf(0.7);

        calls.ShouldBe(0);
        logic.IsColorDrivenByKeyframes.ShouldBeFalse();
    }

    [Fact]
    public void ApplyKeyframeColor_Glide_IsInvokedEveryUpdateWithInterpolatedValue()
    {
        AnimationChainLogic logic = Play(ListOf(true, ColorFrame(alpha: 0, type: "Linear"), ColorFrame(alpha: 200)));
        List<int?> alphas = new();
        logic.ApplyKeyframeColor = color => alphas.Add(color.Alpha);

        logic.AnimateSelf(0.25);
        logic.AnimateSelf(0.25);

        logic.IsColorDrivenByKeyframes.ShouldBeTrue();
        alphas.ShouldBe(new int?[] { 50, 100 });
    }

    [Fact]
    public void ApplyKeyframeColor_FrameChange_RunsAfterApplyFrame()
    {
        AnimationChainLogic logic = Play(ListOf(true, ColorFrame(alpha: 0, type: "Linear"), ColorFrame(alpha: 200)));
        List<string> order = new();
        logic.ApplyFrame = _ => order.Add("frame");
        logic.ApplyKeyframeColor = _ => order.Add("color");

        logic.AnimateSelf(1.0);

        order.ShouldBe(new[] { "frame", "color" });
    }

    [Fact]
    public void ApplyKeyframeColor_SetCurrentFrameIndex_AppliesColorForThatFrame()
    {
        AnimationChainLogic logic = Play(ListOf(true, ColorFrame(alpha: 10, type: "Linear"), ColorFrame(alpha: 200)));
        int? alpha = null;
        logic.ApplyKeyframeColor = color => alpha = color.Alpha;

        logic.CurrentFrameIndex = 1;

        alpha.ShouldBe(200);
    }

    // ---- Sprite ----

    [Fact]
    public void Sprite_AlphaGlide_ChangesMidFrame()
    {
        Sprite sprite = new(null)
        {
            AnimationChains = ListOf(true, ColorFrame(alpha: 0, type: "Linear"), ColorFrame(alpha: 200)),
            CurrentChainName = "c",
            Animate = true,
        };

        sprite.AnimateSelf(0.5);

        sprite.Alpha.ShouldBe(100);
    }

    [Fact]
    public void Sprite_ColorGlide_InterpolatesChannelsUnderAnOperation()
    {
        Sprite sprite = new(null)
        {
            AnimationChains = ListOf(true,
                ColorFrame(red: 0, type: "Linear", op: AnimationFrameColorOperation.Multiply),
                ColorFrame(red: 200)),
            CurrentChainName = "c",
            Animate = true,
        };

        sprite.AnimateSelf(0.5);

        sprite.Red.ShouldBe(100);
        sprite.ColorOperation.ShouldBe(ColorOperation.Modulate);
    }

    [Fact]
    public void Sprite_AddOperation_UnsetChannelsGlideFromZero()
    {
        Sprite sprite = new(null)
        {
            AnimationChains = ListOf(true,
                ColorFrame(type: "Linear", op: AnimationFrameColorOperation.Add),
                ColorFrame(red: 100)),
            CurrentChainName = "c",
            Animate = true,
        };

        sprite.AnimateSelf(0.5);

        sprite.Red.ShouldBe(50);
        sprite.ColorOperation.ShouldBe(ColorOperation.Add);
    }

    [Fact]
    public void Sprite_SkippedFrameAlpha_IsNotAppliedWhenNothingGlides()
    {
        Sprite sprite = new(null)
        {
            AnimationChains = ListOf(true, ColorFrame(alpha: 40), ColorFrame(alpha: 255, skip: true), ColorFrame(alpha: 90)),
            CurrentChainName = "c",
            Animate = true,
        };

        sprite.AnimateSelf(1.5);

        sprite.Alpha.ShouldBe(40);
    }

    [Fact]
    public void Sprite_SkippedFrameWithAuthoredAlphaButNoKeyframeAlpha_DoesNotLeakItsAlpha()
    {
        Sprite sprite = new(null)
        {
            AnimationChains = ListOf(true, ColorFrame(red: 1), ColorFrame(alpha: 30, skip: true), ColorFrame(red: 2)),
            CurrentChainName = "c",
            Animate = true,
        };
        sprite.Alpha = 255;

        sprite.AnimateSelf(1.5);

        sprite.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Sprite_PlainChain_AppliesFrameColorOnlyOnFrameChange()
    {
        Sprite sprite = new(null)
        {
            AnimationChains = ListOf(true, ColorFrame(alpha: 0), ColorFrame(alpha: 200)),
            CurrentChainName = "c",
            Animate = true,
        };
        sprite.Alpha = 255;

        sprite.AnimateSelf(0.5);
        sprite.Alpha.ShouldBe(255);

        sprite.AnimateSelf(0.6);
        sprite.Alpha.ShouldBe(200);
    }
}
