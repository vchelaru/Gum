using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseComboBoxVisual = Gum.Forms.DefaultVisuals.V3.ComboBoxVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled ComboBox visual. Closed control shell mirrors the TextBox /
/// ListBox / ScrollViewer pattern (Surface1 fill + 1 px border at CornerRadius=2
/// + outer Accent focus ring). The V3 sprite-sheet dropdown arrow is replaced
/// with a <c>▼</c> glyph rendered through the bundled icon font.
/// <para>
/// The dropdown popup is left alone — V3.ComboBoxVisual creates it via
/// <c>new ListBox()</c>, which resolves through the Dark Pro Forms template, so
/// the dropdown already gets the themed ListBoxVisual without extra work here.
/// </para>
/// </summary>
public class ComboBoxVisual : BaseComboBoxVisual
{
    private const float CornerRadius = 2f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;
    private const float GlyphRightMargin = 10f;
    private const float GlyphContainerSize = 16f;
    private const int GlyphFontSize = 10;
    // Match the ListBoxItem TextInstance.X so the closed control's selected-item
    // text lines up vertically with the dropdown items below it.
    private const float TextLeftPadding = 6f;
    // Clearance between the right edge of the text frame and the left edge of
    // the dropdown glyph so long item names don't visually crash into the chevron.
    private const float TextRightClearance = 4f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _fill;
    private readonly RectangleRuntime _border;
    private readonly TextRuntime _dropdownGlyph;

    public ComboBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the V3 NineSlice shell, the underline focus indicator, and
        // the sprite-sheet dropdown arrow. The selected-item TextInstance is
        // reparented last so it renders above the new shape stack.
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
        // Match the dropdown ListBoxItem layout: 6 px from the left border,
        // left-aligned, with the frame's right edge clearing the dropdown glyph.
        // V3's default centered layout left ~17 px on the left, which made the
        // closed-control text visibly out-of-line with the dropdown items.
        TextInstance.X = TextLeftPadding;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -(TextLeftPadding + GlyphRightMargin + GlyphContainerSize + TextRightClearance);

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "DarkProComboFill";
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
        fill.FillColor = DarkProStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "DarkProComboBorder";
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
        border.StrokeColor = DarkProStyling.ActiveStyle.Colors.Border;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "DarkProComboFocusRing";
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
        ring.StrokeColor = DarkProStyling.ActiveStyle.Colors.Accent;
        ring.Visible = false;
        return ring;
    }

    private static TextRuntime CreateDropdownGlyph()
    {
        // Oversized container relative to font size — DejaVu Sans Mono's Geometric
        // Shapes block (▼ U+25BC) is monospaced for ASCII but the symbol glyph's
        // advance width is wider than the Latin cell. A snug container clips it.
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "DarkProDropdownGlyph";
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
        glyph.Font = DarkProStyling.ActiveStyle.Text.IconFontFamily;
        glyph.FontSize = GlyphFontSize;
        glyph.Text = "▼";
        glyph.Color = DarkProStyling.ActiveStyle.Colors.Muted;
        return glyph;
    }

    private void WireStates()
    {
        // Shell border tracks interaction state, mirroring the CheckBox/RadioButton
        // and TextBox conventions: gray at rest, gray-lighter on hover, accent on
        // focus / press. The dropdown glyph dims to Muted at rest and brightens to
        // full Text on hover/focus/press so the user gets visible "this is alive"
        // feedback distinct from the border.
        States.Enabled.Apply = () => Apply(
            border: DarkProStyling.ActiveStyle.Colors.Border, text: DarkProStyling.ActiveStyle.Colors.Text,
            glyph: DarkProStyling.ActiveStyle.Colors.Muted, ring: false, fillDisabled: false);

        States.Highlighted.Apply = () => Apply(
            border: DarkProStyling.ActiveStyle.Colors.BorderHover, text: DarkProStyling.ActiveStyle.Colors.Text,
            glyph: DarkProStyling.ActiveStyle.Colors.Text, ring: false, fillDisabled: false);

        States.Focused.Apply = () => Apply(
            border: DarkProStyling.ActiveStyle.Colors.Accent, text: DarkProStyling.ActiveStyle.Colors.Text,
            glyph: DarkProStyling.ActiveStyle.Colors.Text, ring: true, fillDisabled: false);

        States.HighlightedFocused.Apply = () => Apply(
            border: DarkProStyling.ActiveStyle.Colors.Accent, text: DarkProStyling.ActiveStyle.Colors.Text,
            glyph: DarkProStyling.ActiveStyle.Colors.Text, ring: true, fillDisabled: false);

        States.Pushed.Apply = () => Apply(
            border: DarkProStyling.ActiveStyle.Colors.Accent, text: DarkProStyling.ActiveStyle.Colors.Text,
            glyph: DarkProStyling.ActiveStyle.Colors.Text, ring: false, fillDisabled: false);

        States.Disabled.Apply = () => Apply(
            border: DarkProStyling.ActiveStyle.Colors.DisabledBorder, text: DarkProStyling.ActiveStyle.Colors.DisabledText,
            glyph: DarkProStyling.ActiveStyle.Colors.DisabledText, ring: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => Apply(
            border: DarkProStyling.ActiveStyle.Colors.DisabledBorder, text: DarkProStyling.ActiveStyle.Colors.DisabledText,
            glyph: DarkProStyling.ActiveStyle.Colors.DisabledText, ring: true, fillDisabled: true);
    }

    private void Apply(Color border, Color text, Color glyph, bool ring, bool fillDisabled)
    {
        _fill.FillColor = fillDisabled ? DarkProStyling.ActiveStyle.Colors.DisabledFill : DarkProStyling.ActiveStyle.Colors.Surface1;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _dropdownGlyph.Color = glyph;
        _focusRing.Visible = ring;
    }
}
