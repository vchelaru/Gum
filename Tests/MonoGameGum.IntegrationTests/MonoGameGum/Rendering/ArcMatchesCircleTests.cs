using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using Shouldly;
using System;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6254: an <see cref="ArcRuntime"/> with a 360 degree sweep is a stroked <see cref="CircleRuntime"/>
/// with the same box and stroke width, so the two must draw the same pixels (real GL, Apos.Shapes).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ArcMatchesCircleTests : BaseTestClass
{
    internal const int CaptureSize = 100;
    internal const float BoxSize = 60;
    internal const float StrokeWidth = 8;

    internal static readonly XnaColor Backdrop = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    internal static readonly XnaColor ShapeColor = new XnaColor((byte)255, (byte)0, (byte)0, (byte)255);

    [Theory]
    [InlineData(20f)]
    [InlineData(20.5f)]
    [InlineData(20.3f)]
    public void FullSweepArc_DrawsSamePixelsAsStrokedCircle(float position)
    {
        XnaColor[] circle = Render(position, ShapeKind.Circle);
        XnaColor[] arc = Render(position, ShapeKind.FullSweepArc);

        (int count, int max, int x, int y) = Compare(circle, arc);
        max.ShouldBeLessThanOrEqualTo(2,
            $"{count} pixels differ; largest channel difference {max} at ({x},{y}): circle {Format(circle[(y * CaptureSize) + x])} arc {Format(arc[(y * CaptureSize) + x])}");
    }

    [Theory]
    [InlineData(false, 1f)]
    [InlineData(true, 2f)]
    [InlineData(true, 0.5f)]
    public void FullSweepArc_DrawsSamePixelsAsStrokedCircle_WithoutAntialiasingAndUnderZoom(bool isAntialiased, float zoom)
    {
        XnaColor[] circle = Render(20f, ShapeKind.Circle, isAntialiased, zoom);
        XnaColor[] arc = Render(20f, ShapeKind.FullSweepArc, isAntialiased, zoom);

        (int count, int max, int x, int y) = Compare(circle, arc);
        max.ShouldBeLessThanOrEqualTo(2,
            $"{count} pixels differ; largest channel difference {max} at ({x},{y}): circle {Format(circle[(y * CaptureSize) + x])} arc {Format(arc[(y * CaptureSize) + x])}");
    }

    // The arc's mid point at 180 degrees lands on the circle's left edge, where its band is the same
    // 8 pixels. Rounded ends are a different draw call in Apos.Shapes.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PartialSweepArc_AtItsMiddle_HasTheStrokeWidthOfAStrokedCircle(bool isEndRounded)
    {
        XnaColor[] circle = Render(20f, ShapeKind.Circle);
        XnaColor[] arc = Render(20f, ShapeKind.PartialSweepArc, isEndRounded: isEndRounded);

        for (int x = 17; x <= 31; x++)
        {
            Math.Abs(arc[(50 * CaptureSize) + x].R - circle[(50 * CaptureSize) + x].R)
                .ShouldBeLessThanOrEqualTo(2, $"pixel ({x},50)");
        }
    }

    [Fact]
    public void FullSweepArc_AtIntegerPosition_ChangesNoPixelsOutsideItsBox()
    {
        XnaColor[] arc = Render(20f, ShapeKind.FullSweepArc);

        CountChangedOutside(arc, 20f).ShouldBe(0);
    }

    [Fact]
    public void PartialSweepArc_AtIntegerPosition_ChangesNoPixelsOutsideItsBox()
    {
        XnaColor[] arc = Render(20f, ShapeKind.PartialSweepArc);

        CountChangedOutside(arc, 20f).ShouldBe(0);
    }

    internal enum ShapeKind { Circle, FullSweepArc, PartialSweepArc }

    internal static XnaColor[] Render(float position, ShapeKind kind, bool isAntialiased = true, float zoom = 1f, bool isEndRounded = false)
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new();
        root.Width = CaptureSize;
        root.Height = CaptureSize;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = Backdrop });
#pragma warning restore CS0618

        Gum.Wireframe.GraphicalUiElement shape;
        if (kind == ShapeKind.Circle)
        {
            CircleRuntime circle = new();
            circle.IsFilled = false;
            circle.StrokeWidth = StrokeWidth;
            circle.StrokeColor = ShapeColor;
            circle.IsAntialiased = isAntialiased;
            shape = circle;
        }
        else
        {
            ArcRuntime arc = new();
            arc.Color = ShapeColor;
            arc.Thickness = StrokeWidth;
            arc.StartAngle = kind == ShapeKind.FullSweepArc ? 0 : 40;
            arc.SweepAngle = kind == ShapeKind.FullSweepArc ? 360 : 200;
            arc.IsAntialiased = isAntialiased;
            arc.IsEndRounded = isEndRounded;
            shape = arc;
        }
        shape.X = position;
        shape.Y = position;
        shape.Width = BoxSize;
        shape.Height = BoxSize;
        root.AddChild(shape);

        root.AddToManagers(managers, null);
        root.UpdateLayout();
        float originalZoom = managers.Renderer.Camera.Zoom;
        managers.Renderer.Camera.Zoom = zoom;

        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);
        managers.Renderer.Camera.Zoom = originalZoom;

        XnaColor[] pixels = new XnaColor[CaptureSize * CaptureSize];
        capture.GetData(pixels);
        root.RemoveFromManagers();
        return pixels;
    }

    internal static (int Count, int Max, int X, int Y) Compare(XnaColor[] a, XnaColor[] b)
    {
        int count = 0;
        int max = 0;
        int maxX = 0;
        int maxY = 0;
        for (int y = 0; y < CaptureSize; y++)
        {
            for (int x = 0; x < CaptureSize; x++)
            {
                XnaColor ca = a[(y * CaptureSize) + x];
                XnaColor cb = b[(y * CaptureSize) + x];
                int diff = Math.Max(Math.Max(Math.Abs(ca.R - cb.R), Math.Abs(ca.G - cb.G)),
                    Math.Max(Math.Abs(ca.B - cb.B), Math.Abs(ca.A - cb.A)));
                if (diff > 0)
                {
                    count++;
                }
                if (diff > max)
                {
                    max = diff;
                    maxX = x;
                    maxY = y;
                }
            }
        }
        return (count, max, maxX, maxY);
    }

    internal static int CountChangedOutside(XnaColor[] pixels, float position)
    {
        int count = 0;
        for (int y = 0; y < CaptureSize; y++)
        {
            for (int x = 0; x < CaptureSize; x++)
            {
                bool inside = x >= position && x < position + BoxSize && y >= position && y < position + BoxSize;
                XnaColor c = pixels[(y * CaptureSize) + x];
                int diff = Math.Max(Math.Abs(c.R - Backdrop.R), Math.Max(Math.Abs(c.G - Backdrop.G), Math.Abs(c.B - Backdrop.B)));
                if (!inside && diff > 2)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private static string Format(XnaColor c) => $"({c.R},{c.G},{c.B},{c.A})";
}
