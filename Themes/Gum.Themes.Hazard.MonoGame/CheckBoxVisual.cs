using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseCheckBoxVisual = Gum.Forms.DefaultVisuals.V3.CheckBoxVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled CheckBox visual. Replaces the V3 CheckBoxVisual's NineSlice
/// box and underline focus indicator with an Apos.Shapes rounded-rect stack
/// (focus ring, fill, 1px border). The check is rendered as a TextRuntime
/// glyph and the indeterminate marker as a small Apos rounded rect.
/// <para>
/// The box, glyph, and dash are fixed-size sub-boxes anchored to the left of a
/// full-width control, so they're built inline rather than via
/// <see cref="HazardShapes"/> (which centers and sizes shapes to the parent).
/// </para>
/// </summary>
public class CheckBoxVisual : BaseCheckBoxVisual
{
    private const float BoxSize = 16f;
    private const float CornerRadius = 0f;
    private const float BorderThickness = 1f;
    private const float FocusRingInset = 1f;
    private const float BoxToLabelGap = 8f;
    private const float DashWidth = 8f;
    private const float DashHeight = 2f;
    // Glyph runtime is sized larger than the box so icon-font glyphs whose advance
    // widths exceed the box don't get dropped (DejaVu Sans Mono is monospaced for
    // Latin but its Dingbats / Geometric Shapes glyphs are wider than the cell).
    private const float GlyphContainerSize = 24f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _boxFill;
    private readonly RectangleRuntime _boxBorder;
    private readonly TextRuntime _checkGlyph;
    private readonly RectangleRuntime _dashIndicator;

    public CheckBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Drop the base NineSlice box (the InnerCheck sprite was parented to it
        // and goes with it) and the underline focus indicator.
        CheckBoxBackground.Parent = null;
        FocusedIndicator.Parent = null;

        // Match the CSS spec: 16px box + 8px gap = 24px from left to text origin.
        // Base sets Width=-28 assuming a 24px box + 4px gap; adjust to our spec.
        TextInstance.Width = -(BoxSize + BoxToLabelGap);

        _focusRing = CreateFocusRing();
        AddChild(_focusRing);

        _boxFill = CreateBoxFill();
        AddChild(_boxFill);

        _boxBorder = CreateBoxBorder();
        AddChild(_boxBorder);

        _checkGlyph = CreateCheckGlyph();
        AddChild(_checkGlyph);
        // Use the bundled icon font (DejaVu Sans Mono) - Saira Condensed doesn't
        // cover the Dingbats block where U+2713 ✓ lives. The character is pre-baked
        // into the icon font's atlas via BmfcSave.AddCharacters in
        // HazardTheme.Apply.
        _checkGlyph.Font = HazardStyling.ActiveStyle.Text.IconFontFamily;
        _checkGlyph.FontSize = 22;
        _checkGlyph.Text = "✓";
        _checkGlyph.Color = HazardStyling.ActiveStyle.Colors.Accent;
        _checkGlyph.Visible = false;

        _dashIndicator = CreateDashIndicator();
        AddChild(_dashIndicator);
        _dashIndicator.Visible = false;

        WireStates();
    }

    private static RectangleRuntime CreateBoxFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "HazardBoxFill";
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
        fill.FillColor = HazardStyling.ActiveStyle.Colors.Surface1;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBoxBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "HazardBoxBorder";
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
        border.StrokeColor = HazardStyling.ActiveStyle.Colors.Border;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        // (BoxSize + 2px) per axis, centered on the box, so the 1px stroke
        // sits exactly one pixel outside the box border.
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "HazardBoxFocusRing";
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
        ring.StrokeWidth = BorderThickness;
        ring.StrokeWidthUnits = DimensionUnitType.Absolute;
        ring.StrokeColor = HazardStyling.ActiveStyle.Colors.Accent;
        ring.Visible = false;
        return ring;
    }

    private static TextRuntime CreateCheckGlyph()
    {
        // Oversized container for the glyph (GlyphContainerSize > BoxSize) so that
        // icon-font glyphs with advance widths wider than the box still render -
        // DejaVu Sans Mono is monospaced for ASCII Latin but its Dingbats (✓ ✕)
        // and Geometric Shapes (▾ ▴ ▲ ▼ ◀ ▶) glyphs are wider than the Latin cell.
        // The container is centered over the 16-px box by offsetting X by the
        // overhang on the left side.
        const float overhang = (GlyphContainerSize - BoxSize) / 2f;
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "HazardCheckGlyph";
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

    private static RectangleRuntime CreateDashIndicator()
    {
        // Indeterminate dash from the CSS spec: 8x2 pill, accent-colored,
        // centered inside the box.
        RectangleRuntime dash = new RectangleRuntime();
        dash.Name = "HazardDashIndicator";
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
        dash.CornerRadius = 0f;
        dash.IsFilled = true;
        dash.FillColor = HazardStyling.ActiveStyle.Colors.Accent;
        dash.StrokeWidth = 0;
        return dash;
    }

    private void WireStates()
    {
        // -------- Unchecked (Off) --------
        States.EnabledOff.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Border,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, ring: false);

        // Border tracks interaction state only — the inside glyph alone signals
        // value. Hover/Highlighted gets BorderHover (matching TextBox's softer
        // hover→focus progression), focus drives Accent + ring, and the same
        // pattern is mirrored on On / Indeterminate below.
        States.HighlightedOff.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Surface2, border: HazardStyling.ActiveStyle.Colors.BorderHover,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, ring: false);

        States.FocusedOff.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, ring: true);

        States.HighlightedFocusedOff.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Surface2, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, ring: true);

        States.PushedOff.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.PressedFill, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, ring: false);

        States.DisabledOff.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.None, ring: false);

        States.DisabledFocusedOff.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.None, ring: true);

        // -------- Checked (On) --------
        // The box fills with full hazard Accent and the check glyph flips to black
        // Ink (.sv-chk.chk .sv-cbox) — the same "active = accent fill" language used
        // by the pressed Button, the On ToggleButton, and selected list rows. Pressed
        // deepens to AccentPressed amber (.sv-chk.pre.chk).
        States.EnabledOn.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Accent, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.Check, glyphColor: HazardStyling.ActiveStyle.Colors.PressedText,
            ring: false);

        States.HighlightedOn.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Accent, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.TextBright, glyph: GlyphKind.Check, glyphColor: HazardStyling.ActiveStyle.Colors.PressedText,
            ring: false);

        States.FocusedOn.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Accent, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.Check, glyphColor: HazardStyling.ActiveStyle.Colors.PressedText,
            ring: true);

        States.HighlightedFocusedOn.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Accent, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.TextBright, glyph: GlyphKind.Check, glyphColor: HazardStyling.ActiveStyle.Colors.PressedText,
            ring: true);

        States.PushedOn.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.AccentPressed, border: HazardStyling.ActiveStyle.Colors.AccentPressed,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.Check, glyphColor: HazardStyling.ActiveStyle.Colors.PressedText,
            ring: false);

        States.DisabledOn.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.Check,
            glyphColor: HazardStyling.ActiveStyle.Colors.DisabledText, ring: false);

        States.DisabledFocusedOn.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.Check,
            glyphColor: HazardStyling.ActiveStyle.Colors.DisabledText, ring: true);

        // -------- Indeterminate --------
        States.EnabledIndeterminate.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Border,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, ring: false);

        States.HighlightedIndeterminate.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Surface2, border: HazardStyling.ActiveStyle.Colors.BorderHover,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, ring: false);

        States.FocusedIndeterminate.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Surface1, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, ring: true);

        States.HighlightedFocusedIndeterminate.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.Surface2, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, ring: true);

        States.PushedIndeterminate.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.PressedFill, border: HazardStyling.ActiveStyle.Colors.Accent,
            text: HazardStyling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, ring: false);

        States.DisabledIndeterminate.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.Dash, ring: false);

        States.DisabledFocusedIndeterminate.Apply = () => Apply(
            fill: HazardStyling.ActiveStyle.Colors.DisabledFill, border: HazardStyling.ActiveStyle.Colors.DisabledBorder,
            text: HazardStyling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.Dash, ring: true);
    }

    private enum GlyphKind { None, Check, Dash }

    private void Apply(Color fill, Color border, Color text, GlyphKind glyph, bool ring,
        Color? glyphColor = null)
    {
        _boxFill.FillColor = fill;
        _boxBorder.StrokeColor = border;
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
