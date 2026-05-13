using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseCheckBoxVisual = Gum.Forms.DefaultVisuals.V3.CheckBoxVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled CheckBox. 18 px leaf-shaped box, sun-pale border at
/// rest, leaf-bright accent border when interacted with. Checked state fills
/// the box with leaf-bright and draws a dark canopy check glyph for contrast
/// against the bright fill (matching CSS <c>.fg-chk.chk .fg-ck stroke
/// #053f1f</c>). Indeterminate state replaces the glyph with a sun-pale dash.
/// </summary>
public class CheckBoxVisual : BaseCheckBoxVisual
{
    private const float BoxSize = 18f;
    private const float BorderThickness = 1.5f;
    private const float FocusRingInset = 3f;
    private const float FocusRingThickness = 3f;
    private const float BoxToLabelGap = 8f;
    private const float DashWidth = 9f;
    private const float DashHeight = 2f;
    private const float GlyphContainerSize = 22f;

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
        // Theme pre-registers ✓ via BmfcSave so KernSmith bakes it. Rendered
        // in the DejaVu-backed icon family because Nunito doesn't cover the
        // Dingbats block.
        _checkGlyph.Font = ForestGladeTheme.IconFontFamily;
        _checkGlyph.FontSize = 16;
        _checkGlyph.Text = "✓";
        _checkGlyph.Color = new Color(5, 63, 31); // CSS .fg-ck stroke #053f1f
        _checkGlyph.Visible = false;

        _dashIndicator = CreateDashIndicator();
        AddChild(_dashIndicator);
        _dashIndicator.Visible = false;

        WireStates();
    }

    private static RoundedRectangleRuntime CreateBoxFill()
    {
        RoundedRectangleRuntime fill = new RoundedRectangleRuntime();
        fill.Name = "ForestGladeBoxFill";
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
        ForestGladeLeaf.ApplySmall(fill);
        fill.IsFilled = true;
        // CSS .fg-cbox uses a 160deg linear gradient from rgba(232,255,117,.06) to
        // rgba(0,0,0,.35) — translucent over the canopy bg. We approximate with
        // an opaque pre-blend toward dark (canopy-deep slightly darker).
        fill.Color = new Color(3, 28, 32);
        return fill;
    }

    private static RoundedRectangleRuntime CreateBoxBorder()
    {
        RoundedRectangleRuntime border = new RoundedRectangleRuntime();
        border.Name = "ForestGladeBoxBorder";
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
        ForestGladeLeaf.ApplySmall(border);
        border.IsFilled = false;
        border.StrokeWidth = BorderThickness;
        border.StrokeWidthUnits = DimensionUnitType.Absolute;
        // CSS .fg-cbox border: rgba(232,255,117,.30). Higher alpha than .Border so
        // the box is clearly outlined against the page background.
        border.Color = new Color(232, 255, 117, 76);
        return border;
    }

    private static RoundedRectangleRuntime CreateFocusRing()
    {
        RoundedRectangleRuntime ring = new RoundedRectangleRuntime();
        ring.Name = "ForestGladeBoxFocusRing";
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
        // Padded leaf corners — bump radii by inset so the ring tracks the body's
        // shape on each corner.
        ring.CornerRadius = 2f + FocusRingInset;
        ring.CustomRadiusTopLeft = 2f + FocusRingInset;
        ring.CustomRadiusTopRight = 8f + FocusRingInset;
        ring.CustomRadiusBottomRight = 2f + FocusRingInset;
        ring.CustomRadiusBottomLeft = 8f + FocusRingInset;
        ring.IsFilled = false;
        ring.StrokeWidth = FocusRingThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.Color = ForestGladeColors.AccentHalo;
        ring.Visible = false;
        return ring;
    }

    private static TextRuntime CreateCheckGlyph()
    {
        const float overhang = (GlyphContainerSize - BoxSize) / 2f;
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "ForestGladeCheckGlyph";
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
        RoundedRectangleRuntime dash = new RoundedRectangleRuntime();
        dash.Name = "ForestGladeDashIndicator";
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
        dash.Color = ForestGladeColors.SunPale;
        return dash;
    }

    private void WireStates()
    {
        Color restBorder = new Color(232, 255, 117, 76);   // CSS .fg-cbox .30
        Color hoverBorder = new Color(232, 255, 117, 140); // CSS hover .55
        Color focusBorder = ForestGladeColors.LeafBright;
        Color restFill = new Color(3, 28, 32);
        Color checkedFill = ForestGladeColors.LeafBright;
        Color pushedCheckedFill = new Color(0, 126, 41);   // CSS pushed gradient mid
        Color disabledBorder = new Color(232, 255, 117, 26);
        Color disabledFill = new Color(8, 16, 18);

        // -------- Unchecked --------
        States.EnabledOff.Apply = () => Apply(
            fill: restFill, border: restBorder,
            text: ForestGladeColors.Text, glyph: GlyphKind.None, ring: false);

        States.HighlightedOff.Apply = () => Apply(
            fill: restFill, border: hoverBorder,
            text: ForestGladeColors.Text, glyph: GlyphKind.None, ring: false);

        States.FocusedOff.Apply = () => Apply(
            fill: restFill, border: focusBorder,
            text: ForestGladeColors.Text, glyph: GlyphKind.None, ring: true);

        States.HighlightedFocusedOff.Apply = () => Apply(
            fill: restFill, border: focusBorder,
            text: ForestGladeColors.Text, glyph: GlyphKind.None, ring: true);

        States.PushedOff.Apply = () => Apply(
            fill: restFill, border: focusBorder,
            text: ForestGladeColors.Text, glyph: GlyphKind.None, ring: false);

        States.DisabledOff.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, glyph: GlyphKind.None, ring: false);

        States.DisabledFocusedOff.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, glyph: GlyphKind.None, ring: true);

        // -------- Checked --------
        Color darkGlyph = new Color(5, 63, 31);
        Color brightGlyph = new Color(214, 245, 176);

        States.EnabledOn.Apply = () => Apply(
            fill: checkedFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Check, glyphColor: darkGlyph,
            ring: false);

        States.HighlightedOn.Apply = () => Apply(
            fill: checkedFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Check, glyphColor: darkGlyph,
            ring: false);

        States.FocusedOn.Apply = () => Apply(
            fill: checkedFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Check, glyphColor: darkGlyph,
            ring: true);

        States.HighlightedFocusedOn.Apply = () => Apply(
            fill: checkedFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Check, glyphColor: darkGlyph,
            ring: true);

        States.PushedOn.Apply = () => Apply(
            fill: pushedCheckedFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Check, glyphColor: brightGlyph,
            ring: false);

        States.DisabledOn.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, glyph: GlyphKind.Check,
            glyphColor: ForestGladeColors.Disabled, ring: false);

        States.DisabledFocusedOn.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, glyph: GlyphKind.Check,
            glyphColor: ForestGladeColors.Disabled, ring: true);

        // -------- Indeterminate --------
        States.EnabledIndeterminate.Apply = () => Apply(
            fill: restFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Dash, ring: false);

        States.HighlightedIndeterminate.Apply = () => Apply(
            fill: restFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Dash, ring: false);

        States.FocusedIndeterminate.Apply = () => Apply(
            fill: restFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Dash, ring: true);

        States.HighlightedFocusedIndeterminate.Apply = () => Apply(
            fill: restFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Dash, ring: true);

        States.PushedIndeterminate.Apply = () => Apply(
            fill: restFill, border: ForestGladeColors.SunPale,
            text: ForestGladeColors.Text, glyph: GlyphKind.Dash, ring: false);

        States.DisabledIndeterminate.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, glyph: GlyphKind.Dash, ring: false);

        States.DisabledFocusedIndeterminate.Apply = () => Apply(
            fill: disabledFill, border: disabledBorder,
            text: ForestGladeColors.Disabled, glyph: GlyphKind.Dash, ring: true);
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
