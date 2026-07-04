using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;

namespace MonoGameGumInCode.Screens;

// PolygonRuntime survey on the MonoGame side. Mirrors
// Samples/SilkNetGum/SilkNetGum/Screens/PolygonsScreen.cs — same sections, same point lists,
// same parameter sweeps — so visual regressions in one backend are easy to spot against the
// other. Added with the unification of PolygonRuntime under issue #2757.
//
// PolygonRuntime on MG/Raylib wraps LinePolygon, which renders as a polyline. To close a
// shape the point list must repeat the first point at the end. On Skia, the contained
// Polygon renderable auto-closes when IsClosed = true (the default) and draws as an open
// polyline when IsClosed = false. The Skia screen toggles IsClosed for the open-polyline
// section; this side just omits the closing point.
//
// Layout convention: every container that sets WidthUnits / HeightUnits to
// RelativeToChildren also sets Width / Height = 0 so the explicit dim doesn't add padding.
internal class PolygonsScreen : FrameworkElement
{
    const float CellSize = 72;
    const float Center = CellSize / 2;
    const float Radius = 26;

    public PolygonsScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 14;
        root.X = 10;
        root.Y = 10;
        AddChild(root);

        root.AddChild(BuildSection("Common shapes (triangle, square, pentagon, hexagon)", BuildShapesRow()));
        root.AddChild(BuildSection("StrokeWidth (1, 2, 4, 8 px) — hexagon outline", BuildStrokeWidthRow()));
        root.AddChild(BuildSection("Color (white, red, green, yellow) — pentagon outline", BuildColorRow()));
        root.AddChild(BuildSection("Alpha on Color (255, 192, 128, 64) — hexagon outline", BuildAlphaRow()));
        root.AddChild(BuildSection("Concave / complex shapes (5-point star, arrow, plus, chevron)", BuildConcaveRow()));
        root.AddChild(BuildSection("Dashed strokes (solid, 6/4, 2/2, 12/6) — MG's LinePolygon only has a fixed-pattern dotted texture, so any dash+gap > 0 reads as the same binary dot pattern; Skia honors the lengths verbatim", BuildDashedRow()));
        root.AddChild(BuildSection("Open polylines (zigzag, M, V, wave) — MG omits closing point; Skia sets IsClosed = false", BuildOpenRow()));
    }

    static ContainerRuntime BuildSection(string label, GraphicalUiElement body)
    {
        ContainerRuntime section = new();
        section.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        section.StackSpacing = 4;
        section.WidthUnits = DimensionUnitType.RelativeToChildren;
        section.HeightUnits = DimensionUnitType.RelativeToChildren;
        section.Width = 0;
        section.Height = 0;

        TextRuntime header = new();
        header.Text = label;
        header.Red = 220;
        header.Green = 220;
        header.Blue = 220;
        section.AddChild(header);
        section.AddChild(body);
        return section;
    }

    static ContainerRuntime BuildHorizontalRow()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 12;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        return row;
    }

    // Builds a CellSize x CellSize backing frame and parents the supplied polygon to it so
    // the row layout has a predictable per-cell footprint. Polygon points are in the cell's
    // local space (PixelsFromSmall), centered around (Center, Center).
    static RectangleRuntime BuildCell(PolygonRuntime polygon)
    {
        RectangleRuntime frame = new();
        frame.Width = CellSize;
        frame.Height = CellSize;
        frame.FillColor = new Color(50, 50, 70);
        frame.IsFilled = true;
        frame.Children.Add(polygon);
        return frame;
    }

    static PolygonRuntime BuildPolygon(Vector2[] points, Color color, float strokeWidth = 1)
    {
        PolygonRuntime polygon = new();
        polygon.SetPoints(points);
        polygon.Color = color;
        polygon.StrokeWidth = strokeWidth;
        return polygon;
    }

    // Regular n-gon centered at (Center, Center). Top vertex sits straight up. Returns the
    // point list with the first point repeated at the end so MG's LinePolygon renders a
    // closed outline; the Skia screen omits the closing point and relies on IsClosed.
    static Vector2[] RegularPolygon(int sides, float radius)
    {
        var pts = new List<Vector2>(sides + 1);
        for (int i = 0; i < sides; i++)
        {
            float angle = -MathF.PI / 2 + i * MathF.Tau / sides;
            pts.Add(new Vector2(Center + radius * MathF.Cos(angle), Center + radius * MathF.Sin(angle)));
        }
        pts.Add(pts[0]);
        return pts.ToArray();
    }

    static ContainerRuntime BuildShapesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (int sides in new[] { 3, 4, 5, 6 })
        {
            row.AddChild(BuildCell(BuildPolygon(RegularPolygon(sides, Radius), Color.White, 2)));
        }
        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            row.AddChild(BuildCell(BuildPolygon(RegularPolygon(6, Radius), Color.LightGreen, strokeWidth)));
        }
        return row;
    }

    static ContainerRuntime BuildColorRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (Color color in new[] { Color.White, Color.Crimson, Color.LimeGreen, Color.Gold })
        {
            row.AddChild(BuildCell(BuildPolygon(RegularPolygon(5, Radius), color, 2)));
        }
        return row;
    }

    static ContainerRuntime BuildAlphaRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (byte alpha in new byte[] { 255, 192, 128, 64 })
        {
            row.AddChild(BuildCell(BuildPolygon(RegularPolygon(6, Radius), new Color((byte)255, (byte)255, (byte)255, alpha), 2)));
        }
        return row;
    }

    // 5-point star: vertices alternate between an outer and inner radius (golden-ratio inner
    // for the classic look). All shapes are closed outlines — the closing point is appended
    // so LinePolygon (which does not auto-close) draws the final edge back to start.
    static Vector2[] Star()
    {
        const int points = 5;
        float outerRadius = Radius;
        float innerRadius = Radius * 0.4f;
        var pts = new List<Vector2>(points * 2 + 1);
        for (int i = 0; i < points * 2; i++)
        {
            float angle = -MathF.PI / 2 + i * MathF.PI / points;
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            pts.Add(new Vector2(Center + r * MathF.Cos(angle), Center + r * MathF.Sin(angle)));
        }
        pts.Add(pts[0]);
        return pts.ToArray();
    }

    static Vector2[] Arrow()
    {
        // Right-pointing arrow; tail on the left, head spans the right edge.
        return new[]
        {
            new Vector2(Center - 24, Center - 6),
            new Vector2(Center + 6,  Center - 6),
            new Vector2(Center + 6,  Center - 18),
            new Vector2(Center + 24, Center),
            new Vector2(Center + 6,  Center + 18),
            new Vector2(Center + 6,  Center + 6),
            new Vector2(Center - 24, Center + 6),
            new Vector2(Center - 24, Center - 6),
        };
    }

    static Vector2[] Plus()
    {
        return new[]
        {
            new Vector2(Center - 8,  Center - 22),
            new Vector2(Center + 8,  Center - 22),
            new Vector2(Center + 8,  Center - 8),
            new Vector2(Center + 22, Center - 8),
            new Vector2(Center + 22, Center + 8),
            new Vector2(Center + 8,  Center + 8),
            new Vector2(Center + 8,  Center + 22),
            new Vector2(Center - 8,  Center + 22),
            new Vector2(Center - 8,  Center + 8),
            new Vector2(Center - 22, Center + 8),
            new Vector2(Center - 22, Center - 8),
            new Vector2(Center - 8,  Center - 8),
            new Vector2(Center - 8,  Center - 22),
        };
    }

    static Vector2[] Chevron()
    {
        return new[]
        {
            new Vector2(Center - 22, Center - 16),
            new Vector2(Center - 6,  Center - 16),
            new Vector2(Center + 14, Center),
            new Vector2(Center - 6,  Center + 16),
            new Vector2(Center - 22, Center + 16),
            new Vector2(Center - 2,  Center),
            new Vector2(Center - 22, Center - 16),
        };
    }

    static ContainerRuntime BuildConcaveRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        row.AddChild(BuildCell(BuildPolygon(Star(),    Color.Gold,       2)));
        row.AddChild(BuildCell(BuildPolygon(Arrow(),   Color.Cyan,       2)));
        row.AddChild(BuildCell(BuildPolygon(Plus(),    Color.Magenta,    2)));
        row.AddChild(BuildCell(BuildPolygon(Chevron(), Color.LightGreen, 2)));
        return row;
    }

    static PolygonRuntime BuildDashedHexagon(float dash, float gap)
    {
        PolygonRuntime polygon = BuildPolygon(RegularPolygon(6, Radius), Color.White, 2);
        polygon.StrokeDashLength = dash;
        polygon.StrokeGapLength = gap;
        return polygon;
    }

    static ContainerRuntime BuildDashedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        row.AddChild(BuildCell(BuildDashedHexagon(0,  0)));
        row.AddChild(BuildCell(BuildDashedHexagon(6,  4)));
        row.AddChild(BuildCell(BuildDashedHexagon(2,  2)));
        row.AddChild(BuildCell(BuildDashedHexagon(12, 6)));
        return row;
    }

    static Vector2[] Zigzag()
    {
        return new[]
        {
            new Vector2(Center - 24, Center + 16),
            new Vector2(Center - 12, Center - 16),
            new Vector2(Center,      Center + 16),
            new Vector2(Center + 12, Center - 16),
            new Vector2(Center + 24, Center + 16),
        };
    }

    static Vector2[] MShape()
    {
        return new[]
        {
            new Vector2(Center - 22, Center + 20),
            new Vector2(Center - 22, Center - 20),
            new Vector2(Center,      Center + 6),
            new Vector2(Center + 22, Center - 20),
            new Vector2(Center + 22, Center + 20),
        };
    }

    static Vector2[] VShape()
    {
        return new[]
        {
            new Vector2(Center - 22, Center - 20),
            new Vector2(Center,      Center + 22),
            new Vector2(Center + 22, Center - 20),
        };
    }

    static Vector2[] Wave()
    {
        // Three-segment sampled sine: amplitude 14, four points across.
        return new[]
        {
            new Vector2(Center - 24, Center),
            new Vector2(Center - 8,  Center - 14),
            new Vector2(Center + 8,  Center + 14),
            new Vector2(Center + 24, Center),
        };
    }

    static ContainerRuntime BuildOpenRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        row.AddChild(BuildCell(BuildPolygon(Zigzag(), Color.White, 2)));
        row.AddChild(BuildCell(BuildPolygon(MShape(), Color.Cyan,  2)));
        row.AddChild(BuildCell(BuildPolygon(VShape(), Color.Gold,  2)));
        row.AddChild(BuildCell(BuildPolygon(Wave(),   Color.Pink,  2)));
        return row;
    }
}
