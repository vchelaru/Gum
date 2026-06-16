using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseComboBoxVisual = Gum.Forms.DefaultVisuals.V3.ComboBoxVisual;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled ComboBox visual. The closed shell mirrors the TextBox input
/// (peach-light fill, 13 px radius, transparent→peach→blue border with a blue
/// focus ring). The V3 sprite-sheet arrow is replaced with a coral <c>▼</c>
/// glyph, and the field text uses the Quicksand body face.
/// <para>
/// The dropdown popup is left to V3.ComboBoxVisual, which builds it via
/// <c>new ListBox()</c> — that resolves through the Meadow Forms template, so the
/// open list picks up the themed <see cref="ListBoxVisual"/> automatically.
/// </para>
/// </summary>
public class ComboBoxVisual : BaseComboBoxVisual
{
    private const float CornerRadius = 13f;
    private const float BorderThickness = 2.5f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;
    private const float GlyphRightMargin = 12f;
    private const float GlyphContainerSize = 16f;
    private const int GlyphFontSize = 11;
    private const float TextLeftPadding = 14f;
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
        TextInstance.Font = MeadowTheme.BodyFontFamily;
        TextInstance.X = TextLeftPadding;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -(TextLeftPadding + GlyphRightMargin + GlyphContainerSize + TextRightClearance);

        WireStates();
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowComboFill";
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
        fill.FillColor = MeadowColors.PeachLight;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "MeadowComboBorder";
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
        border.StrokeColor = new Color(0, 0, 0, 0);
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "MeadowComboFocusRing";
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
        ring.StrokeColor = MeadowPalette.BlueFocusRing;
        ring.Visible = false;
        return ring;
    }

    private static TextRuntime CreateDropdownGlyph()
    {
        // ▼ renders through the bundled DejaVu Sans Mono icon font under the
        // "Meadow Icons" family. MeadowTheme.Apply pre-registers the glyph.
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "MeadowDropdownGlyph";
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
        glyph.Font = MeadowTheme.IconFontFamily;
        glyph.FontSize = GlyphFontSize;
        glyph.Text = "▼";
        glyph.Color = MeadowColors.CoralDark;
        return glyph;
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(
            fill: MeadowColors.PeachLight, border: new Color(0, 0, 0, 0),
            text: MeadowColors.TealDark, glyph: MeadowColors.CoralDark, ring: false);

        States.Highlighted.Apply = () => Apply(
            fill: MeadowColors.PeachLight, border: MeadowColors.PeachDark,
            text: MeadowColors.TealDark, glyph: MeadowColors.CoralDark, ring: false);

        States.Focused.Apply = () => Apply(
            fill: MeadowColors.White, border: MeadowColors.Blue,
            text: MeadowColors.TealDark, glyph: MeadowColors.CoralDark, ring: true);

        States.HighlightedFocused.Apply = () => Apply(
            fill: MeadowColors.White, border: MeadowColors.Blue,
            text: MeadowColors.TealDark, glyph: MeadowColors.CoralDark, ring: true);

        States.Pushed.Apply = () => Apply(
            fill: MeadowColors.PeachLight, border: MeadowColors.PeachDark,
            text: MeadowColors.TealDark, glyph: MeadowColors.Coral, ring: false);

        States.Disabled.Apply = () => Apply(
            fill: MeadowColors.Cream2, border: MeadowColors.Disabled,
            text: MeadowColors.DisabledInk, glyph: MeadowColors.DisabledInk, ring: false);

        States.DisabledFocused.Apply = () => Apply(
            fill: MeadowColors.Cream2, border: MeadowColors.Disabled,
            text: MeadowColors.DisabledInk, glyph: MeadowColors.DisabledInk, ring: true);
    }

    private void Apply(Color fill, Color border, Color text, Color glyph, bool ring)
    {
        _fill.FillColor = fill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _dropdownGlyph.Color = glyph;
        _focusRing.Visible = ring;
    }
}
