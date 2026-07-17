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

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled ComboBox visual. Closed shell mirrors the TextBox / ListBox /
/// ScrollViewer pattern (Surface1 fill + 2 px pink border at CornerRadius=8 +
/// outer translucent Accent focus ring). The V3 sprite-sheet dropdown arrow is
/// replaced with a <c>▼</c> glyph rendered through Nunito.
/// <para>
/// The dropdown popup is left alone — V3.ComboBoxVisual creates it via
/// <c>new ListBox()</c>, which resolves through the Neon Forms template, so
/// the dropdown picks up the themed <see cref="ListBoxVisual"/> automatically.
/// </para>
/// </summary>
public class ComboBoxVisual : BaseComboBoxVisual
{
    private const float CornerRadius = 1f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 4f;
    private const float FocusRingThickness = 1f;
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
        fill.Name = "NeonComboFill";
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
        border.Name = "NeonComboBorder";
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
        ring.Name = "NeonComboFocusRing";
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

    private static TextRuntime CreateDropdownGlyph()
    {
        // ▼ is rendered through the bundled DejaVu Sans Mono icon font under the
        // "Nunito Icons" family — Nunito itself doesn't cover Geometric Shapes.
        // NeonTheme.Apply pre-registers the glyph via BmfcSave.AddCharacters.
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "NeonDropdownGlyph";
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
        glyph.Font = NeonStyling.ActiveStyle.Text.IconFontFamily;
        glyph.FontSize = GlyphFontSize;
        glyph.Text = "▼";
        glyph.Color = NeonStyling.ActiveStyle.Colors.Accent;
        return glyph;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(
            border: NeonStyling.ActiveStyle.Colors.Border, text: NeonStyling.ActiveStyle.Colors.Text,
            glyph: NeonStyling.ActiveStyle.Colors.Accent, ring: false, fillDisabled: false);

        States.Highlighted.Apply = () => Apply(
            border: NeonStyling.ActiveStyle.Colors.Accent, text: NeonStyling.ActiveStyle.Colors.Text,
            glyph: NeonStyling.ActiveStyle.Colors.Accent, ring: false, fillDisabled: false);

        States.Focused.Apply = () => Apply(
            border: NeonStyling.ActiveStyle.Colors.Accent, text: NeonStyling.ActiveStyle.Colors.Text,
            glyph: NeonStyling.ActiveStyle.Colors.Accent, ring: true, fillDisabled: false);

        States.HighlightedFocused.Apply = () => Apply(
            border: NeonStyling.ActiveStyle.Colors.Accent, text: NeonStyling.ActiveStyle.Colors.Text,
            glyph: NeonStyling.ActiveStyle.Colors.Accent, ring: true, fillDisabled: false);

        States.Pushed.Apply = () => Apply(
            border: NeonStyling.ActiveStyle.Colors.Accent, text: NeonStyling.ActiveStyle.Colors.Text,
            glyph: NeonStyling.ActiveStyle.Colors.Accent, ring: false, fillDisabled: false);

        // Disabled text uses Muted (not the near-black Disabled token) so the
        // currently-selected value stays legible — otherwise a disabled combo
        // looks empty, which reads as a bug.
        States.Disabled.Apply = () => Apply(
            border: NeonStyling.ActiveStyle.Colors.DisabledBorder, text: NeonStyling.ActiveStyle.Colors.Muted,
            glyph: NeonStyling.ActiveStyle.Colors.Muted, ring: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => Apply(
            border: NeonStyling.ActiveStyle.Colors.DisabledBorder, text: NeonStyling.ActiveStyle.Colors.Muted,
            glyph: NeonStyling.ActiveStyle.Colors.Muted, ring: true, fillDisabled: true);
    }

    private void Apply(Color border, Color text, Color glyph, bool ring, bool fillDisabled)
    {
        _fill.FillColor = fillDisabled ? NeonStyling.ActiveStyle.Colors.Background : NeonStyling.ActiveStyle.Colors.Surface1;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _dropdownGlyph.Color = glyph;
        _focusRing.Visible = ring;
    }
}
