using Gum.GueDeriving;
using Gum.Renderables;
using Shouldly;

namespace SokolGum.Tests.Runtimes;

public class SpriteRuntimeTests : BaseTestClass
{
    [Fact]
    public void ContainedRenderable_ShouldBeSprite()
    {
        var sut = new SpriteRuntime();
        sut.RenderableComponent.ShouldBeOfType<Sprite>();
    }

    [Fact]
    public void Texture_ShouldBeNullByDefault()
    {
        var sut = new SpriteRuntime();
        sut.Texture.ShouldBeNull();
    }

    [Fact]
    public void Color_ShouldRoundTrip()
    {
        var color = new Color(100, 150, 200, 255);
        var sut = new SpriteRuntime { Color = color };
        sut.Color.R.ShouldBe((byte)100);
        sut.Color.G.ShouldBe((byte)150);
        sut.Color.B.ShouldBe((byte)200);
    }

    [Fact]
    public void FlipHorizontal_ShouldDefaultToFalse()
    {
        var sut = new SpriteRuntime();
        sut.FlipHorizontal.ShouldBeFalse();
    }

    [Fact]
    public void FlipHorizontal_ShouldRoundTrip()
    {
        var sut = new SpriteRuntime { FlipHorizontal = true };
        sut.FlipHorizontal.ShouldBeTrue();
    }

    [Fact]
    public void FlipVertical_ShouldDefaultToFalse()
    {
        var sut = new SpriteRuntime();
        sut.FlipVertical.ShouldBeFalse();
    }

    [Fact]
    public void FlipVertical_ShouldRoundTrip()
    {
        var sut = new SpriteRuntime { FlipVertical = true };
        sut.FlipVertical.ShouldBeTrue();
    }

    [Fact]
    public void AnimationChains_ShouldBeNullByDefault()
    {
        var sut = new SpriteRuntime();
        sut.AnimationChains.ShouldBeNull();
    }

    [Fact]
    public void Animate_ShouldDefaultToFalse()
    {
        // SpriteAnimationLogic.Animate defaults to false — matches
        // RaylibGum/Skia, callers opt in explicitly.
        var sut = new SpriteRuntime();
        sut.Animate.ShouldBeFalse();
    }

    [Fact]
    public void AnimationSpeed_ShouldDefaultToOne()
    {
        var sut = new SpriteRuntime();
        sut.AnimationSpeed.ShouldBe(1f);
    }

    [Fact]
    public void AnimationSpeed_ShouldRoundTrip()
    {
        var sut = new SpriteRuntime { AnimationSpeed = 2.5f };
        sut.AnimationSpeed.ShouldBe(2.5f);
    }

    [Fact]
    public void Runtime_ShouldForwardAnimationPropertiesToContainedSprite()
    {
        // Runtime is a thin wrapper — each property delegates to the
        // composed Sprite.AnimationLogic. This test guards that the
        // forwarding stays wired through any future refactor.
        var sut = new SpriteRuntime { Animate = true, AnimationSpeed = 3f };
        var sprite = (Sprite)sut.RenderableComponent;
        sprite.Animate.ShouldBeTrue();
        sprite.AnimationSpeed.ShouldBe(3f);
    }
}
