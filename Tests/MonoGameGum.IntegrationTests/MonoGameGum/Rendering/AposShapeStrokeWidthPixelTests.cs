using Microsoft.Xna.Framework.Graphics;
using Gum.DataTypes;
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

    // Skia scales the stroke by the camera matrix: at zoom 0.5 a 1 unit stroke is half a pixel and half intensity.
    [Theory]
    [InlineData(StrokeShapeKind.Rectangle, 0.5f, 1f, 0.5f)]
    [InlineData(StrokeShapeKind.Rectangle, 0.5f, 2f, 1f)]
    [InlineData(StrokeShapeKind.Circle, 0.5f, 1f, 0.5f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 0.5f, 1f, 0.5f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 2f, 0.5f, 1f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 2f, 0.25f, 0.5f)]
    public void SubPixelStroke_UnderCameraZoom_CoverageIsScreenWidth(StrokeShapeKind kind, float zoom, float stroke, float expectedCoverage)
    {
        float measured = MeasureRing(kind, stroke, 0f, 60f, 0f, zoom: zoom);

        measured.ShouldBe(expectedCoverage, tolerance: 0.1f, $"{kind} zoom {zoom} stroke {stroke}");
    }

    // The stroke must stay invisible for a zero or negligible width, never become a faint hairline.
    [Theory]
    [InlineData(StrokeShapeKind.Line, 0f)]
    [InlineData(StrokeShapeKind.Line, 0.0001f)]
    [InlineData(StrokeShapeKind.Rectangle, 0f)]
    [InlineData(StrokeShapeKind.Rectangle, 0.0001f)]
    [InlineData(StrokeShapeKind.Circle, 0f)]
    [InlineData(StrokeShapeKind.Circle, 0.0001f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 0f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 0.0001f)]
    [InlineData(StrokeShapeKind.ColoredCircle, 0f)]
    [InlineData(StrokeShapeKind.ColoredCircle, 0.0001f)]
    public void ZeroOrNegligibleStroke_DrawsNothing(StrokeShapeKind kind, float stroke)
    {
        float measured = MeasureRing(kind, stroke, 0f, 60f, 0f);

        measured.ShouldBe(0f, tolerance: 0.02f, $"{kind} stroke {stroke}");
    }

    // A gradient stroke fades like a solid one (both gradient stops are white, so only the fade shows).
    [Theory]
    [InlineData(StrokeShapeKind.RoundedRectangle, 0.5f)]
    [InlineData(StrokeShapeKind.RoundedRectangle, 0.25f)]
    [InlineData(StrokeShapeKind.ColoredCircle, 0.5f)]
    [InlineData(StrokeShapeKind.Line, 0.5f)]
    public void SubPixelGradientStroke_FadesLikeASolidStroke(StrokeShapeKind kind, float stroke)
    {
        float measured = MeasureRing(kind, stroke, 0f, 60f, 0f, gradient: true);

        measured.ShouldBe(stroke, tolerance: 0.08f, $"{kind} gradient stroke {stroke}");
    }

    // #7380: a Line defaults to ScreenPixel units (constant on-screen thickness, as on Skia); with Absolute
    // units the thickness is world units and scales with Camera.Zoom, as the other shapes' strokes do.
    [Theory]
    [InlineData(DimensionUnitType.ScreenPixel, 0.5f, 12f)]
    [InlineData(DimensionUnitType.ScreenPixel, 1f, 12f)]
    [InlineData(DimensionUnitType.ScreenPixel, 2f, 12f)]
    [InlineData(DimensionUnitType.Absolute, 0.5f, 6f)]
    [InlineData(DimensionUnitType.Absolute, 1f, 12f)]
    [InlineData(DimensionUnitType.Absolute, 2f, 24f)]
    public void LineThickness_UnderCameraZoom_FollowsStrokeWidthUnits(DimensionUnitType units, float zoom, float expected)
    {
        float measured = MeasureRing(StrokeShapeKind.Line, 12f, 0f, 60f, 0f, zoom: zoom, units: units);

        measured.ShouldBe(expected, tolerance: 0.7f, $"units {units} zoom {zoom}");
    }

    // #7379: a Line with no stroke draws nothing, at any zoom and with a dropshadow.
    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(0f, 2f)]
    [InlineData(0f, 0.5f)]
    [InlineData(0.0001f, 1f)]
    public void LineWithZeroOrNegligibleStroke_DrawsNothing_AtAnyZoom(float stroke, float zoom)
    {
        MeasureRing(StrokeShapeKind.Line, stroke, 0f, 60f, 0f, zoom: zoom, units: DimensionUnitType.Absolute)
            .ShouldBe(0f, tolerance: 0.02f, $"stroke {stroke} zoom {zoom}");
        MeasureRing(StrokeShapeKind.Line, stroke, 0f, 60f, 0f, roundedLine: true, zoom: zoom)
            .ShouldBe(0f, tolerance: 0.02f, $"rounded stroke {stroke} zoom {zoom}");
    }

    private static float MeasureRing(StrokeShapeKind kind, float stroke, float cornerRadius, float size, float offset, bool roundedLine = false, float zoom = 1f, bool gradient = false, DimensionUnitType? units = null)
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
                if (units.HasValue) { ((LineRuntime)shape).StrokeWidthUnits = units.Value; }
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
        if (gradient)
        {
            AposShapeRuntime apos = (AposShapeRuntime)shape;
            apos.UseGradient = true;
            apos.GradientType = GradientType.Linear;
            apos.Color1 = white;
            apos.Color2 = white;
            apos.GradientX1 = 0;
            apos.GradientY1 = 0;
            apos.GradientX2 = 100;
            apos.GradientY2 = 0;
        }
#pragma warning restore CS0618
        shape.X = 20 + offset;
        shape.Y = 20 + offset;
        shape.Width = size;
        shape.Height = kind == StrokeShapeKind.Line ? 0 : size;
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

        int column = (int)Math.Floor((20 + offset + size / 2f) * zoom);
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
        int centerRow = (int)((20 + offset + size / 2f) * zoom);
        for (int y = 0; y < centerRow; y++)
        {
            sum += pixels[(y * CaptureSize) + column].R / 255f;
        }
        return sum;
    }
}
