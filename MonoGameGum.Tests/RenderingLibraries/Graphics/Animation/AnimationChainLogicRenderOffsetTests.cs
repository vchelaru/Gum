using Gum.Graphics.Animation;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics.Animation;

/// <summary>
/// The per-frame RelativeX/RelativeY an animated Sprite draws at: authored Y-up, rotated with the
/// sprite, returned in Y-down screen axes. Every backend's Sprite shares this (#6034).
/// </summary>
public class AnimationChainLogicRenderOffsetTests
{
    private static AnimationChainLogic PlayWithOffset(float x, float y)
    {
        AnimationChain chain = new() { Name = "c" };
        chain.Add(new AnimationFrame { FrameLength = 1f, RelativeX = x, RelativeY = y });
        AnimationChainLogic logic = new() { AnimationChains = new AnimationChainList { chain }, Animate = true };
        logic.CurrentChainName = "c";
        return logic;
    }

    [Fact]
    public void GetRenderOffset_NoRotation_NegatesYForYDownScreen()
    {
        AnimationChainLogic sut = PlayWithOffset(10, 5);

        (float X, float Y) offset = sut.GetRenderOffset(0);

        offset.X.ShouldBe(10f, 0.001f);
        offset.Y.ShouldBe(-5f, 0.001f);
    }

    [Fact]
    public void GetRenderOffset_Rotated90_RotatesOffsetWithTheSprite()
    {
        AnimationChainLogic sut = PlayWithOffset(10, 0);

        (float X, float Y) offset = sut.GetRenderOffset(90);

        // The sprite's own +X axis points up the screen after a 90 degree (counter-clockwise) turn.
        offset.X.ShouldBe(0f, 0.001f);
        offset.Y.ShouldBe(-10f, 0.001f);
    }

    [Fact]
    public void GetRenderOffset_NoChain_IsZero()
    {
        AnimationChainLogic sut = new();

        sut.GetRenderOffset(45).ShouldBe((0f, 0f));
    }
}
