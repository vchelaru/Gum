using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
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
    private readonly ColoredRectangleRuntime _dashIndicator;

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

        _bevel = Retro95Bevel.AddTo(_boxContainer, BevelMode.Inset, Retro95Colors.WhiteFill);

        _checkGlyph = CreateCheckGlyph();
        _boxContainer.AddChild(_checkGlyph);
        _checkGlyph.Font = Retro95Theme.IconFontFamily;
        _checkGlyph.FontSize = 12;
        _checkGlyph.Text = "✓";
        _checkGlyph.Color = Retro95Colors.Text;
        _checkGlyph.Visible = false;

        _dashIndicator = CreateDashIndicator();
        _boxContainer.AddChild(_dashIndicator);
        _dashIndicator.Visible = false;

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

    private static ColoredRectangleRuntime CreateDashIndicator()
    {
        ColoredRectangleRuntime dash = new ColoredRectangleRuntime();
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
        dash.Color = Retro95Colors.Selection;
        return dash;
    }

    private void WireStates()
    {
        // -------- Off --------
        States.EnabledOff.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.None);
        States.HighlightedOff.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.None);
        States.FocusedOff.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.None);
        States.HighlightedFocusedOff.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.None);
        States.PushedOff.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.None);
        States.DisabledOff.Apply = () => Apply(fillWhite: false, text: Retro95Colors.DisabledText, glyph: GlyphKind.None);
        States.DisabledFocusedOff.Apply = () => Apply(fillWhite: false, text: Retro95Colors.DisabledText, glyph: GlyphKind.None);

        // -------- On --------
        States.EnabledOn.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Colors.Text);
        States.HighlightedOn.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Colors.Text);
        States.FocusedOn.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Colors.Text);
        States.HighlightedFocusedOn.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Colors.Text);
        States.PushedOn.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Check, glyphColor: Retro95Colors.Text);
        States.DisabledOn.Apply = () => Apply(fillWhite: false, text: Retro95Colors.DisabledText, glyph: GlyphKind.Check, glyphColor: Retro95Colors.DisabledText);
        States.DisabledFocusedOn.Apply = () => Apply(fillWhite: false, text: Retro95Colors.DisabledText, glyph: GlyphKind.Check, glyphColor: Retro95Colors.DisabledText);

        // -------- Indeterminate --------
        States.EnabledIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Dash);
        States.HighlightedIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Dash);
        States.FocusedIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Dash);
        States.HighlightedFocusedIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Dash);
        States.PushedIndeterminate.Apply = () => Apply(fillWhite: true, text: Retro95Colors.Text, glyph: GlyphKind.Dash);
        States.DisabledIndeterminate.Apply = () => Apply(fillWhite: false, text: Retro95Colors.DisabledText, glyph: GlyphKind.Dash);
        States.DisabledFocusedIndeterminate.Apply = () => Apply(fillWhite: false, text: Retro95Colors.DisabledText, glyph: GlyphKind.Dash);
    }

    private enum GlyphKind { None, Check, Dash }

    private void Apply(bool fillWhite, Color text, GlyphKind glyph, Color? glyphColor = null)
    {
        _bevel.SetFill(fillWhite ? Retro95Colors.WhiteFill : Retro95Colors.Surface);
        TextInstance.Color = text;
        _checkGlyph.Visible = glyph == GlyphKind.Check;
        _dashIndicator.Visible = glyph == GlyphKind.Dash;
        if (glyphColor.HasValue)
        {
            _checkGlyph.Color = glyphColor.Value;
        }
    }
}
