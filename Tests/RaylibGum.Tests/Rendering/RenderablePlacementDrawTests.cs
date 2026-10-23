using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Where raylib's Sprite, NineSlice and SolidRectangle (ColoredRectangleRuntime) land, matching
/// MonoGame: drawn at their float world position (#6627) and turned about that position,
/// counterclockwise for a positive rotation (#6626). A pixel is covered when its center is inside
/// the shape, so a shape starting at x 10.6 covers columns 11 and up, where a truncated position
/// would cover column 10.
/// </summary>
public class RenderablePlacementDrawTests : BaseTestClass
{
    const int Size = 100;

    static readonly Color Red = new((byte)255, (byte)0, (byte)0, (byte)255);

    readonly System.Collections.Generic.List<Texture2D> _textures = new();

    public enum Kind { Sprite, NineSlice, ColoredRectangle }

    [Theory]
    [InlineData(Kind.Sprite)]
    [InlineData(Kind.NineSlice)]
    [InlineData(Kind.ColoredRectangle)]
    public void Draw_IntegerPosition_CoversExactPixels(Kind kind)
    {
        Bounds bounds = Measure(Capture(Create(kind, x: 10, y: 20, width: 30, height: 15)));

        bounds.ShouldBe(new Bounds(Count: 30 * 15, Left: 10, Top: 20, Right: 39, Bottom: 34), $"actual {bounds}");
    }

    [Theory]
    [InlineData(Kind.Sprite)]
    [InlineData(Kind.NineSlice)]
    [InlineData(Kind.ColoredRectangle)]
    public void Draw_FractionalPosition_IsNotTruncated(Kind kind)
    {
        // Covers [10.6, 40.6) x [20.6, 35.6): pixel centers 11.5..39.5 and 21.5..34.5.
        Bounds bounds = Measure(Capture(Create(kind, x: 10.6f, y: 20.6f, width: 30, height: 15)));

        bounds.ShouldBe(new Bounds(Count: 30 * 15, Left: 11, Top: 21, Right: 40, Bottom: 35), $"actual {bounds}");
    }

    [Theory]
    [InlineData(Kind.Sprite)]
    [InlineData(Kind.ColoredRectangle)]
    public void Draw_Rotated90_TurnsCounterclockwiseAboutPosition(Kind kind)
    {
        GraphicalUiElement element = Create(kind, x: 50, y: 50, width: 30, height: 10);
        element.Rotation = 90;

        // Local (u, v) lands at (50 + v, 50 - u): x in [50, 60), y in [20, 50).
        Bounds bounds = Measure(Capture(element));

        bounds.ShouldBe(new Bounds(Count: 30 * 10, Left: 50, Top: 20, Right: 59, Bottom: 49), $"actual {bounds}");
    }

    [Theory]
    [InlineData(Kind.Sprite)]
    [InlineData(Kind.ColoredRectangle)]
    public void Draw_InRotatedContainer_TurnsWithContainer(Kind kind)
    {
        ContainerRuntime container = new() { X = 40, Y = 60, Width = 0, Height = 0, Rotation = 30 };
        container.AddChild(Create(kind, x: 10, y: 0, width: 20, height: 10));

        // Local center (20, 5) turned 30 degrees counterclockwise about (40, 60):
        // (40 + 20cos30 + 5sin30, 60 - 20sin30 + 5cos30) = (59.82, 54.33). Pixel indices sit half a
        // pixel left of and above the centers they sample.
        Coverage coverage = MeasureCenter(Capture(container));

        coverage.Count.ShouldBeInRange(190, 210, $"actual {coverage}");
        coverage.CenterX.ShouldBe(59.32f, 0.25f, $"actual {coverage}");
        coverage.CenterY.ShouldBe(53.83f, 0.25f, $"actual {coverage}");
    }

    public override void Dispose()
    {
        foreach (Texture2D texture in _textures)
        {
            UnloadTexture(texture);
        }
        _textures.Clear();
        base.Dispose();
    }

    record struct Bounds(int Count, int Left, int Top, int Right, int Bottom);

    record struct Coverage(int Count, float CenterX, float CenterY);

    static bool IsRed(Color c) => c.R > 150 && c.G < 80 && c.B < 80;

    GraphicalUiElement Create(Kind kind, float x, float y, float width, float height)
    {
        GraphicalUiElement element;
        if (kind == Kind.ColoredRectangle)
        {
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; it is the SolidRectangle-backed runtime #6626 reports.
            element = new ColoredRectangleRuntime { Color = Red };
#pragma warning restore CS0618
        }
        else
        {
            Image image = GenImageColor(6, 6, Red);
            Texture2D texture = LoadTextureFromImage(image);
            UnloadImage(image);
            _textures.Add(texture);
            element = kind == Kind.Sprite
                ? new SpriteRuntime { Texture = texture }
                : new NineSliceRuntime { Texture = texture };
        }

        element.X = x;
        element.Y = y;
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = width;
        element.Height = height;
        return element;
    }

    static Bounds Measure(Color[] pixels)
    {
        int count = 0, left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (IsRed(pixels[(y * Size) + x]))
                {
                    count++;
                    left = System.Math.Min(left, x);
                    top = System.Math.Min(top, y);
                    right = System.Math.Max(right, x);
                    bottom = System.Math.Max(bottom, y);
                }
            }
        }
        return new Bounds(count, left, top, right, bottom);
    }

    static Coverage MeasureCenter(Color[] pixels)
    {
        int count = 0;
        long sumX = 0, sumY = 0;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (IsRed(pixels[(y * Size) + x]))
                {
                    count++;
                    sumX += x;
                    sumY += y;
                }
            }
        }
        return count == 0 ? new Coverage(0, -1, -1) : new Coverage(count, sumX / (float)count, sumY / (float)count);
    }

    // Draws the element inside an upright Size x Size render-target wrapper at the origin and
    // returns the baked texture, rows in top-left draw order.
    Color[] Capture(GraphicalUiElement element)
    {
        ContainerRuntime wrapper = new() { X = 0, Y = 0, Width = Size, Height = Size, IsRenderTarget = true };
        wrapper.AddChild(element);
        GumService.Default.Root.Children.Add(wrapper);
        GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        ClearBackground(Color.Black);
        GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(wrapper)!.Value;
        Color[] pixels = new Color[Size * Size];
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            // A render texture is stored bottom-up in GL, so the image's rows are flipped.
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[(y * Size) + x] = GetImageColor(image, x, Size - 1 - y);
                }
            }
        }
        finally
        {
            UnloadImage(image);
        }

        GumService.Default.Root.Children.Clear();
        return pixels;
    }
}
