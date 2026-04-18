using Gum.Graphics.Animation;
using Gum.Renderables;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using SokolGum;

namespace SokolGum.Tests.Animation;

/// <summary>
/// Covers the shared <see cref="SpriteAnimationLogic"/> playback state machine
/// as composed into SokolGum's <see cref="Sprite"/>. Exercises only the parts
/// that don't require a GPU — frame advance from tick, ApplyFrame side-effects
/// (flip flags, source rect), looping, and non-looping clamp-to-end.
/// </summary>
public class SpriteAnimationLogicTests : BaseTestClass
{
    private static AnimationChainList BuildChain(float frameLength = 0.1f, int frameCount = 3)
    {
        var chain = new AnimationChain { Name = "idle" };
        for (int i = 0; i < frameCount; i++)
        {
            // No Texture on the frame — ApplyAnimationFrame's tex-branch is
            // skipped and SourceRectangle is reset to null. That keeps the
            // test independent of GPU resource creation (Texture2D ctor
            // touches sokol_gfx).
            chain.Add(new AnimationFrame
            {
                FrameLength = frameLength,
                FlipHorizontal = i % 2 == 1,
                FlipVertical = false,
            });
        }
        return new AnimationChainList { chain };
    }

    [Fact]
    public void AnimateSelf_WithoutAnimateFlag_ShouldNotAdvance()
    {
        var sprite = new Sprite
        {
            AnimationChains = BuildChain(),
            CurrentChainName = "idle",
            Animate = false,
            Visible = true,
        };
        var advanced = sprite.AnimateSelf(1.0);
        advanced.ShouldBeFalse();
        sprite.AnimationLogic.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void AnimateSelf_AfterFrameLength_ShouldAdvanceToNextFrame()
    {
        var sprite = new Sprite
        {
            AnimationChains = BuildChain(frameLength: 0.1f),
            CurrentChainName = "idle",
            Animate = true,
            Visible = true,
        };
        // Tick just past the first frame's 0.1s window.
        sprite.AnimateSelf(0.11);
        sprite.AnimationLogic.CurrentFrameIndex.ShouldBe(1);
    }

    [Fact]
    public void AnimateSelf_ShouldInvokeApplyFrame_PropagatingFlipFlags()
    {
        var sprite = new Sprite
        {
            AnimationChains = BuildChain(frameLength: 0.1f),
            CurrentChainName = "idle",
            Animate = true,
            Visible = true,
        };
        // Frame 1 in BuildChain has FlipHorizontal = true.
        sprite.AnimateSelf(0.11);
        sprite.FlipHorizontal.ShouldBeTrue();
    }

    [Fact]
    public void AnimateSelf_ShouldLoopBackToFirstFrame_WhenPastEnd()
    {
        var sprite = new Sprite
        {
            AnimationChains = BuildChain(frameLength: 0.1f, frameCount: 3),
            CurrentChainName = "idle",
            Animate = true,
            Visible = true,
        };
        sprite.AnimationLogic.IsAnimationChainLooping = true;
        // Tick past total length (3 * 0.1s = 0.3s).
        sprite.AnimateSelf(0.35);
        sprite.AnimationLogic.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void AnimateSelf_NonLooping_ShouldStopAfterPastEnd()
    {
        // Non-looping playback disables the Animate flag once the playhead
        // passes TotalLength — subsequent ticks are no-ops. (The shared logic
        // does clamp TimeIntoAnimation to TotalLength but then wraps the
        // frame index via UpdateFrameBasedOffOfTimeIntoAnimation, so the
        // observable contract is "Animate goes false" rather than
        // "frame index pinned to last frame".)
        var sprite = new Sprite
        {
            AnimationChains = BuildChain(frameLength: 0.1f, frameCount: 3),
            CurrentChainName = "idle",
            Animate = true,
            Visible = true,
        };
        sprite.AnimationLogic.IsAnimationChainLooping = false;
        sprite.AnimateSelf(1.0);
        sprite.Animate.ShouldBeFalse();

        // Further ticks shouldn't change state — playback is terminated.
        var frameAfter = sprite.AnimationLogic.CurrentFrameIndex;
        sprite.AnimateSelf(1.0).ShouldBeFalse();
        sprite.AnimationLogic.CurrentFrameIndex.ShouldBe(frameAfter);
    }

    [Fact]
    public void AnimateSelf_HiddenSprite_ShouldNotAdvance()
    {
        // Sprite.AnimateSelf bails early when !Visible — pairs with
        // Renderer.TickRecursively skipping the whole invisible subtree.
        var sprite = new Sprite
        {
            AnimationChains = BuildChain(),
            CurrentChainName = "idle",
            Animate = true,
            Visible = false,
        };
        var advanced = sprite.AnimateSelf(1.0);
        advanced.ShouldBeFalse();
        sprite.AnimationLogic.CurrentFrameIndex.ShouldBe(0);
    }

    [Fact]
    public void AnimationSpeed_ShouldScaleTickAdvance()
    {
        var sprite = new Sprite
        {
            AnimationChains = BuildChain(frameLength: 0.1f),
            CurrentChainName = "idle",
            Animate = true,
            Visible = true,
            AnimationSpeed = 2f,
        };
        // At 2x speed, 0.06s elapsed == 0.12s animation time → past frame 0.
        sprite.AnimateSelf(0.06);
        sprite.AnimationLogic.CurrentFrameIndex.ShouldBe(1);
    }

    [Fact]
    public void AnimationChainCycled_ShouldFire_OnLoopWrap()
    {
        var sprite = new Sprite
        {
            AnimationChains = BuildChain(frameLength: 0.1f, frameCount: 2),
            CurrentChainName = "idle",
            Animate = true,
            Visible = true,
        };
        int cycles = 0;
        sprite.AnimationLogic.AnimationChainCycled += () => cycles++;
        // Tick past one full chain (2 * 0.1 = 0.2s).
        sprite.AnimateSelf(0.25);
        cycles.ShouldBe(1);
    }
}
