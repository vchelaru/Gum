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
using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled Button visual. Replaces the V3 ButtonVisual's NineSlice
/// background and underline focus indicator with a stack of Apos.Shapes
/// rounded rectangles: a filled body, a 1px stroked border, and a 1px
/// stroked focus ring that sits one pixel outside the control and is only
/// visible while focused.
/// </summary>
public class ButtonVisual : BaseButtonVisual
{
    private const float CornerRadius = 2f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the base visuals we're replacing. The text label stays but
        // is re-parented last so the new shape layers render behind it.
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        Width = 96;
        Height = 32;
        WidthUnits = DimensionUnitType.Absolute;
        HeightUnits = DimensionUnitType.Absolute;

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        // Re-attach the base TextInstance on top of the new shape stack.
        AddChild(TextInstance);
        TextInstance.ApplyState(Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle.Text.Normal);

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "DarkProFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.CornerRadius = CornerRadius;
        fill.IsFilled = true;
        fill.FillColor = DarkProStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "DarkProBorder";
        border.X = 0;
        border.Y = 0;
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        border.CornerRadius = CornerRadius;
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = DarkProStyling.ActiveStyle.Colors.Border;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        // Sized to (parent + 2px) per axis and centered, so the 1px stroke
        // sits exactly one pixel outside the border. Matches the
        // `box-shadow: 0 0 0 1px var(--acc)` effect from the mockup CSS.
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "DarkProFocusRing";
        ring.X = 0;
        ring.Y = 0;
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = FocusRingInset * 2f;
        ring.Height = FocusRingInset * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = CornerRadius + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = BorderThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = DarkProStyling.ActiveStyle.Colors.Accent;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        // Replace (don't append to) the base callbacks. The base's
        // SetValuesForState targets the now-detached NineSlice background
        // and the underline FocusedIndicator, neither of which exists in
        // this visual's render tree any more.
        States.Enabled.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface1,
            border: DarkProStyling.ActiveStyle.Colors.Border,
            text: DarkProStyling.ActiveStyle.Colors.Text,
            showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface2,
            border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.Text,
            showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface1,
            border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.Text,
            showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface2,
            border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.Text,
            showFocusRing: true);

        States.Pushed.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.PressedFill,
            border: DarkProStyling.ActiveStyle.Colors.Accent,
            text: DarkProStyling.ActiveStyle.Colors.Text,
            showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.DisabledFill,
            border: DarkProStyling.ActiveStyle.Colors.DisabledBorder,
            text: DarkProStyling.ActiveStyle.Colors.DisabledText,
            showFocusRing: false);

        States.DisabledFocused.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.DisabledFill,
            border: DarkProStyling.ActiveStyle.Colors.DisabledBorder,
            text: DarkProStyling.ActiveStyle.Colors.DisabledText,
            showFocusRing: true);
    }

    private void ApplyPalette(Color fill, Color border, Color text, bool showFocusRing)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _focusRing.Visible = showFocusRing;
    }
}
