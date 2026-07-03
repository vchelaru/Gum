using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;

// Visual smoke test for RectangleRuntime's two-slot model (issue #2768). Unlike the
// circles screen, core MonoGameGum ships defaults for BOTH rectangle slots — fill via
// SolidRectangle, stroke via LineRectangle — so fill, stroke, and fill+stroke all render
// correctly here without MonoGameGumShapes installed. CornerRadius is intentionally
// omitted from this screen — core defaults are hard-cornered; install MonoGameGumShapes
// and see the gallery sample for visually rounded corners.
//
// Layout convention: every container that sets WidthUnits / HeightUnits to
// RelativeToChildren also sets Width / Height = 0. RelativeToChildren means the final
// size is children-extent + the explicit Width/Height; a non-zero value adds extra
// padding the layout almost never wants.
internal class RectanglesScreen : FrameworkElement
{
    public RectanglesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 12;
        root.X = 8;
        root.Y = 8;
        AddChild(root);

        root.AddChild(BuildSection("Sizes (40, 60, 90, 130 wide)", BuildSizesRow()));
        root.AddChild(BuildSection("Alpha on FillColor (255, 192, 128, 64)", BuildAlphaRow()));
        root.AddChild(BuildSection("FillColor / StrokeColor / Fill+Stroke / default", BuildModeRow()));
        root.AddChild(BuildSection("StrokeWidth (1, 2, 4, 8 px on a filled card)", BuildStrokeWidthRow()));
        root.AddChild(BuildSection("Antialiasing (true vs false — visual no-op without MonoGameGumShapes)", BuildAntialiasingRow()));
        root.AddChild(BuildSection("Alignment inside a 220x100 container (Top / Center / Bottom)", BuildAlignmentRow()));
        root.AddChild(BuildSection("Blend (Additive vs Normal) — overlaps brighten toward white with MonoGameGumShapes; visual no-op on the core SolidRectangle default (#3458)", BuildBlendRow()));
    }

    static ContainerRuntime BuildSection(string label, GraphicalUiElement body)
    {
        ContainerRuntime section = new();
        section.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        section.StackSpacing = 4;
        section.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        section.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        section.Width = 0;
        section.Height = 0;

        TextRuntime header = new();
        header.Text = label;
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
            rect.FillColor = new Color(80, 80, 120);
            rect.IsFilled = true;
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
            rect.FillColor = new Color((byte)255, (byte)255, (byte)255, alpha);
            rect.IsFilled = true;
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
        filled.IsFilled = true;
        row.AddChild(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 80; stroked.Height = 50;
        stroked.StrokeColor = Color.Cyan;
        stroked.StrokeWidth = 2;
        row.AddChild(stroked);

        RectangleRuntime both = new();
        both.Width = 80; both.Height = 50;
        both.FillColor = new Color(40, 40, 80);
        both.IsFilled = true;
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
        // LineRectangle (the core stroke default) IS LinePixelWidth-aware, so the stroke
        // grows visibly even without MonoGameGumShapes. On Apos this same code reads as
        // an anti-aliased version of the same visual.
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            RectangleRuntime rect = new();
            rect.Width = 70; rect.Height = 50;
            rect.FillColor = new Color(30, 30, 50);
            rect.IsFilled = true;
            rect.StrokeColor = Color.LightGreen;
            rect.StrokeWidth = strokeWidth;
            row.AddChild(rect);
        }
        return row;
    }

    static ContainerRuntime BuildAntialiasingRow()
    {
        // Issue #2818: IsAntialiased flips both fill and stroke slots when MonoGameGumShapes
        // is installed. Core SolidRectangle / LineRectangle defaults don't implement
        // IAntialiasedRenderable, so this row reads identical here — load MonoGameGumShapes
        // (see MonoGameGumShapesGallery) for the visible smooth-vs-crisp comparison.
        ContainerRuntime row = BuildHorizontalRow();
        foreach (bool aa in new[] { true, false })
        {
            RectangleRuntime rect = new();
            rect.Width = 80;
            rect.Height = 50;
            rect.FillColor = new Color(30, 30, 50);
            rect.IsFilled = true;
            rect.StrokeColor = Color.LightGreen;
            rect.StrokeWidth = 2;
            rect.IsAntialiased = aa;
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

    // Issue #3458 — mirror of the raylib RectanglesScreen "Blend (Additive)" section for
    // side-by-side comparison. Same geometry/colors as the raylib cell: the left triad uses
    // Blend.Additive so overlapping red/green/blue rectangles sum (R+G = yellow, R+G+B ≈ white);
    // the right triad is the identical geometry left at Blend.Normal as an occlusion control.
    //
    // NOTE: on this Apos-less sample the effect is a VISUAL NO-OP. The core rectangle fill default
    // (RenderingLibrary SolidRectangle) does not implement IBlendedRenderable, so
    // ShapeStrokePreRenderMath.PushBlend has nothing to push to and both triads render identically
    // with standard alpha — same graceful degradation this file documents for the Antialiasing row.
    // Install MonoGameGumShapes (see the GumShapesGallery sample) to see the additive brightening;
    // the raylib sample renders it natively. The section is kept here for API/round-trip parity and
    // forward-compat, so the effect lights up automatically once the shapes package is present.
    static ContainerRuntime BuildBlendRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        row.AddChild(BuildBlendTriadCell(Gum.RenderingLibrary.Blend.Additive));
        row.AddChild(BuildBlendTriadCell(Gum.RenderingLibrary.Blend.Normal));
        return row;
    }

    // A dark 150x120 frame with three 70x70 primary-color rectangles arranged so all three overlap
    // in the middle. Under Additive (with MonoGameGumShapes) the overlaps sum toward white; under
    // Normal the last rectangle drawn just covers the earlier ones.
    static ContainerRuntime BuildBlendTriadCell(Gum.RenderingLibrary.Blend blend)
    {
        ContainerRuntime frame = new();
        frame.Width = 150;
        frame.Height = 120;

        RectangleRuntime backdrop = new();
        backdrop.Width = 0;
        backdrop.Height = 0;
        backdrop.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        backdrop.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        backdrop.FillColor = new Color(20, 20, 30);
        backdrop.IsFilled = true;
        frame.Children.Add(backdrop);

        AddBlendRect(frame, blend, new Color(255, 0, 0), x: 10, y: 10);
        AddBlendRect(frame, blend, new Color(0, 255, 0), x: 45, y: 10);
        AddBlendRect(frame, blend, new Color(0, 0, 255), x: 27, y: 42);

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

    static ContainerRuntime BuildHorizontalRow()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 16;
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        return row;
    }

    static RectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // Outer RectangleRuntime is used as the visible frame (the thing whose alignment
        // is obvious). The inner RectangleRuntime is positioned relative to it via YOrigin
        // + PixelsFromSmall/Middle/Large — same convention the CirclesScreen uses.
        RectangleRuntime frame = new();
        frame.Width = 220;
        frame.Height = 100;
        frame.FillColor = new Color(40, 40, 60);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 60;
        rect.Height = 30;
        rect.FillColor = Color.Orange;
        rect.IsFilled = true;
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
