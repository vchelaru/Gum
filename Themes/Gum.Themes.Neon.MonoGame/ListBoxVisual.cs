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

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled ListBox visual. Surface1 fill + 2 px pink border at
/// CornerRadius=8, with an outer translucent Accent focus ring.
/// </summary>
public class ListBoxVisual : BaseListBoxVisual
{
    private const float CornerRadius = 1f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 4f;
    private const float FocusRingThickness = 1f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;

    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        ClipAndScrollContainer.Parent = null;

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        // ClipAndScrollContainer goes between fill and border. Gum's clip
        // container is rectangular — item hover/selection fills extend to its
        // square corners and would visibly poke past the rounded outline.
        // Painting the border last masks those corner regions.
        AddChild(ClipAndScrollContainer);

        _border = CreateBorder();
        AddChild(_border);

        if (VerticalScrollBarInstance != null)
        {
            VerticalScrollBarInstance.X = -2f;
        }

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "NeonListBoxFill";
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
        fill.FillColor = NeonStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "NeonListBoxBorder";
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
        border.StrokeColor = NeonStyling.ActiveStyle.Colors.Border;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "NeonListBoxFocusRing";
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
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = NeonStyling.ActiveStyle.Colors.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyPalette(
            border: NeonStyling.ActiveStyle.Colors.Border, showFocusRing: false);

        States.Highlighted.Apply = () => ApplyPalette(
            border: NeonStyling.ActiveStyle.Colors.Accent, showFocusRing: false);

        States.Focused.Apply = () => ApplyPalette(
            border: NeonStyling.ActiveStyle.Colors.Accent, showFocusRing: true);

        States.HighlightedFocused.Apply = () => ApplyPalette(
            border: NeonStyling.ActiveStyle.Colors.Accent, showFocusRing: true);

        States.Pushed.Apply = () => ApplyPalette(
            border: NeonStyling.ActiveStyle.Colors.Accent, showFocusRing: false);

        States.Disabled.Apply = () => ApplyPalette(
            border: NeonStyling.ActiveStyle.Colors.Disabled, showFocusRing: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => ApplyPalette(
            border: NeonStyling.ActiveStyle.Colors.Disabled, showFocusRing: true, fillDisabled: true);
    }

    private void ApplyPalette(Color border, bool showFocusRing, bool fillDisabled = false)
    {
        _fill.FillColor = fillDisabled ? NeonStyling.ActiveStyle.Colors.Disabled : NeonStyling.ActiveStyle.Colors.Surface1;
        _border.StrokeColor = border;
        _focusRing.Visible = showFocusRing;
    }
}
