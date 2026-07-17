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

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled ComboBox visual. Closed control shell mirrors the TextBox /
/// ListBox / ScrollViewer pattern (Surface1 fill + 1 px border at CornerRadius=2
/// + outer Accent focus ring, all from <see cref="HazardShapes"/>). The V3
/// sprite-sheet dropdown arrow is replaced with a <c>▼</c> glyph rendered through
/// the bundled icon font.
/// <para>
/// The dropdown popup is left alone — V3.ComboBoxVisual creates it via
/// <c>new ListBox()</c>, which resolves through the Hazard Forms template, so
/// the dropdown already gets the themed ListBoxVisual without extra work here.
/// </para>
/// </summary>
public class ComboBoxVisual : BaseComboBoxVisual
{
    private const float CornerRadius = 0f;
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

        _focusRing = HazardShapes.FocusRing(HazardStyling.ActiveStyle.Colors.Accent, CornerRadius, FocusRingInset, BorderThickness);
        AddChild(_focusRing);

        _fill = HazardShapes.Fill(HazardStyling.ActiveStyle.Colors.Surface1, CornerRadius);
        AddChild(_fill);

        _border = HazardShapes.Border(HazardStyling.ActiveStyle.Colors.Border, CornerRadius, BorderThickness);
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

    private static TextRuntime CreateDropdownGlyph()
    {
        // Oversized container relative to font size — DejaVu Sans Mono's Geometric
        // Shapes block (▼ U+25BC) is monospaced for ASCII but the symbol glyph's
        // advance width is wider than the Latin cell. A snug container clips it.
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "HazardDropdownGlyph";
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
        glyph.Font = HazardStyling.ActiveStyle.Text.IconFontFamily;
        glyph.FontSize = GlyphFontSize;
        glyph.Text = "▼";
        glyph.Color = HazardStyling.ActiveStyle.Colors.Muted;
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
            border: HazardStyling.ActiveStyle.Colors.Border, text: HazardStyling.ActiveStyle.Colors.Text,
            glyph: HazardStyling.ActiveStyle.Colors.Muted, ring: false, fillDisabled: false);

        States.Highlighted.Apply = () => Apply(
            border: HazardStyling.ActiveStyle.Colors.BorderHover, text: HazardStyling.ActiveStyle.Colors.Text,
            glyph: HazardStyling.ActiveStyle.Colors.Text, ring: false, fillDisabled: false);

        States.Focused.Apply = () => Apply(
            border: HazardStyling.ActiveStyle.Colors.Accent, text: HazardStyling.ActiveStyle.Colors.Text,
            glyph: HazardStyling.ActiveStyle.Colors.Text, ring: true, fillDisabled: false);

        States.HighlightedFocused.Apply = () => Apply(
            border: HazardStyling.ActiveStyle.Colors.Accent, text: HazardStyling.ActiveStyle.Colors.Text,
            glyph: HazardStyling.ActiveStyle.Colors.Text, ring: true, fillDisabled: false);

        States.Pushed.Apply = () => Apply(
            border: HazardStyling.ActiveStyle.Colors.Accent, text: HazardStyling.ActiveStyle.Colors.Text,
            glyph: HazardStyling.ActiveStyle.Colors.Text, ring: false, fillDisabled: false);

        States.Disabled.Apply = () => Apply(
            border: HazardStyling.ActiveStyle.Colors.DisabledBorder, text: HazardStyling.ActiveStyle.Colors.DisabledText,
            glyph: HazardStyling.ActiveStyle.Colors.DisabledText, ring: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => Apply(
            border: HazardStyling.ActiveStyle.Colors.DisabledBorder, text: HazardStyling.ActiveStyle.Colors.DisabledText,
            glyph: HazardStyling.ActiveStyle.Colors.DisabledText, ring: true, fillDisabled: true);
    }

    private void Apply(Color border, Color text, Color glyph, bool ring, bool fillDisabled)
    {
        _fill.FillColor = fillDisabled ? HazardStyling.ActiveStyle.Colors.DisabledFill : HazardStyling.ActiveStyle.Colors.Surface1;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _dropdownGlyph.Color = glyph;
        _focusRing.Visible = ring;
    }
}
