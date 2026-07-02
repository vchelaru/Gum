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
using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled ListBox visual. Replaces the V3 ListBox's NineSlice background and
/// underline focus indicator with the standard Dark Pro shell: a 1px-bordered rounded
/// rectangle (CornerRadius=2) plus an outer focus ring that lights up while the list
/// has focus.
/// </summary>
public class ListBoxVisual : BaseListBoxVisual
{
    private const float CornerRadius = 2f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the base NineSlice background and underline focus indicator.
        // ClipAndScrollContainer stays in place — it hosts the scrollbar and the
        // inner panel and is added after Background in the base ctor, so the
        // new shape stack inserts cleanly behind it.
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ClipAndScrollContainer.Parent = null;

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        // Reattach the scroll/clip container last so list items render above
        // the new background shapes. The scroll bar's own thumb insets (baked
        // into Dark Pro's ScrollBarVisual) handle most of the visual breathing
        // room from the border; the bar itself only needs a 1 px nudge off
        // the consumer's outer edge (the side facing the border).
        AddChild(ClipAndScrollContainer);

        if (VerticalScrollBarInstance != null)
        {
            VerticalScrollBarInstance.X = -1f;
        }

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "DarkProListBoxFill";
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
        border.Name = "DarkProListBoxBorder";
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
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "DarkProListBoxFocusRing";
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
        // ListBox has no Pushed-while-empty interaction — pushed/highlighted only
        // become meaningful via the items themselves — so the shell stays calm
        // and only reacts to focus. Hover uses BorderHover (gray) the same way
        // TextBox does, since the natural progression is hover → focus and the
        // gray→blue transition reads as a state shift.
        States.Enabled.Apply = () => ApplyPalette(
            border: DarkProStyling.ActiveStyle.Colors.Border, showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            border: DarkProStyling.ActiveStyle.Colors.BorderHover, showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            border: DarkProStyling.ActiveStyle.Colors.Accent, showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            border: DarkProStyling.ActiveStyle.Colors.Accent, showFocusRing: true);

        States.Pushed.Apply = () => ApplyPalette(
            border: DarkProStyling.ActiveStyle.Colors.Accent, showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            border: DarkProStyling.ActiveStyle.Colors.DisabledBorder, showFocusRing: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => ApplyPalette(
            border: DarkProStyling.ActiveStyle.Colors.DisabledBorder, showFocusRing: true, fillDisabled: true);
    }

    private void ApplyPalette(Color border, bool showFocusRing, bool fillDisabled = false)
    {
        _fill.FillColor = fillDisabled ? DarkProStyling.ActiveStyle.Colors.DisabledFill : DarkProStyling.ActiveStyle.Colors.Surface1;
        _border.StrokeColor = border;
        _focusRing.Visible = showFocusRing;
    }
}
