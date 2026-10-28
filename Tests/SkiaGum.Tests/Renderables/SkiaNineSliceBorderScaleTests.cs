using Gum;
using Gum.Content.AnimationChain;
using SkiaGum;
using SkiaGum.Renderables;
using Xunit;
using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Per-side border scale overrides (#6983) on the Skia nine-slice. The same scenarios run against
/// the MonoGame nine-slice (MonoGameGum.IntegrationTests) and the Raylib section layout
/// (RaylibGum.Tests), which must draw identically.
/// </summary>
public class SkiaNineSliceBorderScaleTests
{
    public SkiaNineSliceBorderScaleTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    // A 3x3 grid of distinct colors; the 1 texel frame is the outer ring.
    private static SKColor TexelColor(int x, int y) => new((byte)(40 + x * 80), (byte)(40 + y * 80), 0);

    private static SKBitmap Render(float width, float height, float borderScale,
        float? left, float? top, float? right, float? bottom, bool isTiling)
    {
        using SKBitmap texture = new(3, 3);
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                texture.SetPixel(x, y, TexelColor(x, y));
            }
        }

        AnimationChain chain = new() { Name = "c" };
        chain.Add(new AnimationFrame { FrameLength = 1f, Texture = texture, RightCoordinate = 1f, BottomCoordinate = 1f });

        using SKSurface surface = SKSurface.Create(new SKImageInfo(128, 128));
        GumService.Default.Initialize(surface.Canvas, 128, 128);

        NineSliceRuntime nineSlice = new()
        {
            X = 0,
            Y = 0,
            Width = width,
            Height = height,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            BorderScale = borderScale,
            CustomBorderScaleLeft = left,
            CustomBorderScaleTop = top,
            CustomBorderScaleRight = right,
            CustomBorderScaleBottom = bottom,
            IsTilingMiddleSections = isTiling,
        };
        nineSlice.AnimationChains = new AnimationChainList { chain };
        nineSlice.CurrentChainName = "c";
        nineSlice.Animate = true;
        nineSlice.AnimateSelf(0.0);
        GumService.Default.Root.Children.Add(nineSlice);

        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    [Fact]
    public void Render_NoOverrides_UsesBorderScaleOnEverySide()
    {
        using SKBitmap pixels = Render(60, 60, 10, null, null, null, null, false);

        pixels.GetPixel(5, 5).ShouldBe(TexelColor(0, 0));
        pixels.GetPixel(30, 30).ShouldBe(TexelColor(1, 1));
        pixels.GetPixel(55, 55).ShouldBe(TexelColor(2, 2));
        pixels.GetPixel(15, 5).ShouldBe(TexelColor(1, 0));
    }

    [Fact]
    public void Render_PerSideOverrides_DrawEachBorderAtItsOwnThickness()
    {
        // Left 20, top 5, right 30, bottom falls back to 10 (BorderScale).
        using SKBitmap pixels = Render(100, 100, 10, 20f, 5f, 30f, null, false);

        pixels.GetPixel(10, 50).ShouldBe(TexelColor(0, 1));
        pixels.GetPixel(30, 50).ShouldBe(TexelColor(1, 1));
        pixels.GetPixel(60, 50).ShouldBe(TexelColor(1, 1));
        pixels.GetPixel(80, 50).ShouldBe(TexelColor(2, 1));
        pixels.GetPixel(50, 2).ShouldBe(TexelColor(1, 0));
        pixels.GetPixel(50, 8).ShouldBe(TexelColor(1, 1));
        pixels.GetPixel(50, 95).ShouldBe(TexelColor(1, 2));
        pixels.GetPixel(50, 85).ShouldBe(TexelColor(1, 1));
    }

    [Fact]
    public void Render_ZeroScaleSide_DrawsNoBorderThereAndTheRestFillsTheSpace()
    {
        // Left 20, top 0, right 5, bottom 10 in a 60x60 nine-slice.
        using SKBitmap pixels = Render(60, 60, 10, 20f, 0f, 5f, null, false);

        pixels.GetPixel(10, 25).ShouldBe(TexelColor(0, 1));
        pixels.GetPixel(30, 25).ShouldBe(TexelColor(1, 1));
        pixels.GetPixel(57, 25).ShouldBe(TexelColor(2, 1));
        pixels.GetPixel(10, 55).ShouldBe(TexelColor(0, 2));
        pixels.GetPixel(30, 55).ShouldBe(TexelColor(1, 2));
        pixels.GetPixel(57, 55).ShouldBe(TexelColor(2, 2));
        pixels.GetPixel(30, 2).ShouldBe(TexelColor(1, 1));
        pixels.GetPixel(10, 2).ShouldBe(TexelColor(0, 1));
    }

    [Fact]
    public void Render_ZeroLeftWhileTiling_FillsFromTheLeftEdge()
    {
        using SKBitmap pixels = Render(60, 60, 10, 0f, null, null, null, true);

        pixels.GetPixel(2, 30).ShouldBe(TexelColor(1, 1));
        pixels.GetPixel(2, 2).ShouldBe(TexelColor(1, 0));
        pixels.GetPixel(57, 30).ShouldBe(TexelColor(2, 1));
    }

    [Fact]
    public void CustomBorderScales_ShouldDefaultToNull()
    {
        NineSlice sut = new();

        sut.CustomBorderScaleLeft.ShouldBeNull();
        sut.CustomBorderScaleTop.ShouldBeNull();
        sut.CustomBorderScaleRight.ShouldBeNull();
        sut.CustomBorderScaleBottom.ShouldBeNull();
    }

    [Fact]
    public void SetProperty_CustomBorderScales_ShouldForwardToRuntime()
    {
        NineSliceRuntime sut = new();

        sut.SetProperty("CustomBorderScaleLeft", 1.5f);
        sut.SetProperty("CustomBorderScaleTop", 2.5f);
        sut.SetProperty("CustomBorderScaleRight", 0f);
        sut.SetProperty("CustomBorderScaleBottom", 4f);

        sut.CustomBorderScaleLeft.ShouldBe(1.5f);
        sut.CustomBorderScaleTop.ShouldBe(2.5f);
        sut.CustomBorderScaleRight.ShouldBe(0f);
        sut.CustomBorderScaleBottom.ShouldBe(4f);
        ((NineSlice)sut.RenderableComponent).CustomBorderScaleRight.ShouldBe(0f);
    }

    [Fact]
    public void SetProperty_CustomBorderScaleNull_ShouldClearTheOverride()
    {
        NineSliceRuntime sut = new();
        sut.CustomBorderScaleLeft = 3f;

        sut.SetProperty("CustomBorderScaleLeft", null);

        sut.CustomBorderScaleLeft.ShouldBeNull();
    }

    [Fact]
    public void Render_ZeroLeftOnly_CollapsesTheLeftBorderAndFillsFromTheEdge()
    {
        using SKBitmap pixels = Render(60, 60, 10, 0f, null, null, null, false);

        pixels.GetPixel(2, 30).ShouldBe(TexelColor(1, 1));
        pixels.GetPixel(2, 2).ShouldBe(TexelColor(1, 0));
        pixels.GetPixel(57, 30).ShouldBe(TexelColor(2, 1));
    }

    [Fact]
    public void Render_NegativeOverride_BehavesAsZero()
    {
        using SKBitmap pixels = Render(60, 60, 10, -5f, null, null, null, false);

        pixels.GetPixel(2, 30).ShouldBe(TexelColor(1, 1));
    }

    [Fact]
    public void Render_SmallWidth_SamplesFewerTexelsOfAWideFrame()
    {
        // 6 texel strip, 2 texel frame, scale 10 would need 40 wide; at 20 each band is 10 wide and
        // samples 1 texel, so the left band shows column 0 only.
        using SKBitmap texture = new(6, 6);
        for (int y = 0; y < 6; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                texture.SetPixel(x, y, new SKColor((byte)(30 + x * 40), 0, 0));
            }
        }
        AnimationChain chain = new() { Name = "c" };
        chain.Add(new AnimationFrame { FrameLength = 1f, Texture = texture, RightCoordinate = 1f, BottomCoordinate = 1f });
        using SKSurface surface = SKSurface.Create(new SKImageInfo(128, 128));
        GumService.Default.Initialize(surface.Canvas, 128, 128);
        NineSliceRuntime nineSlice = new()
        {
            Width = 20, Height = 60, BorderScale = 10,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        };
        nineSlice.AnimationChains = new AnimationChainList { chain };
        nineSlice.CurrentChainName = "c";
        nineSlice.Animate = true;
        nineSlice.AnimateSelf(0.0);
        GumService.Default.Root.Children.Add(nineSlice);
        GumService.Default.Draw();
        using SKImage image = surface.Snapshot();
        using SKBitmap pixels = SKBitmap.FromImage(image);

        pixels.GetPixel(8, 30).Red.ShouldBe((byte)30);
    }
}
