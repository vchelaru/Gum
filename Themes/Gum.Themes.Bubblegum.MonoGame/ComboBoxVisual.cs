using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseComboBoxVisual = Gum.Forms.DefaultVisuals.V3.ComboBoxVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled ComboBox visual. Closed shell mirrors the TextBox / ListBox /
/// ScrollViewer pattern (Surface1 fill + 2 px pink border at CornerRadius=8 +
/// outer translucent Accent focus ring). The V3 sprite-sheet dropdown arrow is
/// replaced with a <c>▼</c> glyph rendered through Nunito.
/// <para>
/// The dropdown popup is left alone — V3.ComboBoxVisual creates it via
/// <c>new ListBox()</c>, which resolves through the Bubblegum Forms template, so
/// the dropdown picks up the themed <see cref="ListBoxVisual"/> automatically.
/// </para>
/// </summary>
public class ComboBoxVisual : BaseComboBoxVisual
{
    private const float CornerRadius = 8f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;
    private const float GlyphRightMargin = 12f;
    private const float GlyphContainerSize = 16f;
    private const int GlyphFontSize = 11;
    private const float TextLeftPadding = 12f;
    private const float TextRightClearance = 4f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;
    private readonly TextRuntime _dropdownGlyph;

    public ComboBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        DropdownIndicator.Parent = null;
        TextInstance.Parent = null;

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _fill = CreateFill();
        AddChild(_fill);

        _border = CreateBorder();
        AddChild(_border);

        _dropdownGlyph = CreateDropdownGlyph();
        AddChild(_dropdownGlyph);

        AddChild(TextInstance);
        TextInstance.X = TextLeftPadding;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -(TextLeftPadding + GlyphRightMargin + GlyphContainerSize + TextRightClearance);

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "BubblegumComboFill";
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
        fill.FillColor = BubblegumColors.Surface1;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "BubblegumComboBorder";
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
        border.StrokeColor = BubblegumColors.Border;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "BubblegumComboFocusRing";
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
        ring.StrokeColor = BubblegumPalette.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private static TextRuntime CreateDropdownGlyph()
    {
        // ▼ is rendered through the bundled DejaVu Sans Mono icon font under the
        // "Nunito Icons" family — Nunito itself doesn't cover Geometric Shapes.
        // BubblegumTheme.Apply pre-registers the glyph via BmfcSave.AddCharacters.
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "BubblegumDropdownGlyph";
        glyph.X = -GlyphRightMargin;
        glyph.Y = 0;
        glyph.XUnits = GeneralUnitType.PixelsFromLarge;
        glyph.YUnits = GeneralUnitType.PixelsFromMiddle;
        glyph.XOrigin = HorizontalAlignment.Right;
        glyph.YOrigin = VerticalAlignment.Center;
        glyph.Width = GlyphContainerSize;
        glyph.Height = GlyphContainerSize;
        glyph.WidthUnits = DimensionUnitType.Absolute;
        glyph.HeightUnits = DimensionUnitType.Absolute;
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        glyph.VerticalAlignment = VerticalAlignment.Center;
        glyph.Font = BubblegumTheme.IconFontFamily;
        glyph.FontSize = GlyphFontSize;
        glyph.Text = "▼";
        glyph.Color = BubblegumColors.Accent;
        return glyph;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(
            border: BubblegumColors.Border, text: BubblegumColors.Text,
            glyph: BubblegumColors.Accent, ring: false, fillDisabled: false);

        States.Highlighted.Apply = () => Apply(
            border: BubblegumColors.Accent, text: BubblegumColors.Text,
            glyph: BubblegumColors.Accent, ring: false, fillDisabled: false);

        States.Focused.Apply = () => Apply(
            border: BubblegumColors.Accent, text: BubblegumColors.Text,
            glyph: BubblegumColors.Accent, ring: true, fillDisabled: false);

        States.HighlightedFocused.Apply = () => Apply(
            border: BubblegumColors.Accent, text: BubblegumColors.Text,
            glyph: BubblegumColors.Accent, ring: true, fillDisabled: false);

        States.Pushed.Apply = () => Apply(
            border: BubblegumColors.AccentDark, text: BubblegumColors.Text,
            glyph: BubblegumColors.AccentDark, ring: false, fillDisabled: false);

        States.Disabled.Apply = () => Apply(
            border: BubblegumColors.Disabled, text: BubblegumColors.Disabled,
            glyph: BubblegumColors.Disabled, ring: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => Apply(
            border: BubblegumColors.Disabled, text: BubblegumColors.Disabled,
            glyph: BubblegumColors.Disabled, ring: true, fillDisabled: true);
    }

    private void Apply(Color border, Color text, Color glyph, bool ring, bool fillDisabled)
    {
        _fill.FillColor = fillDisabled ? BubblegumColors.DisabledFill : BubblegumColors.Surface1;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _dropdownGlyph.Color = glyph;
        _focusRing.Visible = ring;
    }
}
