using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
#if RAYLIB
using Color = Raylib_cs.Color;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Color = Microsoft.Xna.Framework.Color;
#endif

#if RAYLIB
namespace Examples.Shapes;
#elif SKIA
namespace SilkNetGum.Screens;
#else
namespace MonoGameGumInCode.Screens;
#endif

// Issue #4029: converged into a single shared file (was three drifted copies at
// Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/RectanglesScreen.cs, Samples/raylib/Screens/
// RectanglesScreen.cs, and Samples/SilkNetGum/SilkNetGumSample/Screens/RectanglesScreen.cs)
// following the same pattern as CirclesScreen.cs (#4024). Linked into Samples/raylib/GumTest.csproj
// and Samples/SilkNetGum/SilkNetGumSample/SilkNetGumSample.csproj via <Compile Include ... Link>.
//
// Colors are always built via `new Color(r, g, b, a)` rather than named statics -- SKColor has no
// static named-color members (only the separate SkiaSharp.SKColors class does), so a shared named
// color would need a second alias for no real benefit given how few colors overlap across all
// three backends' palettes.
//
// CornerRadius and the per-corner CustomRadius* overrides run on every backend -- all three
// RectangleRuntime implementations expose and render CornerRadius, and MG (Apos.Shapes two-slot)
// and Skia (RoundedRectangle) both fully render per-corner radii. raylib exposes CustomRadius*
// as a round-trip parity surface only (DrawRectangleRounded takes a single roundness, no per-
// corner mesh yet) -- left ungated on purpose so the gap is visible: the per-corner row still
// runs on raylib, it just renders as a uniformly-rounded rectangle instead of opposite corners
// differing.
//
// Antialiasing is likewise ungated on every backend even though raylib has no per-shape AA
// (framebuffer MSAA only, via SetConfigFlags(Msaa4xHint) in Program.Main) -- toggling
// IsAntialiased there renders identically both ways, same as CirclesScreen.
//
// Rotation (outline): raylib's LineRectangle stroke pass is solid-color only (no gradient-on-
// stroke path), so BuildRotatedGradientRectCell(filled: false) renders a solid outline instead of
// the black-to-white gradient the other backends show. Left as-is rather than special-cased (same
// treatment as CirclesScreen.BuildRotationRow), not swapped out for an "unsupported" label.
//
// Blend runs on every backend except Skia (#if !SKIA) -- Skia's RectangleRuntime has no Blend
// property at all, so calling it is a compile error there, not a no-op.
//
// Layout convention: every container that sets WidthUnits / HeightUnits to RelativeToChildren
// also sets Width / Height = 0. RelativeToChildren means the final size is children-extent + the
// explicit Width/Height; a non-zero value adds extra padding the layout almost never wants.
internal class RectanglesScreen : FrameworkElement
{
    public RectanglesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // Two-column root so the screen grows wide rather than tall as rows accumulate. No
        // ScrollViewer parity in SkiaGum yet, so this is the cheapest layout that works across all
        // three backends.
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

        left.Children.Add(BuildSection("Sizes", BuildSizesRow()));
        left.Children.Add(BuildSection("Alpha", BuildAlphaRow()));
        left.Children.Add(BuildSection("Modes", BuildModeRow()));
        left.Children.Add(BuildSection("Stroke width", BuildStrokeWidthRow()));
        left.Children.Add(BuildSection("Alignment", BuildAlignmentRow()));
        left.Children.Add(BuildSection("Corner radius", BuildCornerRadiusRow()));
        left.Children.Add(BuildSection("Per-corner radii", BuildPerCornerRow()));
        left.Children.Add(BuildSection("Gradients", BuildGradientRow()));

        right.Children.Add(BuildSection("Antialiasing", BuildAntialiasingRow()));
        right.Children.Add(BuildSection("Dropshadow", BuildDropshadowRow()));
        right.Children.Add(BuildSection("Dashed strokes", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("Fill + stroke", BuildBothColorsRow()));
        right.Children.Add(BuildSection("Inscribed", BuildInscribedRow()));
        right.Children.Add(BuildSection("Rotation (filled)", BuildRotationRow(filled: true)));
        right.Children.Add(BuildSection("Rotation (outline)", BuildRotationRow(filled: false)));
#if !SKIA
        right.Children.Add(BuildSection("Blend (additive #3458)", BuildBlendRow()));
#endif
    }

#if !SKIA
    // Issue #3458 — two overlapping-triad cells sit side by side: the left cell uses
    // Blend.Additive on all three rectangles so where red/green/blue overlap the channels sum
    // and the intersection brightens (R+G = yellow, R+G+B ~= white). The right cell is the SAME
    // geometry with Blend left at the default Normal, so the topmost rectangle simply occludes
    // the others with no brightening -- a side-by-side control that makes the additive effect
    // obvious.
    static ContainerRuntime BuildBlendRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        row.Children.Add(BuildBlendTriadCell(Gum.RenderingLibrary.Blend.Additive));
        row.Children.Add(BuildBlendTriadCell(Gum.RenderingLibrary.Blend.Normal));
        return row;
    }

    // A dark 150x120 frame with three 70x70 primary-color rectangles arranged so all three
    // overlap in the middle. Under Additive the overlaps sum toward white; under Normal the last
    // rectangle drawn just covers the earlier ones.
    static ContainerRuntime BuildBlendTriadCell(Gum.RenderingLibrary.Blend blend)
    {
        ContainerRuntime frame = new();
        frame.Width = 150;
        frame.Height = 120;

        RectangleRuntime backdrop = new();
        backdrop.Width = 0;
        backdrop.Height = 0;
        backdrop.WidthUnits = DimensionUnitType.RelativeToParent;
        backdrop.HeightUnits = DimensionUnitType.RelativeToParent;
        backdrop.FillColor = new Color(20, 20, 30, 255);
        backdrop.IsFilled = true;
        frame.Children.Add(backdrop);

        AddBlendRect(frame, blend, new Color(255, 0, 0, 255), x: 10, y: 10);
        AddBlendRect(frame, blend, new Color(0, 255, 0, 255), x: 45, y: 10);
        AddBlendRect(frame, blend, new Color(0, 0, 255, 255), x: 27, y: 42);

        return frame;
    }

    static void AddBlendRect(ContainerRuntime frame, Gum.RenderingLibrary.Blend blend, Color color, float x, float y)
    {
        RectangleRuntime rect = new();
        rect.Width = 70;
        rect.Height = 70;
        rect.X = x;
        rect.Y = y;
        rect.FillColor = color;
        rect.IsFilled = true;
        rect.Blend = blend;
        frame.Children.Add(rect);
    }
#endif

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

    static ContainerRuntime BuildSizesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float width in new[] { 40f, 60f, 90f, 130f })
        {
            RectangleRuntime rect = new();
            rect.Width = width;
            rect.Height = 40;
            rect.StrokeColor = new Color(255, 255, 255, 255);
            row.Children.Add(rect);
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
            row.Children.Add(rect);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime filled = new();
        filled.Width = 80; filled.Height = 50;
        filled.FillColor = new Color(220, 20, 60, 255); // Crimson
        filled.IsFilled = true;
        row.Children.Add(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 80; stroked.Height = 50;
        stroked.StrokeColor = new Color(0, 255, 255, 255); // Cyan
        stroked.StrokeWidth = 2;
        row.Children.Add(stroked);

        RectangleRuntime both = new();
        both.Width = 80; both.Height = 50;
        both.FillColor = new Color(40, 40, 80, 255);
        both.IsFilled = true;
        both.StrokeColor = new Color(255, 255, 0, 255); // Yellow
        both.StrokeWidth = 2;
        row.Children.Add(both);

        RectangleRuntime defaultRect = new();
        defaultRect.Width = 80; defaultRect.Height = 50;
        row.Children.Add(defaultRect);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            RectangleRuntime rect = new();
            rect.Width = 70;
            rect.Height = 50;
            rect.StrokeColor = new Color(144, 238, 144, 255); // LightGreen
            rect.StrokeWidth = strokeWidth;
            row.Children.Add(rect);
        }
        return row;
    }

    static ContainerRuntime BuildAlignmentRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (VerticalAlignment alignment in new[] { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom })
        {
            row.Children.Add(BuildAlignmentCell(alignment));
        }
        return row;
    }

    // Issue #2757/#2814 — uniform CornerRadius in pixels, rendered on every backend: MG's
    // Apos.Shapes two-slot fill+stroke, Skia's internal RoundedRectangle, and raylib's
    // DrawRectangleRounded (converted from pixels to raylib's 0..1 roundness fraction at draw
    // time).
    static ContainerRuntime BuildCornerRadiusRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float cornerRadius in new[] { 0f, 6f, 16f, 28f })
        {
            RectangleRuntime rect = new();
            rect.Width = 80; rect.Height = 60;
            rect.FillColor = new Color(40, 40, 80, 255);
            rect.IsFilled = true;
            rect.StrokeColor = new Color(255, 165, 0, 255); // Orange
            rect.StrokeWidth = 2;
            rect.CornerRadius = cornerRadius;
            row.Children.Add(rect);
        }
        return row;
    }

    // Issue #2818 — per-corner radii reach the renderer via the runtime's CustomRadius*
    // pass-through. MG and Skia render all four corners independently; raylib rounds this trip
    // (see file header) so this cell renders as a uniformly-rounded rectangle there instead of
    // opposite corners differing — left ungated so that gap is visible rather than hidden.
    static ContainerRuntime BuildPerCornerRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        RectangleRuntime rect = new();
        rect.Width = 120; rect.Height = 70;
        rect.FillColor = new Color(40, 40, 80, 255);
        rect.IsFilled = true;
        rect.StrokeColor = new Color(255, 165, 0, 255); // Orange
        rect.StrokeWidth = 2;
        rect.CustomRadiusTopLeft = 20;
        rect.CustomRadiusTopRight = 2;
        rect.CustomRadiusBottomRight = 20;
        rect.CustomRadiusBottomLeft = 2;
        row.Children.Add(rect);
        return row;
    }

    // Each cell exercises a different gradient configuration; with two-slot fill+stroke
    // composition, RectangleRuntime pushes gradient state through to both the fill and stroke
    // renderables, so a single gradient covers the filled card.
    static ContainerRuntime BuildGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Linear horizontal: white → steel blue
        RectangleRuntime linearH = new();
        linearH.Width = 70; linearH.Height = 50;
        linearH.FillColor = new Color(255, 255, 255, 255); // gradient start stop is the fill color
        linearH.IsFilled = true;
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color2 = new Color(70, 130, 180, 255); // SteelBlue
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 70; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: gold → crimson
        RectangleRuntime linearV = new();
        linearV.Width = 70; linearV.Height = 50;
        linearV.FillColor = new Color(255, 215, 0, 255); // Gold
        linearV.IsFilled = true;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color2 = new Color(220, 20, 60, 255); // Crimson
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 50;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta
        RectangleRuntime linearD = new();
        linearD.Width = 70; linearD.Height = 50;
        linearD.FillColor = new Color(0, 255, 255, 255); // Cyan
        linearD.IsFilled = true;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color2 = new Color(255, 0, 255, 255); // Magenta
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 70; linearD.GradientY2 = 50;
        row.Children.Add(linearD);

        // Radial centered: white → dark green
        RectangleRuntime radial = new();
        radial.Width = 70; radial.Height = 50;
        radial.FillColor = new Color(255, 255, 255, 255);
        radial.IsFilled = true;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color2 = new Color(0, 100, 0, 255); // DarkGreen
        radial.GradientX1 = 35; radial.GradientY1 = 25;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 35;
        row.Children.Add(radial);

        return row;
    }

    // Issue #2818 visual acceptance: two pairs (filled card + 1 px outline frame), once with
    // IsAntialiased = true (the default — soft edges) and once false (crisp pixels). The 1 px
    // stroke makes the AA bloom obvious. On raylib this is a no-op -- no per-shape AA there, see
    // the file header -- so both cells in a pair render identically.
    static ContainerRuntime BuildAntialiasingRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        foreach (bool aa in new[] { true, false })
        {
            RectangleRuntime filled = new();
            filled.Width = 60; filled.Height = 50;
            filled.FillColor = new Color(218, 165, 32, 255); // Goldenrod
            filled.IsFilled = true;
            filled.IsAntialiased = aa;
            row.Children.Add(filled);

            RectangleRuntime frame = new();
            frame.Width = 60; frame.Height = 50;
            frame.StrokeColor = new Color(255, 255, 255, 255);
            frame.StrokeWidth = 1;
            frame.IsAntialiased = aa;
            row.Children.Add(frame);
        }

        return row;
    }

    // Issue #2818 visual acceptance: four cells — first is the baseline (no shadow), the
    // remaining three exercise different shadow configurations. Plus a fifth (#2851) cell that
    // fades the body's alpha to confirm the shadow fades alongside it rather than leaving an
    // opaque ghost.
    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        Color goldenrod = new Color(218, 165, 32, 255);

        // Baseline: no shadow.
        RectangleRuntime baseline = new();
        baseline.Width = 60; baseline.Height = 50;
        baseline.FillColor = goldenrod;
        baseline.IsFilled = true;
        row.Children.Add(baseline);

        // Soft shadow: noticeable offset, generous blur, default opaque black.
        RectangleRuntime soft = new();
        soft.Width = 60; soft.Height = 50;
        soft.FillColor = goldenrod;
        soft.IsFilled = true;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 14;
        soft.DropshadowOffsetY = 14;
        soft.DropshadowBlur = 4;
        row.Children.Add(soft);

        // Hard offset: bigger offset, no blur, semi-transparent black.
        RectangleRuntime hard = new();
        hard.Width = 60; hard.Height = 50;
        hard.FillColor = goldenrod;
        hard.IsFilled = true;
        hard.HasDropshadow = true;
        hard.DropshadowColor = new Color(0, 0, 0, 160);
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlur = 0;
        row.Children.Add(hard);

        // Colored shadow: magenta cast, real offset so the cast is visible against the blue
        // background (offset = 0 would tuck the entire shadow under the opaque card and leave
        // only a thin halo, which on a blue page reads as nothing).
        RectangleRuntime colored = new();
        colored.Width = 60; colored.Height = 50;
        colored.FillColor = goldenrod;
        colored.IsFilled = true;
        colored.HasDropshadow = true;
        colored.DropshadowColor = new Color(220, 40, 160, 220);
        colored.DropshadowOffsetX = 16;
        colored.DropshadowOffsetY = 16;
        colored.DropshadowBlur = 6;
        row.Children.Add(colored);

        // Issue #2851 visual acceptance: same soft-shadow config as the second cell, but with the
        // body's alpha cut to 80. The shadow must fade alongside the body rather than leaving an
        // opaque ghost behind a translucent card.
        RectangleRuntime fadedBody = new();
        fadedBody.Width = 60; fadedBody.Height = 50;
        fadedBody.FillColor = new Color(218, 165, 32, 80);
        fadedBody.IsFilled = true;
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 14;
        fadedBody.DropshadowOffsetY = 14;
        fadedBody.DropshadowBlur = 4;
        row.Children.Add(fadedBody);

        return row;
    }

    // Issue #2818 visual acceptance: four cells stepping through dash/gap patterns. First cell is
    // the solid-stroke baseline (dash=0). Dashing applies to stroke only.
    static ContainerRuntime BuildDashedStrokeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Baseline: solid stroke (dash=0).
        RectangleRuntime solid = new();
        solid.Width = 60; solid.Height = 50;
        solid.StrokeColor = new Color(255, 255, 255, 255);
        solid.StrokeWidth = 2;
        row.Children.Add(solid);

        // Short 6/4 dash.
        RectangleRuntime short64 = new();
        short64.Width = 60; short64.Height = 50;
        short64.StrokeColor = new Color(255, 255, 255, 255);
        short64.StrokeWidth = 2;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.Children.Add(short64);

        // Tight 2/2 dotted.
        RectangleRuntime dotted = new();
        dotted.Width = 60; dotted.Height = 50;
        dotted.StrokeColor = new Color(255, 255, 255, 255);
        dotted.StrokeWidth = 1;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        row.Children.Add(dotted);

        // Long-dash motif: 12/6 with a thicker stroke.
        RectangleRuntime longDash = new();
        longDash.Width = 60; longDash.Height = 50;
        longDash.StrokeColor = new Color(144, 238, 144, 255); // LightGreen
        longDash.StrokeWidth = 3;
        longDash.StrokeDashLength = 12;
        longDash.StrokeGapLength = 6;
        row.Children.Add(longDash);

        return row;
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

    // Visual acceptance for #2757/#2814/#2818 — both layers (fill + stroke) render simultaneously
    // regardless of setter order.
    static ContainerRuntime BuildBothColorsRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime strokeLast = new();
        strokeLast.Width = 80; strokeLast.Height = 50;
        strokeLast.FillColor = new Color(220, 20, 60, 255); // Crimson
        strokeLast.IsFilled = true;
        strokeLast.StrokeColor = new Color(0, 255, 255, 255); // Cyan
        strokeLast.StrokeWidth = 4;
        row.Children.Add(strokeLast);

        RectangleRuntime fillLast = new();
        fillLast.Width = 80; fillLast.Height = 50;
        fillLast.StrokeColor = new Color(255, 0, 255, 255); // Magenta
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = new Color(255, 215, 0, 255); // Gold
        fillLast.IsFilled = true;
        row.Children.Add(fillLast);

        return row;
    }

    // Visual contract for #2757/#2814 — the stroke slot mirrors the runtime's Width/Height each
    // frame, and stroke-inset handling keeps the frame inscribed inside the bounds. Cells get
    // progressively thicker strokes (1, 4, 8, 12) — every frame must stay inside the gray
    // rectangle.
    static ContainerRuntime BuildInscribedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 4f, 8f, 12f })
        {
            row.Children.Add(BuildInscribedCell(strokeWidth));
        }
        return row;
    }

    static RectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        RectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.FillColor = new Color(60, 60, 80, 255);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 64;
        rect.Height = 64;
        rect.FillColor = new Color(46, 139, 87, 255); // SeaGreen
        rect.IsFilled = true;
        rect.StrokeColor = new Color(255, 255, 0, 255); // Yellow
        rect.StrokeWidth = strokeWidth;
        rect.StrokeWidthUnits = DimensionUnitType.Absolute;
        frame.Children.Add(rect);
        return frame;
    }

    // Rotation row — black→white horizontal gradient on rectangles, rotated in 60° steps
    // (0/60/120/180). Gradient endpoints are 0→20 px (less than the 70 px width) so the
    // transition is concentrated in a narrow band — the resulting hard light/dark edge makes the
    // rotation angle obvious. Cells use a fixed-size frame because Rotation pushes content outside
    // the natural bounding box, which breaks the RelativeToChildren row sizing.
    //
    // Two rows: "filled" sets FillColor opaque so the gradient lights up the fill slot; "outline"
    // sets IsFilled = false so the gradient lights up the stroke slot. raylib's LineRectangle
    // stroke pass is solid-color only (no gradient-on-stroke path), so the outline row renders a
    // plain solid frame on that backend instead of a gradient -- left as-is rather than special-
    // cased, so the gap is visible.
    static ContainerRuntime BuildRotationRow(bool filled)
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float rotation in new[] { 0f, 60f, 120f, 180f })
        {
            row.Children.Add(BuildRotatedGradientRectCell(rotation, filled));
        }
        return row;
    }

    static RectangleRuntime BuildRotatedGradientRectCell(float rotation, bool filled)
    {
        RectangleRuntime frame = new();
        frame.Width = 100;
        frame.Height = 100;
        frame.FillColor = new Color(60, 60, 80, 255);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 70;
        rect.Height = 50;
        rect.XOrigin = HorizontalAlignment.Center;
        rect.XUnits = GeneralUnitType.PixelsFromMiddle;
        rect.YOrigin = VerticalAlignment.Center;
        rect.YUnits = GeneralUnitType.PixelsFromMiddle;
        // The gradient start stop is the active body color: FillColor when filled, StrokeColor
        // when only stroked. Set the appropriate slot to black so the gradient starts dark in
        // both variants.
        if (filled)
        {
            rect.FillColor = new Color(0, 0, 0, 255);
            rect.IsFilled = true;
        }
        else
        {
            rect.IsFilled = false;
            rect.StrokeColor = new Color(0, 0, 0, 255);
        }
        rect.UseGradient = true;
        rect.GradientType = GradientType.Linear;
        rect.Color2 = new Color(255, 255, 255, 255);
        rect.GradientX1 = 0; rect.GradientY1 = 0;
        rect.GradientX2 = 20; rect.GradientY2 = 0;
        rect.Rotation = rotation;
        frame.Children.Add(rect);
        return frame;
    }

    static RectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // Visible frame so the alignment is obvious. Children are positioned relative to it via
        // YOrigin + PixelsFromSmall/Middle/Large.
        RectangleRuntime frame = new();
        frame.Width = 128;
        frame.Height = 100;
        frame.FillColor = new Color(50, 50, 70, 255);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 50;
        rect.Height = 30;
        rect.FillColor = new Color(255, 165, 0, 255); // Orange
        rect.IsFilled = true;
        rect.XOrigin = HorizontalAlignment.Center;
        rect.XUnits = GeneralUnitType.PixelsFromMiddle;
        rect.YOrigin = alignment;
        rect.YUnits = alignment switch
        {
            VerticalAlignment.Top => GeneralUnitType.PixelsFromSmall,
            VerticalAlignment.Center => GeneralUnitType.PixelsFromMiddle,
            VerticalAlignment.Bottom => GeneralUnitType.PixelsFromLarge,
            _ => GeneralUnitType.PixelsFromMiddle,
        };
        frame.Children.Add(rect);
        return frame;
    }
}
