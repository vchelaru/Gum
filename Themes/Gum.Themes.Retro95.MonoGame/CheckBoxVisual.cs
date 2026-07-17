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

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled CheckBox visual. A 13 px sunken box with a white fill (matches
/// <c>.rc-cbox</c>), navy <c>✓</c> glyph when checked, navy <c>■</c> bar when
/// indeterminate. Disabled state grays out the fill and label.
/// </summary>
public class CheckBoxVisual : BaseCheckBoxVisual
{
    private const float BoxSize = 13f;
    private const float BoxToLabelGap = 6f;
    private const float GlyphContainerSize = 17f;
    private const float DashWidth = 7f;
    private const float DashHeight = 2f;

    private readonly ContainerRuntime _boxContainer;
    private readonly Retro95Bevel _bevel;
    private readonly TextRuntime _checkGlyph;
    private readonly RectangleRuntime _dashIndicator;
    private readonly Retro95DottedFocusRect _focusRect;

    public CheckBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        CheckBoxBackground.Parent = null;
        FocusedIndicator.Parent = null;

        // V3 default is Height=24 (sized for a 24×24 NineSlice box). Win95 boxes
        // are 13×13 — we shrink to 16 so vertically-stacked checkboxes / radios
        // sit at the tight cadence Win95 used, not the airy spacing of V3 default.
        Height = 16;
        HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

        TextInstance.X = BoxSize + BoxToLabelGap;
        TextInstance.Width = -(BoxSize + BoxToLabelGap);

        _boxContainer = new ContainerRuntime();
        _boxContainer.Name = "Retro95CheckBoxContainer";
        // ContainerRuntime sets HasEvents = true in its constructor, which would
        // swallow clicks on the 13×13 box area. Disable so clicks bubble up to
        // the CheckBox root visual (which is the InteractiveGue that fires Click).
        _boxContainer.HasEvents = false;
        _boxContainer.X = 0;
        _boxContainer.Y = 0;
        _boxContainer.XUnits = GeneralUnitType.PixelsFromSmall;
        _boxContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
        _boxContainer.XOrigin = HorizontalAlignment.Left;
        _boxContainer.YOrigin = VerticalAlignment.Center;
        _boxContainer.Width = BoxSize;
        _boxContainer.Height = BoxSize;
        _boxContainer.WidthUnits = DimensionUnitType.Absolute;
        _boxContainer.HeightUnits = DimensionUnitType.Absolute;
        AddChild(_boxContainer);

        _bevel = Retro95Bevel.AddTo(_boxContainer, BevelMode.Inset, Retro95Styling.ActiveStyle.Colors.WhiteFill);

        _checkGlyph = CreateCheckGlyph();
        _boxContainer.AddChild(_checkGlyph);
        _checkGlyph.Font = Retro95Styling.ActiveStyle.Text.IconFontFamily;
        _checkGlyph.FontSize = 12;
        _checkGlyph.Text = "✓";
        _checkGlyph.Color = Retro95Styling.ActiveStyle.Colors.Text;
        _checkGlyph.Visible = false;

        _dashIndicator = CreateDashIndicator();
        _boxContainer.AddChild(_dashIndicator);
        _dashIndicator.Visible = false;

        // Win95 puts the dotted focus ring tightly around the label text, not
        // the box. We anchor the rect to the left edge of the text area and
        // size it to the text (RelativeToParent on width; the visual's height
        // is 16 so we cover the full row height for the rect).
        _focusRect = new Retro95DottedFocusRect(this, inset: 0f);
        _focusRect.Container.X = BoxSize + BoxToLabelGap - 2f;
        _focusRect.Container.XUnits = GeneralUnitType.PixelsFromSmall;
        _focusRect.Container.XOrigin = HorizontalAlignment.Left;
        _focusRect.Container.Width = -(BoxSize + BoxToLabelGap - 4f);
        _focusRect.Container.WidthUnits = DimensionUnitType.RelativeToParent;
        _focusRect.Container.Height = 0f;
        _focusRect.Container.HeightUnits = DimensionUnitType.RelativeToParent;

        WireStates();
    }

    private static TextRuntime CreateCheckGlyph()
    {
        const float overhang = (GlyphContainerSize - BoxSize) / 2f;
        TextRuntime glyph = new TextRuntime();
        glyph.Name = "Retro95CheckGlyph";
        glyph.X = -overhang;
        glyph.Y = -overhang;
        glyph.XUnits = GeneralUnitType.PixelsFromSmall;
        glyph.YUnits = GeneralUnitType.PixelsFromSmall;
        glyph.XOrigin = HorizontalAlignment.Left;
        glyph.YOrigin = VerticalAlignment.Top;
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
        RectangleRuntime dash = new RectangleRuntime();
        dash.Name = "Retro95CheckDash";
        dash.X = 0;
        dash.Y = 0;
        dash.XUnits = GeneralUnitType.PixelsFromMiddle;
        dash.YUnits = GeneralUnitType.PixelsFromMiddle;
        dash.XOrigin = HorizontalAlignment.Center;
        dash.YOrigin = VerticalAlignment.Center;
        dash.Width = DashWidth;
        dash.Height = DashHeight;
        dash.WidthUnits = DimensionUnitType.Absolute;
        dash.HeightUnits = DimensionUnitType.Absolute;
        dash.IsFilled = true;
        dash.FillColor = Retro95Styling.ActiveStyle.Colors.Selection;
        dash.StrokeWidth = 0;
        return dash;
    }

    private void WireStates()
    {
        // -------- Off --------
        States.EnabledOff.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, focus: false);
        States.HighlightedOff.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, focus: false);
        States.FocusedOff.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, focus: true);
        States.HighlightedFocusedOff.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, focus: true);
        States.PushedOff.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.None, focus: false);
        States.DisabledOff.Apply = () => Apply(fillWhite: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.None, focus: false);
        States.DisabledFocusedOff.Apply = () => Apply(fillWhite: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.None, focus: true);

        // -------- On --------
        States.EnabledOn.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Styling.ActiveStyle.Colors.Text, focus: false);
        States.HighlightedOn.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Styling.ActiveStyle.Colors.Text, focus: false);
        States.FocusedOn.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Styling.ActiveStyle.Colors.Text, focus: true);
        States.HighlightedFocusedOn.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Styling.ActiveStyle.Colors.Text, focus: true);
        States.PushedOn.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Styling.ActiveStyle.Colors.Text, focus: false);
        States.DisabledOn.Apply = () => Apply(fillWhite: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.Check, glyphColor: Retro95Styling.ActiveStyle.Colors.DisabledText, focus: false);
        States.DisabledFocusedOn.Apply = () => Apply(fillWhite: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.Check, glyphColor: Retro95Styling.ActiveStyle.Colors.DisabledText, focus: true);

        // -------- Indeterminate --------
        States.EnabledIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, focus: false);
        States.HighlightedIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, focus: false);
        States.FocusedIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, focus: true);
        States.HighlightedFocusedIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, focus: true);
        States.PushedIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Styling.ActiveStyle.Colors.Text, glyph: GlyphKind.Dash, focus: false);
        States.DisabledIndeterminate.Apply = () => Apply(fillWhite: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.Dash, focus: false);
        States.DisabledFocusedIndeterminate.Apply = () => Apply(fillWhite: false, text: Retro95Styling.ActiveStyle.Colors.DisabledText, glyph: GlyphKind.Dash, focus: true);
    }

    private enum GlyphKind { None, Check, Dash }

    private void Apply(bool fillWhite, Color text, GlyphKind glyph, bool focus, Color? glyphColor = null)
    {
        _bevel.SetFill(fillWhite ? Retro95Styling.ActiveStyle.Colors.WhiteFill : Retro95Styling.ActiveStyle.Colors.Surface);
        TextInstance.Color = text;
        _checkGlyph.Visible = glyph == GlyphKind.Check;
        _dashIndicator.Visible = glyph == GlyphKind.Dash;
        if (glyphColor.HasValue)
        {
            _checkGlyph.Color = glyphColor.Value;
        }
        if (focus) _focusRect.Show(); else _focusRect.Hide();
    }
}
