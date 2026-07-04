using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;

// Apos.Shapes gallery for ArcRuntime. Mirrors the post-#2891 layout of
// Samples/SilkNetGum/SilkNetGum/Screens/ArcsScreen.cs cell-for-cell so visual regressions in
// one backend are easy to spot against the other. The cross-backend lock-step contract is
// non-directional now — both galleries move together.
//
// Skia / Apos parity caveats (issue #2892):
//   - Arcs are stroke-only on both backends (the chord-fill seal in #2891 is Skia-only; Apos's
//     Arc.Render has always ignored IsFilled). The Skia "filled" cell in the AA row becomes a
//     thick-band cell here so the row keeps its shape without inventing a fill mode.
//   - Apos's Arc.RenderInternal only honors dashing on the butt-cap path (IsEndRounded = false).
//     The dashed-stroke row pins butt caps so a regression that breaks one branch surfaces.
//   - Apos's edge AA is signed-distance-field (1 px bloom on, crisp at aaSize = 0). Skia uses
//     path AA. Subtle visual differences between the two are expected, not bugs.
internal class ArcsScreen : FrameworkElement
{
    public ArcsScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

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

        left.AddChild(BuildSection("Sweep angles (90, 180, 270, 360)", BuildSweepRow()));
        left.AddChild(BuildSection("Thickness progression: thin → fat → wedge (W/2)", BuildThicknessProgressionRow()));
        left.AddChild(BuildSection("End caps: butt / rounded", BuildEndCapRow()));
        left.AddChild(BuildSection("Gradients on arcs (including gradient wedge)", BuildStrokeGradientRow()));

        right.AddChild(BuildSection("Dashed strokes", BuildDashedStrokeRow()));
        right.AddChild(BuildSection("Antialiasing (AA on / AA off)", BuildAntialiasingRow()));
        right.AddChild(BuildSection("Dropshadow (off / soft / hard / colored / faded body)", BuildDropshadowRow()));
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

    static ContainerRuntime BuildSweepRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float sweep in new[] { 90f, 180f, 270f, 360f })
        {
            ArcRuntime arc = new();
            arc.Width = 60;
            arc.Height = 60;
            arc.SweepAngle = sweep;
            arc.Thickness = 8;
            arc.Color = Color.Goldenrod;
            row.AddChild(arc);
        }
        return row;
    }

    // Visual progression as Thickness grows. The last cell hits Thickness = Width / 2 — Apos's
    // DrawRing reaches the wedge by setting the centerline radius equal to Thickness/2, same
    // math as Skia's stroke-inset collapse. See ArcRuntime.Thickness docs and the Arc.cs
    // radius1 -= 1f shader-quirk comment for how Apos compensates at the wedge tip.
    static ContainerRuntime BuildThicknessProgressionRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        // Square 60×60; W/2 = 30 is the wedge configuration.
        foreach (float thickness in new[] { 2f, 8f, 18f, 30f })
        {
            ArcRuntime arc = new();
            arc.Width = 60;
            arc.Height = 60;
            arc.SweepAngle = 90;
            arc.Thickness = thickness;
            arc.Color = Color.LightGreen;
            row.AddChild(arc);
        }
        return row;
    }

    // IsEndRounded default flipped to false in #2728 — this row pins both ends visible so the
    // butt/round distinction is obvious. Sweep < 360 so the caps are actually present.
    static ContainerRuntime BuildEndCapRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        ArcRuntime butt = new();
        butt.Width = 60; butt.Height = 60;
        butt.SweepAngle = 180;
        butt.Thickness = 10;
        butt.Color = Color.White;
        butt.IsEndRounded = false;
        row.AddChild(butt);

        ArcRuntime rounded = new();
        rounded.Width = 60; rounded.Height = 60;
        rounded.SweepAngle = 180;
        rounded.Thickness = 10;
        rounded.Color = Color.White;
        rounded.IsEndRounded = true;
        row.AddChild(rounded);

        return row;
    }

    // Gradients on stroked arcs. Locks in two things on the Apos side: (a) Apos's DrawRing has
    // always honored gradients through its (gradient, gradient) overload — if any cell renders
    // solid, that path has regressed; (b) the final cell is a gradient-filled pie wedge via
    // Thickness = Width / 2, demonstrating that the wedge config composes cleanly with the
    // gradient. Mirrors the Skia row 1:1.
    static ContainerRuntime BuildStrokeGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Linear horizontal: white → steel blue
        ArcRuntime linearH = new();
        linearH.Width = 60; linearH.Height = 60;
        linearH.SweepAngle = 270;
        linearH.Thickness = 8;
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color1 = Color.White;
        linearH.Color2 = Color.SteelBlue;
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 60; linearH.GradientY2 = 0;
        row.AddChild(linearH);

        // Linear vertical: gold → crimson
        ArcRuntime linearV = new();
        linearV.Width = 60; linearV.Height = 60;
        linearV.SweepAngle = 270;
        linearV.Thickness = 8;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color1 = Color.Gold;
        linearV.Color2 = Color.Crimson;
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 60;
        row.AddChild(linearV);

        // Linear diagonal: cyan → magenta
        ArcRuntime linearD = new();
        linearD.Width = 60; linearD.Height = 60;
        linearD.SweepAngle = 270;
        linearD.Thickness = 8;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color1 = Color.Cyan;
        linearD.Color2 = Color.Magenta;
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 60; linearD.GradientY2 = 60;
        row.AddChild(linearD);

        // Radial centered: white → dark green
        ArcRuntime radial = new();
        radial.Width = 60; radial.Height = 60;
        radial.SweepAngle = 270;
        radial.Thickness = 8;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color1 = Color.White;
        radial.Color2 = Color.DarkGreen;
        radial.GradientX1 = 30; radial.GradientY1 = 30;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 30;
        row.AddChild(radial);

        // Gradient-filled wedge: Thickness = Width / 2 collapses the band to a pie wedge, and
        // the gradient applies across it. Tighter sweep so the wedge shape is unmistakable.
        ArcRuntime wedge = new();
        wedge.Width = 60; wedge.Height = 60;
        wedge.SweepAngle = 90;
        wedge.Thickness = 30; // W / 2 → wedge
        wedge.UseGradient = true;
        wedge.GradientType = GradientType.Linear;
        wedge.Color1 = Color.Orange;
        wedge.Color2 = Color.DeepPink;
        wedge.GradientX1 = 0; wedge.GradientY1 = 0;
        wedge.GradientX2 = 60; wedge.GradientY2 = 60;
        row.AddChild(wedge);

        return row;
    }

    // Mirrors the dashed-stroke row from the Skia gallery. Caps pinned to butt (IsEndRounded
    // = false, which is the default) because Apos's Arc.RenderInternal only routes through
    // DrawRing — and therefore honors dashing — on the butt path. The rounded path goes
    // through DrawArc which has no dash support, so flipping any cell to rounded here would
    // silently swallow the dash pattern.
    static ContainerRuntime BuildDashedStrokeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        ArcRuntime solid = new();
        solid.Width = 60; solid.Height = 60;
        solid.SweepAngle = 270;
        solid.Thickness = 2;
        solid.Color = Color.White;
        row.AddChild(solid);

        ArcRuntime short64 = new();
        short64.Width = 60; short64.Height = 60;
        short64.SweepAngle = 270;
        short64.Thickness = 2;
        short64.Color = Color.White;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.AddChild(short64);

        ArcRuntime dotted = new();
        dotted.Width = 60; dotted.Height = 60;
        dotted.SweepAngle = 270;
        dotted.Thickness = 1;
        dotted.Color = Color.White;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        row.AddChild(dotted);

        ArcRuntime longDash = new();
        longDash.Width = 60; longDash.Height = 60;
        longDash.SweepAngle = 270;
        longDash.Thickness = 3;
        longDash.Color = Color.LightGreen;
        longDash.StrokeDashLength = 12;
        longDash.StrokeGapLength = 6;
        row.AddChild(longDash);

        return row;
    }

    // Issue #2798 — IsAntialiased toggle. Two pairs per AA state (thick band + 1 px stroke),
    // AA on then off. The Skia gallery's first cell in each pair is a chord-filled arc via
    // FillColor; Apos arcs have no fill mode (Arc.Render ignores IsFilled), so the cell is a
    // thick stroke here instead. The AA-off + 1 px cell is the diagnostic one: with
    // IsAntialiased = false Apos drops aaSize to 0, producing crisp pixelated edges.
    static ContainerRuntime BuildAntialiasingRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (bool aa in new[] { true, false })
        {
            ArcRuntime thick = new();
            thick.Width = 60; thick.Height = 60;
            thick.SweepAngle = 270;
            thick.Thickness = 10;
            thick.Color = Color.Goldenrod;
            thick.IsAntialiased = aa;
            row.AddChild(thick);

            ArcRuntime ring = new();
            ring.Width = 60; ring.Height = 60;
            ring.SweepAngle = 270;
            ring.Thickness = 1;
            ring.Color = Color.White;
            ring.IsAntialiased = aa;
            row.AddChild(ring);
        }
        return row;
    }

    // Issue #2851 visual acceptance: five cells — baseline, soft shadow, hard-offset shadow,
    // colored shadow, and "faded body" (body alpha cut to 80, same soft-shadow config as cell
    // two). Pre-fix the faded-body cell rendered an opaque shadow ghost behind a translucent
    // arc; post-fix the shadow fades alongside the body, matching SkiaGum.
    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        ArcRuntime baseline = new();
        baseline.Width = 60; baseline.Height = 60;
        baseline.SweepAngle = 270;
        baseline.Thickness = 10;
        baseline.Color = Color.Goldenrod;
        row.AddChild(baseline);

        ArcRuntime soft = new();
        soft.Width = 60; soft.Height = 60;
        soft.SweepAngle = 270;
        soft.Thickness = 10;
        soft.Color = Color.Goldenrod;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 14;
        soft.DropshadowOffsetY = 14;
        soft.DropshadowBlur = 4;
        row.AddChild(soft);

        ArcRuntime hard = new();
        hard.Width = 60; hard.Height = 60;
        hard.SweepAngle = 270;
        hard.Thickness = 10;
        hard.Color = Color.Goldenrod;
        hard.HasDropshadow = true;
        hard.DropshadowColor = new Color(0, 0, 0, 160);
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlur = 0;
        row.AddChild(hard);

        ArcRuntime colored = new();
        colored.Width = 60; colored.Height = 60;
        colored.SweepAngle = 270;
        colored.Thickness = 10;
        colored.Color = Color.Goldenrod;
        colored.HasDropshadow = true;
        colored.DropshadowColor = new Color(220, 40, 160, 220);
        colored.DropshadowOffsetX = 16;
        colored.DropshadowOffsetY = 16;
        colored.DropshadowBlur = 6;
        row.AddChild(colored);

        ArcRuntime fadedBody = new();
        fadedBody.Width = 60; fadedBody.Height = 60;
        fadedBody.SweepAngle = 270;
        fadedBody.Thickness = 10;
        fadedBody.Color = new Color((byte)218, (byte)165, (byte)32, (byte)80);
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 14;
        fadedBody.DropshadowOffsetY = 14;
        fadedBody.DropshadowBlur = 4;
        row.AddChild(fadedBody);

        return row;
    }
}
