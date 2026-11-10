using Gum;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A sprite's tint alpha fades Add linearly over an opaque backdrop: the texture is opaque gray
/// (40,40,40), the tint (255,128,0), and the expected value is
/// <c>backdrop * (1 - a) + clamp(tex + tint) * a</c>. Same setup as RaylibGum.Tests'
/// SpriteColorOperationAlphaTests, so the two runtimes can be compared.
/// </summary>
public class SpriteColorOperationAlphaTests
{
    public SpriteColorOperationAlphaTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    [Theory]
    [InlineData(255, 0, 0, 0, 255, 168, 40)]
    [InlineData(128, 0, 0, 0, 128, 84, 20)]
    [InlineData(64, 0, 0, 0, 64, 42, 10)]
    [InlineData(255, 70, 70, 90, 255, 168, 40)]
    [InlineData(128, 70, 70, 90, 163, 119, 65)]
    [InlineData(64, 70, 70, 90, 116, 95, 77)]
    public void Render_Add_FadesByTintAlpha(byte alpha, byte backR, byte backG, byte backB,
        byte expectedR, byte expectedG, byte expectedB)
    {
        using SKBitmap backdropTexture = new(4, 4);
        backdropTexture.Erase(new SKColor(backR, backG, backB, 255));
        using SKBitmap texture = new(4, 4);
        texture.Erase(new SKColor(40, 40, 40, 255));
        using SKSurface surface = SKSurface.Create(new SKImageInfo(64, 64));
        GumService.Default.Initialize(surface.Canvas, 64, 64);

        SpriteRuntime backdrop = new()
        {
            Texture = backdropTexture,
            Width = 64,
            Height = 64,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
        };
        SpriteRuntime sprite = new()
        {
            Texture = texture,
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
            Red = 255,
            Green = 128,
            Blue = 0,
            Alpha = alpha,
            ColorOperation = ColorOperation.Add,
        };
        GumService.Default.Root.Children.Add(backdrop);
        GumService.Default.Root.Children.Add(sprite);
        GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        using SKBitmap pixels = SKBitmap.FromImage(image);
        SKColor actual = pixels.GetPixel(20, 20);

        bool matches = Math.Abs(actual.Red - expectedR) <= 3
            && Math.Abs(actual.Green - expectedG) <= 3
            && Math.Abs(actual.Blue - expectedB) <= 3;
        Assert.True(matches,
            $"Expected ({expectedR},{expectedG},{expectedB}) +-3 but drew ({actual.Red},{actual.Green},{actual.Blue})");
    }
}
