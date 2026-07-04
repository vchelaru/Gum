using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Skia gallery for ArcRuntime. Mirrors the layout of CirclesScreen so the same backend can be
// audited at a glance — every property/feature an Arc actually supports gets its own row. The
// MonoGameGumInCode ArcsScreen is the Apos-side counterpart and moves in lock-step with
// this file (issue #2892 closed the cross-backend gap), so any row added or restructured here
// should land on the Apos side in the same PR. This is also the reference surface for the
// raylib Arc port (#2866); features demonstrated here are the parity bar (chord fill is
// Skia-specific and will not visually match raylib's wedge fill).
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
        this.AddChild(root);

        ContainerRuntime left = BuildColumn();
        ContainerRuntime right = BuildColumn();
        root.Children.Add(left);
        root.Children.Add(right);

        left.Children.Add(BuildSection("Sweep angles (90, 180, 270, 360)", BuildSweepRow()));
        left.Children.Add(BuildSection("Thickness progression: thin → fat → wedge (W/2)", BuildThicknessProgressionRow()));
        left.Children.Add(BuildSection("End caps: butt / rounded", BuildEndCapRow()));
        left.Children.Add(BuildSection("Gradients on arcs (including gradient wedge)", BuildStrokeGradientRow()));

        right.Children.Add(BuildSection("Dashed strokes", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("Antialiasing (AA on / AA off)", BuildAntialiasingRow()));
        right.Children.Add(BuildSection("Dropshadow (off / soft / hard / colored / faded body)", BuildDropshadowRow()));
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
        section.Children.Add(header);
        section.Children.Add(body);
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
            arc.StrokeColor = SKColors.Goldenrod;
            row.Children.Add(arc);
        }
        return row;
    }

    // Demonstrates the visual progression as Thickness grows on a square arc. The last cell
    // hits Thickness = Width / 2 — the band's inner edge collapses to the center and butt-end
    // caps become radial edges, producing a true pie wedge. This is the supported path to a
    // "filled wedge" arc on both Skia and Apos backends. See ArcRuntime.Thickness for the math.
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
            arc.StrokeColor = SKColors.LightGreen;
            row.Children.Add(arc);
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
        butt.StrokeColor = SKColors.White;
        butt.IsEndRounded = false;
        row.Children.Add(butt);

        ArcRuntime rounded = new();
        rounded.Width = 60; rounded.Height = 60;
        rounded.SweepAngle = 180;
        rounded.Thickness = 10;
        rounded.StrokeColor = SKColors.White;
        rounded.IsEndRounded = true;
        row.Children.Add(rounded);

        return row;
    }

    // Gradients on stroked arcs (the only mode arcs support after the chord-fill seal). Locks
    // in two things: (a) #2790's silent gradient-suppression regression for single-slot shapes
    // — if any cell renders solid, the regression is back; (b) the final cell is a
    // gradient-filled pie wedge via Thickness = Width / 2, demonstrating that the wedge config
    // composes cleanly with gradients.
    static ContainerRuntime BuildStrokeGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Linear horizontal: white → blue
        ArcRuntime linearH = new();
        linearH.Width = 60; linearH.Height = 60;
        linearH.SweepAngle = 270;
        linearH.Thickness = 8;
        linearH.StrokeColor = SKColors.White;
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color1 = SKColors.White;
        linearH.Color2 = SKColors.SteelBlue;
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 60; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: gold → crimson
        ArcRuntime linearV = new();
        linearV.Width = 60; linearV.Height = 60;
        linearV.SweepAngle = 270;
        linearV.Thickness = 8;
        linearV.StrokeColor = SKColors.White;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color1 = SKColors.Gold;
        linearV.Color2 = SKColors.Crimson;
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 60;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta
        ArcRuntime linearD = new();
        linearD.Width = 60; linearD.Height = 60;
        linearD.SweepAngle = 270;
        linearD.Thickness = 8;
        linearD.StrokeColor = SKColors.White;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color1 = SKColors.Cyan;
        linearD.Color2 = SKColors.Magenta;
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 60; linearD.GradientY2 = 60;
        row.Children.Add(linearD);

        // Radial centered: white → dark green
        ArcRuntime radial = new();
        radial.Width = 60; radial.Height = 60;
        radial.SweepAngle = 270;
        radial.Thickness = 8;
        radial.StrokeColor = SKColors.White;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color1 = SKColors.White;
        radial.Color2 = SKColors.DarkGreen;
        radial.GradientX1 = 30; radial.GradientY1 = 30;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 30;
        row.Children.Add(radial);

        // Gradient-filled wedge: Thickness = Width / 2 collapses the band to a pie wedge, and
        // the gradient applies across it. Tighter sweep so the wedge shape is unmistakable.
        ArcRuntime wedge = new();
        wedge.Width = 60; wedge.Height = 60;
        wedge.SweepAngle = 90;
        wedge.Thickness = 30; // W / 2 → wedge
        wedge.StrokeColor = SKColors.White;
        wedge.UseGradient = true;
        wedge.GradientType = GradientType.Linear;
        wedge.Color1 = SKColors.Orange;
        wedge.Color2 = SKColors.DeepPink;
        wedge.GradientX1 = 0; wedge.GradientY1 = 0;
        wedge.GradientX2 = 60; wedge.GradientY2 = 60;
        row.Children.Add(wedge);

        return row;
    }

    // Mirrors the dashed-stroke row from CirclesScreen — same dash/gap pairs so a dashed arc
    // and dashed circle stay visually comparable.
    static ContainerRuntime BuildDashedStrokeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        ArcRuntime solid = new();
        solid.Width = 60; solid.Height = 60;
        solid.SweepAngle = 270;
        solid.Thickness = 2;
        solid.StrokeColor = SKColors.White;
        row.Children.Add(solid);

        ArcRuntime short64 = new();
        short64.Width = 60; short64.Height = 60;
        short64.SweepAngle = 270;
        short64.Thickness = 2;
        short64.StrokeColor = SKColors.White;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.Children.Add(short64);

        ArcRuntime dotted = new();
        dotted.Width = 60; dotted.Height = 60;
        dotted.SweepAngle = 270;
        dotted.Thickness = 1;
        dotted.StrokeColor = SKColors.White;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        row.Children.Add(dotted);

        ArcRuntime longDash = new();
        longDash.Width = 60; longDash.Height = 60;
        longDash.SweepAngle = 270;
        longDash.Thickness = 3;
        longDash.StrokeColor = SKColors.LightGreen;
        longDash.StrokeDashLength = 12;
        longDash.StrokeGapLength = 6;
        row.Children.Add(longDash);

        return row;
    }

    // Issue #2798 — IsAntialiased toggle. Two pairs (chord-fill + 1 px stroke), AA on then off,
    // matching the CirclesScreen layout so the two galleries read the same.
    static ContainerRuntime BuildAntialiasingRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (bool aa in new[] { true, false })
        {
            ArcRuntime filled = new();
            filled.Width = 60; filled.Height = 60;
            filled.SweepAngle = 270;
            filled.FillColor = SKColors.Goldenrod;
            filled.IsAntialiased = aa;
            row.Children.Add(filled);

            ArcRuntime ring = new();
            ring.Width = 60; ring.Height = 60;
            ring.SweepAngle = 270;
            ring.Thickness = 1;
            ring.StrokeColor = SKColors.White;
            ring.IsAntialiased = aa;
            row.Children.Add(ring);
        }
        return row;
    }

    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        ArcRuntime baseline = new();
        baseline.Width = 60; baseline.Height = 60;
        baseline.SweepAngle = 270;
        baseline.Thickness = 10;
        baseline.StrokeColor = SKColors.Goldenrod;
        row.Children.Add(baseline);

        ArcRuntime soft = new();
        soft.Width = 60; soft.Height = 60;
        soft.SweepAngle = 270;
        soft.Thickness = 10;
        soft.StrokeColor = SKColors.Goldenrod;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 14;
        soft.DropshadowOffsetY = 14;
        soft.DropshadowBlur = 4;
        row.Children.Add(soft);

        ArcRuntime hard = new();
        hard.Width = 60; hard.Height = 60;
        hard.SweepAngle = 270;
        hard.Thickness = 10;
        hard.StrokeColor = SKColors.Goldenrod;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlur = 0;
        row.Children.Add(hard);

        ArcRuntime colored = new();
        colored.Width = 60; colored.Height = 60;
        colored.SweepAngle = 270;
        colored.Thickness = 10;
        colored.StrokeColor = SKColors.Goldenrod;
        colored.HasDropshadow = true;
        colored.DropshadowRed = 220; colored.DropshadowGreen = 40; colored.DropshadowBlue = 160; colored.DropshadowAlpha = 220;
        colored.DropshadowOffsetX = 16;
        colored.DropshadowOffsetY = 16;
        colored.DropshadowBlur = 6;
        row.Children.Add(colored);

        ArcRuntime fadedBody = new();
        fadedBody.Width = 60; fadedBody.Height = 60;
        fadedBody.SweepAngle = 270;
        fadedBody.Thickness = 10;
        fadedBody.StrokeColor = new SKColor(218, 165, 32, 80);
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 14;
        fadedBody.DropshadowOffsetY = 14;
        fadedBody.DropshadowBlur = 4;
        row.Children.Add(fadedBody);

        return row;
    }
}
