using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics.Animation;
using Shouldly;

namespace RaylibGum.Tests.Runtimes;

public class NineSliceRuntimeTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        NineSliceRuntime sut = new();
        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Animate_ShouldRouteThroughContainedAnimationLogic()
    {
        NineSliceRuntime sut = new();
        sut.Animate = true;
        ((NineSlice)sut.RenderableComponent).AnimationLogic.Animate.ShouldBeTrue();
    }

    [Fact]
    public void AnimationChains_AssignmentAndTimeAdvance_ShouldSwitchTextureAndSourceRectangle()
    {
        // Two frames pointing at two distinct textures; after ticking past the first
        // frame length, the Raylib NineSlice should have swapped to frame[1]'s texture
        // and source rect via AnimationLogic.ApplyFrame.
        Texture2D textureA = new Texture2D { Width = 40, Height = 40, Id = 1 };
        Texture2D textureB = new Texture2D { Width = 40, Height = 40, Id = 2 };

        AnimationFrame frameA = new AnimationFrame
        {
            Texture = textureA,
            FrameLength = 1f,
            LeftCoordinate = 0f,
            RightCoordinate = 0.5f,
            TopCoordinate = 0f,
            BottomCoordinate = 0.5f,
        };
        AnimationFrame frameB = new AnimationFrame
        {
            Texture = textureB,
            FrameLength = 1f,
            LeftCoordinate = 0.5f,
            RightCoordinate = 1f,
            TopCoordinate = 0.5f,
            BottomCoordinate = 1f,
        };

        AnimationChain chain = new AnimationChain { Name = "TestChain" };
        chain.Add(frameA);
        chain.Add(frameB);

        AnimationChainList chains = new AnimationChainList();
        chains.Add(chain);

        NineSliceRuntime sut = new();
        sut.AnimationChains = chains;
        sut.Animate = true;

        NineSlice contained = (NineSlice)sut.RenderableComponent;
        contained.AnimationLogic.UpdateToCurrentAnimationFrame();

        contained.Texture.ShouldNotBeNull();
        contained.Texture.Value.Id.ShouldBe<uint>(1);

        contained.AnimationLogic.AnimateSelf(1.1);

        contained.Texture.Value.Id.ShouldBe<uint>(2);
        contained.SourceRectangle.ShouldNotBeNull();
        contained.SourceRectangle.Value.X.ShouldBe(20f);
        contained.SourceRectangle.Value.Y.ShouldBe(20f);
        contained.SourceRectangle.Value.Width.ShouldBe(20f);
        contained.SourceRectangle.Value.Height.ShouldBe(20f);
    }

    [Fact]
    public void AnimationChains_ShouldRouteThroughContainedAnimationLogic()
    {
        NineSliceRuntime sut = new();
        AnimationChainList chains = new AnimationChainList();
        sut.AnimationChains = chains;
        ((NineSlice)sut.RenderableComponent).AnimationLogic.AnimationChains.ShouldBe(chains);
    }

    [Fact]
    public void AnimationLogic_ShouldExposeSharedPlaybackState()
    {
        NineSlice sut = new NineSlice();
        sut.AnimationLogic.ShouldNotBeNull();
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        sut.Alpha = 128;
        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Blue_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        sut.Blue = 64;
        sut.Blue.ShouldBe(64);
    }

    [Fact]
    public void Color_ShouldDefaultToWhite()
    {
        NineSliceRuntime sut = new();
        sut.Color.ShouldBe(Color.White);
    }

    [Fact]
    public void Color_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        Color expected = new Color(10, 20, 30, 40);
        sut.Color = expected;
        sut.Color.ShouldBe(expected);
    }

    [Fact]
    public void ExposeChildrenEvents_ShouldBeTrue()
    {
        NineSliceRuntime sut = new();
        sut.ExposeChildrenEvents.ShouldBeTrue();
    }

    [Fact]
    public void Green_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        sut.Green = 32;
        sut.Green.ShouldBe(32);
    }

    [Fact]
    public void HasEvents_ShouldDefaultToFalse()
    {
        NineSliceRuntime sut = new();
        sut.HasEvents.ShouldBeFalse();
    }

    [Fact]
    public void Height_ShouldDefaultTo100()
    {
        NineSliceRuntime sut = new();
        sut.Height.ShouldBe(100);
    }

    [Fact]
    public void Red_ShouldRoundTrip()
    {
        NineSliceRuntime sut = new();
        sut.Red = 200;
        sut.Red.ShouldBe(200);
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        NineSliceRuntime sut = new();
        sut.Visible.ShouldBeTrue();
    }

    [Fact]
    public void Width_ShouldDefaultTo100()
    {
        NineSliceRuntime sut = new();
        sut.Width.ShouldBe(100);
    }
}
