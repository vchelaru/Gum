using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumShapesGallery.Screens;

// RectangleRuntime survey on the shapes-package side (issues #2768 / #2814). MonoGameGumShapes
// overrides both core rectangle slots with Apos RoundedRectangle — IsFilled=true for fill,
// IsFilled=false for stroke — so CornerRadius renders, strokes are anti-aliased, and the
// same runtime draws fill + stroke simultaneously (the design's headline use case).
//
// Mirrors CirclesScreen layout (two-column root, same section helpers). Section coverage
// stays close to the circles gallery; sections that exercise APIs CircleRuntime exposes on
// the MG side but RectangleRuntime does not (UseGradient, HasDropshadow, StrokeDashLength,
// IsAntialiased) are documented as N/A in the right column rather than silently dropped, so
// the visual gap is obvious next to the Skia mirror (SilkNetGum/Screens/RectanglesScreen)
// which DOES support all of those on its two-slot RectangleRuntime.
//
// Layout convention: every container that sets WidthUnits / HeightUnits to
// RelativeToChildren also sets Width / Height = 0.
internal class RectanglesScreen : FrameworkElement
{
    public RectanglesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // Two-column root — matches CirclesScreen so the screen grows wide rather than tall.
        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        root.StackSpacing = 24;
        root.X = 10;
        root.Y = 10;
        AddChild(root);

        ContainerRuntime left = BuildColumn();
        ContainerRuntime right = BuildColumn();
        root.AddChild(left);
        root.AddChild(right);

        left.AddChild(BuildSection("Sizes (40, 60, 90, 130 wide) — default outline", BuildSizesRow()));

        left.AddChild(BuildSection("Alpha on StrokeColor (255, 192, 128, 64)", BuildAlphaRow()));
        left.AddChild(BuildSection("Modes: FillColor, StrokeColor, Fill+Stroke, default", BuildModeRow()));
        left.AddChild(BuildSection("StrokeWidth (1, 2, 4, 8 px on a filled card)", BuildStrokeWidthRow()));
        left.AddChild(BuildSection("Alignment inside a 128x100 frame (Top / Center / Bottom)", BuildAlignmentRow()));
        left.AddChild(BuildSection("CornerRadius (0, 6, 16, 28 — visibly rounded on Apos)", BuildCornerRadiusRow()));

        left.AddChild(BuildSection("Per-corner radii (TL=20, TR=2, BR=20, BL=2 — opposite corners)", BuildPerCornerRow()));
        left.AddChild(BuildSection("Gradients (linear horizontal / vertical / diagonal / radial)", BuildGradientRow()));

        right.AddChild(BuildSection("Antialiasing (default ON, then OFF) — 1 px stroke makes the bloom obvious (#2818)", BuildAntialiasingRow()));
        right.AddChild(BuildSection("Dropshadow (off / soft / hard offset / colored) — fill-only push avoids doubling (#2818)", BuildDropshadowRow()));
        right.AddChild(BuildSection("Dashed strokes (solid / 6/4 / 2/2 dotted / long-dash) — stroke-only push (#2818)", BuildDashedStrokeRow()));
        right.AddChild(BuildSection("FillColor + StrokeColor on the same instance — both layers render simultaneously (#2768 / #2814)", BuildBothColorsRow()));
        right.AddChild(BuildSection("Inscribed in a 64x64 frame — stroke must stay inside the gray rectangle's bounds at every StrokeWidth (#2768 / #2814 visual contract; mirrors SilkNetGum)", BuildInscribedRow()));
    }

    static ContainerRuntime BuildColumn()
    {
        ContainerRuntime column = new();
        column.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        column.StackSpacing = 14;
        column.WidthUnits = DimensionUnitType.RelativeToChildren;
        column.HeightUnits = DimensionUnitType.RelativeToChildren;
        column.Width = 0;
        column.Height = 0;
        return column;
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

    static ContainerRuntime BuildSizesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float width in new[] { 40f, 60f, 90f, 130f })
        {
            RectangleRuntime rect = new();
            rect.Width = width;
            rect.Height = 40;
            rect.CornerRadius = 10;
            rect.StrokeWidth = 1;
            rect.StrokeColor = Color.White;
            rect.IsAntialiased = true;

            row.AddChild(rect);
        }
        return row;
    }

    static ContainerRuntime BuildAlphaRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (byte alpha in new byte[] { 255, 192, 128, 64 })
        {
            RectangleRuntime rect = new();
            rect.Width = 60;
            rect.Height = 40;
            rect.StrokeColor = new Color((byte)255, (byte)255, (byte)255, alpha);
            row.AddChild(rect);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime filled = new();
        filled.Width = 80; filled.Height = 50;
        filled.FillColor = Color.Crimson;
        row.AddChild(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 80; stroked.Height = 50;
        stroked.StrokeColor = Color.Cyan;
        stroked.StrokeWidth = 2;
        row.AddChild(stroked);

        RectangleRuntime both = new();
        both.Width = 80; both.Height = 50;
        both.FillColor = new Color(40, 40, 80);
        both.StrokeColor = Color.Yellow;
        both.StrokeWidth = 2;
        row.AddChild(both);

        RectangleRuntime defaultRect = new();
        defaultRect.Width = 80; defaultRect.Height = 50;
        row.AddChild(defaultRect);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            RectangleRuntime rect = new();
            rect.Width = 70; rect.Height = 50;
            rect.FillColor = new Color(30, 30, 50);
            rect.StrokeColor = Color.LightGreen;
            rect.StrokeWidth = strokeWidth;
            row.AddChild(rect);
        }
        return row;
    }

    static ContainerRuntime BuildCornerRadiusRow()
    {
        // Showcase the visual payoff of installing MonoGameGumShapes — only the Apos
        // backend honors CornerRadius. The no-package version of this screen omits this
        // row entirely.
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float cornerRadius in new[] { 0f, 6f, 16f, 28f })
        {
            RectangleRuntime rect = new();
            rect.Width = 80; rect.Height = 60;
            rect.FillColor = new Color(40, 40, 80);
            rect.StrokeColor = Color.Orange;
            rect.StrokeWidth = 2;
            rect.IsAntialiased = true;
            rect.CornerRadius = cornerRadius;
            row.AddChild(rect);
        }
        return row;
    }

    // Issue #2818 — per-corner radii reach the renderer via the runtime's CustomRadius*
    // pass-through (decision 4). Apos.Shapes' RoundedRectangle uses the CornerRadii overload
    // when any per-corner value is non-null; null falls back to CornerRadius.
    static ContainerRuntime BuildPerCornerRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        RectangleRuntime rect = new();
        rect.Width = 120; rect.Height = 70;
        rect.FillColor = new Color(40, 40, 80);
        rect.StrokeColor = Color.Orange;
        rect.StrokeWidth = 2;
        rect.CustomRadiusTopLeft = 20;
        rect.CustomRadiusTopRight = 2;
        rect.CustomRadiusBottomRight = 20;
        rect.CustomRadiusBottomLeft = 2;
        row.AddChild(rect);
        return row;
    }

    // Issue #2818 — mirror of CirclesScreen.BuildGradientRow. RectangleRuntime pushes
    // gradient state through to both Apos RoundedRectangles (fill and stroke). Cell sizes
    // (70x50 cells, gradient endpoints in cell-local coords) match the SilkNetGum sibling so
    // the two galleries lay out the same.
    static ContainerRuntime BuildGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime linearH = new();
        linearH.Width = 70; linearH.Height = 50;
        linearH.FillColor = Color.White;
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color1 = Color.White;
        linearH.Color2 = Color.SteelBlue;
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 70; linearH.GradientY2 = 0;
        row.AddChild(linearH);

        RectangleRuntime linearV = new();
        linearV.Width = 70; linearV.Height = 50;
        linearV.FillColor = Color.White;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color1 = Color.Gold;
        linearV.Color2 = Color.Crimson;
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 50;
        row.AddChild(linearV);

        RectangleRuntime linearD = new();
        linearD.Width = 70; linearD.Height = 50;
        linearD.FillColor = Color.White;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color1 = Color.Cyan;
        linearD.Color2 = Color.Magenta;
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 70; linearD.GradientY2 = 50;
        row.AddChild(linearD);

        RectangleRuntime radial = new();
        radial.Width = 70; radial.Height = 50;
        radial.FillColor = Color.White;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color1 = Color.White;
        radial.Color2 = Color.DarkGreen;
        radial.GradientX1 = 35; radial.GradientY1 = 25;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 35;
        row.AddChild(radial);

        return row;
    }

    // Issue #2818 — mirror of CirclesScreen.BuildAntialiasingRow. Cell size 60x50 matches
    // the SilkNetGum sibling.
    static ContainerRuntime BuildAntialiasingRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (bool aa in new[] { true, false })
        {
            RectangleRuntime filled = new();
            filled.Width = 60; filled.Height = 50;
            filled.FillColor = Color.Goldenrod;
            filled.IsAntialiased = aa;
            row.AddChild(filled);

            RectangleRuntime ring = new();
            ring.Width = 60; ring.Height = 50;
            ring.StrokeColor = Color.White;
            ring.StrokeWidth = 1;
            ring.IsAntialiased = aa;
            row.AddChild(ring);
        }
        return row;
    }

    // Issue #2818 — mirror of CirclesScreen.BuildDropshadowRow. Shadow pushes to the fill
    // slot only; rendered once per cell rather than doubling. Cell size 60x50 matches
    // SilkNetGum.
    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime baseline = new();
        baseline.Width = 60; baseline.Height = 50;
        baseline.FillColor = Color.Goldenrod;
        row.AddChild(baseline);

        RectangleRuntime soft = new();
        soft.Width = 60; soft.Height = 50;
        soft.FillColor = Color.Goldenrod;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 14;
        soft.DropshadowOffsetY = 14;
        soft.DropshadowBlurX = 4;
        soft.DropshadowBlurY = 4;
        row.AddChild(soft);

        RectangleRuntime hard = new();
        hard.Width = 60; hard.Height = 50;
        hard.FillColor = Color.Goldenrod;
        hard.HasDropshadow = true;
        hard.DropshadowColor = new Color(0, 0, 0, 160);
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlurX = 0;
        hard.DropshadowBlurY = 0;
        row.AddChild(hard);

        RectangleRuntime colored = new();
        colored.Width = 60; colored.Height = 50;
        colored.FillColor = Color.Goldenrod;
        colored.HasDropshadow = true;
        colored.DropshadowColor = new Color(220, 40, 160, 220);
        colored.DropshadowOffsetX = 16;
        colored.DropshadowOffsetY = 16;
        colored.DropshadowBlurX = 6;
        colored.DropshadowBlurY = 6;
        row.AddChild(colored);

        // Issue #2851 visual acceptance — body alpha multiplies into the shadow alpha.
        RectangleRuntime fadedBody = new();
        fadedBody.Width = 60; fadedBody.Height = 50;
        fadedBody.FillColor = new Color((byte)218, (byte)165, (byte)32, (byte)80);
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 14;
        fadedBody.DropshadowOffsetY = 14;
        fadedBody.DropshadowBlurX = 4;
        fadedBody.DropshadowBlurY = 4;
        row.AddChild(fadedBody);

        return row;
    }

    // Issue #2818 — mirror of CirclesScreen.BuildDashedStrokeRow. Dashing applies to the
    // stroke slot only; the Apos RoundedRectangle's RenderDashed path is guarded by !IsFilled.
    // Cell size 60x50 matches SilkNetGum.
    static ContainerRuntime BuildDashedStrokeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime solid = new();
        solid.Width = 60; solid.Height = 50;
        solid.StrokeColor = Color.White;
        solid.StrokeWidth = 2;
        row.AddChild(solid);

        RectangleRuntime short64 = new();
        short64.Width = 60; short64.Height = 50;
        short64.StrokeColor = Color.White;
        short64.StrokeWidth = 2;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.AddChild(short64);

        RectangleRuntime dotted = new();
        dotted.Width = 60; dotted.Height = 50;
        dotted.StrokeColor = Color.White;
        dotted.StrokeWidth = 1;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        row.AddChild(dotted);

        RectangleRuntime longDash = new();
        longDash.Width = 60; longDash.Height = 50;
        longDash.StrokeColor = Color.LightGreen;
        longDash.StrokeWidth = 3;
        longDash.StrokeDashLength = 12;
        longDash.StrokeGapLength = 6;
        row.AddChild(longDash);

        return row;
    }

    static ContainerRuntime BuildAlignmentRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (VerticalAlignment alignment in new[] { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom })
        {
            row.AddChild(BuildAlignmentCell(alignment));
        }
        return row;
    }

    // Visual acceptance for #2768 / #2814 — mirrors the SilkNetGum row of the same name. Both
    // layers (fill + stroke) render simultaneously regardless of setter order; pre-two-slot
    // only the most-recently-set non-null color was visible.
    static ContainerRuntime BuildBothColorsRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime strokeLast = new();
        strokeLast.Width = 80; strokeLast.Height = 50;
        strokeLast.FillColor = Color.Crimson;
        strokeLast.StrokeColor = Color.Cyan;
        strokeLast.StrokeWidth = 4;
        row.AddChild(strokeLast);

        RectangleRuntime fillLast = new();
        fillLast.Width = 80; fillLast.Height = 50;
        fillLast.StrokeColor = Color.Magenta;
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = Color.Gold;
        row.AddChild(fillLast);

        return row;
    }

    // Visual contract for #2768 / #2814 — mirrors the SilkNetGum RectanglesScreen row of the
    // same name. RenderableRegistry's Apos two-slot runtime mirrors the runtime's Width/Height
    // onto the stroke slot in PreRender; the renderer's stroke-inset handling keeps the
    // frame inscribed inside the bounds. Cells get progressively thicker strokes
    // (1, 4, 8, 12) — every frame must stay inside the gray rectangle.
    static ContainerRuntime BuildInscribedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 4f, 8f, 12f })
        {
            row.AddChild(BuildInscribedCell(strokeWidth));
        }
        return row;
    }

    static RectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        RectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.FillColor = new Color(60, 60, 80);

        RectangleRuntime rect = new();
        rect.Width = 64;
        rect.Height = 64;
        rect.FillColor = Color.SeaGreen;
        rect.StrokeColor = Color.Yellow;
        rect.StrokeWidth = strokeWidth;
        rect.StrokeWidthUnits = DimensionUnitType.Absolute;
        frame.Children.Add(rect);
        return frame;
    }

    static ContainerRuntime BuildHorizontalRow()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 16;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        return row;
    }

    static RectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // Frame size matches the Skia (SilkNetGum) RectanglesScreen sibling so the two
        // galleries lay out the same. The inner RectangleRuntime is positioned via
        // YOrigin + PixelsFromSmall/Middle/Large.
        RectangleRuntime frame = new();
        frame.Width = 128;
        frame.Height = 100;
        frame.FillColor = new Color(50, 50, 70);

        RectangleRuntime rect = new();
        rect.Width = 50;
        rect.Height = 30;
        rect.FillColor = Color.Orange;
        rect.XOrigin = HorizontalAlignment.Center;
        rect.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        rect.YOrigin = alignment;
        rect.YUnits = alignment switch
        {
            VerticalAlignment.Top => Gum.Converters.GeneralUnitType.PixelsFromSmall,
            VerticalAlignment.Center => Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            VerticalAlignment.Bottom => Gum.Converters.GeneralUnitType.PixelsFromLarge,
            _ => Gum.Converters.GeneralUnitType.PixelsFromMiddle,
        };
        frame.Children.Add(rect);
        return frame;
    }
}
