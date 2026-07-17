using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.Hazard;

/// <summary>
/// Factory helpers for the Apos.Shapes runtimes a theme builds its chrome from.
/// Every visual in this theme paints itself with the same handful of shapes -
/// a filled body, a stroked border, an offset focus ring - so those are defined
/// once here instead of being hand-rolled (~15 lines each) in every visual.
///
/// This is generic theme INFRASTRUCTURE, not the theme's identity: the methods
/// take the color / radius / thickness as arguments, and the look comes from what
/// each visual passes in (read from <see cref="HazardStyling.ActiveStyle"/>). The file is
/// part of the template and is copied into each cloned theme - it is deliberately
/// NOT a shared library, so a theme stays a self-contained reference.
///
/// All shapes are centered on and sized RelativeToParent, so they track their
/// parent's size automatically. A focus ring is sized slightly LARGER than its
/// parent (via <c>inset</c>) so its stroke sits just outside the body, and starts
/// hidden (<c>Visible = false</c>) - flip <c>Visible</c> in a state callback.
/// </summary>
internal static class HazardShapes
{
    /// <summary>A filled rounded rectangle that exactly fills its parent.</summary>
    public static RectangleRuntime Fill(Color color, float cornerRadius = 0f, string name = "Fill")
    {
        RectangleRuntime rect = new RectangleRuntime { Name = name };
        Stretch(rect, extraSize: 0f);
        rect.CornerRadius = cornerRadius;
        rect.IsFilled = true;
        rect.FillColor = color;
        rect.StrokeWidth = 0;
        return rect;
    }

    /// <summary>A stroked (outline-only) rounded rectangle that exactly fills its parent.</summary>
    public static RectangleRuntime Border(Color color, float cornerRadius = 0f, float thickness = 1f, string name = "Border")
    {
        RectangleRuntime rect = new RectangleRuntime { Name = name };
        Stretch(rect, extraSize: 0f);
        rect.CornerRadius = cornerRadius;
        rect.IsFilled = false;
        rect.StrokeWidth = thickness;
        rect.StrokeWidthUnits = DimensionUnitType.Absolute;
        rect.StrokeColor = color;
        return rect;
    }

    /// <summary>
    /// A stroked rounded rectangle sized <paramref name="inset"/> pixels outside its
    /// parent on every side, initially hidden - the standard focus ring. The corner
    /// radius is bumped by <paramref name="inset"/> so the ring stays concentric with
    /// a body of radius <paramref name="cornerRadius"/>.
    /// </summary>
    public static RectangleRuntime FocusRing(Color color, float cornerRadius = 0f, float inset = 1f, float thickness = 1f, string name = "FocusRing")
    {
        RectangleRuntime ring = new RectangleRuntime { Name = name };
        Stretch(ring, extraSize: inset * 2f);
        ring.CornerRadius = cornerRadius + inset;
        ring.IsFilled = false;
        ring.StrokeWidth = thickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = color;
        ring.Visible = false;
        return ring;
    }

    /// <summary>A filled circle that exactly fills its parent.</summary>
    public static CircleRuntime FilledCircle(Color color, string name = "Fill")
    {
        CircleRuntime circle = new CircleRuntime { Name = name };
        Stretch(circle, extraSize: 0f);
        circle.IsFilled = true;
        circle.FillColor = color;
        circle.StrokeWidth = 0;
        return circle;
    }

    /// <summary>A stroked (outline-only) circle that exactly fills its parent.</summary>
    public static CircleRuntime CircleBorder(Color color, float thickness = 1f, string name = "Border")
    {
        CircleRuntime circle = new CircleRuntime { Name = name };
        Stretch(circle, extraSize: 0f);
        circle.IsFilled = false;
        circle.StrokeWidth = thickness;
        circle.StrokeWidthUnits = DimensionUnitType.Absolute;
        circle.StrokeColor = color;
        return circle;
    }

    /// <summary>
    /// A stroked circle sized <paramref name="inset"/> pixels outside its parent,
    /// initially hidden - the circular equivalent of <see cref="FocusRing"/>.
    /// </summary>
    public static CircleRuntime CircleFocusRing(Color color, float inset = 2f, float thickness = 1f, string name = "FocusRing")
    {
        CircleRuntime ring = new CircleRuntime { Name = name };
        Stretch(ring, extraSize: inset * 2f);
        ring.IsFilled = false;
        ring.StrokeWidth = thickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = color;
        ring.Visible = false;
        return ring;
    }

    /// <summary>
    /// Positions a shape centered on its parent and sized to the parent's dimensions
    /// plus <paramref name="extraSize"/> pixels per axis (0 = exactly the parent size;
    /// a positive value grows it past the parent, as a focus ring needs).
    /// </summary>
    private static void Stretch(GraphicalUiElement shape, float extraSize)
    {
        shape.X = 0;
        shape.Y = 0;
        shape.XUnits = GeneralUnitType.PixelsFromMiddle;
        shape.YUnits = GeneralUnitType.PixelsFromMiddle;
        shape.XOrigin = HorizontalAlignment.Center;
        shape.YOrigin = VerticalAlignment.Center;
        shape.Width = extraSize;
        shape.Height = extraSize;
        shape.WidthUnits = DimensionUnitType.RelativeToParent;
        shape.HeightUnits = DimensionUnitType.RelativeToParent;
    }
}
