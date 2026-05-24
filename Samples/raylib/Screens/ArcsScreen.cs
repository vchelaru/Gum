using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;

namespace Examples.Shapes;

// Issue #2866 - raylib mirror of the post-PR-#2891 Skia ArcsScreen
// (Samples/SilkNetGum/SilkNetGum/Screens/ArcsScreen.cs). Section order and parameter sweeps are
// kept aligned so visual regressions in either backend are easy to spot when the two galleries
// run side by side.
//
// What's intentionally NOT mirrored:
//   * "Gradients" - deferred to a follow-up; the issue comment lists it as a stretch goal.
//   * "Antialiasing" - raylib has no per-shape AA. Framebuffer MSAA (Msaa4xHint in Program.cs)
//     is the only AA path, so toggling IsAntialiased would render identically.
// End caps DO render on raylib post-#2895 (DrawCircleSector half-disks synthesized in LineArc),
// so the "End caps" row IS mirrored.
internal class ArcsScreen : FrameworkElement
{
    public ArcsScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        root.StackSpacing = 24;
        root.X = 10;
        root.Y = 10;
        this.AddChild(root);

        ContainerRuntime left = BuildColumn();
        ContainerRuntime right = BuildColumn();
        root.Children.Add(left);
        root.Children.Add(right);

        left.Children.Add(BuildSection("Sweep angles (90, 180, 270, 360)", BuildSweepRow()));
        left.Children.Add(BuildSection("Thickness progression: thin -> fat -> wedge (W/2)", BuildThicknessProgressionRow()));
        left.Children.Add(BuildSection("End caps: butt / rounded", BuildEndCapRow()));

        right.Children.Add(BuildSection("Dashed strokes", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("Dropshadow (off / soft / hard / colored / faded body / rounded faded body)", BuildDropshadowRow()));
    }

    static ContainerRuntime BuildColumn()
    {
        ContainerRuntime column = new();
        column.ChildrenLayout = ChildrenLayout.TopToBottomStack;
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
        section.ChildrenLayout = ChildrenLayout.TopToBottomStack;
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
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
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
            arc.StrokeColor = new Color(218, 165, 32, 255);
            row.Children.Add(arc);
        }
        return row;
    }

    // Thickness progression - last cell hits Thickness = Width / 2 (a true pie wedge). Same
    // values as the Skia gallery so the two galleries read identically side by side.
    static ContainerRuntime BuildThicknessProgressionRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float thickness in new[] { 2f, 8f, 18f, 30f })
        {
            ArcRuntime arc = new();
            arc.Width = 60;
            arc.Height = 60;
            arc.SweepAngle = 90;
            arc.Thickness = thickness;
            arc.StrokeColor = new Color(144, 238, 144, 255);
            row.Children.Add(arc);
        }
        return row;
    }

    // Issue #2895 - butt vs. rounded caps on raylib. Sweep < 360 so the caps are actually
    // present. Mirrors the Skia gallery's BuildEndCapRow exactly so the two galleries read the
    // same when audited side by side.
    static ContainerRuntime BuildEndCapRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        ArcRuntime butt = new();
        butt.Width = 60; butt.Height = 60;
        butt.SweepAngle = 180;
        butt.Thickness = 10;
        butt.StrokeColor = Color.White;
        butt.IsEndRounded = false;
        row.Children.Add(butt);

        ArcRuntime rounded = new();
        rounded.Width = 60; rounded.Height = 60;
        rounded.SweepAngle = 180;
        rounded.Thickness = 10;
        rounded.StrokeColor = Color.White;
        rounded.IsEndRounded = true;
        row.Children.Add(rounded);

        return row;
    }

    // Mirrors the Skia gallery's dashed-stroke row - same dash/gap pairs and same Thickness
    // values so dashed arcs look comparable across backends.
    static ContainerRuntime BuildDashedStrokeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        ArcRuntime solid = new();
        solid.Width = 60; solid.Height = 60;
        solid.SweepAngle = 270;
        solid.Thickness = 2;
        solid.StrokeColor = Color.White;
        row.Children.Add(solid);

        ArcRuntime short64 = new();
        short64.Width = 60; short64.Height = 60;
        short64.SweepAngle = 270;
        short64.Thickness = 2;
        short64.StrokeColor = Color.White;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.Children.Add(short64);

        ArcRuntime dotted = new();
        dotted.Width = 60; dotted.Height = 60;
        dotted.SweepAngle = 270;
        dotted.Thickness = 1;
        dotted.StrokeColor = Color.White;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        row.Children.Add(dotted);

        ArcRuntime longDash = new();
        longDash.Width = 60; longDash.Height = 60;
        longDash.SweepAngle = 270;
        longDash.Thickness = 3;
        longDash.StrokeColor = new Color(144, 238, 144, 255);
        longDash.StrokeDashLength = 12;
        longDash.StrokeGapLength = 6;
        row.Children.Add(longDash);

        return row;
    }

    // Last cell is the #2851 visual acceptance: same soft-shadow config as the second cell but
    // with the body alpha cut to 80. The shadow must fade with the body - matches Skia/MG.
    // Pre-fix the raylib LineArc dropshadow would emit DropshadowColor straight through and
    // leave a too-opaque shadow ghost behind.
    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        Color goldenrod = new Color(218, 165, 32, 255);

        ArcRuntime baseline = new();
        baseline.Width = 60; baseline.Height = 60;
        baseline.SweepAngle = 270;
        baseline.Thickness = 10;
        baseline.StrokeColor = goldenrod;
        row.Children.Add(baseline);

        ArcRuntime soft = new();
        soft.Width = 60; soft.Height = 60;
        soft.SweepAngle = 270;
        soft.Thickness = 10;
        soft.StrokeColor = goldenrod;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 14;
        soft.DropshadowOffsetY = 14;
        soft.DropshadowBlurX = 4;
        soft.DropshadowBlurY = 4;
        row.Children.Add(soft);

        ArcRuntime hard = new();
        hard.Width = 60; hard.Height = 60;
        hard.SweepAngle = 270;
        hard.Thickness = 10;
        hard.StrokeColor = goldenrod;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlurX = 0;
        hard.DropshadowBlurY = 0;
        row.Children.Add(hard);

        ArcRuntime colored = new();
        colored.Width = 60; colored.Height = 60;
        colored.SweepAngle = 270;
        colored.Thickness = 10;
        colored.StrokeColor = goldenrod;
        colored.HasDropshadow = true;
        colored.DropshadowRed = 220; colored.DropshadowGreen = 40; colored.DropshadowBlue = 160; colored.DropshadowAlpha = 220;
        colored.DropshadowOffsetX = 16;
        colored.DropshadowOffsetY = 16;
        colored.DropshadowBlurX = 6;
        colored.DropshadowBlurY = 6;
        row.Children.Add(colored);

        ArcRuntime fadedBody = new();
        fadedBody.Width = 60; fadedBody.Height = 60;
        fadedBody.SweepAngle = 270;
        fadedBody.Thickness = 10;
        fadedBody.StrokeColor = new Color((byte)218, (byte)165, (byte)32, (byte)80);
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 14;
        fadedBody.DropshadowOffsetY = 14;
        fadedBody.DropshadowBlurX = 4;
        fadedBody.DropshadowBlurY = 4;
        row.Children.Add(fadedBody);

        // Issue #2895 acceptance cell: rounded-cap arc at body alpha 80, NO dropshadow. Locks in
        // the overdraw-free cap rule - if the cap were a full DrawCircle instead of a half-disk,
        // the cap region would double-composite over the band's flat end and read as a darker
        // crescent at the two endpoints. With the DrawCircleSector half-disk approach, endpoint
        // alpha must match the alpha of the band's middle.
        ArcRuntime roundedFaded = new();
        roundedFaded.Width = 60; roundedFaded.Height = 60;
        roundedFaded.SweepAngle = 180;
        roundedFaded.Thickness = 14;
        roundedFaded.StrokeColor = new Color((byte)255, (byte)255, (byte)255, (byte)80);
        roundedFaded.IsEndRounded = true;
        row.Children.Add(roundedFaded);

        return row;
    }
}
