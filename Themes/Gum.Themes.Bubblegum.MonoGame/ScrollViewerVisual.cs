using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseScrollViewerVisual = Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled ScrollViewer visual. Same shell pattern as
/// <see cref="ListBoxVisual"/> — Surface1 fill + 2 px pink border at
/// CornerRadius=8 with an outer translucent Accent focus ring.
/// </summary>
public class ScrollViewerVisual : BaseScrollViewerVisual
{
    private const float CornerRadius = 8f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ScrollViewerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ScrollAndClipContainer.Parent = null;

        _focusRing = BubblegumShapes.FocusRing(
            color: BubblegumStyling.ActiveStyle.Colors.FocusRing,
            cornerRadius: CornerRadius,
            inset: FocusRingInset,
            thickness: FocusRingThickness,
            name: "BubblegumScrollViewerFocusRing");
        AddChild(_focusRing);

        _fill = BubblegumShapes.Fill(
            color: BubblegumStyling.ActiveStyle.Colors.Surface1,
            cornerRadius: CornerRadius,
            name: "BubblegumScrollViewerFill");
        AddChild(_fill);

        // ScrollAndClipContainer goes between fill and border so the rounded
        // border paints on top of clipped content — Gum's clip container is
        // rectangular, so content extending into the corners would otherwise
        // poke past the rounded outline.
        AddChild(ScrollAndClipContainer);

        _border = BubblegumShapes.Border(
            color: BubblegumStyling.ActiveStyle.Colors.Border,
            cornerRadius: CornerRadius,
            thickness: BorderThickness,
            name: "BubblegumScrollViewerBorder");
        AddChild(_border);

        VerticalScrollBarInstance.X = -2f;

        WireStates();
    }

    private void WireStates()
    {
        States.Enabled.Apply = () =>
        {
            _border.StrokeColor = BubblegumStyling.ActiveStyle.Colors.Border;
            _focusRing.Visible = false;
        };

        States.Focused.Apply = () =>
        {
            _border.StrokeColor = BubblegumStyling.ActiveStyle.Colors.Accent;
            _focusRing.Visible = true;
        };
    }
}
