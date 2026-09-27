using Gum.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Runtime.CompilerServices;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

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

    private static Texture2D CreateTexture() =>
        (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));

    [Fact]
    public void NineSlice_Clone_ShouldAnimateIndependently()
    {
        Texture2D firstTexture = CreateTexture();
        Texture2D secondTexture = CreateTexture();
        NineSlice original = new();
        original.AnimationChains = CreateChains(firstTexture, secondTexture);
        original.CurrentChainName = "First";
        int originalCycles = 0;
        original.AnimationChainCycled += () => originalCycles++;

        NineSlice clone = original.Clone();
        clone.CurrentChainName.ShouldBe("First");
        clone.CurrentChainName = "Second";
        clone.Animate = true;
        clone.AnimateSelf(1.5);

        original.CurrentChainName.ShouldBe("First");
        original.Animate.ShouldBeFalse();
        original.CenterTexture.ShouldBeSameAs(firstTexture);
        originalCycles.ShouldBe(0);
        clone.CenterTexture.ShouldBeSameAs(secondTexture);
    }

    [Fact]
    public void Sprite_Clone_ShouldAnimateIndependently()
    {
        Texture2D firstTexture = CreateTexture();
        Texture2D secondTexture = CreateTexture();
        Sprite original = new((Texture2D?)null);
        original.AnimationChains = CreateChains(firstTexture, secondTexture);
        original.CurrentChainName = "First";
        int originalCycles = 0;
        original.AnimationChainCycled += () => originalCycles++;

        Sprite clone = original.Clone();
        clone.CurrentChainName.ShouldBe("First");
        clone.CurrentChainName = "Second";
        clone.Animate = true;
        clone.AnimateSelf(1.5);

        original.CurrentChainName.ShouldBe("First");
        original.Animate.ShouldBeFalse();
        original.Texture.ShouldBeSameAs(firstTexture);
        originalCycles.ShouldBe(0);
        clone.Texture.ShouldBeSameAs(secondTexture);
    }
}
