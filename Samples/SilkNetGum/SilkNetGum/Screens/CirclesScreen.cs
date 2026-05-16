using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Skia mirror of MonoGameGumShapesGallery/Screens/CirclesScreen.cs (issue #2785). The two
// files should stay in lock-step structurally — same sections, same parameter sweeps — so
// visual regressions in one backend are easy to spot against the other.
//
// What forces the two files apart:
//   - Base class. Forms (FrameworkElement / Dock / AddChild) hasn't reached the Skia runtime
//     yet, so this screen derives from GraphicalUiElement and uses Children.Add directly.
//   - Color type. XNA Microsoft.Xna.Framework.Color becomes SKColor; named colors come from
//     SkiaSharp.SKColors instead of Color.X.
//   - The MonoGame CirclesScreen also uses VerticalAlignment from the FRB Forms types in its
//     alignment switch; here we use the same RenderingLibrary.Graphics.VerticalAlignment.
//
// Everything else — section layout, sweep values, property names (Radius, StrokeColor,
// FillColor, StrokeWidth, gradient props) — is identical to the MonoGame version.
internal class CirclesScreen : GraphicalUiElement
{
    public CirclesScreen() : base(new InvisibleRenderable())
    {
        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 14;
        root.X = 10;
        root.Y = 10;
        this.Children.Add(root);

        root.Children.Add(BuildSection("Sizes (radius 16, 24, 32, 48) — default outline", BuildSizesRow()));
        root.Children.Add(BuildSection("Alpha on StrokeColor (255, 192, 128, 64)", BuildAlphaRow()));
        root.Children.Add(BuildSection("Modes: FillColor, StrokeColor, default", BuildModeRow()));
        root.Children.Add(BuildSection("StrokeWidth (1, 2, 4, 8 px)", BuildStrokeWidthRow()));
        root.Children.Add(BuildSection("Alignment inside a 220x100 frame (Top / Center / Bottom)", BuildAlignmentRow()));
        root.Children.Add(BuildSection("Gradients (linear / radial / diagonal / centered)", BuildGradientRow()));
        root.Children.Add(BuildSection("Antialiasing (default ON, then OFF) — 1 px stroke makes the bloom obvious (#2798)", BuildAntialiasingRow()));
        root.Children.Add(BuildSection("Known limitation: FillColor + StrokeColor on the same instance — only the most-recently-set one renders today (see #2790).", BuildBothColorsRow()));
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

    static ContainerRuntime BuildSizesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float radius in new[] { 16f, 24f, 32f, 48f })
        {
            CircleRuntime circle = new();
            circle.Radius = radius;
            circle.StrokeColor = SKColors.White;
            row.Children.Add(circle);
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
            circle.StrokeColor = new SKColor(255, 255, 255, alpha);
            row.Children.Add(circle);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        CircleRuntime filled = new();
        filled.Radius = 28;
        filled.FillColor = SKColors.Crimson;
        row.Children.Add(filled);

        CircleRuntime stroked = new();
        stroked.Radius = 28;
        stroked.StrokeColor = SKColors.Cyan;
        stroked.StrokeWidth = 3;
        row.Children.Add(stroked);

        CircleRuntime defaultCircle = new();
        defaultCircle.Radius = 28;
        row.Children.Add(defaultCircle);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            CircleRuntime circle = new();
            circle.Radius = 24;
            circle.StrokeColor = SKColors.LightGreen;
            circle.StrokeWidth = strokeWidth;
            row.Children.Add(circle);
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

    static ContainerRuntime BuildGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Linear horizontal: white → blue
        CircleRuntime linearH = new();
        linearH.Radius = 28;
        linearH.FillColor = SKColors.White; // fill mode; gradient overrides solid color
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color1 = SKColors.White;
        linearH.Color2 = SKColors.SteelBlue;
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 56; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: yellow → red
        CircleRuntime linearV = new();
        linearV.Radius = 28;
        linearV.FillColor = SKColors.White;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color1 = SKColors.Gold;
        linearV.Color2 = SKColors.Crimson;
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 56;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta
        CircleRuntime linearD = new();
        linearD.Radius = 28;
        linearD.FillColor = SKColors.White;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color1 = SKColors.Cyan;
        linearD.Color2 = SKColors.Magenta;
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 56; linearD.GradientY2 = 56;
        row.Children.Add(linearD);

        // Radial centered: white → dark green
        CircleRuntime radial = new();
        radial.Radius = 28;
        radial.FillColor = SKColors.White;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color1 = SKColors.White;
        radial.Color2 = SKColors.DarkGreen;
        radial.GradientX1 = 28; radial.GradientY1 = 28;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 28;
        row.Children.Add(radial);

        return row;
    }

    // Visual acceptance test for #2790. Today the contained Skia renderable has a single color
    // slot + IsFilled toggle, so SkiaShapeRuntime.FillColor and StrokeColor route through the
    // same fields — the most recently set non-null wins. Each cell sets both colors, varying
    // the order, so the limitation is obvious and the fix in #2790 has a clear before/after.
    // When two-slot composition lands, every cell here should show a filled crimson disk with
    // a cyan ring around it (and gold + magenta in the second cell).
    static ContainerRuntime BuildBothColorsRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Stroke set last → only stroke (cyan ring) renders today.
        CircleRuntime strokeWins = new();
        strokeWins.Radius = 28;
        strokeWins.FillColor = SKColors.Crimson;
        strokeWins.StrokeColor = SKColors.Cyan;
        strokeWins.StrokeWidth = 4;
        row.Children.Add(strokeWins);

        // Fill set last → only fill (gold disk) renders today.
        CircleRuntime fillWins = new();
        fillWins.Radius = 28;
        fillWins.StrokeColor = SKColors.Magenta;
        fillWins.StrokeWidth = 4;
        fillWins.FillColor = SKColors.Gold;
        row.Children.Add(fillWins);

        return row;
    }

    // Issue #2798 visual acceptance: two pairs (filled disk + 1 px outline ring), once with
    // IsAntialiased = true (the default — soft edges) and once false (crisp pixels). On Skia
    // this flips SKPaint.IsAntialias on the contained renderable.
    static ContainerRuntime BuildAntialiasingRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        foreach (bool aa in new[] { true, false })
        {
            CircleRuntime filled = new();
            filled.Radius = 28;
            filled.FillColor = SKColors.Goldenrod;
            filled.IsAntialiased = aa;
            row.Children.Add(filled);

            CircleRuntime ring = new();
            ring.Radius = 28;
            ring.StrokeColor = SKColors.White;
            ring.StrokeWidth = 1;
            ring.IsAntialiased = aa;
            row.Children.Add(ring);
        }

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

    static ColoredRectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // ColoredRectangle is used as a visible frame so the alignment is obvious. Children
        // are positioned relative to it via YOrigin + PixelsFromSmall/Middle/Large.
        ColoredRectangleRuntime frame = new();
        frame.Width = 220;
        frame.Height = 100;
        frame.Color = new SKColor(50, 50, 70);

        CircleRuntime circle = new();
        circle.Radius = 22;
        circle.FillColor = SKColors.Orange;
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
