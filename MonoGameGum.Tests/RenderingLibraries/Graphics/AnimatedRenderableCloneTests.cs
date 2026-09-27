using Gum.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Runtime.CompilerServices;
using Xunit;
using Rectangle = System.Drawing.Rectangle;

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
    /// <summary>
    /// A user-supplied legacy <see cref="IAnimation"/> that opts into cloning. It advances by the
    /// time passed per call, so sharing one instance would move both renderables twice as fast.
    /// </summary>
    private class CloneableAnimation : IAnimation, ICloneable
    {
        public int CallCount;
        public bool FlipHorizontal => false;
        public bool FlipVertical => false;
        public Texture2D CurrentTexture { get; set; } = null!;
        public Rectangle? SourceRectangle => null;
        public int CurrentFrameIndex => 0;
        public void AnimationActivity(double currentTime) => CallCount++;
        public object Clone() => MemberwiseClone();
    }

    [Fact]
    public void NineSlice_Clone_ShouldCloneCloneableLegacyAnimation()
    {
        CloneableAnimation animation = new() { CurrentTexture = CreateTexture() };
        NineSlice original = new() { Animation = animation, Animate = true };

        NineSlice clone = original.Clone();
        clone.AnimationActivity(1);

        clone.Animation.ShouldNotBeSameAs(animation);
        animation.CallCount.ShouldBe(0);
    }

    [Fact]
    public void Sprite_Clone_ShouldCloneCloneableLegacyAnimation()
    {
        CloneableAnimation animation = new() { CurrentTexture = CreateTexture() };
        Sprite original = new((Texture2D?)null) { Animation = animation, Animate = true };

        Sprite clone = original.Clone();
        clone.AnimationActivity(1);

        clone.Animation.ShouldNotBeSameAs(animation);
        animation.CallCount.ShouldBe(0);
    }

    [Fact]
    public void Sprite_Clone_ShouldShareNonCloneableLegacyAnimation()
    {
        // IAnimation has no clone contract, so an implementation that does not opt in through
        // ICloneable stays shared rather than being copied by guesswork.
        NonCloneableAnimation animation = new();
        Sprite original = new((Texture2D?)null) { Animation = animation };

        Sprite clone = original.Clone();

        clone.Animation.ShouldBeSameAs(animation);
    }

    private class NonCloneableAnimation : IAnimation
    {
        public bool FlipHorizontal => false;
        public bool FlipVertical => false;
        public Texture2D CurrentTexture => null!;
        public Rectangle? SourceRectangle => null;
        public int CurrentFrameIndex => 0;
        public void AnimationActivity(double currentTime) { }
    }
}
