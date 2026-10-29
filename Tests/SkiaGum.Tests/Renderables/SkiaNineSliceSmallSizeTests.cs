using CrossRuntimeTests;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A nine-slice shorter than its two borders still draws (#7029): a 3x16 texture sliced at 1 drew
/// nothing at height 1, because half a border rounded to 0 texels and every band with no texels is
/// skipped.
/// </summary>
public class SkiaNineSliceSmallSizeTests : BaseTestClass
{
    // Texel(x, y) is the color the texture holds at that texel.
    private static SKColor TexelColor(int x, int y)
    {
        (byte r, byte g, byte b) = NineSliceCases.Texel(x, y);
        return new SKColor(r, g, b);
    }

    private static SKBitmap Render(int height, float zoom)
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;

        using SKSurface surface = SKSurface.Create(new SKImageInfo(100, 40));
        SystemManagers? previousDefault = SystemManagers.Default;
        SystemManagers managers = new();
        managers.Initialize();
        SystemManagers.Default = managers;
        managers.Canvas = surface.Canvas;
        managers.Renderer.ClearsCanvas = false;
        managers.Renderer.Camera.Zoom = zoom;

        using SKBitmap texture = new(3, 16);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                texture.SetPixel(x, y, TexelColor(x, y));
            }
        }

        NineSliceRuntime nineSlice = new();
        nineSlice.Texture = texture;
        nineSlice.CustomFrameTextureCoordinateWidth = 1;
        nineSlice.WidthUnits = DimensionUnitType.Absolute;
        nineSlice.HeightUnits = DimensionUnitType.Absolute;
        nineSlice.X = 10;
        nineSlice.Y = 5;
        nineSlice.Width = 3;
        nineSlice.Height = height;
        nineSlice.AddToManagers(managers, null);

        try
        {
            surface.Canvas.Clear(SKColors.Black);
            managers.Draw();
            using SKImage image = surface.Snapshot();
            return SKBitmap.FromImage(image);
        }
        finally
        {
            nineSlice.RemoveFromManagers();
            SystemManagers.Default = previousDefault;
        }
    }

    [Fact]
    public void Render_HeightOneAtZoom2_DrawsTheTopAndBottomBorders()
    {
        // 3x1 world units at zoom 2 is 6x2 pixels at (20, 10): one row of each border, three columns
        // (left, middle, right) two pixels wide.
        using SKBitmap pixels = Render(height: 1, zoom: 2);

        pixels.GetPixel(21, 10).ShouldBe(TexelColor(0, 0));
        pixels.GetPixel(23, 10).ShouldBe(TexelColor(1, 0));
        pixels.GetPixel(25, 10).ShouldBe(TexelColor(2, 0));
        pixels.GetPixel(21, 11).ShouldBe(TexelColor(0, 15));
        pixels.GetPixel(23, 11).ShouldBe(TexelColor(1, 15));
        pixels.GetPixel(25, 11).ShouldBe(TexelColor(2, 15));
        pixels.GetPixel(23, 9).ShouldBe(SKColors.Black);
        pixels.GetPixel(23, 12).ShouldBe(SKColors.Black);
    }

    [Fact]
    public void Render_HeightTwoAtZoom2_DrawsOneBorderRowEachAndNoMiddle()
    {
        using SKBitmap pixels = Render(height: 2, zoom: 2);

        pixels.GetPixel(23, 10).ShouldBe(TexelColor(1, 0));
        pixels.GetPixel(23, 11).ShouldBe(TexelColor(1, 0));
        pixels.GetPixel(23, 12).ShouldBe(TexelColor(1, 15));
        pixels.GetPixel(23, 13).ShouldBe(TexelColor(1, 15));
    }

    [Fact]
    public void Render_HeightThreeAtZoom2_DrawsTheMiddleBetweenTheBorders()
    {
        using SKBitmap pixels = Render(height: 3, zoom: 2);

        pixels.GetPixel(23, 10).ShouldBe(TexelColor(1, 0));
        pixels.GetPixel(23, 14).ShouldBe(TexelColor(1, 15));
        pixels.GetPixel(23, 12).ShouldNotBe(SKColors.Black);
        pixels.GetPixel(23, 13).ShouldNotBe(SKColors.Black);
    }

    [Fact]
    public void Render_HeightOneAtZoom1_DrawsSomething()
    {
        using SKBitmap pixels = Render(height: 1, zoom: 1);

        pixels.GetPixel(11, 5).ShouldNotBe(SKColors.Black);
    }
}
