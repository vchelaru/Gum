using Gum.Graphics.Animation;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A cloned animated renderable plays its animation on its own, without moving the source's
/// playback or writing frames into the source (#5198).
/// </summary>
public class AnimatedRenderableCloneTests
{
    private static AnimationChainList CreateChains(SKBitmap firstTexture, SKBitmap secondTexture)
    {
        AnimationChain first = new() { Name = "First" };
        first.Add(new AnimationFrame { FrameLength = 1, Texture = firstTexture, RightCoordinate = 1, BottomCoordinate = 1 });
        AnimationChain second = new() { Name = "Second" };
        second.Add(new AnimationFrame { FrameLength = 1, Texture = secondTexture, RightCoordinate = 1, BottomCoordinate = 1 });

        AnimationChainList chains = new();
        chains.Add(first);
        chains.Add(second);
        return chains;
    }

    [Fact]
    public void NineSlice_Clone_ShouldAnimateIndependently()
    {
        using SKBitmap firstTexture = new(8, 8);
        using SKBitmap secondTexture = new(16, 16);
        NineSlice original = new();
        original.AnimationLogic.AnimationChains = CreateChains(firstTexture, secondTexture);
        original.AnimationLogic.CurrentChainName = "First";
        int originalCycles = 0;
        original.AnimationLogic.AnimationChainCycled += () => originalCycles++;

        NineSlice clone = (NineSlice)original.Clone();
        clone.AnimationLogic.CurrentChainName.ShouldBe("First");
        clone.AnimationLogic.CurrentChainName = "Second";
        clone.AnimationLogic.Animate = true;
        clone.AnimateSelf(1.5);

        original.AnimationLogic.CurrentChainName.ShouldBe("First");
        original.AnimationLogic.Animate.ShouldBeFalse();
        original.Texture.ShouldBeSameAs(firstTexture);
        originalCycles.ShouldBe(0);
        clone.Texture.ShouldBeSameAs(secondTexture);
    }

    [Fact]
    public void Sprite_Clone_ShouldAnimateIndependently()
    {
        using SKBitmap firstTexture = new(8, 8);
        using SKBitmap secondTexture = new(16, 16);
        Sprite original = new();
        original.AnimationLogic.AnimationChains = CreateChains(firstTexture, secondTexture);
        original.AnimationLogic.CurrentChainName = "First";
        int originalCycles = 0;
        original.AnimationLogic.AnimationChainCycled += () => originalCycles++;

        Sprite clone = (Sprite)original.Clone();
        clone.AnimationLogic.CurrentChainName.ShouldBe("First");
        clone.AnimationLogic.CurrentChainName = "Second";
        clone.AnimationLogic.Animate = true;
        clone.AnimateSelf(1.5);

        original.AnimationLogic.CurrentChainName.ShouldBe("First");
        original.AnimationLogic.Animate.ShouldBeFalse();
        original.Texture.ShouldBeSameAs(firstTexture);
        originalCycles.ShouldBe(0);
        clone.Texture.ShouldBeSameAs(secondTexture);
    }
}
