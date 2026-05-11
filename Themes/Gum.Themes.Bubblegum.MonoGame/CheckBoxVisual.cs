using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseCheckBoxVisual = Gum.Forms.DefaultVisuals.V3.CheckBoxVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled CheckBox visual. Replaces the V3 NineSlice box and underline
/// focus indicator with an Apos.Shapes rounded-rect stack — 20 px box, 6 px
/// corner radius, 2 px pink border, white fill at rest, accent fill when checked.
/// The focus ring is a translucent 3 px Accent stroke sitting ~2 px outside the
/// box. The check glyph is rendered as a Text "✓" in Nunito; if the bundled
/// Nunito subset doesn't cover U+2713 the glyph will fail to render — accepted
/// caveat for v1, file an issue to swap in a primitive-built check.
/// </summary>
public class CheckBoxVisual : BaseCheckBoxVisual
{
    private const float BoxSize = 20f;
    private const float CornerRadius = 6f;
    private const float BorderThickness = 2f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;
    private const float BoxToLabelGap = 8f;
    private const float DashWidth = 10f;
    private const float DashHeight = 2f;
    // Glyph runtime is sized a hair larger than the box so the Nunito ✓ glyph
    // (which uses a slightly wider advance than ASCII Latin) doesn't get clipped.
    private const float GlyphContainerSize = 24f;

    private readonly RoundedRectangleRuntime _focusRing;
    private readonly RoundedRectangleRuntime _boxFill;
    private readonly RoundedRectangleRuntime _boxBorder;
    private readonly TextRuntime _checkGlyph;
    private readonly RoundedRectangleRuntime _dashIndicator;

    public CheckBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        CheckBoxBackground.Parent = null;
        FocusedIndicator.Parent = null;

        TextInstance.Width = -(BoxSize + BoxToLabelGap);

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _boxFill = CreateBoxFill();
        AddChild(_boxFill);

        _boxBorder = CreateBoxBorder();
        AddChild(_boxBorder);

        _checkGlyph = CreateCheckGlyph();
        AddChild(_checkGlyph);
        // Nunito (the bundled theme font) is used directly for the check glyph.
        // Bubblegum's CSS source draws checks as inline SVG, so there's no icon
        // font bundled. If Nunito's subset lacks U+2713 the glyph will fail to
        // render — visual gap is non-blocking; replace with primitives later.
        _checkGlyph.Font = BubblegumTheme.FontFamily;
        _checkGlyph.FontSize = 18;
        _checkGlyph.Text = "✓";
        _checkGlyph.Color = Color.White;
        _checkGlyph.Visible = false;

        _dashIndicator = CreateDashIndicator();
        AddChild(_dashIndicator);
        _dashIndicator.Visible = false;

        WireStates();
    }

    private static RoundedRectangleRuntime CreateBoxFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "BubblegumBoxFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromSmall;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Left;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = BoxSize;
        fill.Height = BoxSize;
        fill.WidthUnits = DimensionUnitType.Absolute;
        fill.HeightUnits = DimensionUnitType.Absolute;
        fill.CornerRadius = CornerRadius;
        fill.IsFilled = true;
        fill.Color = BubblegumColors.Surface1;
        return fill;
    }

    private static RoundedRectangleRuntime CreateBoxBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "BubblegumBoxBorder";
        border.X = 0;
        border.Y = 0;
        border.XUnits = GeneralUnitType.PixelsFromSmall;
        border.YUnits = GeneralUnitType.PixelsFromMiddle;
        border.XOrigin = HorizontalAlignment.Left;
        border.YOrigin = VerticalAlignment.Center;
        border.Width = BoxSize;
        border.Height = BoxSize;
        border.WidthUnits = DimensionUnitType.Absolute;
        border.HeightUnits = DimensionUnitType.Absolute;
        border.CornerRadius = CornerRadius;
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        border.Color = BubblegumColors.Border;
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "BubblegumBoxFocusRing";
        ring.X = -FocusRingInset;
        ring.Y = 0;
        ring.XUnits = GeneralUnitType.PixelsFromSmall;
        ring.YUnits = GeneralUnitType.PixelsFromMiddle;
        ring.XOrigin = HorizontalAlignment.Left;
        ring.YOrigin = VerticalAlignment.Center;
        ring.Width = BoxSize + (FocusRingInset * 2f);
        ring.Height = BoxSize + (FocusRingInset * 2f);
        ring.WidthUnits = DimensionUnitType.Absolute;
        ring.HeightUnits = DimensionUnitType.Absolute;
        ring.CornerRadius = CornerRadius + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = BubblegumPalette.FocusRing;
        ring.Visible = false;
        return ring;
    }

    private static TextRuntime CreateCheckGlyph()
    {
        const float overhang = (GlyphContainerSize - BoxSize) / 2f;
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "BubblegumCheckGlyph";
        glyph.X = -overhang;
        glyph.Y = 0;
        glyph.XUnits = GeneralUnitType.PixelsFromSmall;
        glyph.YUnits = GeneralUnitType.PixelsFromMiddle;
        glyph.XOrigin = HorizontalAlignment.Left;
        glyph.YOrigin = VerticalAlignment.Center;
        glyph.Width = GlyphContainerSize;
        glyph.Height = GlyphContainerSize;
        glyph.WidthUnits = DimensionUnitType.Absolute;
        glyph.HeightUnits = DimensionUnitType.Absolute;
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        glyph.VerticalAlignment = VerticalAlignment.Center;
        return glyph;
    }

    private static RoundedRectangleRuntime CreateDashIndicator()
    {
        // Indeterminate pill: 10x2 (matches .bb-ck-dash), accent-colored, centered.
        RoundedRectangleRuntime dash = new RoundedRectangleRuntime();
        dash.Name = "BubblegumDashIndicator";
        dash.X = BoxSize / 2f;
        dash.Y = 0;
        dash.XUnits = GeneralUnitType.PixelsFromSmall;
        dash.YUnits = GeneralUnitType.PixelsFromMiddle;
        dash.XOrigin = HorizontalAlignment.Center;
        dash.YOrigin = VerticalAlignment.Center;
        dash.Width = DashWidth;
        dash.Height = DashHeight;
        dash.WidthUnits = DimensionUnitType.Absolute;
        dash.HeightUnits = DimensionUnitType.Absolute;
        dash.CornerRadius = 1f;
        dash.IsFilled = true;
        dash.Color = BubblegumColors.Accent;
        return dash;
    }

    private void WireStates()
    {
        // -------- Unchecked (Off) --------
        States.EnabledOff.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Border,
            text: BubblegumColors.Text, glyph: GlyphKind.None, ring: false);

        States.HighlightedOff.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.None, ring: false);

        States.FocusedOff.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.None, ring: true);

        States.HighlightedFocusedOff.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.None, ring: true);

        States.PushedOff.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.None, ring: false);

        States.DisabledOff.Apply = () => Apply(
            fill: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled,
            text: BubblegumColors.Disabled, glyph: GlyphKind.None, ring: false);

        States.DisabledFocusedOff.Apply = () => Apply(
            fill: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled,
            text: BubblegumColors.Disabled, glyph: GlyphKind.None, ring: true);

        // -------- Checked (On) --------
        // Accent-filled box, white check glyph (matches .bb-chk.chk).
        States.EnabledOn.Apply = () => Apply(
            fill: BubblegumColors.Accent, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.Check, glyphColor: Color.White,
            ring: false);

        States.HighlightedOn.Apply = () => Apply(
            fill: BubblegumColors.Accent, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.Check, glyphColor: Color.White,
            ring: false);

        States.FocusedOn.Apply = () => Apply(
            fill: BubblegumColors.Accent, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.Check, glyphColor: Color.White,
            ring: true);

        States.HighlightedFocusedOn.Apply = () => Apply(
            fill: BubblegumColors.Accent, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.Check, glyphColor: Color.White,
            ring: true);

        States.PushedOn.Apply = () => Apply(
            fill: BubblegumColors.AccentDark, border: BubblegumColors.AccentDark,
            text: BubblegumColors.Text, glyph: GlyphKind.Check, glyphColor: Color.White,
            ring: false);

        States.DisabledOn.Apply = () => Apply(
            fill: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled,
            text: BubblegumColors.Disabled, glyph: GlyphKind.Check,
            glyphColor: BubblegumColors.Disabled, ring: false);

        States.DisabledFocusedOn.Apply = () => Apply(
            fill: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled,
            text: BubblegumColors.Disabled, glyph: GlyphKind.Check,
            glyphColor: BubblegumColors.Disabled, ring: true);

        // -------- Indeterminate --------
        // .bb-chk.ind keeps the white fill and just shows the dash, with accent border.
        States.EnabledIndeterminate.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.Dash, ring: false);

        States.HighlightedIndeterminate.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.Dash, ring: false);

        States.FocusedIndeterminate.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.Dash, ring: true);

        States.HighlightedFocusedIndeterminate.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.Accent,
            text: BubblegumColors.Text, glyph: GlyphKind.Dash, ring: true);

        States.PushedIndeterminate.Apply = () => Apply(
            fill: BubblegumColors.Surface1, border: BubblegumColors.AccentDark,
            text: BubblegumColors.Text, glyph: GlyphKind.Dash, ring: false);

        States.DisabledIndeterminate.Apply = () => Apply(
            fill: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled,
            text: BubblegumColors.Disabled, glyph: GlyphKind.Dash, ring: false);

        States.DisabledFocusedIndeterminate.Apply = () => Apply(
            fill: BubblegumColors.DisabledFill, border: BubblegumColors.Disabled,
            text: BubblegumColors.Disabled, glyph: GlyphKind.Dash, ring: true);
    }

    private enum GlyphKind { None, Check, Dash }

    private void Apply(Color fill, Color border, Color text, GlyphKind glyph, bool ring,
        Color? glyphColor = null)
    {
        _boxFill.Color = fill;
        _boxBorder.Color = border;
        TextInstance.Color = text;
        _focusRing.Visible = ring;
        _checkGlyph.Visible = glyph == GlyphKind.Check;
        _dashIndicator.Visible = glyph == GlyphKind.Dash;
        if (glyphColor.HasValue)
        {
            _checkGlyph.Color = glyphColor.Value;
        }
    }
}
