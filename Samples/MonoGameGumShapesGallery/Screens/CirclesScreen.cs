using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumShapesGallery.Screens;

// CircleRuntime survey on the shapes-package side (issue #2761 / PR #2767). This sample
// references MonoGameGumShapes, so RenderableRegistry resolves ICircleRenderable to the
// Apos.Shapes Circle — every CircleRuntime here renders through Apos with full fill,
// stroke, and stroke-width support. The exact same code in MonoGameGumInCode (no shapes
// package) degrades to outline-only via DefaultCircleRenderable; compare both to see the
// construct-time-binding model in action.
//
// Deliberately uses CircleRuntime directly. ColoredCircleRuntime is on the obsolete path
// (separate follow-up to this issue) and will eventually become an [Obsolete] shim.
//
// Layout convention: every container that sets WidthUnits / HeightUnits to
// RelativeToChildren also sets Width / Height = 0. RelativeToChildren means the final
// size is children-extent + the explicit Width/Height; a non-zero value adds extra
// padding the layout almost never wants.
internal class CirclesScreen : FrameworkElement
{
    public CirclesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // Two-column root so the screen grows wide rather than tall as rows accumulate. No
        // ScrollViewer parity in SkiaGum yet, so this is the cheapest layout that works on
        // both backends (mirrored in SilkNetGum/Screens/CirclesScreen).
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

        // Column split mirrors SilkNetGum/Screens/CirclesScreen so visual regressions in one
        // backend are easy to spot against the other.
        left.AddChild(BuildSection("Sizes (radius 16, 24, 32, 48) — default outline", BuildSizesRow()));
        left.AddChild(BuildSection("Alpha on StrokeColor (255, 192, 128, 64)", BuildAlphaRow()));
        left.AddChild(BuildSection("Modes: FillColor, StrokeColor, default", BuildModeRow()));
        left.AddChild(BuildSection("StrokeWidth (1, 2, 4, 8 px)", BuildStrokeWidthRow()));
        left.AddChild(BuildSection("Alignment inside a 220x100 frame (Top / Center / Bottom)", BuildAlignmentRow()));
        left.AddChild(BuildSection("Gradients (linear / radial / diagonal / centered)", BuildGradientRow()));

        right.AddChild(BuildSection("Antialiasing (default ON, then OFF) — 1 px stroke makes the bloom obvious (#2798)", BuildAntialiasingRow()));
        right.AddChild(BuildSection("Dropshadow (off / soft / hard offset / colored) — fill-only push avoids doubling (#2797)", BuildDropshadowRow()));
        right.AddChild(BuildSection("Dashed strokes (solid / 6/4 / 2/2 dotted / long-dash) — stroke-only push (#2796)", BuildDashedStrokeRow()));
        right.AddChild(BuildSection("FillColor + StrokeColor on the same instance — both layers render simultaneously (#2790)", BuildBothColorsRow()));
        right.AddChild(BuildSection("Inscribed in a 64x64 frame — stroke must stay inside the gray rectangle's bounds at every StrokeWidth (#2790 visual contract; mirrors SilkNetGum)", BuildInscribedRow()));
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

    // Rows where the stroke *width* isn't the demo's topic explicitly set StrokeWidth = 2 so
    // the Apos.Shapes AA bloom (which renders ~1 px of soft edge OVER the nominal stroke,
    // unlike Skia which fits AA WITHIN the stroke) is a small fraction of the total ring and
    // the two backends read as the same thickness. StrokeWidthRow + AntialiasingRow keep the
    // default 1 px because that bloom is the lesson there.
    static ContainerRuntime BuildSizesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float radius in new[] { 16f, 24f, 32f, 48f })
        {
            CircleRuntime circle = new();
            circle.Radius = radius;
            circle.StrokeColor = Color.White;
            circle.StrokeWidth = 2;
            row.AddChild(circle);
        }
        return row;
    }

    static ContainerRuntime BuildAlphaRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (byte alpha in new byte[] { 255, 192, 128, 64 })
        {
            CircleRuntime circle = new();
            circle.Radius = 24;
            circle.StrokeColor = new Color((byte)255, (byte)255, (byte)255, alpha);
            circle.StrokeWidth = 2;
            row.AddChild(circle);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        CircleRuntime filled = new();
        filled.Radius = 28;
        filled.FillColor = Color.Crimson;
        row.AddChild(filled);

        CircleRuntime stroked = new();
        stroked.Radius = 28;
        stroked.StrokeColor = Color.Cyan;
        stroked.StrokeWidth = 3;
        row.AddChild(stroked);

        CircleRuntime defaultCircle = new();
        defaultCircle.Radius = 28;
        row.AddChild(defaultCircle);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            CircleRuntime circle = new();
            circle.Radius = 24;
            circle.StrokeColor = Color.LightGreen;
            circle.StrokeWidth = strokeWidth;
            row.AddChild(circle);
        }
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

    // Mirror of the gradient row on Samples/SilkNetGum/SilkNetGum/Screens/CirclesScreen.cs
    // (issue #2791). Each cell exercises a different gradient configuration; with
    // MonoGameGumShapes loaded, CircleRuntime pushes gradient state through to both Apos
    // Circles (fill and stroke), so a single gradient covers the filled disk.
    static ContainerRuntime BuildGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Linear horizontal: white → blue
        CircleRuntime linearH = new();
        linearH.Radius = 28;
        linearH.FillColor = Color.White; // fill mode; gradient overrides solid color
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color1 = Color.White;
        linearH.Color2 = Color.SteelBlue;
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 56; linearH.GradientY2 = 0;
        row.AddChild(linearH);

        // Linear vertical: gold → crimson
        CircleRuntime linearV = new();
        linearV.Radius = 28;
        linearV.FillColor = Color.White;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color1 = Color.Gold;
        linearV.Color2 = Color.Crimson;
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 56;
        row.AddChild(linearV);

        // Linear diagonal: cyan → magenta
        CircleRuntime linearD = new();
        linearD.Radius = 28;
        linearD.FillColor = Color.White;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color1 = Color.Cyan;
        linearD.Color2 = Color.Magenta;
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 56; linearD.GradientY2 = 56;
        row.AddChild(linearD);

        // Radial centered: white → dark green
        CircleRuntime radial = new();
        radial.Radius = 28;
        radial.FillColor = Color.White;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color1 = Color.White;
        radial.Color2 = Color.DarkGreen;
        radial.GradientX1 = 28; radial.GradientY1 = 28;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 28;
        row.AddChild(radial);

        return row;
    }

    // Issue #2798 visual acceptance: two pairs (filled disk + 1 px outline ring), once with
    // IsAntialiased = true (the default — soft edges) and once false (crisp pixels).
    // The 1 px stroke makes the AA bloom obvious.
    static ContainerRuntime BuildAntialiasingRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        foreach (bool aa in new[] { true, false })
        {
            CircleRuntime filled = new();
            filled.Radius = 28;
            filled.FillColor = Color.Goldenrod;
            filled.IsAntialiased = aa;
            row.AddChild(filled);

            CircleRuntime ring = new();
            ring.Radius = 28;
            ring.StrokeColor = Color.White;
            ring.StrokeWidth = 1;
            ring.IsAntialiased = aa;
            row.AddChild(ring);
        }

        return row;
    }

    // Issue #2797 visual acceptance: four cells — first is the baseline (no shadow), the
    // remaining three exercise different shadow configurations. The runtime pushes shadow
    // params to the fill slot only (not the stroke), so the shadow renders once per cell
    // rather than doubling up. Mirrored on the Skia side of the gallery.
    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Baseline: no shadow.
        CircleRuntime baseline = new();
        baseline.Radius = 28;
        baseline.FillColor = Color.Goldenrod;
        row.AddChild(baseline);

        // Soft shadow: small offset, generous blur, default opaque black.
        CircleRuntime soft = new();
        soft.Radius = 28;
        soft.FillColor = Color.Goldenrod;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 2;
        soft.DropshadowOffsetY = 2;
        soft.DropshadowBlurX = 4;
        soft.DropshadowBlurY = 4;
        row.AddChild(soft);

        // Hard offset: bigger offset, no blur, semi-transparent black.
        CircleRuntime hard = new();
        hard.Radius = 28;
        hard.FillColor = Color.Goldenrod;
        hard.HasDropshadow = true;
        hard.DropshadowColor = new Color(0, 0, 0, 160);
        hard.DropshadowOffsetX = 6;
        hard.DropshadowOffsetY = 6;
        hard.DropshadowBlurX = 0;
        hard.DropshadowBlurY = 0;
        row.AddChild(hard);

        // Colored shadow: deep blue glow underneath.
        CircleRuntime colored = new();
        colored.Radius = 28;
        colored.FillColor = Color.Goldenrod;
        colored.HasDropshadow = true;
        colored.DropshadowColor = new Color(0, 80, 200, 200);
        colored.DropshadowOffsetX = 0;
        colored.DropshadowOffsetY = 0;
        colored.DropshadowBlurX = 6;
        colored.DropshadowBlurY = 6;
        row.AddChild(colored);

        return row;
    }

    // Issue #2796 visual acceptance: four cells stepping through dash/gap patterns. First
    // cell is the solid-stroke baseline (dash=0). Remaining three exercise short dashes,
    // dotted, and a long-dash motif. Dashing applies to stroke only — the runtime skips
    // pushing dash/gap to the fill slot since Apos' Circle.RenderDashed is guarded by
    // !IsFilled. Mirrored on the Skia side via SkiaShapeRuntime.StrokeDashLength.
    static ContainerRuntime BuildDashedStrokeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Baseline: solid stroke (dash=0).
        CircleRuntime solid = new();
        solid.Radius = 28;
        solid.StrokeColor = Color.White;
        solid.StrokeWidth = 2;
        row.AddChild(solid);

        // Short 6/4 dash.
        CircleRuntime short64 = new();
        short64.Radius = 28;
        short64.StrokeColor = Color.White;
        short64.StrokeWidth = 2;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.AddChild(short64);

        // Tight 2/2 dotted. IsAntialiased=false avoids the AA bloom widening a 1 px stroke
        // and matches the Win95-style dotted focus rect (see Themes/Retro95).
        CircleRuntime dotted = new();
        dotted.Radius = 28;
        dotted.StrokeColor = Color.White;
        dotted.StrokeWidth = 1;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        dotted.IsAntialiased = false;
        row.AddChild(dotted);

        // Long-dash motif: 12/6 with a thicker stroke.
        CircleRuntime longDash = new();
        longDash.Radius = 28;
        longDash.StrokeColor = Color.LightGreen;
        longDash.StrokeWidth = 3;
        longDash.StrokeDashLength = 12;
        longDash.StrokeGapLength = 6;
        row.AddChild(longDash);

        return row;
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

    // Visual acceptance for #2790 — mirrors the SilkNetGum row of the same name. Both layers
    // (fill + stroke) render simultaneously regardless of setter order; pre-#2790 only the
    // most-recently-set non-null color was visible.
    static ContainerRuntime BuildBothColorsRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        CircleRuntime strokeLast = new();
        strokeLast.Radius = 28;
        strokeLast.FillColor = Color.Crimson;
        strokeLast.StrokeColor = Color.Cyan;
        strokeLast.StrokeWidth = 4;
        row.AddChild(strokeLast);

        CircleRuntime fillLast = new();
        fillLast.Radius = 28;
        fillLast.StrokeColor = Color.Magenta;
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = Color.Gold;
        row.AddChild(fillLast);

        return row;
    }

    // Visual contract for #2790 — mirrors the SilkNetGum CirclesScreen row of the same name.
    // RenderableRegistry's Apos two-slot runtime mirrors the runtime's Width/Height onto the
    // stroke slot in PreRender; the renderer's stroke-inset handling keeps the ring inscribed
    // inside the bounds. Cells get progressively thicker strokes (1, 4, 8, 12) — every ring
    // must stay inside the gray rectangle. Bleeding past the frame means PreRender mirroring
    // is broken.
    static ContainerRuntime BuildInscribedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 4f, 8f, 12f })
        {
            row.AddChild(BuildInscribedCell(strokeWidth));
        }
        return row;
    }

    static ColoredRectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        ColoredRectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.Color = new Color(60, 60, 80);

        CircleRuntime circle = new();
        circle.Radius = 32;
        circle.FillColor = Color.SeaGreen;
        circle.StrokeColor = Color.Yellow;
        circle.StrokeWidth = strokeWidth;
        circle.StrokeWidthUnits = DimensionUnitType.Absolute;
        frame.Children.Add(circle);
        return frame;
    }

    static ColoredRectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // ColoredRectangle is used as a visible frame so the alignment is obvious. Children
        // are positioned relative to it via YOrigin + PixelsFromSmall/Middle/Large.
        ColoredRectangleRuntime frame = new();
        frame.Width = 220;
        frame.Height = 100;
        frame.Color = new Color(50, 50, 70);

        CircleRuntime circle = new();
        circle.Radius = 22;
        circle.FillColor = Color.Orange;
        circle.XOrigin = HorizontalAlignment.Center;
        circle.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        circle.YOrigin = alignment;
        circle.YUnits = alignment switch
        {
            VerticalAlignment.Top => Gum.Converters.GeneralUnitType.PixelsFromSmall,
            VerticalAlignment.Center => Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            VerticalAlignment.Bottom => Gum.Converters.GeneralUnitType.PixelsFromLarge,
            _ => Gum.Converters.GeneralUnitType.PixelsFromMiddle,
        };
        frame.Children.Add(circle);
        return frame;
    }
}
