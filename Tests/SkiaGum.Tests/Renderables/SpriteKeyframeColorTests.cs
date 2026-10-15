using Gum.Graphics.Animation;
using Shouldly;
using SkiaGum.Renderables;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A color curve on an animation chain changes the sprite's alpha every update, not only on
/// frame change, and a clone keeps doing so.
/// </summary>
public class SpriteKeyframeColorTests
{
    private static AnimationChainList CreateChains()
    {
        AnimationChain chain = new() { Name = "Fade" };
        chain.Add(new AnimationFrame { FrameLength = 1, Alpha = 0, ColorInterpolation = "Linear" });
        chain.Add(new AnimationFrame { FrameLength = 1, Alpha = 200 });
        AnimationChainList chains = new();
        chains.Add(chain);
        return chains;
    }

    [Fact]
    public void AnimateSelf_AlphaGlide_ChangesAlphaMidFrame()
    {
        Sprite sut = new();
        sut.AnimationLogic.AnimationChains = CreateChains();
        sut.AnimationLogic.CurrentChainName = "Fade";
        sut.AnimationLogic.Animate = true;

        sut.AnimateSelf(0.5);

        sut.Alpha.ShouldBe(100);
    }

    [Fact]
    public void Clone_AlphaGlide_ChangesCloneAlphaMidFrame()
    {
        Sprite original = new();
        original.AnimationLogic.AnimationChains = CreateChains();
        original.AnimationLogic.CurrentChainName = "Fade";
        Sprite clone = (Sprite)original.Clone();
        clone.AnimationLogic.Animate = true;

        clone.AnimateSelf(0.5);

        clone.Alpha.ShouldBe(100);
    }
}
