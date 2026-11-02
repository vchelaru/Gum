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
/// use never did, so these run on a real GPU surface. Linear filtering bleeds into the border by design and is not
/// covered here.
/// </summary>
[Collection(GpuCollection.Name)]
public class SpriteSeamTests
{
    static readonly SKColor Red = new SKColor(255, 0, 0);

    readonly WglGpuContext _gpu;

    public SpriteSeamTests(WglGpuContext gpu)
    {
        _gpu = gpu;
    }

    /// <summary>
    /// A grid of Columns x Rows sprites of SpriteSize gum units placed end to end in a container, each drawing the
    /// CellSize square at (CellLeft, CellLeft) of an atlas that is CellSize + 2 * CellLeft wide, with the texels
    /// around the cell painted Border.
    /// </summary>
    record Layout(
        float ContainerX,
        float ContainerY,
        float Zoom,
        int Columns = 3,
        int Rows = 1,
        int SpriteSize = 7,
        int CellSize = 7,
        int CellLeft = 1,
        SKColor? Border = null,
        bool FlipHorizontal = false,
        bool FlipVertical = false);

    SKBitmap Render(Layout layout)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        SKFilterMode previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
        int width = (int)MathF.Ceiling((layout.ContainerX + layout.Columns * layout.SpriteSize) * layout.Zoom) + 8;
        int height = (int)MathF.Ceiling((layout.ContainerY + layout.Rows * layout.SpriteSize) * layout.Zoom) + 8;
        try
        {
            return _gpu.Render(width, height, 0, canvas =>
            {
                var managers = new SystemManagers();
                managers.Initialize();
                SystemManagers.Default = managers;
                managers.Canvas = canvas;
                managers.Renderer.ClearsCanvas = false;
                managers.Renderer.Camera.Zoom = layout.Zoom;
                RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;

                int atlasSize = layout.CellSize + 2 * layout.CellLeft;
                using var atlas = new SKBitmap(atlasSize, atlasSize);
                atlas.Erase(layout.Border ?? SKColors.Transparent);
                for (int x = 0; x < layout.CellSize; x++)
                {
                    for (int y = 0; y < layout.CellSize; y++)
                    {
                        atlas.SetPixel(layout.CellLeft + x, layout.CellLeft + y, SKColors.White);
                    }
                }

                var container = new ContainerRuntime
                {
                    X = layout.ContainerX,
                    Y = layout.ContainerY,
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.Absolute,
                    Width = layout.Columns * layout.SpriteSize,
                    Height = layout.Rows * layout.SpriteSize,
                };
                container.AddToManagers(managers, null);
                for (int row = 0; row < layout.Rows; row++)
                {
                    for (int column = 0; column < layout.Columns; column++)
                    {
                        var sprite = new SpriteRuntime
                        {
                            Texture = atlas,
                            TextureAddress = TextureAddress.Custom,
                            TextureLeft = layout.CellLeft,
                            TextureTop = layout.CellLeft,
                            TextureWidth = layout.CellSize,
                            TextureHeight = layout.CellSize,
                            WidthUnits = DimensionUnitType.Absolute,
                            HeightUnits = DimensionUnitType.Absolute,
                            X = column * layout.SpriteSize,
                            Y = row * layout.SpriteSize,
                            Width = layout.SpriteSize,
                            Height = layout.SpriteSize,
                            FlipHorizontal = layout.FlipHorizontal,
                            FlipVertical = layout.FlipVertical,
                        };
                        container.Children.Add(sprite);
                    }
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

    static int CountColor(SKBitmap pixels, SKColor color)
    {
        int count = 0;
        for (int y = 0; y < pixels.Height; y++)
        {
            for (int x = 0; x < pixels.Width; x++)
            {
                SKColor pixel = pixels.GetPixel(x, y);
                if (pixel.Red == color.Red && pixel.Green == color.Green && pixel.Blue == color.Blue)
                {
                    count++;
                }
            }
        }
        return count;
    }

    // The first and last pixel whose center is clearly inside [left, right] (device pixels), so a pixel center sitting
    // exactly on an edge, which either neighbor may own, is not asserted.
    static (int First, int Last) PixelsInside(float left, float right) =>
        ((int)MathF.Floor(left - 0.5f + 0.001f) + 1, (int)MathF.Ceiling(right - 0.5f - 0.001f) - 1);

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
        using SKBitmap pixels = Render(new Layout(containerX, 5, zoom));

        (int first, int last) = PixelsInside(containerX * zoom, (containerX + 21) * zoom);
        int row = (int)((5 + 3) * zoom);
        for (int x = first; x <= last; x++)
        {
            pixels.GetPixel(x, row).Red.ShouldBe((byte)255, $"seam at pixel {x}, container X {containerX}, zoom {zoom}");
        }
    }

    [Theory]
    [InlineData(6.75f)]
    [InlineData(6.5f)]
    public void Draw_PointFilterSprite_ShouldNeverSampleOutsideSourceRectangle(float containerX)
    {
        // Border texels are red; any red pixel means a texel outside the source rectangle was sampled.
        using SKBitmap pixels = Render(new Layout(containerX, 5, 2f, Border: Red));
        CountColor(pixels, Red).ShouldBe(0);
    }

    public static IEnumerable<object[]> FlippedCases()
    {
        foreach ((bool h, bool v) in new[] { (true, false), (false, true), (true, true) })
        {
            foreach (float containerX in new[] { 7f, 6.75f, 6.5f, 6.25f })
            {
                foreach (float zoom in new[] { 1f, 2f, 3f })
                {
                    yield return new object[] { h, v, containerX, zoom };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(FlippedCases))]
    public void Draw_FlippedPointFilterSprites_ShouldNeverSampleOutsideSourceRectangle(
        bool flipHorizontal, bool flipVertical, float containerX, float zoom)
    {
        var layout = new Layout(containerX, 2.5f, zoom, Columns: 3, Rows: 3, Border: Red,
            FlipHorizontal: flipHorizontal, FlipVertical: flipVertical);
        using SKBitmap pixels = Render(layout);
        CountColor(pixels, Red).ShouldBe(0, $"red border pixels, flip H {flipHorizontal} V {flipVertical}, container X {containerX}, zoom {zoom}");
    }

    [Theory]
    [InlineData(32f, 0.5f)]
    [InlineData(32f, 6.75f)]
    [InlineData(64f, 0.5f)]
    [InlineData(64f, 6.75f)]
    public void Draw_PointFilterSpriteAtHighZoom_ShouldNotBleedBorder(float zoom, float containerX)
    {
        using SKBitmap pixels = Render(new Layout(containerX, 0.5f, zoom, Columns: 1, Border: Red));
        CountColor(pixels, Red).ShouldBe(0);
    }

    [Theory]
    [InlineData(4f, 0.5f)]
    [InlineData(4f, 0.25f)]
    [InlineData(2f, 0.5f)]
    public void Draw_PointFilterSpriteMagnifiedFromOneTexel_ShouldNotBleedBorder(float zoom, float containerX)
    {
        // One texel stretched over 10 gum units: 20 to 40 device pixels per texel.
        using SKBitmap pixels = Render(new Layout(containerX, 0.5f, zoom, Columns: 1, SpriteSize: 10, CellSize: 1, Border: Red));
        CountColor(pixels, Red).ShouldBe(0);
    }

    [Theory]
    [InlineData(6.75f, 1f)]
    [InlineData(6.75f, 2f)]
    [InlineData(6.5f, 1f)]
    [InlineData(6.5f, 3f)]
    [InlineData(6.25f, 2f)]
    public void Draw_PointFilterSpriteTouchingAtlasEdge_ShouldNotShrinkOrSeam(float containerX, float zoom)
    {
        // The source rectangle is the whole 7x7 atlas, so nothing may be sampled or clipped past its edge.
        using SKBitmap pixels = Render(new Layout(containerX, 2.5f, zoom, Columns: 3, Rows: 1, CellLeft: 0));

        (int first, int last) = PixelsInside(containerX * zoom, (containerX + 21) * zoom);
        (int firstRow, int lastRow) = PixelsInside(2.5f * zoom, 9.5f * zoom);
        for (int y = firstRow; y <= lastRow; y++)
        {
            for (int x = first; x <= last; x++)
            {
                pixels.GetPixel(x, y).Red.ShouldBe((byte)255, $"not white at ({x}, {y}), container X {containerX}, zoom {zoom}");
            }
        }
    }
}
