using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum's copy of the shared-pattern shape factory (the same helper set
/// <c>TemplateShapes</c> defines for the Template theme). Every visual in this
/// theme paints its chrome from the same handful of Apos.Shapes runtimes - a
/// filled body, a stroked border, an offset focus ring - so those are built
/// once here instead of being hand-rolled (~15 lines each) in every visual.
///
/// Bubblegum adds two dropshadow variants (<see cref="FillWithDropshadow"/> and
/// <see cref="FilledCircleWithDropshadow"/>) on top of the shared set, for the
/// soft pink "lift" shadows under the Button, Window, ToggleButton, and slider
/// thumb. The look comes from what each visual passes in (read from
/// <see cref="BubblegumStyling.ActiveStyle"/>'s <see cref="BubblegumColors"/>), not
/// from this file - this is generic theme infrastructure, kept self-contained per
/// theme rather than shared.
///
/// All shapes are centered on and sized RelativeToParent, so they track their
/// parent's size automatically. A focus ring is sized slightly LARGER than its
/// parent (via <c>inset</c>) so its stroke sits just outside the body, and
/// starts hidden (<c>Visible = false</c>) - flip <c>Visible</c> in a state callback.
/// </summary>
internal static class BubblegumShapes
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

    /// <summary>
    /// A filled rounded rectangle that exactly fills its parent, with a native
    /// Apos.Shapes Gaussian drop shadow - the soft pink "lift" under Bubblegum's
    /// Button / Window / ToggleButton bodies.
    /// </summary>
    public static RectangleRuntime FillWithDropshadow(Color color, float cornerRadius, Color shadowColor, float offsetX, float offsetY, float blur, string name = "Fill")
    {
        RectangleRuntime rect = Fill(color, cornerRadius, name);
        rect.HasDropshadow = true;
        rect.DropshadowColor = shadowColor;
        rect.DropshadowOffsetX = offsetX;
        rect.DropshadowOffsetY = offsetY;
        rect.DropshadowBlur = blur;
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

    /// <summary>
    /// A filled circle that exactly fills its parent, with a native Apos.Shapes
    /// Gaussian drop shadow - the soft pink shadow under Bubblegum's slider thumb.
    /// </summary>
    public static CircleRuntime FilledCircleWithDropshadow(Color color, Color shadowColor, float offsetX, float offsetY, float blur, string name = "Fill")
    {
        CircleRuntime circle = FilledCircle(color, name);
        circle.HasDropshadow = true;
        circle.DropshadowColor = shadowColor;
        circle.DropshadowOffsetX = offsetX;
        circle.DropshadowOffsetY = offsetY;
        circle.DropshadowBlur = blur;
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
