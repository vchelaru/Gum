using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled ListBox visual. Surface1 fill + 2 px pink border at
/// CornerRadius=8, with an outer translucent Accent focus ring.
/// </summary>
public class ListBoxVisual : BaseListBoxVisual
{
    private const float CornerRadius = 8f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ClipAndScrollContainer.Parent = null;

        _focusRing = BubblegumShapes.FocusRing(
            color: BubblegumPalette.FocusRing,
            cornerRadius: CornerRadius,
            inset: FocusRingInset,
            thickness: FocusRingThickness,
            name: "BubblegumListBoxFocusRing");
        AddChild(_focusRing);

        _fill = BubblegumShapes.Fill(
            color: BubblegumColors.Surface1,
            cornerRadius: CornerRadius,
            name: "BubblegumListBoxFill");
        AddChild(_fill);

        // ClipAndScrollContainer goes between fill and border. Gum's clip
        // container is rectangular — item hover/selection fills extend to its
        // square corners and would visibly poke past the rounded outline.
        // Painting the border last masks those corner regions.
        AddChild(ClipAndScrollContainer);

        _border = BubblegumShapes.Border(
            color: BubblegumColors.Border,
            cornerRadius: CornerRadius,
            thickness: BorderThickness,
            name: "BubblegumListBoxBorder");
        AddChild(_border);

        if (VerticalScrollBarInstance != null)
        {
            VerticalScrollBarInstance.X = -2f;
        }

        WireStates();
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyPalette(
            border: BubblegumColors.Border, showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            border: BubblegumColors.Accent, showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            border: BubblegumColors.Accent, showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            border: BubblegumColors.Accent, showFocusRing: true);

        States.Pushed.Apply = () => ApplyPalette(
            border: BubblegumColors.AccentDark, showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            border: BubblegumColors.Disabled, showFocusRing: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => ApplyPalette(
            border: BubblegumColors.Disabled, showFocusRing: true, fillDisabled: true);
    }

    private void ApplyPalette(Color border, bool showFocusRing, bool fillDisabled = false)
    {
        _fill.FillColor = fillDisabled ? BubblegumColors.DisabledFill : BubblegumColors.Surface1;
        _border.StrokeColor = border;
        _focusRing.Visible = showFocusRing;
    }
}
