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

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade ComboBox (closed). Leaf-medium shell mirrors the TextBox
/// chrome — glassy fill, sun-pale border, accent halo on focus. The
/// V3 sprite-sheet dropdown arrow is replaced with a sun-pale ▾ glyph
/// rendered through the icon font. The open dropdown leverages
/// <see cref="ListBoxVisual"/> via DefaultFormsTemplates and inherits the
/// theme's list styling.
/// </summary>
public class ComboBoxVisual : BaseComboBoxVisual
{
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 3f;
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
        fill.Name = "ForestGladeComboFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyMedium(fill);
        fill.IsFilled = true;
        fill.StrokeWidth = 0;
        // Same vertical dark gradient as the TextBox decoration so the closed
        // ComboBox reads as a sibling input chrome.
        fill.UseGradient = true;
        fill.GradientType = GradientType.Linear;
        fill.GradientX1Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientY1Units = GeneralUnitType.PixelsFromSmall;
        fill.GradientX1 = 0f;
        fill.GradientY1 = 0f;
        fill.GradientX2Units = GeneralUnitType.PixelsFromMiddle;
        fill.GradientY2Units = GeneralUnitType.PixelsFromLarge;
        fill.GradientX2 = 0f;
        fill.GradientY2 = 0f;
        fill.FillColor = new Color(2, 22, 25);
        fill.Color2 = new Color(4, 36, 40);
        return fill;
    }

    private static RectangleRuntime CreateBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "ForestGladeComboBorder";
        border.XUnits = GeneralUnitType.PixelsFromMiddle;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Center;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = 0;
        border.Height = 0;
        border.WidthUnits = DimensionUnitType.RelativeToParent;
        border.HeightUnits = DimensionUnitType.RelativeToParent;
        ForestGladeLeaf.ApplyMedium(border);
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.StrokeColor = new Color(232, 255, 117, 56);
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        const float halo = FocusRingInset;
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "ForestGladeComboFocusRing";
        ring.XUnits = GeneralUnitType.PixelsFromMiddle;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Center;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = halo * 2f;
        ring.Height = halo * 2f;
        ring.WidthUnits = DimensionUnitType.RelativeToParent;
        ring.HeightUnits = DimensionUnitType.RelativeToParent;
        ring.CornerRadius = 2f + halo;
        ring.CustomRadiusTopLeft = 2f + halo;
        ring.CustomRadiusTopRight = 12f + halo;
        ring.CustomRadiusBottomRight = 2f + halo;
        ring.CustomRadiusBottomLeft = 12f + halo;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = ForestGladeStyling.ActiveStyle.Colors.AccentHalo;
        ring.Visible = false;
        return ring;
    }

    private static TextRuntime CreateDropdownGlyph()
    {
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "ForestGladeComboGlyph";
        glyph.X = -GlyphRightMargin;
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
        glyph.Font = ForestGladeStyling.ActiveStyle.Text.IconFontFamily;
        glyph.FontSize = GlyphFontSize;
        glyph.Text = "▼";
        glyph.Color = ForestGladeStyling.ActiveStyle.Colors.SunPale;
        return glyph;
    }

    private void WireStates()
    {
        Color restBorder = new Color(232, 255, 117, 56);
        Color hoverBorder = new Color(232, 255, 117, 115);
        Color focusBorder = ForestGladeStyling.ActiveStyle.Colors.LeafBright;
        Color disabledBorder = new Color(232, 255, 117, 20);

        States.Enabled.Apply = () => Apply(
            border: restBorder, text: ForestGladeStyling.ActiveStyle.Colors.Text,
            glyph: ForestGladeStyling.ActiveStyle.Colors.SunPale, ring: false, fillDisabled: false);

        States.Highlighted.Apply = () => Apply(
            border: hoverBorder, text: ForestGladeStyling.ActiveStyle.Colors.Text,
            glyph: ForestGladeStyling.ActiveStyle.Colors.SunPale, ring: false, fillDisabled: false);

        States.Focused.Apply = () => Apply(
            border: focusBorder, text: ForestGladeStyling.ActiveStyle.Colors.Text,
            glyph: ForestGladeStyling.ActiveStyle.Colors.SunPale, ring: true, fillDisabled: false);

        States.HighlightedFocused.Apply = () => Apply(
            border: focusBorder, text: ForestGladeStyling.ActiveStyle.Colors.Text,
            glyph: ForestGladeStyling.ActiveStyle.Colors.SunPale, ring: true, fillDisabled: false);

        States.Pushed.Apply = () => Apply(
            border: focusBorder, text: ForestGladeStyling.ActiveStyle.Colors.Text,
            glyph: ForestGladeStyling.ActiveStyle.Colors.SunPale, ring: false, fillDisabled: false);

        // Disabled text uses Muted (not the near-black Disabled token) so the
        // currently-selected value stays legible.
        States.Disabled.Apply = () => Apply(
            border: disabledBorder, text: ForestGladeStyling.ActiveStyle.Colors.Muted,
            glyph: ForestGladeStyling.ActiveStyle.Colors.Muted, ring: false, fillDisabled: true);

        States.DisabledFocused.Apply = () => Apply(
            border: disabledBorder, text: ForestGladeStyling.ActiveStyle.Colors.Muted,
            glyph: ForestGladeStyling.ActiveStyle.Colors.Muted, ring: true, fillDisabled: true);
    }

    private void Apply(Color border, Color text, Color glyph, bool ring, bool fillDisabled)
    {
        Color baseFill = fillDisabled ? ForestGladeStyling.ActiveStyle.Colors.InputFillDisabled : ForestGladeStyling.ActiveStyle.Colors.InputFill;
        _fill.FillColor = Darken(baseFill, 0.65f);
        _fill.Color2 = baseFill;
        _border.StrokeColor = border;
        TextInstance.Color = text;
        _dropdownGlyph.Color = glyph;
        _focusRing.Visible = ring;
    }

    private static Color Darken(Color c, float factor)
    {
#if SKIA
        return new Color((byte)(c.Red * factor), (byte)(c.Green * factor), (byte)(c.Blue * factor), c.Alpha);
#else
        return new Color((byte)(c.R * factor), (byte)(c.G * factor), (byte)(c.B * factor), c.A);
#endif
    }
}
