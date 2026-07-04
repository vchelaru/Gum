using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;

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
        left.AddChild(BuildSection("Sizes", BuildSizesRow()));
        left.AddChild(BuildSection("Alpha", BuildAlphaRow()));
        left.AddChild(BuildSection("Modes", BuildModeRow()));
        left.AddChild(BuildSection("Stroke width", BuildStrokeWidthRow()));
        left.AddChild(BuildSection("Alignment", BuildAlignmentRow()));
        left.AddChild(BuildSection("Gradients", BuildGradientRow()));
        left.AddChild(BuildSection("Rotation (filled)", BuildRotationRow(filled: true)));
        left.AddChild(BuildSection("Rotation (outline)", BuildRotationRow(filled: false)));

        right.AddChild(BuildSection("Antialiasing", BuildAntialiasingRow()));
        right.AddChild(BuildSection("Dropshadow", BuildDropshadowRow()));
        right.AddChild(BuildSection("Dashed strokes", BuildDashedStrokeRow()));
        right.AddChild(BuildSection("Fill + stroke", BuildBothColorsRow()));
        right.AddChild(BuildSection("Hairline bleed (#2834)", BuildHairlineBleedRow()));
        right.AddChild(BuildSection("Inscribed", BuildInscribedRow()));
        right.AddChild(BuildSection("Non-square aspect", BuildNonSquareRow()));
        right.AddChild(BuildSection("Blend (additive #3491)", BuildBlendRow()));
    }

    // Issue #3491 — mirror of the raylib CirclesScreen "Blend (additive)" cell so the galleries
    // stay comparable side-by-side. Three filled RGB circles overlapping on a black frame, each
    // with Blend = Additive. On the shapes-package (Apos) side the blend folds into each circle's
    // batch key, so the overlaps read yellow (R+G) / cyan (G+B) / magenta (R+B) and the center
    // white. Blend is XNALIKE-only on CircleRuntime, so there's no Skia mirror (SkiaShapeRuntime
    // has no Blend); this cell is the MonoGame/raylib counterpart pair.
    static RectangleRuntime BuildBlendRow()
    {
        RectangleRuntime frame = new();
        frame.Width = 130;
        frame.Height = 110;
        frame.FillColor = Color.Black;
        frame.IsFilled = true;

        (Color color, float x, float y)[] discs =
        {
            (Color.Red, 0f, -14f),
            (Color.Lime, -14f, 10f),
            (Color.Blue, 14f, 10f),
        };

        foreach ((Color color, float x, float y) in discs)
        {
            CircleRuntime circle = new();
            circle.Radius = 26;
            circle.FillColor = color;
            circle.IsFilled = true;
            circle.Blend = Gum.RenderingLibrary.Blend.Additive;
            circle.XOrigin = HorizontalAlignment.Center;
            circle.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            circle.X = x;
            circle.YOrigin = VerticalAlignment.Center;
            circle.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            circle.Y = y;
            frame.Children.Add(circle);
        }

        return frame;
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
        foreach (float radius in new[] { 16f, 24f, 32f, 48f })
        {
            CircleRuntime circle = new();
            circle.Radius = radius;
            circle.StrokeColor = Color.White;
            circle.StrokeWidth = 1;
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
        filled.IsFilled = true;
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
        linearH.FillColor = Color.White; // gradient start stop is the fill color
        linearH.IsFilled = true;
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color2 = Color.SteelBlue;
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 56; linearH.GradientY2 = 0;
        row.AddChild(linearH);

        // Linear vertical: gold → crimson
        CircleRuntime linearV = new();
        linearV.Radius = 28;
        linearV.FillColor = Color.Gold;
        linearV.IsFilled = true;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color2 = Color.Crimson;
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 56;
        row.AddChild(linearV);

        // Linear diagonal: cyan → magenta
        CircleRuntime linearD = new();
        linearD.Radius = 28;
        linearD.FillColor = Color.Cyan;
        linearD.IsFilled = true;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color2 = Color.Magenta;
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 56; linearD.GradientY2 = 56;
        row.AddChild(linearD);

        // Radial centered: white → dark green
        CircleRuntime radial = new();
        radial.Radius = 28;
        radial.FillColor = Color.White;
        radial.IsFilled = true;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
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
            filled.IsFilled = true;
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
        baseline.IsFilled = true;
        row.AddChild(baseline);

        // Soft shadow: noticeable offset, generous blur, default opaque black.
        CircleRuntime soft = new();
        soft.Radius = 28;
        soft.FillColor = Color.Goldenrod;
        soft.IsFilled = true;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 14;
        soft.DropshadowOffsetY = 14;
        soft.DropshadowBlur = 4;
        row.AddChild(soft);

        // Hard offset: bigger offset, no blur, semi-transparent black.
        CircleRuntime hard = new();
        hard.Radius = 28;
        hard.FillColor = Color.Goldenrod;
        hard.IsFilled = true;
        hard.HasDropshadow = true;
        hard.DropshadowColor = new Color(0, 0, 0, 160);
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlur = 0;
        row.AddChild(hard);

        // Colored shadow: magenta cast, real offset so the cast is visible against the blue
        // background (offset = 0 would tuck the entire shadow under the opaque disk and leave
        // only a thin halo, which on a blue page reads as nothing).
        CircleRuntime colored = new();
        colored.Radius = 28;
        colored.FillColor = Color.Goldenrod;
        colored.IsFilled = true;
        colored.HasDropshadow = true;
        colored.DropshadowColor = new Color(220, 40, 160, 220);
        colored.DropshadowOffsetX = 16;
        colored.DropshadowOffsetY = 16;
        colored.DropshadowBlur = 6;
        row.AddChild(colored);

        // Issue #2851 visual acceptance: same soft-shadow config as the second cell, but with
        // the body's alpha cut to 80. The shadow must fade alongside the body — matches Skia
        // (and therefore the Gum tool/viewport). Pre-fix the Apos.Shapes side left an opaque
        // shadow ghost behind a translucent disk.
        CircleRuntime fadedBody = new();
        fadedBody.Radius = 28;
        fadedBody.FillColor = new Color((byte)218, (byte)165, (byte)32, (byte)80);
        fadedBody.IsFilled = true;
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 14;
        fadedBody.DropshadowOffsetY = 14;
        fadedBody.DropshadowBlur = 4;
        row.AddChild(fadedBody);

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

        // Tight 2/2 dotted. AA stays ON — the runtime's AA-bloom compensation (#2790) pushes
        // a near-zero thickness with aaSize = 1 so the dashes render as crisp 1 px dots with
        // smooth (not jaggy) edges. Pre-#2790 this cell forced IsAntialiased = false to avoid
        // a fat AA-bloomed dash, accepting jaggies on the circle's curve as the lesser evil;
        // that trade is no longer required.
        CircleRuntime dotted = new();
        dotted.Radius = 28;
        dotted.StrokeColor = Color.White;
        dotted.StrokeWidth = 1;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
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
        strokeLast.IsFilled = true;
        strokeLast.StrokeColor = Color.Cyan;
        strokeLast.StrokeWidth = 4;
        row.AddChild(strokeLast);

        CircleRuntime fillLast = new();
        fillLast.Radius = 28;
        fillLast.StrokeColor = Color.Magenta;
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = Color.Gold;
        fillLast.IsFilled = true;
        row.AddChild(fillLast);

        return row;
    }

    // Visual repro for #2834 — hairline white stroke over a red fill. The two-slot
    // composition draws the fill and stroke as separate antialiased renderables, so the
    // AA pixels at the fill's outer edge overlap the AA pixels at the stroke's inner edge.
    // 50%-white-over-50%-red composites to pink, leaving a visible halo on the inside of
    // the ring. The artifact is ~1 px wide regardless of stroke width, so it's most
    // obvious at 1–2 px and fades at 3–4 px. Run this row in MG, Skia, and raylib side by
    // side to compare — Skia is the primary repro per the issue.
    static ContainerRuntime BuildHairlineBleedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 3f, 4f })
        {
            CircleRuntime circle = new();
            circle.Radius = 28;
            circle.FillColor = Color.Red;
            circle.IsFilled = true;
            circle.StrokeColor = Color.White;
            circle.StrokeWidth = strokeWidth;
            row.AddChild(circle);
        }
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

    // Visual acceptance for #2852 — wide, tall, and square cells sharing the same render code.
    // The gray frame is the circle's bounding box; the circle inside must use min(W,H) for its
    // diameter and sit centered. Before the #2852 fix, the wide cell drew a Width/2 circle
    // overflowing the short axis with the center pushed below the box.
    static ContainerRuntime BuildNonSquareRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach ((float w, float h) in new[] { (200f, 50f), (50f, 120f), (100f, 100f) })
        {
            row.AddChild(BuildNonSquareCell(w, h));
        }
        return row;
    }

    static RectangleRuntime BuildNonSquareCell(float width, float height)
    {
        RectangleRuntime frame = new();
        frame.Width = width;
        frame.Height = height;
        frame.FillColor = new Color(60, 60, 80);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Width = width;
        circle.Height = height;
        circle.WidthUnits = DimensionUnitType.Absolute;
        circle.HeightUnits = DimensionUnitType.Absolute;
        circle.FillColor = Color.SeaGreen;
        circle.IsFilled = true;
        circle.StrokeColor = Color.Yellow;
        circle.StrokeWidth = 1;
        frame.Children.Add(circle);
        return frame;
    }

    static RectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        RectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.FillColor = new Color(60, 60, 80);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 32;
        circle.FillColor = Color.SeaGreen;
        circle.IsFilled = true;
        circle.StrokeColor = Color.Yellow;
        circle.StrokeWidth = strokeWidth;
        circle.StrokeWidthUnits = DimensionUnitType.Absolute;
        frame.Children.Add(circle);
        return frame;
    }

    // Rotation row — black→white horizontal gradient on circles, rotated in 60° steps
    // (0/60/120/180). Plain circles are rotation-symmetric, so the gradient is what makes
    // the rotation visible. Endpoints are 0→20 px (less than the 56 px diameter) so the
    // transition is concentrated in a narrow band — the resulting hard light/dark edge
    // makes the rotation angle obvious. Cells use a fixed-size frame because Rotation
    // pushes content outside the natural bounding box, which breaks the
    // RelativeToChildren row sizing. Mirrors the same row on the SilkNet and raylib sides.
    //
    // Two rows: "filled" sets FillColor opaque so the gradient lights up the fill slot;
    // "outline" sets IsFilled = false so the gradient lights up the stroke slot. Both rows
    // exercise the same #2956 contract from opposite ends. raylib's CirclesScreen replaces
    // the outline row with a "Not supported in raylib" label because raylib's LineCircle
    // stroke pass is solid-color only and a solid outline on a rotation-symmetric circle
    // has no rotation signal to show.
    static ContainerRuntime BuildRotationRow(bool filled)
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float rotation in new[] { 0f, 60f, 120f, 180f })
        {
            row.AddChild(BuildRotatedGradientCircleCell(rotation, filled));
        }
        return row;
    }

    static RectangleRuntime BuildRotatedGradientCircleCell(float rotation, bool filled)
    {
        RectangleRuntime frame = new();
        frame.Width = 70;
        frame.Height = 70;
        frame.FillColor = new Color(60, 60, 80);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 28;
        circle.XOrigin = HorizontalAlignment.Center;
        circle.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        circle.YOrigin = VerticalAlignment.Center;
        circle.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        // The gradient start stop is the active body color: FillColor when filled,
        // StrokeColor when only stroked. Set the appropriate slot to Black so the
        // gradient starts dark in both variants.
        if (filled)
        {
            circle.FillColor = Color.Black;
            circle.IsFilled = true;
        }
        else
        {
            circle.IsFilled = false;
            circle.StrokeColor = Color.Black;
        }
        circle.UseGradient = true;
        circle.GradientType = GradientType.Linear;
        circle.Color2 = Color.White;
        circle.GradientX1 = 0; circle.GradientY1 = 0;
        circle.GradientX2 = 20; circle.GradientY2 = 0;
        circle.Rotation = rotation;
        frame.Children.Add(circle);
        return frame;
    }

    static RectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // Visible frame so the alignment is obvious. Children are positioned relative to it
        // via YOrigin + PixelsFromSmall/Middle/Large.
        RectangleRuntime frame = new();
        // Narrowed from 220 to 128 to keep the left column from forcing the page wider than
        // the right column needs (the long section labels in the right column would otherwise
        // get clipped or push the overall layout). 128 still gives 3 cells * 60 px clearance
        // for the three alignment circles plus row spacing.
        frame.Width = 128;
        frame.Height = 100;
        frame.FillColor = new Color(50, 50, 70);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 22;
        circle.FillColor = Color.Orange;
        circle.IsFilled = true;
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
