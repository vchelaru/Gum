using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;

namespace Gum.Themes.Template.Variants;

/// <summary>
/// "Rich" variant of the Template ListBox. Compared to the flat
/// <see cref="Gum.Themes.Template.ListBoxVisual"/> this changes ONLY the shapes:
/// the border is rendered DASHED (via Apos.Shapes' native
/// <see cref="Gum.GueDeriving.RectangleRuntime.StrokeDashLength"/> /
/// <see cref="Gum.GueDeriving.RectangleRuntime.StrokeGapLength"/>) and the
/// CornerRadius is bumped to 12. The border-on-top rounded-clip ordering, the
/// palette tokens, and the states are identical to the flat source.
/// <para>
/// Part of the opt-in Variants gallery - NOT registered by default. See
/// <see cref="TemplateTheme.RegisterVisuals"/>.
/// </para>
/// </summary>
public class ListBoxVisual : BaseListBoxVisual
{
    // Rounder shell (flat source used 2).
    private const float CornerRadius = 12f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;
    // Dash pattern (Apos.Shapes renders these natively on the stroke).
    private const float DashLength = 5f;
    private const float GapLength = 4f;

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

        _focusRing = TemplateShapes.FocusRing(TemplatePalette.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = TemplateShapes.Fill(TemplatePalette.Surface1, CornerRadius);
        AddChild(_fill);

        _border = TemplateShapes.Border(TemplatePalette.Border, CornerRadius, BorderThickness);
        // Dashed stroke - rendered natively by Apos.Shapes (one node, no faked rects).
        _border.StrokeDashLength = DashLength;
        _border.StrokeGapLength = GapLength;
        AddChild(_border);

        // Reattach the scroll/clip container last so list items render above the
        // new background shapes.
        AddChild(ClipAndScrollContainer);

        // The scroll bar's own thumb insets (baked into Template's ScrollBarVisual)
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
            border: TemplatePalette.Border, showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            border: TemplatePalette.BorderHover, showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            border: TemplatePalette.Accent, showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            border: TemplatePalette.Accent, showFocusRing: true);

        States.Pushed.Apply = () => ApplyPalette(
            border: TemplatePalette.Accent, showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            border: TemplatePalette.DisabledBorder, showFocusRing: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => ApplyPalette(
            border: TemplatePalette.DisabledBorder, showFocusRing: true, fillDisabled: true);
    }

    private void ApplyPalette(Color border, bool showFocusRing, bool fillDisabled = false)
    {
        _fill.FillColor = fillDisabled ? TemplatePalette.DisabledFill : TemplatePalette.Surface1;
        _border.StrokeColor = border;
        _focusRing.Visible = showFocusRing;
    }
}
