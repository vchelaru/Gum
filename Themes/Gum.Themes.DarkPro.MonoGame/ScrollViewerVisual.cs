using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseScrollViewerVisual = Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled ScrollViewer visual. Same shell pattern as
/// <see cref="ListBoxVisual"/> — 1 px-bordered rounded rectangle (CornerRadius=2)
/// with an outer Accent focus ring — and the internal scroll/clip container
/// inset 1 px on every side so the Dark Pro ScrollBars don't sit flush against
/// the border. The Forms-side <c>new ScrollBar()</c> calls in the V3 base ctor
/// resolve through the Dark Pro template, so the scrollbar visuals themselves
/// are already correct; this subclass only fixes the shell and the inset.
/// </summary>
public class ScrollViewerVisual : BaseScrollViewerVisual
{
    private const float CornerRadius = 2f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;

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

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        AddChild(ScrollAndClipContainer);

        // No per-consumer scroll bar positioning needed — Dark Pro's
        // ScrollBarVisual bakes its own thumb insets, so the bar's bounding
        // box can sit flush against the border (the bar's track is transparent;
        // only the thumb is visible, and the thumb has built-in breathing
        // room on every side).

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "DarkProScrollViewerFill";
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
        fill.Color = DarkProColors.Surface1;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "DarkProScrollViewerBorder";
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
        border.Color = DarkProColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "DarkProScrollViewerFocusRing";
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
        ring.Color = DarkProColors.Accent;
        ring.Visible = false;
        return ring;
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
            _border.Color = DarkProColors.Border;
            _focusRing.Visible = false;
        };

        States.Focused.Apply = () =>
        {
            _border.Color = DarkProColors.Accent;
            _focusRing.Visible = true;
        };
    }
}
