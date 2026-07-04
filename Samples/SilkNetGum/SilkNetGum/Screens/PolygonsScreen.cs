using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SilkNetGum.Screens;

// Skia mirror of Samples/MonoGameGumInCode/Screens/PolygonsScreen.cs (issue #2757).
// The two files should stay in lock-step structurally — same sections, same point lists,
// same parameter sweeps — so visual regressions in one backend are easy to spot against the
// other.
//
// What forces the two files apart:
//   - Color type. Microsoft.Xna.Framework.Color becomes SKColor; named colors come from
//     SkiaSharp.SKColors.
//   - Open polylines. MG's LinePolygon doesn't auto-close, so the MG screen omits the
//     closing point. Skia's Polygon auto-closes when IsClosed = true, so this screen sets
//     IsClosed = false on the open-polyline cells instead.
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
        this.AddChild(root);

        root.Children.Add(BuildSection("Common shapes (triangle, square, pentagon, hexagon)", BuildShapesRow()));
        root.Children.Add(BuildSection("StrokeWidth (1, 2, 4, 8 px) — hexagon outline", BuildStrokeWidthRow()));
        root.Children.Add(BuildSection("Color (white, red, green, yellow) — pentagon outline", BuildColorRow()));
        root.Children.Add(BuildSection("Alpha on StrokeColor (255, 192, 128, 64) — hexagon outline", BuildAlphaRow()));
        root.Children.Add(BuildSection("Concave / complex shapes (5-point star, arrow, plus, chevron)", BuildConcaveRow()));
        root.Children.Add(BuildSection("Dashed strokes (solid, 6/4, 2/2, 12/6) — Skia honors dash/gap verbatim via SKPathEffect.CreateDash; raylib walks the perimeter with a continuous arc-length cursor (#2757); MG still shows the binary dotted texture (no per-segment dash control)", BuildDashedRow()));
        root.Children.Add(BuildSection("Open polylines (zigzag, M, V, wave) — Skia and raylib set IsClosed = false; MG omits closing point", BuildOpenRow()));
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
        section.Children.Add(header);
        section.Children.Add(body);
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

    static RectangleRuntime BuildCell(PolygonRuntime polygon)
    {
        RectangleRuntime frame = new();
        frame.Width = CellSize;
        frame.Height = CellSize;
        frame.FillColor = new SKColor(50, 50, 70);
        frame.IsFilled = true;
        frame.Children.Add(polygon);
        return frame;
    }

    static PolygonRuntime BuildPolygon(Vector2[] points, SKColor color, float strokeWidth = 1, bool closed = true)
    {
        PolygonRuntime polygon = new();
        polygon.SetPoints(points);
        polygon.StrokeColor = color;
        polygon.StrokeWidth = strokeWidth;
        polygon.IsClosed = closed;
        return polygon;
    }

    // Regular n-gon centered at (Center, Center). Top vertex sits straight up. Closing point
    // is omitted; Skia's IsClosed = true (the default) draws the final edge back to start.
    // (Mirrors MG's point list, which DOES include the closing point so LinePolygon's
    // open-polyline rendering paints a closed outline; the visual result is identical.)
    static Vector2[] RegularPolygon(int sides, float radius)
    {
        var pts = new List<Vector2>(sides);
        for (int i = 0; i < sides; i++)
        {
            float angle = -MathF.PI / 2 + i * MathF.Tau / sides;
            pts.Add(new Vector2(Center + radius * MathF.Cos(angle), Center + radius * MathF.Sin(angle)));
        }
        return pts.ToArray();
    }

    static ContainerRuntime BuildShapesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (int sides in new[] { 3, 4, 5, 6 })
        {
            row.Children.Add(BuildCell(BuildPolygon(RegularPolygon(sides, Radius), SKColors.White, 2)));
        }
        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            row.Children.Add(BuildCell(BuildPolygon(RegularPolygon(6, Radius), SKColors.LightGreen, strokeWidth)));
        }
        return row;
    }

    static ContainerRuntime BuildColorRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (SKColor color in new[] { SKColors.White, SKColors.Crimson, SKColors.LimeGreen, SKColors.Gold })
        {
            row.Children.Add(BuildCell(BuildPolygon(RegularPolygon(5, Radius), color, 2)));
        }
        return row;
    }

    static ContainerRuntime BuildAlphaRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (byte alpha in new byte[] { 255, 192, 128, 64 })
        {
            row.Children.Add(BuildCell(BuildPolygon(RegularPolygon(6, Radius), new SKColor(255, 255, 255, alpha), 2)));
        }
        return row;
    }

    static Vector2[] Star()
    {
        const int points = 5;
        float outerRadius = Radius;
        float innerRadius = Radius * 0.4f;
        var pts = new List<Vector2>(points * 2);
        for (int i = 0; i < points * 2; i++)
        {
            float angle = -MathF.PI / 2 + i * MathF.PI / points;
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            pts.Add(new Vector2(Center + r * MathF.Cos(angle), Center + r * MathF.Sin(angle)));
        }
        return pts.ToArray();
    }

    static Vector2[] Arrow()
    {
        return new[]
        {
            new Vector2(Center - 24, Center - 6),
            new Vector2(Center + 6,  Center - 6),
            new Vector2(Center + 6,  Center - 18),
            new Vector2(Center + 24, Center),
            new Vector2(Center + 6,  Center + 18),
            new Vector2(Center + 6,  Center + 6),
            new Vector2(Center - 24, Center + 6),
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
        };
    }

    static ContainerRuntime BuildConcaveRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        row.Children.Add(BuildCell(BuildPolygon(Star(),    SKColors.Gold,       2)));
        row.Children.Add(BuildCell(BuildPolygon(Arrow(),   SKColors.Cyan,       2)));
        row.Children.Add(BuildCell(BuildPolygon(Plus(),    SKColors.Magenta,    2)));
        row.Children.Add(BuildCell(BuildPolygon(Chevron(), SKColors.LightGreen, 2)));
        return row;
    }

    static PolygonRuntime BuildDashedHexagon(float dash, float gap)
    {
        PolygonRuntime polygon = BuildPolygon(RegularPolygon(6, Radius), SKColors.White, 2);
        polygon.StrokeDashLength = dash;
        polygon.StrokeGapLength = gap;
        return polygon;
    }

    static ContainerRuntime BuildDashedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        row.Children.Add(BuildCell(BuildDashedHexagon(0,  0)));
        row.Children.Add(BuildCell(BuildDashedHexagon(6,  4)));
        row.Children.Add(BuildCell(BuildDashedHexagon(2,  2)));
        row.Children.Add(BuildCell(BuildDashedHexagon(12, 6)));
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
        row.Children.Add(BuildCell(BuildPolygon(Zigzag(), SKColors.White, 2, closed: false)));
        row.Children.Add(BuildCell(BuildPolygon(MShape(), SKColors.Cyan,  2, closed: false)));
        row.Children.Add(BuildCell(BuildPolygon(VShape(), SKColors.Gold,  2, closed: false)));
        row.Children.Add(BuildCell(BuildPolygon(Wave(),   SKColors.Pink,  2, closed: false)));
        return row;
    }
}
