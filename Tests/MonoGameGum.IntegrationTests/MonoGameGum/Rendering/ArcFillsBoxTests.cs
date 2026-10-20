using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6256: an <see cref="ArcRuntime"/> fills its whole box, like Skia's, when the box is not square.
/// The circle is drawn at the shorter side and the batch view stretches the longer axis, so the stroke is
/// exact on the short axis and thicker along the long one (thickness * long / short). Skia's reference
/// numbers for the same boxes: outer extent equal to the box, stroke 8.0 on both axes. Real GL, Apos.Shapes.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ArcFillsBoxTests : BaseTestClass
{
    private const int CaptureSize = 240;
    private const float StrokeWidth = 8;

    private static readonly XnaColor Backdrop = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);

    [Theory]
    [InlineData(20f, 20f, 60f, 40f)]
    [InlineData(20f, 20f, 40f, 60f)]
    [InlineData(20f, 20f, 200f, 20f)]
    [InlineData(20f, 20f, 20f, 200f)]
    public void FullSweepArc_InNonSquareBox_HasItsOuterExtentOnTheBox(float x, float y, float width, float height)
    {
        Scene scene = RenderArc(arc => { }, x, y, width, height);

        (int left, int top, int right, int bottom) = CoveredBounds(scene.Pixels);

        left.ShouldBe((int)x);
        top.ShouldBe((int)y);
        right.ShouldBe((int)(x + width) - 1);
        bottom.ShouldBe((int)(y + height) - 1);
        // The anti-aliasing ramp is stretched with the arc, so a very long box has a soft tip.
        MaxCoverageOutside(scene.Pixels, x, y, width, height).ShouldBeLessThan(0.15);
    }

    [Theory]
    [InlineData(20.3f, 20.6f, 59.5f, 39.7f)]
    [InlineData(20.6f, 20.3f, 39.7f, 59.5f)]
    public void FullSweepArc_InNonSquareBoxAtFractionalPosition_IsCenteredOnTheBox(float x, float y, float width, float height)
    {
        Scene scene = RenderArc(arc => { }, x, y, width, height);

        (double cx, double cy) = CoverageCentroid(scene.Pixels);

        cx.ShouldBe(x + (width / 2.0), 0.2);
        cy.ShouldBe(y + (height / 2.0), 0.2);
        (int left, int top, int right, int bottom) = CoveredBounds(scene.Pixels);
        left.ShouldBeInRange((int)Math.Round(x) - 1, (int)Math.Round(x) + 1);
        top.ShouldBeInRange((int)Math.Round(y) - 1, (int)Math.Round(y) + 1);
        right.ShouldBeInRange((int)Math.Round(x + width) - 2, (int)Math.Round(x + width));
        bottom.ShouldBeInRange((int)Math.Round(y + height) - 2, (int)Math.Round(y + height));
    }

    [Fact]
    public void FullSweepArc_InRotatedBox_FillsTheRotatedBox()
    {
        // A 60x40 box turned 90 degrees counterclockwise about its top-left corner at (20,80) occupies x 20..60, y 20..80.
        Scene scene = RenderArc(arc => arc.Rotation = 90, 20, 80, 60, 40);

        (int left, int top, int right, int bottom) = CoveredBounds(scene.Pixels);

        left.ShouldBe(20);
        top.ShouldBe(20);
        right.ShouldBe(59);
        bottom.ShouldBe(79);
        MaxCoverageOutside(scene.Pixels, 20, 20, 40, 60).ShouldBeLessThan(0.05);
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(0.5f)]
    public void FullSweepArc_InNonSquareBox_FillsTheBoxUnderCameraZoom(float zoom)
    {
        Scene scene = RenderArc(arc => { }, 20, 20, 60, 40, zoom);

        (int left, int top, int right, int bottom) = CoveredBounds(scene.Pixels);

        left.ShouldBe((int)(20 * zoom));
        top.ShouldBe((int)(20 * zoom));
        right.ShouldBe((int)(80 * zoom) - 1);
        bottom.ShouldBe((int)(60 * zoom) - 1);
    }

    [Fact]
    public void FullSweepArc_InWideBox_IsExactOnTheShortAxisAndThickerOnTheLongAxis()
    {
        Scene scene = RenderArc(arc => { }, 20, 20, 60, 40);

        // Left edge at the box's middle row, top edge at the middle column.
        double horizontal = Thickness(scene.Pixels, 40, vertical: false, from: 16, to: 40);
        double vertical = Thickness(scene.Pixels, 50, vertical: true, from: 16, to: 40);

        vertical.ShouldBe(StrokeWidth, 0.2);
        horizontal.ShouldBe(StrokeWidth * 60 / 40, 0.8);
    }

    [Fact]
    public void FullSweepArc_InTallBox_IsExactOnTheShortAxisAndThickerOnTheLongAxis()
    {
        Scene scene = RenderArc(arc => { }, 20, 20, 40, 60);

        double horizontal = Thickness(scene.Pixels, 50, vertical: false, from: 16, to: 40);
        double vertical = Thickness(scene.Pixels, 40, vertical: true, from: 16, to: 40);

        horizontal.ShouldBe(StrokeWidth, 0.2);
        vertical.ShouldBe(StrokeWidth * 60 / 40, 0.8);
    }

    [Theory]
    [InlineData(1f, 60f, 40f)]
    [InlineData(2f, 60f, 40f)]
    [InlineData(1f, 200f, 20f)]
    [InlineData(2f, 200f, 20f)]
    [InlineData(1f, 20f, 200f)]
    [InlineData(2f, 20f, 200f)]
    public void ThinStroke_InNonSquareBox_NeverLeavesAGap(float stroke, float width, float height)
    {
        Scene scene = RenderArc(arc => arc.Thickness = stroke, 10, 10, width, height);

        // A closed band crosses every column of the box once in each half (top and bottom), and every row
        // once in each half (left and right). A gap anywhere leaves one of those halves nearly empty; an
        // unbroken anti-aliased line adds up to about its stroke width.
        int left = 10;
        int top = 10;
        int right = 10 + (int)width;
        int bottom = 10 + (int)height;
        int middleX = (left + right) / 2;
        int middleY = (top + bottom) / 2;

        for (int x = left + 2; x < right - 2; x++)
        {
            SumCoverage(scene.Pixels, x, x, top, middleY).ShouldBeGreaterThan(0.6, $"no band in the top half of column {x}");
            SumCoverage(scene.Pixels, x, x, middleY, bottom).ShouldBeGreaterThan(0.6, $"no band in the bottom half of column {x}");
        }
        for (int y = top + 2; y < bottom - 2; y++)
        {
            SumCoverage(scene.Pixels, left, middleX, y, y).ShouldBeGreaterThan(0.6, $"no band in the left half of row {y}");
            SumCoverage(scene.Pixels, middleX, right, y, y).ShouldBeGreaterThan(0.6, $"no band in the right half of row {y}");
        }
    }

    [Fact]
    public void PartialSweepArc_InWideBox_CoversOnlyItsSweepAndStaysInsideTheBox()
    {
        // 40 to 240 degrees counterclockwise: the top (90), left (180) and bottom-left, not the right (0) or bottom (270).
        Scene scene = RenderArc(arc =>
        {
            arc.StartAngle = 40;
            arc.SweepAngle = 200;
        }, 20, 20, 60, 40);

        Coverage(scene.Pixels, 50, 22).ShouldBeGreaterThan(0.9);
        Coverage(scene.Pixels, 22, 40).ShouldBeGreaterThan(0.9);
        Coverage(scene.Pixels, 77, 40).ShouldBeLessThan(0.1);
        Coverage(scene.Pixels, 50, 57).ShouldBeLessThan(0.1);
        MaxCoverageOutside(scene.Pixels, 20, 20, 60, 40).ShouldBeLessThan(0.05);
    }

    [Fact]
    public void RoundedEndArc_InWideBox_StaysInsideTheBox()
    {
        Scene scene = RenderArc(arc =>
        {
            arc.StartAngle = 40;
            arc.SweepAngle = 200;
            arc.IsEndRounded = true;
        }, 20, 20, 60, 40);

        Coverage(scene.Pixels, 22, 40).ShouldBeGreaterThan(0.9);
        MaxCoverageOutside(scene.Pixels, 20, 20, 60, 40).ShouldBeLessThan(0.05);
    }

    [Fact]
    public void Dropshadow_InWideBox_IsOffsetByTheShadowOffsetInWorldSpace()
    {
        // The shadow is not squashed with the arc: a 10 pixel down offset moves it 10 pixels down.
        Scene scene = RenderArc(arc =>
        {
            arc.HasDropshadow = true;
            arc.DropshadowColor = new XnaColor((byte)0, (byte)255, (byte)0, (byte)255);
            arc.DropshadowOffsetY = 10;
            arc.DropshadowBlur = 0;
            arc.Color = new XnaColor((byte)255, (byte)0, (byte)0, (byte)255);
        }, 20, 20, 60, 40);

        // Below the arc's bottom edge (row 59) the shadow's bottom edge reaches row 69.
        ((int)scene.Pixels[(65 * CaptureSize) + 50].G).ShouldBeGreaterThan(200);
        ((int)scene.Pixels[(72 * CaptureSize) + 50].G).ShouldBeLessThan(120);
    }

    [Fact]
    public void LinearGradient_InWideBox_RunsAcrossTheBoxNotTheUnstretchedCircle()
    {
        // Color1 left of box x=5, Color2 right of box x=7 (screen x 25 and 27). The long axis is stretched, so
        // endpoints given in box coordinates must be mapped back before Apos reads them.
        Scene scene = RenderArc(arc =>
        {
            arc.UseGradient = true;
            arc.GradientType = GradientType.Linear;
            arc.Color1 = new XnaColor((byte)255, (byte)0, (byte)0, (byte)255);
            arc.Color2 = new XnaColor((byte)0, (byte)0, (byte)255, (byte)255);
            arc.GradientX1 = 5;
            arc.GradientY1 = 0;
            arc.GradientX2 = 7;
            arc.GradientY2 = 0;
        }, 20, 20, 60, 40);

        XnaColor beforeTransition = scene.Pixels[(40 * CaptureSize) + 22];
        XnaColor afterTransition = scene.Pixels[(40 * CaptureSize) + 30];

        ((int)beforeTransition.R).ShouldBeGreaterThan(beforeTransition.B);
        ((int)afterTransition.B).ShouldBeGreaterThan(afterTransition.R);
    }

    [Fact]
    public void SquareArc_PaysNoExtraShapeBatchFlushes_AndNonSquarePaysTwo()
    {
        int square = RenderArc(arc => { }, 20, 20, 50, 50).ShapeBatchBegins;
        int wide = RenderArc(arc => { }, 20, 20, 60, 40).ShapeBatchBegins;

        (wide - square).ShouldBe(2);
    }

    [Fact]
    public void DashedArc_InWideBox_StillDrawsDashesAlongTheWholeBand()
    {
        Scene scene = RenderArc(arc =>
        {
            arc.StrokeDashLength = 6;
            arc.StrokeGapLength = 6;
        }, 20, 20, 60, 40);

        // Walk the top edge row inside the band: there must be both drawn and empty stretches.
        int drawn = 0;
        int empty = 0;
        for (int x = 24; x < 76; x++)
        {
            if (Coverage(scene.Pixels, x, 22) > 0.5)
            {
                drawn++;
            }
            else
            {
                empty++;
            }
        }
        drawn.ShouldBeGreaterThan(5);
        empty.ShouldBeGreaterThan(5);
    }

    internal sealed record Scene(XnaColor[] Pixels, int ShapeBatchBegins);

    private static Scene RenderArc(Action<ArcRuntime> configure, float x, float y, float width, float height, float zoom = 1f)
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

        ArcRuntime arc = new();
        arc.Color = new XnaColor((byte)255, (byte)0, (byte)0, (byte)255);
        arc.Thickness = StrokeWidth;
        arc.StartAngle = 0;
        arc.SweepAngle = 360;
        configure(arc);
        arc.X = x;
        arc.Y = y;
        arc.Width = width;
        arc.Height = height;
        root.AddChild(arc);

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
        int begins = managers.Renderer.RenderStateChangeStatistics.ShapeBatchBeginCount;

        XnaColor[] pixels = new XnaColor[CaptureSize * CaptureSize];
        capture.GetData(pixels);
        root.RemoveFromManagers();
        return new Scene(pixels, begins);
    }

    // The red channel runs from the backdrop (0) to the arc's red (1).
    private static double Coverage(XnaColor[] pixels, int x, int y) =>
        (pixels[(y * CaptureSize) + x].R - Backdrop.R) / (double)(255 - Backdrop.R);

    private static (int Left, int Top, int Right, int Bottom) CoveredBounds(XnaColor[] pixels)
    {
        int left = CaptureSize;
        int top = CaptureSize;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < CaptureSize; y++)
        {
            for (int x = 0; x < CaptureSize; x++)
            {
                if (Coverage(pixels, x, y) > 0.5)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }
        }
        return (left, top, right, bottom);
    }

    private static (double X, double Y) CoverageCentroid(XnaColor[] pixels)
    {
        double sum = 0;
        double sumX = 0;
        double sumY = 0;
        for (int y = 0; y < CaptureSize; y++)
        {
            for (int x = 0; x < CaptureSize; x++)
            {
                double c = Math.Max(0, Coverage(pixels, x, y));
                sum += c;
                sumX += c * (x + 0.5);
                sumY += c * (y + 0.5);
            }
        }
        return (sumX / sum, sumY / sum);
    }

    private static double SumCoverage(XnaColor[] pixels, int fromX, int toX, int fromY, int toY)
    {
        double max = 0;
        for (int y = fromY; y <= toY; y++)
        {
            for (int x = fromX; x <= toX; x++)
            {
                max += Math.Max(0, Coverage(pixels, x, y));
            }
        }
        return max;
    }

    private static double MaxCoverageOutside(XnaColor[] pixels, float x, float y, float width, float height)
    {
        double max = 0;
        for (int py = 0; py < CaptureSize; py++)
        {
            for (int px = 0; px < CaptureSize; px++)
            {
                bool inside = px + 1 > x && px < x + width && py + 1 > y && py < y + height;
                if (!inside)
                {
                    max = Math.Max(max, Coverage(pixels, px, py));
                }
            }
        }
        return max;
    }

    // Sum of coverage along a row (or column, when vertical) from one pixel to another: the band's thickness.
    private static double Thickness(XnaColor[] pixels, int lineIndex, bool vertical, int from, int to)
    {
        double sum = 0;
        for (int i = from; i < to; i++)
        {
            sum += vertical ? Coverage(pixels, lineIndex, i) : Coverage(pixels, i, lineIndex);
        }
        return sum;
    }
}
