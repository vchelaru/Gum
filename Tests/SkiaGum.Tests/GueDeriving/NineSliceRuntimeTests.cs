using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Animation;
using SkiaGum;
using SkiaGum.Renderables;
using SkiaSharp;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

public class NineSliceRuntimeTests
{
    public NineSliceRuntimeTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    // ---- Dispatcher routing pins (issue #3639 / ADR 0011) -----------------------------------
    // These drive the STRING property name through the production Skia dispatcher (via
    // SetProperty) and assert the value lands on NineSliceRuntime, including its
    // NotifyPropertyChanged side effect -- before the ADR 0011 redispatch these wrote straight to
    // the contained NineSlice renderable and skipped that notification.

    [Fact]
    public void Dispatch_Alpha_RoutesToRuntimeAndNotifies()
    {
        NineSliceRuntime sut = new();
        var seen = new System.Collections.Generic.List<string>();
        sut.PropertyChanged += (_, e) => seen.Add(e.PropertyName!);

        sut.SetProperty("Alpha", 128);

        sut.Alpha.ShouldBe(128);
        seen.ShouldContain(nameof(NineSliceRuntime.Alpha));
    }

    [Fact]
    public void Dispatch_RedGreenBlue_RouteToRuntime()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("Red", 10);
        sut.SetProperty("Green", 20);
        sut.SetProperty("Blue", 30);

        sut.Red.ShouldBe(10);
        sut.Green.ShouldBe(20);
        sut.Blue.ShouldBe(30);
    }

    [Fact]
    public void Dispatch_Color_ConvertsDrawingColorAndRoutesToRuntime()
    {
        NineSliceRuntime sut = new();
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(10, 20, 30, 40);

        sut.SetProperty("Color", drawingColor);

        sut.Color.ShouldBe(new SKColor(20, 30, 40, 10));
    }

    [Fact]
    public void Dispatch_Blend_RoutesToRuntime()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("Blend", Gum.RenderingLibrary.Blend.Additive);

        sut.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
    }

    [Fact]
    public void Dispatch_BorderScale_RoutesToRuntime()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("BorderScale", 2.5f);

        sut.BorderScale.ShouldBe(2.5f);
    }

    [Fact]
    public void Dispatch_IsTilingMiddleSections_RoutesToRuntime()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("IsTilingMiddleSections", true);

        sut.IsTilingMiddleSections.ShouldBeTrue();
    }

    [Fact]
    public void Dispatch_CustomFrameTextureCoordinateWidth_RoutesToRuntime()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("CustomFrameTextureCoordinateWidth", 0.25f);

        sut.CustomFrameTextureCoordinateWidth.ShouldBe(0.25f);
    }

    [Fact]
    public void Animate_ShouldRouteThroughContainedAnimationLogic()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.Animate = true;
        ((NineSlice)sut.RenderableComponent).AnimationLogic.Animate.ShouldBeTrue();
    }

    [Fact]
    public void AnimationChains_AssignmentAndTimeAdvance_ShouldSwitchTextureAndSourceRectangle()
    {
        // Two frames pointing at two distinct bitmaps; after ticking past the first
        // frame length, the Skia NineSlice should have swapped to frame[1]'s texture
        // and source rect via AnimationLogic.ApplyFrame.
        SKBitmap textureA = new SKBitmap(40, 40);
        SKBitmap textureB = new SKBitmap(40, 40);

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

        NineSliceRuntime sut = new NineSliceRuntime();
        sut.AnimationChains = chains;
        sut.Animate = true;

        // Force initial frame application (matches Sprite flow: chain assigned +
        // Animate = true is enough once a tick lands on frame 0).
        ((NineSlice)sut.RenderableComponent).AnimationLogic.UpdateToCurrentAnimationFrame();

        NineSlice contained = (NineSlice)sut.RenderableComponent;
        contained.Texture.ShouldBe(textureA);

        // Advance past the first frame.
        ((NineSlice)sut.RenderableComponent).AnimationLogic.AnimateSelf(1.1);

        contained.Texture.ShouldBe(textureB);
        contained.SourceRectangle.ShouldNotBeNull();
        contained.SourceRectangle.Value.Left.ShouldBe(20);
        contained.SourceRectangle.Value.Top.ShouldBe(20);
        contained.SourceRectangle.Value.Width.ShouldBe(20);
        contained.SourceRectangle.Value.Height.ShouldBe(20);
    }

    [Fact]
    public void AnimationChains_ShouldRouteThroughContainedAnimationLogic()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
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
    public void BorderScale_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.BorderScale = 2.5f;
        sut.BorderScale.ShouldBe(2.5f);
        ((NineSlice)sut.RenderableComponent).BorderScale.ShouldBe(2.5f);
    }

    [Fact]
    public void Color_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.Color = SKColors.Lime;
        sut.Color.ShouldBe(SKColors.Lime);
        ((NineSlice)sut.RenderableComponent).Color.ShouldBe(SKColors.Lime);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.Width.ShouldBe(100);
        sut.Height.ShouldBe(100);
        sut.RenderableComponent.ShouldBeOfType<NineSlice>();
    }

    [Fact]
    public void CustomFrameTextureCoordinateWidth_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.CustomFrameTextureCoordinateWidth = 12f;
        sut.CustomFrameTextureCoordinateWidth.ShouldBe(12f);
        ((NineSlice)sut.RenderableComponent).CustomFrameTextureCoordinateWidth.ShouldBe(12f);
    }

    [Fact]
    public void IsTilingMiddleSections_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        sut.IsTilingMiddleSections = true;
        sut.IsTilingMiddleSections.ShouldBeTrue();
        ((NineSlice)sut.RenderableComponent).IsTilingMiddleSections.ShouldBeTrue();
    }

    [Fact]
    public void Texture_ShouldForwardToContainedNineSlice()
    {
        NineSliceRuntime sut = new NineSliceRuntime();
        SKBitmap bitmap = new SKBitmap(30, 30);
        sut.Texture = bitmap;
        sut.Texture.ShouldBe(bitmap);
        ((NineSlice)sut.RenderableComponent).Texture.ShouldBe(bitmap);
    }
}

public class NineSliceRenderableTests
{
    [Fact]
    public void Color_ShouldRoundTrip()
    {
        NineSlice sut = new NineSlice();
        sut.Color = SKColors.Blue;
        sut.Color.ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void Defaults_ShouldMatchExpected()
    {
        NineSlice sut = new NineSlice();
        sut.IsTilingMiddleSections.ShouldBeFalse();
        sut.BorderScale.ShouldBe(1f);
        sut.CustomFrameTextureCoordinateWidth.ShouldBeNull();
        sut.Texture.ShouldBeNull();
        sut.Image.ShouldBeNull();
        // White (identity under SKBlendMode.Modulate) so a freshly constructed
        // NineSlice draws untinted instead of inheriting RenderableShapeBase's red.
        sut.Color.ShouldBe(SKColors.White);
    }

    [Fact]
    public void Texture_SettingBitmap_ShouldPopulateImage()
    {
        NineSlice sut = new NineSlice();
        SKBitmap bitmap = new SKBitmap(16, 16);
        sut.Texture = bitmap;
        sut.Image.ShouldNotBeNull();
    }

    [Fact]
    public void Texture_SettingNull_ShouldClearImage()
    {
        NineSlice sut = new NineSlice();
        sut.Texture = new SKBitmap(8, 8);
        sut.Texture = null;
        sut.Image.ShouldBeNull();
    }
}
