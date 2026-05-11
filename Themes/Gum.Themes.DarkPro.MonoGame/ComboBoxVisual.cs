using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
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

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _fill;
    private readonly RoundedRectangleRuntime _border;
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
        // V3 sets TextInstance.Width=-8 (4 px gutter each side). Tighten to leave
        // clear room for the new dropdown glyph at the right edge so long item
        // names don't visually crash into it.
        TextInstance.Width = -(GlyphRightMargin + GlyphContainerSize + 8f);

        WireStates();
    }

    private static RoundedRectangleRuntime CreateFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
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
        fill.Color = DarkProColors.Surface1;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
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
        border.Color = DarkProColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
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
        ring.Color = DarkProColors.Accent;
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
        glyph.Font = DarkProTheme.IconFontFamily;
        glyph.FontSize = GlyphFontSize;
        glyph.Text = "▼";
        glyph.Color = DarkProColors.Muted;
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
            border: DarkProColors.Border, text: DarkProColors.Text,
            glyph: DarkProColors.Muted, ring: false, fillDisabled: false);

        States.Highlighted.Apply = () => Apply(
            border: DarkProColors.BorderHover, text: DarkProColors.Text,
            glyph: DarkProColors.Text, ring: false, fillDisabled: false);

        States.Focused.Apply = () => Apply(
            border: DarkProColors.Accent, text: DarkProColors.Text,
            glyph: DarkProColors.Text, ring: true, fillDisabled: false);

        States.HighlightedFocused.Apply = () => Apply(
            border: DarkProColors.Accent, text: DarkProColors.Text,
            glyph: DarkProColors.Text, ring: true, fillDisabled: false);

        States.Pushed.Apply = () => Apply(
            border: DarkProColors.Accent, text: DarkProColors.Text,
            glyph: DarkProColors.Text, ring: false, fillDisabled: false);

        States.Disabled.Apply = () => Apply(
            border: DarkProColors.DisabledBorder, text: DarkProColors.DisabledText,
            glyph: DarkProColors.DisabledText, ring: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => Apply(
            border: DarkProColors.DisabledBorder, text: DarkProColors.DisabledText,
            glyph: DarkProColors.DisabledText, ring: true, fillDisabled: true);
    }

    private void Apply(Color border, Color text, Color glyph, bool ring, bool fillDisabled)
    {
        _fill.Color = fillDisabled ? DarkProColors.DisabledFill : DarkProColors.Surface1;
        _border.Color = border;
        TextInstance.Color = text;
        _dropdownGlyph.Color = glyph;
        _focusRing.Visible = ring;
    }
}
