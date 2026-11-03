using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Under ReplaceAlpha and MinAlpha an <see cref="ArcRuntime"/> masks its whole Width x Height bounds, not only
/// the swept stroke (#7525): the stroke keeps what is below it and the rest of the bounds, including the part of
/// the ring a partial sweep leaves out, is erased. Skia is the reference; the same scene and expected pixels are
/// in SkiaGum.Tests' ArcAlphaMaskTests.
/// Raylib is a known inconsistency, not tested (RaylibGum.Tests has no pixel harness): LineArc only blends the pixels
/// the swept band draws, so the rest of the bounds is not erased.
/// Each 36x36 arc has a 6px full-alpha stroke, over an opaque red render target and an opaque backdrop.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ArcAlphaMaskSceneTests : BaseTestClass
{
    private const int Width = 120;
    private const int Height = 80;
    private const int Size = 36;

    private static readonly XnaColor Red = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);
    private static readonly XnaColor Backdrop = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);

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
        XnaColor[] pixels = Render(blend);

        AssertPixel(pixels, 2, 2, TopRight, Red, "ring");
        AssertPixel(pixels, 2, 2, BottomLeft, Red, "ring");
        AssertPixel(pixels, 2, 2, (18, 18), Backdrop, "inside the ring");
        AssertPixel(pixels, 2, 2, (1, 1), Backdrop, "bounds corner");
        AssertPixel(pixels, 2, 2, (-1, 18), Red, "outside the bounds");
    }

    // The swept quarter keeps the ring; the three quarters left out are still inside the bounds, so they are erased.
    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    public void PartialSweep90_KeepsOnlyTheSweptRing_ErasesTheRestOfTheBounds(Gum.RenderingLibrary.Blend blend)
    {
        XnaColor[] pixels = Render(blend);

        AssertPixel(pixels, 42, 2, TopRight, Red, "swept ring");
        AssertPixel(pixels, 42, 2, TopLeft, Backdrop, "ring zone left out of the sweep");
        AssertPixel(pixels, 42, 2, BottomLeft, Backdrop, "ring zone left out of the sweep");
        AssertPixel(pixels, 42, 2, BottomRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(pixels, 42, 2, (18, 18), Backdrop, "inside the ring");
        AssertPixel(pixels, 42, 2, (-1, 18), Red, "outside the bounds");
    }

    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    public void PartialSweep270_KeepsOnlyTheSweptRing_ErasesTheRestOfTheBounds(Gum.RenderingLibrary.Blend blend)
    {
        XnaColor[] pixels = Render(blend);

        AssertPixel(pixels, 82, 2, TopRight, Red, "swept ring");
        AssertPixel(pixels, 82, 2, TopLeft, Red, "swept ring");
        AssertPixel(pixels, 82, 2, BottomLeft, Red, "swept ring");
        AssertPixel(pixels, 82, 2, BottomRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(pixels, 82, 2, (18, 18), Backdrop, "inside the ring");
    }

    // A rotated arc masks its rotated bounds: the arcs below are turned 90 degrees, so the quarter that was top
    // right is top left, and the quarter left out of the 270 sweep (bottom right) is top right.
    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    public void RotatedPartialSweeps_MaskTheRotatedBounds(Gum.RenderingLibrary.Blend blend)
    {
        XnaColor[] pixels = Render(blend);

        // Sweep 90, rotated: the bounds are x 2..38, y 42..78.
        AssertPixel(pixels, 2, 42, TopLeft, Red, "swept ring");
        AssertPixel(pixels, 2, 42, TopRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(pixels, 2, 42, BottomRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(pixels, 2, 42, (18, 18), Backdrop, "inside the ring");
        AssertPixel(pixels, 2, 42, (-1, 18), Red, "outside the bounds");

        // Sweep 270, rotated: the bounds are x 42..78, y 42..78.
        AssertPixel(pixels, 42, 42, TopLeft, Red, "swept ring");
        AssertPixel(pixels, 42, 42, BottomRight, Red, "swept ring");
        AssertPixel(pixels, 42, 42, TopRight, Backdrop, "ring zone left out of the sweep");
        AssertPixel(pixels, 42, 42, (18, 18), Backdrop, "inside the ring");
    }

    private static void AssertPixel(XnaColor[] pixels, int left, int top, (int X, int Y) offset, XnaColor expected, string name)
    {
        int x = left + offset.X;
        int y = top + offset.Y;
        XnaColor actual = pixels[(y * Width) + x];
        System.Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(3, $"{name}: R at ({x}, {y}) was {actual.R}");
        System.Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(3, $"{name}: G at ({x}, {y}) was {actual.G}");
        System.Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(3, $"{name}: B at ({x}, {y}) was {actual.B}");
    }

    private static XnaColor[] Render(Gum.RenderingLibrary.Blend blend)
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new();
        root.Width = Width;
        root.Height = Height;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = Width, Height = Height, Color = Backdrop });
        ContainerRuntime renderTarget = new();
        renderTarget.Width = Width;
        renderTarget.Height = Height;
        renderTarget.IsRenderTarget = true;
        renderTarget.AddChild(new ColoredRectangleRuntime { Width = Width, Height = Height, Color = Red });
#pragma warning restore CS0618

        // Row 0: full ring, sweep 90, sweep 270. Row 1: sweeps 90 and 270 turned 90 degrees (the box turns about
        // its top left, so it sits 36px lower to land in the row).
        AddArc(renderTarget, 2, 2, 360, 0, blend);
        AddArc(renderTarget, 42, 2, 90, 0, blend);
        AddArc(renderTarget, 82, 2, 270, 0, blend);
        AddArc(renderTarget, 2, 42 + Size, 90, 90, blend);
        AddArc(renderTarget, 42, 42 + Size, 270, 90, blend);
        root.AddChild(renderTarget);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, Width, Height, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);
        XnaColor[] pixels = new XnaColor[Width * Height];
        capture.GetData(pixels);
        root.RemoveFromManagers();
        return pixels;
    }

    private static void AddArc(ContainerRuntime parent, float x, float y, float sweep, float rotation,
        Gum.RenderingLibrary.Blend blend)
    {
        ArcRuntime arc = new()
        {
            X = x, Y = y, Width = Size, Height = Size, StartAngle = 0, SweepAngle = sweep,
            Thickness = 6, Color = XnaColor.White, Rotation = rotation, Blend = blend,
        };
        parent.AddChild(arc);
    }
}
