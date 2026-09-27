using Gum.Graphics.Animation;
using Gum.Renderables;
using Raylib_cs;
using Shouldly;
using System;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// A cloned animated renderable plays its animation on its own, without moving the source's
/// playback or writing frames into the source (#5198).
/// </summary>
public class AnimatedRenderableCloneTests
{
    private static AnimationChainList CreateChains(Texture2D firstTexture, Texture2D secondTexture)
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
        Texture2D firstTexture = new() { Id = 1, Width = 8, Height = 8 };
        Texture2D secondTexture = new() { Id = 2, Width = 16, Height = 16 };
        NineSlice original = new();
        original.AnimationChains = CreateChains(firstTexture, secondTexture);
        original.AnimationLogic.CurrentChainName = "First";
        int originalCycles = 0;
        original.AnimationLogic.AnimationChainCycled += () => originalCycles++;

        NineSlice clone = (NineSlice)((ICloneable)original).Clone();
        clone.AnimationLogic.CurrentChainName.ShouldBe("First");
        clone.AnimationLogic.CurrentChainName = "Second";
        clone.AnimationLogic.Animate = true;
        clone.AnimateSelf(1.5);

        original.AnimationLogic.CurrentChainName.ShouldBe("First");
        original.AnimationLogic.Animate.ShouldBeFalse();
        original.Texture!.Value.Id.ShouldBe(1u);
        originalCycles.ShouldBe(0);
        clone.Texture!.Value.Id.ShouldBe(2u);
    }

    [Fact]
    public void Sprite_Clone_ShouldAnimateIndependently()
    {
        Texture2D firstTexture = new() { Id = 1, Width = 8, Height = 8 };
        Texture2D secondTexture = new() { Id = 2, Width = 16, Height = 16 };
        Sprite original = new();
        original.AnimationChains = CreateChains(firstTexture, secondTexture);
        original.CurrentChainName = "First";
        int originalCycles = 0;
        original.AnimationLogic.AnimationChainCycled += () => originalCycles++;

        Sprite clone = (Sprite)((ICloneable)original).Clone();
        clone.CurrentChainName.ShouldBe("First");
        clone.CurrentChainName = "Second";
        clone.Animate = true;
        clone.AnimateSelf(1.5);

        original.CurrentChainName.ShouldBe("First");
        original.Animate.ShouldBeFalse();
        original.Texture!.Value.Id.ShouldBe(1u);
        originalCycles.ShouldBe(0);
        clone.Texture!.Value.Id.ShouldBe(2u);
    }
}
