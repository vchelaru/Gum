using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled ListBoxItem visual. Soft rounded rows (matches the 11 px pills
/// in <c>.pp-lb-item</c>) — transparent at rest, peach-light tint on hover, a
/// sage band with a sage-dark inset outline when selected, and teal text on the
/// selected band. Row text uses the Quicksand body face.
/// </summary>
public class ListBoxItemVisual : BaseListBoxItemVisual
{
    private const float CornerRadius = 8f;

    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _selectedInset;

    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        _selectedInset = CreateSelectedInset();
        AddChild(_selectedInset);

        AddChild(TextInstance);
        TextInstance.Font = MeadowTheme.BodyFontFamily;
        TextInstance.X = 12f;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -24f;

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowListItemFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = -6f;
        fill.Height = -3f;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.CornerRadius = CornerRadius;
        fill.IsFilled = true;
        fill.FillColor = Color.Transparent;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateSelectedInset()
    {
        // The 2 px sage-dark inset outline on a selected row (CSS
        // box-shadow: inset 0 0 0 2px var(--saged)). Tracks the fill's inset size.
        RectangleRuntime inset = new RectangleRuntime();
        inset.Name = "MeadowListItemSelectedInset";
        inset.X = 0;
        inset.Y = 0;
        inset.XUnits = GeneralUnitType.PixelsFromMiddle;
        inset.YUnits = GeneralUnitType.PixelsFromMiddle;
        inset.XOrigin = HorizontalAlignment.Center;
        inset.YOrigin = VerticalAlignment.Center;
        inset.Width = -6f;
        inset.Height = -3f;
        inset.WidthUnits = DimensionUnitType.RelativeToParent;
        inset.HeightUnits = DimensionUnitType.RelativeToParent;
        inset.CornerRadius = CornerRadius;
        inset.IsFilled = false;
        inset.StrokeWidth = 2f;
        inset.StrokeWidthUnits = DimensionUnitType.Absolute;
        inset.StrokeColor = MeadowColors.SageDark;
        inset.Visible = false;
        return inset;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: MeadowColors.TealDark, selected: false);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: MeadowPalette.HoverRow, text: MeadowColors.TealDark, selected: false);

        States.Selected.Apply = () => ApplyPalette(
            fill: MeadowPalette.SelectedRow, text: MeadowPalette.SelectedRowText, selected: true);

        States.Focused.Apply = () => ApplyPalette(
            fill: MeadowPalette.SelectedRow, text: MeadowPalette.SelectedRowText, selected: true);

        States.Disabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: MeadowColors.DisabledInk, selected: false);
    }

    private void ApplyPalette(Color fill, Color text, bool selected)
    {
        _fill.FillColor = fill;
        _selectedInset.Visible = selected;
        TextInstance.Color = text;
    }
}
