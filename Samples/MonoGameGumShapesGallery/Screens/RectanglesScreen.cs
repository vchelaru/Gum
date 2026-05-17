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

        right.AddChild(BuildSection("FillColor + StrokeColor on the same instance — both layers render simultaneously (#2768 / #2814)", BuildBothColorsRow()));
        right.AddChild(BuildSection("Inscribed in a 64x64 frame — stroke must stay inside the gray rectangle's bounds at every StrokeWidth (#2768 / #2814 visual contract; mirrors SilkNetGum)", BuildInscribedRow()));
        // N/A on MG RectangleRuntime: UseGradient, IsAntialiased, HasDropshadow,
        // StrokeDashLength/StrokeGapLength are exposed on MG CircleRuntime but not yet on
        // MG RectangleRuntime. Skia RectangleRuntime DOES support all of these via its
        // SkiaShapeRuntime base — see SilkNetGum/Screens/RectanglesScreen for the visual
        // contract those rows enforce. Plumb the equivalent props through to MG
        // RectangleRuntime and add the four rows below to close the gap.
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
            rect.StrokeColor = Color.White;
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
            rect.CornerRadius = cornerRadius;
            row.AddChild(rect);
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

    static ColoredRectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        ColoredRectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.Color = new Color(60, 60, 80);

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

    static ColoredRectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // Frame size matches the Skia (SilkNetGum) RectanglesScreen sibling so the two
        // galleries lay out the same. The inner RectangleRuntime is positioned via
        // YOrigin + PixelsFromSmall/Middle/Large.
        ColoredRectangleRuntime frame = new();
        frame.Width = 128;
        frame.Height = 100;
        frame.Color = new Color(50, 50, 70);

        RectangleRuntime rect = new();
        rect.Width = 60;
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
