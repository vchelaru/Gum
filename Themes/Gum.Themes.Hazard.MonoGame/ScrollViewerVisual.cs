using Gum.DataTypes;
using Gum.GueDeriving;
using BaseScrollViewerVisual = Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled ScrollViewer visual. Same shell pattern as
/// <see cref="ListBoxVisual"/> — 1 px-bordered rounded rectangle (CornerRadius=2,
/// built via <see cref="HazardShapes"/>) with an outer Accent focus ring — and
/// the internal scroll/clip container inset 1 px on every side so the Hazard
/// ScrollBars don't sit flush against the border. The Forms-side
/// <c>new ScrollBar()</c> calls in the V3 base ctor resolve through the Hazard
/// template, so the scrollbar visuals themselves are already correct; this subclass
/// only fixes the shell and the inset.
/// </summary>
public class ScrollViewerVisual : BaseScrollViewerVisual
{
    private const float CornerRadius = 0f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ScrollViewerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the base NineSlice background and underline focus indicator.
        // ScrollAndClipContainer stays (it owns the scrollbars and the clip
        // container) and is reattached last so its content renders above the
        // new shape stack.
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ScrollAndClipContainer.Parent = null;

        _focusRing = HazardShapes.FocusRing(HazardStyling.ActiveStyle.Colors.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Surface1, CornerRadius);
        AddChild(_fill);

        _border = HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness);
        AddChild(_border);

        AddChild(ScrollAndClipContainer);

        // Hazard's ScrollBarVisual bakes its own thumb insets, so the bar
        // only needs a 1 px nudge off the consumer's outer edge (the side
        // facing the border) on top of that.
        VerticalScrollBarInstance.X = -1f;

        WireStates();
    }

    private void WireStates()
    {
        // The V3 ScrollViewer state set is just Enabled / Focused — visibility
        // states (VerticalScrollVisible / HorizontalScrollVisible / etc.) are
        // a separate category that adjusts margins, not visual chrome. Replace
        // the V3 callbacks (which target the now-detached NineSlice) with the
        // shape-stack equivalents. The V3 base populated each state's Variables
        // with "FocusedIndicator.Visible = true/false" — those assignments
        // still fire against the detached FocusedIndicator each time the state
        // applies, but they're harmless because FocusedIndicator has no parent.
        States.Enabled.Apply = () =>
        {
            _border.StrokeColor = HazardStyling.ActiveStyle.Colors.Border;
            _focusRing.Visible = false;
        };

        States.Focused.Apply = () =>
        {
            _border.StrokeColor = HazardStyling.ActiveStyle.Colors.Accent;
            _focusRing.Visible = true;
        };
    }
}
