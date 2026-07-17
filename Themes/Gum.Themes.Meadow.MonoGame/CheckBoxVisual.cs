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
using BaseCheckBoxVisual = Gum.Forms.DefaultVisuals.V3.CheckBoxVisual;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow-styled CheckBox visual. A 22 px rounded box (7 px radius) with a 2.5 px
/// peach border and white fill at rest; checked turns the box sage-green with a
/// white check glyph, and indeterminate shows a sage dash. Focus paints a soft
/// sage halo ring just outside the box.
/// </summary>
public class CheckBoxVisual : BaseCheckBoxVisual
{
    private const float BoxSize = 22f;
    private const float CornerRadius = 7f;
    private const float BorderThickness = 2.5f;
    private const float FocusRingInset = 2f;
    private const float FocusRingThickness = 3f;
    private const float BoxToLabelGap = 10f;
    private const float DashWidth = 11f;
    private const float DashHeight = 3f;
    // Glyph runtime sized a hair larger than the box so the ✓ glyph (wider
    // advance than ASCII Latin) isn't clipped.
    private const float GlyphContainerSize = 26f;

    private readonly RectangleRuntime _focusRing;
    private readonly RectangleRuntime _boxFill;
    private readonly RectangleRuntime _boxBorder;
    private readonly TextRuntime _checkGlyph;
    private readonly RectangleRuntime _dashIndicator;

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
        // ✓ renders through the bundled DejaVu Sans Mono icon font under the
        // "Meadow Icons" family. MeadowTheme.Apply pre-registers the glyph via
        // BmfcSave.AddCharacters so KernSmith bakes it into the atlas.
        _checkGlyph.Font = MeadowStyling.ActiveStyle.Text.IconFontFamily;
        _checkGlyph.FontSize = 19;
        _checkGlyph.Text = "✓";
        _checkGlyph.Color = MeadowStyling.ActiveStyle.Colors.White;
        _checkGlyph.Visible = false;

        _dashIndicator = CreateDashIndicator();
        AddChild(_dashIndicator);
        _dashIndicator.Visible = false;

        WireStates();
    }

    private static RectangleRuntime CreateBoxFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "MeadowBoxFill";
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
        fill.FillColor = MeadowStyling.ActiveStyle.Colors.White;
        fill.StrokeWidth = 0;
        return fill;
    }

    private static RectangleRuntime CreateBoxBorder()
    {
        RectangleRuntime border = new RectangleRuntime();
        border.Name = "MeadowBoxBorder";
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
        border.StrokeColor = MeadowStyling.ActiveStyle.Colors.PeachDark;
        return border;
    }

    private static RectangleRuntime CreateFocusRing()
    {
        RectangleRuntime ring = new RectangleRuntime();
        ring.Name = "MeadowBoxFocusRing";
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
        ring.StrokeColor = MeadowStyling.ActiveStyle.Colors.SageFocusRing;
        ring.Visible = false;
        return ring;
    }

    private static TextRuntime CreateCheckGlyph()
    {
        const float overhang = (GlyphContainerSize - BoxSize) / 2f;
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "MeadowCheckGlyph";
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
        // Indeterminate pill: 11x3 (matches .pp-ck-dash), sage-colored, centered.
        RectangleRuntime dash = new RectangleRuntime();
        dash.Name = "MeadowDashIndicator";
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
        dash.CornerRadius = 1.5f;
        dash.IsFilled = true;
        dash.FillColor = MeadowStyling.ActiveStyle.Colors.SageDark;
        dash.StrokeWidth = 0;
        return dash;
    }

    private void WireStates()
    {
        // -------- Unchecked (Off) --------
        States.EnabledOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.PeachDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.None, ring: false);

        States.HighlightedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.None, ring: false);

        States.FocusedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.None, ring: true);

        States.HighlightedFocusedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.None, ring: true);

        States.PushedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.None, ring: false);

        States.DisabledOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, glyph: GlyphKind.None, ring: false);

        States.DisabledFocusedOff.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, glyph: GlyphKind.None, ring: true);

        // -------- Checked (On) -------- sage-filled box, white check.
        States.EnabledOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.SageDark, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Check, glyphColor: MeadowStyling.ActiveStyle.Colors.White,
            ring: false);

        States.HighlightedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.SageDark, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Check, glyphColor: MeadowStyling.ActiveStyle.Colors.White,
            ring: false);

        States.FocusedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.SageDark, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Check, glyphColor: MeadowStyling.ActiveStyle.Colors.White,
            ring: true);

        States.HighlightedFocusedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.SageDark, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Check, glyphColor: MeadowStyling.ActiveStyle.Colors.White,
            ring: true);

        // Pressed-checked deepens to teal (CSS .pp-chk.pre.chk).
        States.PushedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Teal, border: MeadowStyling.ActiveStyle.Colors.Teal,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Check, glyphColor: MeadowStyling.ActiveStyle.Colors.White,
            ring: false);

        States.DisabledOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, glyph: GlyphKind.Check,
            glyphColor: MeadowStyling.ActiveStyle.Colors.DisabledInk, ring: false);

        States.DisabledFocusedOn.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, glyph: GlyphKind.Check,
            glyphColor: MeadowStyling.ActiveStyle.Colors.DisabledInk, ring: true);

        // -------- Indeterminate -------- white box, sage dash + border.
        States.EnabledIndeterminate.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Dash, ring: false);

        States.HighlightedIndeterminate.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Dash, ring: false);

        States.FocusedIndeterminate.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Dash, ring: true);

        States.HighlightedFocusedIndeterminate.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.SageDark,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Dash, ring: true);

        States.PushedIndeterminate.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.White, border: MeadowStyling.ActiveStyle.Colors.Teal,
            text: MeadowStyling.ActiveStyle.Colors.TealDark, glyph: GlyphKind.Dash, ring: false);

        States.DisabledIndeterminate.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, glyph: GlyphKind.Dash, ring: false);

        States.DisabledFocusedIndeterminate.Apply = () => Apply(
            fill: MeadowStyling.ActiveStyle.Colors.Cream2, border: MeadowStyling.ActiveStyle.Colors.Disabled,
            text: MeadowStyling.ActiveStyle.Colors.DisabledInk, glyph: GlyphKind.Dash, ring: true);
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
        if (glyph == GlyphKind.Dash)
        {
            // Channel-wise equality so it compiles on XNA/raylib (no == operator on raylib's Color)
            // and Skia (whose SKColor exposes Red/Green/Blue/Alpha instead of R/G/B/A).
#if SKIA
            bool borderIsDisabled =
                border.Red == MeadowStyling.ActiveStyle.Colors.Disabled.Red &&
                border.Green == MeadowStyling.ActiveStyle.Colors.Disabled.Green &&
                border.Blue == MeadowStyling.ActiveStyle.Colors.Disabled.Blue &&
                border.Alpha == MeadowStyling.ActiveStyle.Colors.Disabled.Alpha;
#else
            bool borderIsDisabled =
                border.R == MeadowStyling.ActiveStyle.Colors.Disabled.R &&
                border.G == MeadowStyling.ActiveStyle.Colors.Disabled.G &&
                border.B == MeadowStyling.ActiveStyle.Colors.Disabled.B &&
                border.A == MeadowStyling.ActiveStyle.Colors.Disabled.A;
#endif
            _dashIndicator.FillColor = borderIsDisabled
                ? MeadowStyling.ActiveStyle.Colors.Disabled : MeadowStyling.ActiveStyle.Colors.SageDark;
        }
        if (glyphColor.HasValue)
        {
            _checkGlyph.Color = glyphColor.Value;
        }
    }
}
