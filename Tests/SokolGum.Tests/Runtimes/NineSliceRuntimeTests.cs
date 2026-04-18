using Gum.GueDeriving;
using Gum.Renderables;
using Shouldly;

namespace SokolGum.Tests.Runtimes;

public class NineSliceRuntimeTests : BaseTestClass
{
    [Fact]
    public void ContainedRenderable_ShouldBeNineSlice()
    {
        var sut = new NineSliceRuntime();
        sut.RenderableComponent.ShouldBeOfType<NineSlice>();
    }

    [Fact]
    public void Texture_ShouldBeNullByDefault()
    {
        var sut = new NineSliceRuntime();
        sut.Texture.ShouldBeNull();
    }

    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        var sut = new NineSliceRuntime();
        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        var sut = new NineSliceRuntime { Alpha = 128 };
        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Red_Green_Blue_ShouldRoundTrip()
    {
        var sut = new NineSliceRuntime { Red = 50, Green = 100, Blue = 150 };
        sut.Red.ShouldBe(50);
        sut.Green.ShouldBe(100);
        sut.Blue.ShouldBe(150);
    }

    [Fact]
    public void CustomFrameTextureCoordinateWidth_ShouldBeNullByDefault()
    {
        var sut = new NineSliceRuntime();
        sut.CustomFrameTextureCoordinateWidth.ShouldBeNull();
    }

    [Fact]
    public void CustomFrameTextureCoordinateWidth_ShouldRoundTrip()
    {
        var sut = new NineSliceRuntime { CustomFrameTextureCoordinateWidth = 12f };
        sut.CustomFrameTextureCoordinateWidth.ShouldBe(12f);
    }

    [Fact]
    public void AnimationChains_ShouldBeNullByDefault()
    {
        var sut = new NineSliceRuntime();
        sut.AnimationChains.ShouldBeNull();
    }

    [Fact]
    public void Animate_ShouldDefaultToFalse()
    {
        // NineSlice composes SpriteAnimationLogic for parity with Sprite.
        // SokolGum is currently the only backend that animates NineSlice.
        var sut = new NineSliceRuntime();
        sut.Animate.ShouldBeFalse();
    }

    [Fact]
    public void AnimationSpeed_ShouldDefaultToOne()
    {
        var sut = new NineSliceRuntime();
        sut.AnimationSpeed.ShouldBe(1f);
    }
}
