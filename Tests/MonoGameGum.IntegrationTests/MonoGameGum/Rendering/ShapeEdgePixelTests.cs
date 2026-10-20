using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

public enum EdgeShapeKind { ColoredRectangle, Rectangle, RoundedRectangle, Circle, Arc, Line, Polygon }

public enum EdgePaint { Fill, Stroke, FillAndStroke }

public enum EdgeBlend { Normal, ReplaceAlpha, MinAlpha }

public enum EdgePlacement { Integer, Square, Fractional, Rotated }

/// <summary>
/// Issue #6235: a pixel audit over every MonoGame shape. Each shape is drawn inside a render-target
/// container (so the Replace/Min alpha blends mean something), the capture is read back, and two
/// things are asserted against a reference scene drawn without the shape:
/// <list type="bullet">
/// <item><description>no pixel outside the shape's bounding rectangle changed, and</description></item>
/// <item><description>no pixel inside the bounds is far from the shape's true coverage (a dropped edge
/// pixel, or a pixel drawn where the shape is not).</description></item>
/// </list>
/// Coverage is the red channel between the backdrop and the fill. With Normal the shape draws fill over
/// the backdrop. With ReplaceAlpha and MinAlpha the render target holds the fill and the shape is an
/// opaque mask, so the fill shows where the mask is and the backdrop where it erased.
/// Limits: a pixel the bounds only partly cover (fractional or rotated edges) cannot be exact, because
/// the alpha-replacing blends write the whole pixel, so those get a wide tolerance; Line
/// anti-aliasing is centered on the edge, so half a pixel of fringe outside its bounds is accepted.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ShapeEdgePixelTests : BaseTestClass
{
    internal const int CaptureSize = 100;
    private const int Samples = 6;
    private const float StrokeWidth = 4;
    private const float CornerRadius = 10;
    private const float LineWidth = 6;
    private const float ArcThickness = 8;
    private const float ArcStart = 30;
    private const float ArcSweep = 250;

    internal static readonly XnaColor Backdrop = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    internal static readonly XnaColor Fill = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);

    internal static IEnumerable<EdgeCase> AllCases()
    {
        foreach (EdgeBlend blend in Enum.GetValues<EdgeBlend>())
        {
            foreach (EdgePlacement placement in new[] { EdgePlacement.Integer, EdgePlacement.Fractional })
            {
                yield return new EdgeCase(EdgeShapeKind.ColoredRectangle, EdgePaint.Fill, blend, placement);
                yield return new EdgeCase(EdgeShapeKind.Arc, EdgePaint.Stroke, blend, placement);
                yield return new EdgeCase(EdgeShapeKind.Line, EdgePaint.Stroke, blend, placement);
            }
            // An Arc fills its box like the other shapes (#6256), so it gets the rotated placement too.
            yield return new EdgeCase(EdgeShapeKind.Arc, EdgePaint.Stroke, blend, EdgePlacement.Rotated);
            foreach (EdgeShapeKind kind in new[] { EdgeShapeKind.Rectangle, EdgeShapeKind.RoundedRectangle, EdgeShapeKind.Circle })
            {
                foreach (EdgePaint paint in Enum.GetValues<EdgePaint>())
                {
                    foreach (EdgePlacement placement in Enum.GetValues<EdgePlacement>())
                    {
                        yield return new EdgeCase(kind, paint, blend, placement);
                    }
                }
            }
        }
        yield return new EdgeCase(EdgeShapeKind.Polygon, EdgePaint.Stroke, EdgeBlend.Normal, EdgePlacement.Integer);
    }

    public static TheoryData<EdgeShapeKind, EdgePaint, EdgeBlend, EdgePlacement> Cases()
    {
        TheoryData<EdgeShapeKind, EdgePaint, EdgeBlend, EdgePlacement> data = new();
        foreach (EdgeCase edgeCase in AllCases())
        {
            data.Add(edgeCase.Kind, edgeCase.Paint, edgeCase.Blend, edgeCase.Placement);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Shape_DoesNotTouchPixelsOutsideBounds_AndKeepsItsEdges(
        EdgeShapeKind kind, EdgePaint paint, EdgeBlend blend, EdgePlacement placement)
    {
        EdgeCase edgeCase = new(kind, paint, blend, placement);

        List<string> violations = Audit(edgeCase);

        string detail = $"{edgeCase}: {violations.Count} bad pixels, first {Math.Min(violations.Count, 10)}:"
            + Environment.NewLine + string.Join(Environment.NewLine, violations.Take(10));
        violations.Count.ShouldBe(0, detail);
    }

    [Fact]
    public void AlphaMask_InsideClippingContainer_ErasesInsideTheClip_AndTouchesNothingOutside()
    {
        using ShapeGameLease game = new ShapeGameLease();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new();
        root.Width = CaptureSize;
        root.Height = CaptureSize;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = Backdrop });
        ContainerRuntime renderTarget = new();
        renderTarget.Width = CaptureSize;
        renderTarget.Height = CaptureSize;
        renderTarget.IsRenderTarget = true;
        renderTarget.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = Fill });
#pragma warning restore CS0618

        // The clip is (30,30) to (70,70). The mask's bounds are (25,25) to (75,75), so they extend past the clip.
        ContainerRuntime clip = new();
        clip.X = 30;
        clip.Y = 30;
        clip.Width = 40;
        clip.Height = 40;
        clip.ClipsChildren = true;
        renderTarget.AddChild(clip);
        CircleRuntime mask = new();
        mask.X = -5;
        mask.Y = -5;
        mask.Width = 50;
        mask.Height = 50;
        mask.IsFilled = true;
        mask.StrokeWidth = 0;
        mask.FillColor = new XnaColor((byte)255, (byte)255, (byte)255, (byte)255);
        mask.Blend = Gum.RenderingLibrary.Blend.ReplaceAlpha;
        clip.AddChild(mask);
        root.AddChild(renderTarget);

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

        // Inside the clip and the mask's bounds, but outside the circle: erased. Inside the circle: kept.
        // Outside the clip, though inside the bounds and outside the circle: untouched.
        AssertRed(pixels, 31, 31, Backdrop);
        AssertRed(pixels, 50, 50, Fill);
        AssertRed(pixels, 27, 27, Fill);
        AssertRed(pixels, 27, 50, Fill);
    }

    private static void AssertRed(XnaColor[] pixels, int x, int y, XnaColor expected)
    {
        Math.Abs(pixels[(y * CaptureSize) + x].R - expected.R).ShouldBeLessThanOrEqualTo(3, $"pixel ({x},{y})");
    }

    internal sealed record EdgeCase(EdgeShapeKind Kind, EdgePaint Paint, EdgeBlend Blend, EdgePlacement Placement)
    {
        public override string ToString() => $"{Kind}/{Paint}/{Blend}/{Placement}";
    }

    internal sealed record AuditResult(EdgeCase Case, XnaColor[] Pixels, XnaColor[] Baseline, Geometry Geometry);

    /// <summary>The shape's placement and its true outline in capture pixels.</summary>
    internal sealed class Geometry
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float RotationDegrees;
        public EdgeCase Case = null!;

        // Bounds in the shape's local space (origin at its top-left, before rotation).
        public (double Left, double Top, double Right, double Bottom) LocalBounds;

        public (double lx, double ly) ToLocal(double px, double py)
        {
            double r = RotationDegrees * Math.PI / 180.0;
            double dx = px - X;
            double dy = py - Y;
            return (dx * Math.Cos(r) - dy * Math.Sin(r), dx * Math.Sin(r) + dy * Math.Cos(r));
        }

        public bool InBounds(double px, double py)
        {
            (double lx, double ly) = ToLocal(px, py);
            return lx >= LocalBounds.Left && lx <= LocalBounds.Right && ly >= LocalBounds.Top && ly <= LocalBounds.Bottom;
        }

        public bool InBody(double px, double py)
        {
            (double lx, double ly) = ToLocal(px, py);
            return InBodyLocal(lx, ly);
        }

        private bool InBodyLocal(double lx, double ly)
        {
            bool stroked = Case.Paint == EdgePaint.Stroke;
            switch (Case.Kind)
            {
                case EdgeShapeKind.ColoredRectangle:
                    return lx >= 0 && lx <= Width && ly >= 0 && ly <= Height;
                case EdgeShapeKind.Rectangle:
                    return InRoundedRect(lx, ly, 0, stroked);
                case EdgeShapeKind.RoundedRectangle:
                    return InRoundedRect(lx, ly, CornerRadius, stroked);
                case EdgeShapeKind.Circle:
                {
                    double radius = Math.Min(Width, Height) / 2.0;
                    double d = Math.Sqrt(Math.Pow(lx - Width / 2.0, 2) + Math.Pow(ly - Height / 2.0, 2));
                    return d <= radius && (!stroked || d > radius - StrokeWidth);
                }
                case EdgeShapeKind.Arc:
                {
                    // The arc is a circle at the shorter side stretched along the longer one to fill the box.
                    double shortSide = Math.Min(Width, Height);
                    double px = (lx - Width / 2.0) * shortSide / Width;
                    double py = (ly - Height / 2.0) * shortSide / Height;
                    double d = Math.Sqrt((px * px) + (py * py));
                    if (d > shortSide / 2.0 || d <= shortSide / 2.0 - ArcThickness)
                    {
                        return false;
                    }
                    // Gum arc angles run counterclockwise on screen.
                    double degrees = -Math.Atan2(py, px) * 180.0 / Math.PI;
                    double along = ((degrees - ArcStart) % 360.0 + 360.0) % 360.0;
                    return along <= ArcSweep;
                }
                case EdgeShapeKind.Line:
                    return lx >= 0 && lx <= Width && Math.Abs(ly) <= LineWidth / 2.0;
                default:
                    return false;
            }
        }

        private bool InRoundedRect(double lx, double ly, double radius, bool stroked)
        {
            double qx = Math.Abs(lx - Width / 2.0) - (Width / 2.0 - radius);
            double qy = Math.Abs(ly - Height / 2.0) - (Height / 2.0 - radius);
            double d = Math.Sqrt(Math.Pow(Math.Max(qx, 0), 2) + Math.Pow(Math.Max(qy, 0), 2))
                + Math.Min(Math.Max(qx, qy), 0) - radius;
            return d <= 0 && (!stroked || d > -StrokeWidth);
        }

        public double BodyCoverage(int pixelX, int pixelY) => Coverage(pixelX, pixelY, InBody);

        public double BoundsCoverage(int pixelX, int pixelY) => Coverage(pixelX, pixelY, InBounds);

        // Line anti-aliasing is centred on the geometric edge (no half-pixel inset like Rectangle, Circle and
        // Arc), so up to half a pixel of fringe outside the bounds is accepted for it. Polygon is a
        // one-pixel SpriteBatch outline.
        private double Fringe => Case.Kind switch
        {
            EdgeShapeKind.Line => 0.5,
            EdgeShapeKind.Polygon => 1,
            _ => 0,
        };

        public double FringeBoundsCoverage(int pixelX, int pixelY)
        {
            double fringe = Fringe;
            return Coverage(pixelX, pixelY, (px, py) =>
            {
                (double lx, double ly) = ToLocal(px, py);
                return lx >= LocalBounds.Left - fringe && lx <= LocalBounds.Right + fringe
                    && ly >= LocalBounds.Top - fringe && ly <= LocalBounds.Bottom + fringe;
            });
        }

        private static double Coverage(int pixelX, int pixelY, Func<double, double, bool> inside)
        {
            int hits = 0;
            for (int sy = 0; sy < Samples; sy++)
            {
                for (int sx = 0; sx < Samples; sx++)
                {
                    if (inside(pixelX + (sx + 0.5) / Samples, pixelY + (sy + 0.5) / Samples))
                    {
                        hits++;
                    }
                }
            }
            return hits / (double)(Samples * Samples);
        }
    }

    internal static Geometry CreateGeometry(EdgeCase edgeCase)
    {
        Geometry geometry = new() { Case = edgeCase };
        switch (edgeCase.Placement)
        {
            case EdgePlacement.Integer:
                geometry.X = 20;
                geometry.Y = 20;
                geometry.Width = 60;
                geometry.Height = 44;
                break;
            case EdgePlacement.Square:
                // Odd size: a circle's center lands on a pixel center.
                geometry.X = 20;
                geometry.Y = 20;
                geometry.Width = 61;
                geometry.Height = 61;
                break;
            case EdgePlacement.Fractional:
                geometry.X = 20.3f;
                geometry.Y = 20.6f;
                geometry.Width = 59.5f;
                geometry.Height = 43.7f;
                break;
            case EdgePlacement.Rotated:
                geometry.X = 35;
                geometry.Y = 30;
                geometry.Width = 50;
                geometry.Height = 26;
                geometry.RotationDegrees = 30;
                break;
        }

        switch (edgeCase.Kind)
        {
            case EdgeShapeKind.Line:
                geometry.Y += 22;
                geometry.Height = 0;
                geometry.LocalBounds = (0, -LineWidth / 2.0, geometry.Width, LineWidth / 2.0);
                break;
            case EdgeShapeKind.Polygon:
                geometry.LocalBounds = (0, 0, geometry.Width, geometry.Height);
                break;
            default:
                geometry.LocalBounds = (0, 0, geometry.Width, geometry.Height);
                break;
        }
        return geometry;
    }

    internal static List<string> Audit(EdgeCase edgeCase)
    {
        AuditResult result = RenderCase(edgeCase);
        return Evaluate(result);
    }

    internal static List<string> Evaluate(AuditResult result)
    {
        EdgeCase edgeCase = result.Case;
        Geometry geometry = result.Geometry;
        List<string> violations = new();
        bool isMask = edgeCase.Blend != EdgeBlend.Normal;
        bool crisp = edgeCase.Placement == EdgePlacement.Integer
            && (edgeCase.Kind == EdgeShapeKind.ColoredRectangle
                || (edgeCase.Kind == EdgeShapeKind.Rectangle && edgeCase.Paint == EdgePaint.Fill));

        for (int y = 0; y < CaptureSize; y++)
        {
            for (int x = 0; x < CaptureSize; x++)
            {
                XnaColor actual = result.Pixels[(y * CaptureSize) + x];
                XnaColor baseline = result.Baseline[(y * CaptureSize) + x];

                if (geometry.FringeBoundsCoverage(x, y) == 0)
                {
                    int diff = Math.Max(Math.Abs(actual.R - baseline.R), Math.Max(Math.Abs(actual.G - baseline.G), Math.Abs(actual.B - baseline.B)));
                    if (diff > 2)
                    {
                        violations.Add($"outside bounds ({x},{y}): expected {Format(baseline)} actual {Format(actual)}");
                    }
                    continue;
                }

                if (edgeCase.Kind == EdgeShapeKind.Polygon || geometry.BoundsCoverage(x, y) == 0)
                {
                    // Polygon is a one-pixel outline. Elsewhere this is only the anti-aliasing fringe Line
                    // draws up to half a pixel past its bounds.
                    continue;
                }

                double body = geometry.BodyCoverage(x, y);
                double bounds = geometry.BoundsCoverage(x, y);
                double expected = isMask ? 1 - bounds + body : body;
                double measured = (actual.R - Backdrop.R) / (double)(Fill.R - Backdrop.R);
                // A pixel the bounds only partly cover cannot be exact: a blend that replaces alpha writes the
                // whole pixel. Line anti-aliasing and an Arc's radial end edges are centred on the edge, so they also differ a little.
                double tolerance = crisp ? 0.05 : (bounds is > 0 and < 1) || edgeCase.Kind is EdgeShapeKind.Arc or EdgeShapeKind.Line ? 0.8 : 0.5;
                if (Math.Abs(measured - expected) > tolerance)
                {
                    string kind = measured < expected ? "dropped edge" : "extra pixel";
                    violations.Add($"{kind} ({x},{y}): expected coverage {expected:0.00} actual {measured:0.00} ({Format(actual)})");
                }
            }
        }

        return violations;
    }

    private static string Format(XnaColor color) => $"({color.R},{color.G},{color.B})";

    internal static AuditResult RenderCase(EdgeCase edgeCase, Geometry? geometryOverride = null)
    {
        using ShapeGameLease game = new ShapeGameLease();
        Geometry geometry = geometryOverride ?? CreateGeometry(edgeCase);
        XnaColor[] baseline = RenderScene(game.GraphicsDevice, edgeCase, geometry, shapeVisible: false);
        XnaColor[] pixels = RenderScene(game.GraphicsDevice, edgeCase, geometry, shapeVisible: true);
        return new AuditResult(edgeCase, pixels, baseline, geometry);
    }

    private static XnaColor[] RenderScene(GraphicsDevice gd, EdgeCase edgeCase, Geometry geometry, bool shapeVisible)
    {
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        bool isMask = edgeCase.Blend != EdgeBlend.Normal;

        ContainerRuntime root = new();
        root.Width = CaptureSize;
        root.Height = CaptureSize;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = Backdrop });

        ContainerRuntime renderTarget = new();
        renderTarget.Width = CaptureSize;
        renderTarget.Height = CaptureSize;
        renderTarget.IsRenderTarget = true;
        if (isMask)
        {
            renderTarget.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = Fill });
        }
#pragma warning restore CS0618

        XnaColor shapeColor = isMask ? new XnaColor((byte)255, (byte)255, (byte)255, (byte)255) : Fill;
        Gum.RenderingLibrary.Blend blend = edgeCase.Blend switch
        {
            EdgeBlend.ReplaceAlpha => Gum.RenderingLibrary.Blend.ReplaceAlpha,
            EdgeBlend.MinAlpha => Gum.RenderingLibrary.Blend.MinAlpha,
            _ => Gum.RenderingLibrary.Blend.Normal,
        };

        if (shapeVisible)
        {
            renderTarget.AddChild(CreateShape(edgeCase, geometry, shapeColor, blend));
        }
        root.AddChild(renderTarget);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureSize * CaptureSize];
        capture.GetData(pixels);

        root.RemoveFromManagers();
        return pixels;
    }

    private static Gum.Wireframe.GraphicalUiElement CreateShape(
        EdgeCase edgeCase, Geometry geometry, XnaColor color, Gum.RenderingLibrary.Blend blend)
    {
        bool filled = edgeCase.Paint != EdgePaint.Stroke;
        float stroke = edgeCase.Paint == EdgePaint.Fill ? 0 : StrokeWidth;

        switch (edgeCase.Kind)
        {
            case EdgeShapeKind.ColoredRectangle:
            {
#pragma warning disable CS0618
                ColoredRectangleRuntime rectangle = new();
                rectangle.Color = color;
                rectangle.Blend = blend;
#pragma warning restore CS0618
                Place(rectangle, geometry);
                return rectangle;
            }
            case EdgeShapeKind.Rectangle:
            case EdgeShapeKind.RoundedRectangle:
            {
                RectangleRuntime rectangle = new();
                rectangle.CornerRadius = edgeCase.Kind == EdgeShapeKind.RoundedRectangle ? CornerRadius : 0;
                rectangle.IsFilled = filled;
                rectangle.StrokeWidth = stroke;
                rectangle.FillColor = color;
                rectangle.StrokeColor = color;
                rectangle.Blend = blend;
                Place(rectangle, geometry);
                return rectangle;
            }
            case EdgeShapeKind.Circle:
            {
                CircleRuntime circle = new();
                circle.IsFilled = filled;
                circle.StrokeWidth = stroke;
                circle.FillColor = color;
                circle.StrokeColor = color;
                circle.Blend = blend;
                Place(circle, geometry);
                return circle;
            }
            case EdgeShapeKind.Arc:
            {
                ArcRuntime arc = new();
                arc.Color = color;
                arc.Thickness = ArcThickness;
                arc.StartAngle = ArcStart;
                arc.SweepAngle = ArcSweep;
                ((IBlendedRenderable)arc.RenderableComponent!).Blend = blend;
                Place(arc, geometry);
                return arc;
            }
            case EdgeShapeKind.Line:
            {
                LineRuntime line = new();
                line.Color = color;
                line.StrokeWidth = LineWidth;
                line.IsRounded = false;
                ((IBlendedRenderable)line.RenderableComponent!).Blend = blend;
                Place(line, geometry);
                return line;
            }
            default:
            {
                PolygonRuntime polygon = new();
                polygon.Color = color;
                polygon.SetPoints(new System.Numerics.Vector2[]
                {
                    new(0, 0), new(geometry.Width, 0), new(geometry.Width, geometry.Height), new(0, geometry.Height), new(0, 0),
                });
                Place(polygon, geometry);
                return polygon;
            }
        }
    }

    private static void Place(Gum.Wireframe.GraphicalUiElement element, Geometry geometry)
    {
        element.X = geometry.X;
        element.Y = geometry.Y;
        element.Width = geometry.Width;
        element.Height = geometry.Height;
        element.Rotation = geometry.RotationDegrees;
    }
}
