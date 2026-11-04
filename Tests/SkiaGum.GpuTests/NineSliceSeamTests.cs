using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// #7102: seams (background showing through) at NineSlice section edges and between stacked children placed at
/// fractional positions and zooms, on a GPU surface. Every texture here is solid white drawn over black, so any pixel
/// that is clearly inside the drawn bounds and not white is a seam. Text, antialiasing and one pixel rounding
/// differences are deliberately not covered.
/// </summary>
[Collection(GpuCollection.Name)]
public class NineSliceSeamTests
{
    readonly WglGpuContext _gpu;

    public NineSliceSeamTests(WglGpuContext gpu)
    {
        _gpu = gpu;
    }

    static SKBitmap WhiteBitmap(int size)
    {
        var bitmap = new SKBitmap(size, size);
        bitmap.Erase(SKColors.White);
        return bitmap;
    }

    SKBitmap Render(float zoom, float width, float height, SKFilterMode filter, Action<SystemManagers, SKBitmap> build)
    {
        GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        SKFilterMode previousFilter = RenderingLibrary.Graphics.Renderer.TextureFilter;
        int pixelWidth = (int)MathF.Ceiling(width * zoom) + 8;
        int pixelHeight = (int)MathF.Ceiling(height * zoom) + 8;
        try
        {
            return _gpu.Render(pixelWidth, pixelHeight, 0, canvas =>
            {
                var managers = new SystemManagers();
                managers.Initialize();
                SystemManagers.Default = managers;
                managers.Canvas = canvas;
                managers.Renderer.ClearsCanvas = false;
                managers.Renderer.Camera.Zoom = zoom;
                RenderingLibrary.Graphics.Renderer.TextureFilter = filter;
                using SKBitmap texture = WhiteBitmap(9);
                canvas.Clear(SKColors.Black);
                build(managers, texture);
                managers.Draw();
            });
        }
        finally
        {
            RenderingLibrary.Graphics.Renderer.TextureFilter = previousFilter;
        }
    }

    // The first and last pixel whose center is clearly inside [left, right] (device pixels).
    static (int First, int Last) PixelsInside(float left, float right) =>
        ((int)MathF.Floor(left - 0.5f + 0.001f) + 1, (int)MathF.Ceiling(right - 0.5f - 0.001f) - 1);

    static void ShouldBeSolidWhite(SKBitmap pixels, float left, float top, float right, float bottom, string context)
    {
        (int firstX, int lastX) = PixelsInside(left, right);
        (int firstY, int lastY) = PixelsInside(top, bottom);
        for (int y = firstY; y <= lastY; y++)
        {
            for (int x = firstX; x <= lastX; x++)
            {
                SKColor pixel = pixels.GetPixel(x, y);
                (pixel.Red, pixel.Green, pixel.Blue).ShouldBe(((byte)255, (byte)255, (byte)255), $"seam at ({x}, {y}), {context}");
            }
        }
    }

    static readonly float[] Offsets = { 3f, 3.1f, 3.25f, 3.33f, 3.5f, 3.75f };

    public static IEnumerable<object[]> PositionCases()
    {
        foreach (SKFilterMode filter in new[] { SKFilterMode.Nearest, SKFilterMode.Linear })
        {
            foreach (float zoom in new[] { 1f, 1.5f, 2f, 3f })
            {
                foreach (float x in Offsets)
                {
                    foreach (float y in Offsets)
                    {
                        yield return new object[] { filter, zoom, x, y };
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(PositionCases))]
    public void Draw_NineSliceAtFractionalPosition_ShouldHaveNoSeam(SKFilterMode filter, float zoom, float x, float y)
    {
        const float width = 20.5f;
        const float height = 17.25f;
        using SKBitmap pixels = Render(zoom, x + width, y + height, filter, (managers, texture) =>
        {
            var nineSlice = new NineSliceRuntime
            {
                Texture = texture,
                TextureAddress = TextureAddress.EntireTexture,
                X = x,
                Y = y,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
                Width = width,
                Height = height,
            };
            nineSlice.AddToManagers(managers, null);
        });

        ShouldBeSolidWhite(pixels, x * zoom, y * zoom, (x + width) * zoom, (y + height) * zoom, $"filter {filter}, x {x}, y {y}, zoom {zoom}");
    }

    [Theory]
    [InlineData(1f, 3.5f, 2.25f)]
    [InlineData(1.5f, 3.5f, 2.25f)]
    [InlineData(2f, 3.5f, 2.25f)]
    [InlineData(3f, 3.5f, 2.25f)]
    [InlineData(1.5f, 3.33f, 2.75f)]
    [InlineData(3f, 3.1f, 2.5f)]
    public void Draw_NineSliceWithBorderScale_ShouldHaveNoSeam(float zoom, float x, float y)
    {
        const float width = 23.5f;
        const float height = 19.75f;
        using SKBitmap pixels = Render(zoom, x + width, y + height, SKFilterMode.Nearest, (managers, texture) =>
        {
            var nineSlice = new NineSliceRuntime
            {
                Texture = texture,
                TextureAddress = TextureAddress.EntireTexture,
                X = x,
                Y = y,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
                Width = width,
                Height = height,
                BorderScale = 1.5f,
            };
            nineSlice.AddToManagers(managers, null);
        });

        ShouldBeSolidWhite(pixels, x * zoom, y * zoom, (x + width) * zoom, (y + height) * zoom, $"border scale 1.5, zoom {zoom}");
    }

    // Zooms that never land on a tidy value: 1, 1.0712835, 1.142567, ... up to 3.85. The case count follows SweepScale
    // (every step on CI, about a tenth locally). Offsets and sizes come from a fixed seed so a failure reproduces.
    public static IEnumerable<object[]> AwkwardZoomCases()
    {
        const int steps = 41;
        for (int i = 0; i < steps; i++)
        {
            if (!MonoGameGum.Tests.SweepScale.Includes(i))
            {
                continue;
            }
            var random = new Random(7102 + i);
            float zoom = 1f + i * 0.0712835f;
            float x = 2f + (float)random.NextDouble() * 4f;
            float y = 1f + (float)random.NextDouble() * 4f;
            float offset = (float)random.NextDouble();
            float childHeight = 6f + (float)random.NextDouble() * 3f;
            float spacing = i % 2 == 0 ? 0f : -(float)random.NextDouble();
            SKFilterMode filter = i % 3 == 0 ? SKFilterMode.Linear : SKFilterMode.Nearest;
            yield return new object[] { filter, zoom, x, y, offset, childHeight, spacing };
        }
    }

    [Theory]
    [MemberData(nameof(AwkwardZoomCases))]
    public void Draw_AtAwkwardZoomsAndOffsets_ShouldHaveNoSeam(
        SKFilterMode filter, float zoom, float x, float y, float offset, float childHeight, float spacing)
    {
        Draw_NineSliceAtFractionalPosition_ShouldHaveNoSeam(filter, zoom, x, y);
        Draw_StackedChildrenAtFractionalOffset_ShouldHaveNoSeam(filter, zoom, offset, childHeight, spacing, useNineSlice: false);
        Draw_StackedChildrenAtFractionalOffset_ShouldHaveNoSeam(filter, zoom, offset, childHeight, spacing, useNineSlice: true);
    }

    public static IEnumerable<object[]> StackedCases()
    {
        foreach (SKFilterMode filter in new[] { SKFilterMode.Nearest, SKFilterMode.Linear })
        {
            foreach (float zoom in new[] { 1f, 1.5f, 2f, 3f })
            {
                foreach (float offset in new[] { 0f, 0.25f, 0.5f, 0.75f, 0.33f })
                {
                    foreach (float childHeight in new[] { 7f, 7.4f, 7.5f })
                    {
                        foreach (float spacing in new[] { 0f, -0.5f })
                        {
                            foreach (bool useNineSlice in new[] { false, true })
                            {
                                yield return new object[] { filter, zoom, offset, childHeight, spacing, useNineSlice };
                            }
                        }
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(StackedCases))]
    public void Draw_StackedChildrenAtFractionalOffset_ShouldHaveNoSeam(
        SKFilterMode filter, float zoom, float offset, float childHeight, float spacing, bool useNineSlice)
    {
        // An outer and an inner container both sit at fractional offsets, so the children's absolute position is the
        // sum of two fractions.
        float outerX = 2f + offset;
        float outerY = 1f + offset;
        float innerX = 1.5f + offset / 2;
        float innerY = 1.25f + offset / 3;
        const float width = 12f;
        const int count = 4;
        float totalHeight = count * childHeight + (count - 1) * spacing;
        float x = outerX + innerX;
        float y = outerY + innerY;
        using SKBitmap pixels = Render(zoom, x + width, y + totalHeight, filter, (managers, texture) =>
        {
            var outer = new ContainerRuntime
            {
                X = outerX,
                Y = outerY,
                WidthUnits = DimensionUnitType.RelativeToChildren,
                HeightUnits = DimensionUnitType.RelativeToChildren,
                Width = 0,
                Height = 0,
            };
            outer.AddToManagers(managers, null);
            var container = new ContainerRuntime
            {
                X = innerX,
                Y = innerY,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.RelativeToChildren,
                Width = width,
                Height = 0,
                ChildrenLayout = ChildrenLayout.TopToBottomStack,
                StackSpacing = spacing,
            };
            outer.Children.Add(container);
            for (int i = 0; i < count; i++)
            {
                GraphicalUiElement child = useNineSlice
                    ? new NineSliceRuntime { Texture = texture, TextureAddress = TextureAddress.EntireTexture }
                    : new SpriteRuntime { Texture = texture, TextureAddress = TextureAddress.EntireTexture };
                child.WidthUnits = DimensionUnitType.Absolute;
                child.HeightUnits = DimensionUnitType.Absolute;
                child.Width = width;
                child.Height = childHeight;
                container.Children.Add(child);
            }
        });

        ShouldBeSolidWhite(pixels, x * zoom, y * zoom, (x + width) * zoom, (y + totalHeight) * zoom,
            $"filter {filter}, offset {offset}, child height {childHeight}, spacing {spacing}, nine slice {useNineSlice}, zoom {zoom}");
    }
}
