using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled ListBox visual. Replaces the V3 ListBox's NineSlice background and
/// underline focus indicator with the standard Hazard shell (built via
/// <see cref="HazardShapes"/>): a 1px-bordered rounded rectangle (CornerRadius=2)
/// plus an outer focus ring that lights up while the list has focus.
/// </summary>
public class ListBoxVisual : BaseListBoxVisual
{
    private const float CornerRadius = 0f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the base NineSlice background and underline focus indicator.
        // ClipAndScrollContainer is detached so the new shape stack inserts
        // cleanly behind it, then reattached last (it hosts the scrollbar and
        // the inner panel).
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ClipAndScrollContainer.Parent = null;

        _focusRing = HazardShapes.FocusRing(HazardPalette.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = HazardShapes.Fill(HazardPalette.Surface1, CornerRadius);
        AddChild(_fill);

        _border = HazardShapes.Border(HazardPalette.Border, CornerRadius, BorderThickness);
        AddChild(_border);

        // Reattach the scroll/clip container last so list items render above the
        // new background shapes.
        AddChild(ClipAndScrollContainer);

        // The scroll bar's own thumb insets (baked into Hazard's ScrollBarVisual)
        // handle most of the visual breathing room from the border; the bar itself
        // only needs a 1 px nudge off the consumer's outer edge (the side facing the
        // border).
        if (VerticalScrollBarInstance != null)
        {
            VerticalScrollBarInstance.X = -1f;
        }

        WireStates();
    }

    private void WireStates()
    {
        // ListBox has no Pushed-while-empty interaction — pushed/highlighted only
        // become meaningful via the items themselves — so the shell stays calm
        // and only reacts to focus. Hover uses BorderHover (gray) the same way
        // TextBox does, since the natural progression is hover → focus and the
        // gray→blue transition reads as a state shift.
        States.Enabled.Apply = () => ApplyPalette(
            border: HazardPalette.Border, showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            border: HazardPalette.BorderHover, showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            border: HazardPalette.Accent, showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            border: HazardPalette.Accent, showFocusRing: true);

        States.Pushed.Apply = () => ApplyPalette(
            border: HazardPalette.Accent, showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            border: HazardPalette.DisabledBorder, showFocusRing: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => ApplyPalette(
            border: HazardPalette.DisabledBorder, showFocusRing: true, fillDisabled: true);
    }

    private void ApplyPalette(Color border, bool showFocusRing, bool fillDisabled = false)
    {
        _fill.FillColor = fillDisabled ? HazardPalette.DisabledFill : HazardPalette.Surface1;
        _border.StrokeColor = border;
        _focusRing.Visible = showFocusRing;
    }
}
