using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
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

        _focusRing = BubblegumShapes.FocusRing(
            color: BubblegumStyling.ActiveStyle.Colors.FocusRing,
            cornerRadius: CornerRadius,
            inset: FocusRingInset,
            thickness: FocusRingThickness,
            name: "BubblegumComboFocusRing");
        AddChild(_focusRing);

        _fill = BubblegumShapes.Fill(
            color: BubblegumStyling.ActiveStyle.Colors.Surface1,
            cornerRadius: CornerRadius,
            name: "BubblegumComboFill");
        AddChild(_fill);

        _border = BubblegumShapes.Border(
            color: BubblegumStyling.ActiveStyle.Colors.Border,
            cornerRadius: CornerRadius,
            thickness: BorderThickness,
            name: "BubblegumComboBorder");
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
        glyph.Font = BubblegumStyling.ActiveStyle.Text.IconFontFamily;
        glyph.FontSize = GlyphFontSize;
        glyph.Text = "▼";
        glyph.Color = BubblegumStyling.ActiveStyle.Colors.Accent;
        return glyph;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(
            border: BubblegumStyling.ActiveStyle.Colors.Border, text: BubblegumStyling.ActiveStyle.Colors.Text,
            glyph: BubblegumStyling.ActiveStyle.Colors.Accent, ring: false, fillDisabled: false);

        States.Highlighted.Apply = () => Apply(
            border: BubblegumStyling.ActiveStyle.Colors.Accent, text: BubblegumStyling.ActiveStyle.Colors.Text,
            glyph: BubblegumStyling.ActiveStyle.Colors.Accent, ring: false, fillDisabled: false);

        States.Focused.Apply = () => Apply(
            border: BubblegumStyling.ActiveStyle.Colors.Accent, text: BubblegumStyling.ActiveStyle.Colors.Text,
            glyph: BubblegumStyling.ActiveStyle.Colors.Accent, ring: true, fillDisabled: false);

        States.HighlightedFocused.Apply = () => Apply(
            border: BubblegumStyling.ActiveStyle.Colors.Accent, text: BubblegumStyling.ActiveStyle.Colors.Text,
            glyph: BubblegumStyling.ActiveStyle.Colors.Accent, ring: true, fillDisabled: false);

        States.Pushed.Apply = () => Apply(
            border: BubblegumStyling.ActiveStyle.Colors.AccentDark, text: BubblegumStyling.ActiveStyle.Colors.Text,
            glyph: BubblegumStyling.ActiveStyle.Colors.AccentDark, ring: false, fillDisabled: false);

        States.Disabled.Apply = () => Apply(
            border: BubblegumStyling.ActiveStyle.Colors.Disabled, text: BubblegumStyling.ActiveStyle.Colors.Disabled,
            glyph: BubblegumStyling.ActiveStyle.Colors.Disabled, ring: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => Apply(
            border: BubblegumStyling.ActiveStyle.Colors.Disabled, text: BubblegumStyling.ActiveStyle.Colors.Disabled,
            glyph: BubblegumStyling.ActiveStyle.Colors.Disabled, ring: true, fillDisabled: true);
    }

    private void Apply(Color border, Color text, Color glyph, bool ring, bool fillDisabled)
    {
        _fill.FillColor = fillDisabled ? BubblegumStyling.ActiveStyle.Colors.DisabledFill : BubblegumStyling.ActiveStyle.Colors.Surface1;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _dropdownGlyph.Color = glyph;
        _focusRing.Visible = ring;
    }
}
