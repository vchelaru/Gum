using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

public enum StrokeShapeKind { Line, Rectangle, RoundedRectangle, ColoredCircle, Arc, Circle }

/// <summary>
/// Issue #7309: an authored StrokeWidth must draw a ring of exactly that many pixels (what Skia draws).
/// The measured ring is the summed white coverage down the shape's center column from the top edge to the
/// center, on a black backdrop. RectangleRuntime and CircleRuntime are the controls: they already convert
/// the stroke for Apos's 1 px anti-alias fringe.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class AposShapeStrokeWidthPixelTests : BaseTestClass
{
    private const int CaptureSize = 120;

    [Theory]
    [InlineData(StrokeShapeKind.Line, 1f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.Line, 4f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.Line, 12f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.Rectangle, 1f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.Rectangle, 4f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.Rectangle, 4f, 12f, 61f, 0f)]
    [InlineData(StrokeShapeKind.Rectangle, 12f, 12f, 61f, 0f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 1f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 2f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 4f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 4f, 12f, 61f, 0f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 12f, 12f, 61f, 0f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 4f, 12f, 61f, 0.5f)]
    [InlineData(StrokeShapeKind.Circle, 1f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.Circle, 4f, 0f, 61f, 0f)]
    [InlineData(StrokeShapeKind.Circle, 12f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.ColoredCircle, 1f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.ColoredCircle, 2f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.ColoredCircle, 4f, 0f, 61f, 0f)]
    [InlineData(StrokeShapeKind.ColoredCircle, 12f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.Arc, 4f, 0f, 60f, 0f)]
    [InlineData(StrokeShapeKind.Arc, 12f, 0f, 61f, 0f)]
    public void StrokeRing_IsAuthoredWidth(StrokeShapeKind kind, float stroke, float cornerRadius, float size, float offset)
    {
        float measured = MeasureRing(kind, stroke, cornerRadius, size, offset);

        measured.ShouldBe(stroke, tolerance: 0.7f, $"{kind} stroke {stroke} radius {cornerRadius} size {size} offset {offset}");
    }

    // #7350: Skia draws a stroke under 1 px as fractional coverage (a 0.5 px stroke is half intensity over
    // 1 px). Apos cannot draw thinner than its 1 px hairline, so the stroke alpha is scaled by the width.
    [Theory]
    [InlineData(StrokeShapeKind.Line, false, 0.25f)]
    [InlineData(StrokeShapeKind.Line, false, 0.5f)]
    [InlineData(StrokeShapeKind.Line, true, 0.5f)]
    [InlineData(StrokeShapeKind.Line, false, 0.75f)]
    [InlineData(StrokeShapeKind.Rectangle, false, 0.25f)]
    [InlineData(StrokeShapeKind.Rectangle, false, 0.5f)]
    [InlineData(StrokeShapeKind.Rectangle, false, 0.75f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, false, 0.25f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, false, 0.5f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, false, 0.75f)]
    [InlineData(StrokeShapeKind.Circle, false, 0.25f)]
    [InlineData(StrokeShapeKind.Circle, false, 0.5f)]
    [InlineData(StrokeShapeKind.Circle, false, 0.75f)]
    [InlineData(StrokeShapeKind.ColoredCircle, false, 0.5f)]
    public void SubPixelStroke_CoverageIsAuthoredWidth(StrokeShapeKind kind, bool roundedLine, float stroke)
    {
        float measured = MeasureRing(kind, stroke, 0f, 60f, 0f, roundedLine);

        measured.ShouldBe(stroke, tolerance: 0.08f, $"{kind} rounded {roundedLine} stroke {stroke}");
    }

    private static float MeasureRing(StrokeShapeKind kind, float stroke, float cornerRadius, float size, float offset, bool roundedLine = false)
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        XnaColor white = new XnaColor((byte)255, (byte)255, (byte)255, (byte)255);

        ContainerRuntime root = new();
        root.Width = CaptureSize;
        root.Height = CaptureSize;
#pragma warning disable CS0618 // Obsolete runtimes are the Apos-backed standards under test.
        root.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = XnaColor.Black });

        GraphicalUiElement shape;
        switch (kind)
        {
            case StrokeShapeKind.Line:
                shape = new LineRuntime { StrokeWidth = stroke, IsRounded = roundedLine, Color = white };
                break;
            case StrokeShapeKind.Rectangle:
                shape = new RectangleRuntime { IsFilled = false, StrokeWidth = stroke, CornerRadius = cornerRadius, StrokeColor = white };
                break;
            case StrokeShapeKind.RoundedRectangle:
                shape = new RoundedRectangleRuntime { IsFilled = false, StrokeWidth = stroke, CornerRadius = cornerRadius, Color = white };
                break;
            case StrokeShapeKind.Circle:
                shape = new CircleRuntime { IsFilled = false, StrokeWidth = stroke, StrokeColor = white };
                break;
            case StrokeShapeKind.ColoredCircle:
                shape = new ColoredCircleRuntime { IsFilled = false, StrokeWidth = stroke, Color = white };
                break;
            default:
                shape = new ArcRuntime { Thickness = stroke, StartAngle = 0, SweepAngle = 360, Color = white };
                break;
        }
#pragma warning restore CS0618
        shape.X = 20 + offset;
        shape.Y = 20 + offset;
        shape.Width = size;
        shape.Height = kind == StrokeShapeKind.Line ? 0 : size;
        root.AddChild(shape);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);
        XnaColor[] pixels = new XnaColor[CaptureSize * CaptureSize];
        capture.GetData(pixels);
        root.RemoveFromManagers();

        int column = (int)Math.Floor(20 + offset + size / 2f);
        float sum = 0;
        if (kind == StrokeShapeKind.Line)
        {
            // A horizontal line: the whole column is its thickness.
            for (int y = 0; y < CaptureSize; y++)
            {
                sum += pixels[(y * CaptureSize) + column].R / 255f;
            }
            return sum;
        }
        int centerRow = (int)(20 + offset + size / 2f);
        for (int y = 0; y < centerRow; y++)
        {
            sum += pixels[(y * CaptureSize) + column].R / 255f;
        }
        return sum;
    }
}
