using Gum.Wireframe;
using SkiaGum.GueDeriving;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

public class SpriteRuntimeTests
{
    [Fact]
    public void AnimationChainFrameIndex_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.AnimationChainFrameIndex = 3;

        sut.AnimationChainFrameIndex.ShouldBe(3);
    }

    [Fact]
    public void AnimationChainSpeed_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.AnimationChainSpeed = 2.5f;

        sut.AnimationChainSpeed.ShouldBe(2.5f);
    }


    [Fact]
    public void AnimationChainTime_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.AnimationChainTime = 1.5;

        sut.AnimationChainTime.ShouldBe(1.5);
    }

    [Fact]
    public void IsAnimationChainLooping_ShouldDefaultToTrue()
    {
        SpriteRuntime sut = new();

        sut.IsAnimationChainLooping.ShouldBeTrue();
    }

    [Fact]
    public void IsAnimationChainLooping_ShouldForwardToContainedSprite()
    {
        SpriteRuntime sut = new();

        sut.IsAnimationChainLooping = false;

        sut.IsAnimationChainLooping.ShouldBeFalse();
    }

    [Fact]
    public void Width_ShouldBeDefault()
    {
        SpriteRuntime sut = new();
        sut.Width.ShouldBe(100);
        sut.WidthUnits.ShouldBe(Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile);
    }
}
