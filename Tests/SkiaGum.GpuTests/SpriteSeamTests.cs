using Gum.DataTypes;
using Gum.Managers;
using Gum.Converters;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// #7030: sprites that meet end to end on a half screen pixel left a one pixel seam on a GPU surface. At a half
/// pixel the first pixel center of a sprite sits exactly on a texel boundary of the atlas, and Ganesh picked the
/// texel left of the source rectangle (the transparent border around the cell). The CPU surface the other Skia tests
/// use never did, so these run on a real GPU surface.
/// </summary>
public class SpriteSeamTests
{
    // 7x7 white cell at (1, 1) of a 9x9 atlas; the border texels are the color the seam would show.
    static SKBitmap CreateAtlas(SKColor border)
    {
        var atlas = new SKBitmap(9, 9);
        atlas.Erase(border);
        for (int x = 1; x < 8; x++)
        {
            for (int y = 1; y < 8; y++)
            {
                atlas.SetPixel(x, y, SKColors.White);
            }
        }
        return atlas;
    }

    static SKBitmap RenderThreeSprites(float containerX, float zoom, SKFilterMode filter, int sampleCount, SKColor atlasBorder)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        SKFilterMode previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
        try
        {
            return WglGpuContext.Instance.Render(100, 40, sampleCount, canvas =>
            {
                var managers = new SystemManagers();
                managers.Initialize();
                SystemManagers.Default = managers;
                managers.Canvas = canvas;
                managers.Renderer.ClearsCanvas = false;
                managers.Renderer.Camera.Zoom = zoom;
                RenderingLibrary.Graphics.Renderer.TextureFilter = filter;

                using SKBitmap atlas = CreateAtlas(atlasBorder);
                var container = new ContainerRuntime
                {
                    X = containerX,
                    Y = 5,
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.Absolute,
                    Width = 21,
                    Height = 7,
                };
                container.AddToManagers(managers, null);
                for (int i = 0; i < 3; i++)
                {
                    var sprite = new SpriteRuntime
                    {
                        Texture = atlas,
                        TextureAddress = TextureAddress.Custom,
                        TextureLeft = 1,
                        TextureTop = 1,
                        TextureWidth = 7,
                        TextureHeight = 7,
                        WidthUnits = DimensionUnitType.Absolute,
                        HeightUnits = DimensionUnitType.Absolute,
                        X = i * 7,
                        Width = 7,
                        Height = 7,
                    };
                    container.Children.Add(sprite);
                }
                canvas.Clear(SKColors.Black);
                managers.Draw();
            });
        }
        finally
        {
            RenderingLibrary.Graphics.Renderer.TextureFilter = previousFilter;
        }
    }

    [Theory]
    [InlineData(7f, 1f)]
    [InlineData(7f, 2f)]
    [InlineData(6.75f, 2f)]
    [InlineData(6.5f, 2f)]
    [InlineData(6.25f, 2f)]
    [InlineData(6.5f, 1f)]
    [InlineData(6.5f, 3f)]
    [InlineData(6.75f, 3f)]
    public void Draw_PointFilterSpritesEndToEnd_ShouldHaveNoSeam(float containerX, float zoom)
    {
        using SKBitmap pixels = RenderThreeSprites(containerX, zoom, SKFilterMode.Nearest, 0, SKColors.Transparent);

        // The run spans containerX * zoom to (containerX + 21) * zoom; check the pixels strictly inside it.
        int first = (int)MathF.Ceiling(containerX * zoom) + 1;
        int last = (int)MathF.Floor((containerX + 21) * zoom) - 2;
        int row = (int)((5 + 3) * zoom);
        for (int x = first; x <= last; x++)
        {
            SKColor color = pixels.GetPixel(x, row);
            color.Red.ShouldBe((byte)255, $"seam at pixel {x}, container X {containerX}, zoom {zoom}");
        }
    }

    [Theory]
    [InlineData(6.75f)]
    [InlineData(6.5f)]
    public void Draw_PointFilterSprite_ShouldNeverSampleOutsideSourceRectangle(float containerX)
    {
        // Border texels are red; any red pixel means a texel outside the source rectangle was sampled.
        using SKBitmap pixels = RenderThreeSprites(containerX, 2f, SKFilterMode.Nearest, 0, new SKColor(255, 0, 0));
        int row = 16;
        for (int x = 0; x < pixels.Width; x++)
        {
            SKColor color = pixels.GetPixel(x, row);
            (color.Red == 255 && color.Green == 0 && color.Blue == 0).ShouldBeFalse($"red border texel sampled at pixel {x}");
        }
    }
}
