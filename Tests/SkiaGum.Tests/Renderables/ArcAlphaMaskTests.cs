using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Under ReplaceAlpha and MinAlpha an <see cref="ArcRuntime"/> masks its whole Width x Height bounds, not only
/// the swept stroke (#7525): the stroke keeps what is below it and the rest of the bounds, including the part of
/// the ring a partial sweep leaves out, is erased. Skia is the reference; the same scene and expected pixels are
/// in MonoGameGum.IntegrationTests' ArcAlphaMaskSceneTests.
/// Each 36x36 arc has a 6px full-alpha stroke, over an opaque red render target and an opaque backdrop.
/// </summary>
public class ArcAlphaMaskTests
{
    private const int Width = 120;
    private const int Height = 80;
    private const int Size = 36;

    private static readonly SKColor Red = new SKColor(200, 30, 30);
    private static readonly SKColor Backdrop = new SKColor(70, 70, 90);

    // Probe points on the middle of the ring, 45 degrees into each quadrant (center 18,18, radius 15).
    private static readonly (int X, int Y) TopRight = (29, 7);
    private static readonly (int X, int Y) TopLeft = (7, 7);
    private static readonly (int X, int Y) BottomLeft = (7, 29);
    private static readonly (int X, int Y) BottomRight = (29, 29);

    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    public void FullRing_KeepsTheRing_ErasesTheRestOfTheBounds(Gum.RenderingLibrary.Blend blend)
    {
        using SKBitmap bitmap = Render(blend);

        AssertPixel(bitmap, 2, 2, TopRight, Red, "ring");
        AssertPixel(bitmap, 2, 2, BottomLeft, Red, "ring");
        AssertPixel(bitmap, 2, 2, (18, 18), Backdrop, "inside the ring");
        AssertPixel(bitmap, 2, 2, (1, 1), Backdrop, "bounds corner");
        AssertPixel(bitmap, 2, 2, (-1, 18), Red, "outside the bounds");
    }

    // The swept quarter keeps the ring; the three quarters left out are still inside the bounds, so they are erased.
    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    public void PartialSweep90_KeepsOnlyTheSweptRing_ErasesTheRestOfTheBounds(Gum.RenderingLibrary.Blend blend)
    {
        using SKBitmap bitmap = Render(blend);

        AssertPixel(bitmap, 42, 2, TopRight, Red, "swept ring");
        AssertPixel(bitmap, 42, 2, TopLeft, Backdrop, "ring zone left out of the sweep");
        AssertPixel(bitmap, 42, 2, BottomLeft, Backdrop, "ring zone left out of the sweep");
        AssertPixel(bitmap, 42, 2, BottomRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(bitmap, 42, 2, (18, 18), Backdrop, "inside the ring");
        AssertPixel(bitmap, 42, 2, (-1, 18), Red, "outside the bounds");
    }

    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    public void PartialSweep270_KeepsOnlyTheSweptRing_ErasesTheRestOfTheBounds(Gum.RenderingLibrary.Blend blend)
    {
        using SKBitmap bitmap = Render(blend);

        AssertPixel(bitmap, 82, 2, TopRight, Red, "swept ring");
        AssertPixel(bitmap, 82, 2, TopLeft, Red, "swept ring");
        AssertPixel(bitmap, 82, 2, BottomLeft, Red, "swept ring");
        AssertPixel(bitmap, 82, 2, BottomRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(bitmap, 82, 2, (18, 18), Backdrop, "inside the ring");
    }

    // A rotated arc masks its rotated bounds: the arcs below are turned 90 degrees, so the quarter that was top
    // right is top left, and the quarter left out of the 270 sweep (bottom right) is top right.
    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    public void RotatedPartialSweeps_MaskTheRotatedBounds(Gum.RenderingLibrary.Blend blend)
    {
        using SKBitmap bitmap = Render(blend);

        // Sweep 90, rotated: the bounds are x 2..38, y 42..78.
        AssertPixel(bitmap, 2, 42, TopLeft, Red, "swept ring");
        AssertPixel(bitmap, 2, 42, TopRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(bitmap, 2, 42, BottomRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(bitmap, 2, 42, (18, 18), Backdrop, "inside the ring");
        AssertPixel(bitmap, 2, 42, (-1, 18), Red, "outside the bounds");

        // Sweep 270, rotated: the bounds are x 42..78, y 42..78.
        AssertPixel(bitmap, 42, 42, TopLeft, Red, "swept ring");
        AssertPixel(bitmap, 42, 42, BottomRight, Red, "swept ring");
        AssertPixel(bitmap, 42, 42, TopRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(bitmap, 42, 42, (18, 18), Backdrop, "inside the ring");
    }

    private static void AssertPixel(SKBitmap bitmap, int left, int top, (int X, int Y) offset, SKColor expected, string name)
    {
        int x = left + offset.X;
        int y = top + offset.Y;
        SKColor actual = bitmap.GetPixel(x, y);
        ((int)actual.Red).ShouldBeInRange(expected.Red - 3, expected.Red + 3, $"{name}: red at ({x}, {y})");
        ((int)actual.Green).ShouldBeInRange(expected.Green - 3, expected.Green + 3, $"{name}: green at ({x}, {y})");
        ((int)actual.Blue).ShouldBeInRange(expected.Blue - 3, expected.Blue + 3, $"{name}: blue at ({x}, {y})");
    }

    private static SKBitmap Render(Gum.RenderingLibrary.Blend blend)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(Width, Height));
        Gum.GumService.Default.Initialize(surface.Canvas, Width, Height);
        ContainerRuntime root = new() { Width = Width, Height = Height };
        root.Children.Add(new RectangleRuntime
        {
            Width = Width, Height = Height, IsFilled = true, StrokeWidth = 0, FillColor = Backdrop,
        });
        ContainerRuntime renderTarget = new() { Width = Width, Height = Height, IsRenderTarget = true };
        renderTarget.Children.Add(new RectangleRuntime
        {
            Width = Width, Height = Height, IsFilled = true, StrokeWidth = 0, FillColor = Red,
        });

        // Row 0: full ring, sweep 90, sweep 270. Row 1: sweeps 90 and 270 turned 90 degrees (the box turns about
        // its top left, so it sits 36px lower to land in the row).
        AddArc(renderTarget, 2, 2, 360, 0, blend);
        AddArc(renderTarget, 42, 2, 90, 0, blend);
        AddArc(renderTarget, 82, 2, 270, 0, blend);
        AddArc(renderTarget, 2, 42 + Size, 90, 90, blend);
        AddArc(renderTarget, 42, 42 + Size, 270, 90, blend);

        root.Children.Add(renderTarget);
        Gum.GumService.Default.Root.Children.Add(root);

        Gum.GumService.Default.Draw();

        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    private static void AddArc(ContainerRuntime parent, float x, float y, float sweep, float rotation,
        Gum.RenderingLibrary.Blend blend)
    {
        ArcRuntime arc = new()
        {
            X = x, Y = y, Width = Size, Height = Size, StartAngle = 0, SweepAngle = sweep,
            Thickness = 6, StrokeColor = SKColors.White, Rotation = rotation, Blend = blend,
        };
        parent.Children.Add(arc);
    }
}
