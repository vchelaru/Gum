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
using BaseComboBoxVisual = Gum.Forms.DefaultVisuals.V3.ComboBoxVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled ComboBox visual. White inset-beveled field with a raised-bevel
/// dropdown button on the right (matches <c>.rc-cbo</c>). The dropdown popup picks
/// up <see cref="ListBoxVisual"/> automatically through Forms' template resolution.
/// </summary>
public class ComboBoxVisual : BaseComboBoxVisual
{
    private const float DropdownButtonWidth = 18f;
    private const float TextLeftPadding = 6f;

    private readonly ContainerRuntime _fieldContainer;
    private readonly Retro95Bevel _fieldBevel;
    private readonly ContainerRuntime _dropdownButtonContainer;
    private readonly Retro95Bevel _dropdownBevel;
    private readonly TextRuntime _dropdownGlyph;
    private readonly Retro95DottedFocusRect _focusRect;

    public ComboBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        DropdownIndicator.Parent = null;
        TextInstance.Parent = null;

        _fieldContainer = new ContainerRuntime();
        _fieldContainer.Name = "Retro95ComboField";
        // ContainerRuntime defaults HasEvents=true; disable so clicks reach the
        // ComboBox root (which V3 wires up to open the dropdown).
        _fieldContainer.HasEvents = false;
        _fieldContainer.X = 0;
        _fieldContainer.Y = 0;
        _fieldContainer.XUnits = GeneralUnitType.PixelsFromSmall;
        _fieldContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
        _fieldContainer.XOrigin = HorizontalAlignment.Left;
        _fieldContainer.YOrigin = VerticalAlignment.Center;
        _fieldContainer.Width = -DropdownButtonWidth;
        _fieldContainer.Height = 0f;
        _fieldContainer.WidthUnits = DimensionUnitType.RelativeToParent;
        _fieldContainer.HeightUnits = DimensionUnitType.RelativeToParent;
        AddChild(_fieldContainer);

        _fieldBevel = Retro95Bevel.AddTo(_fieldContainer, BevelMode.Inset, Retro95Styling.ActiveStyle.Colors.WhiteFill);

        _dropdownButtonContainer = new ContainerRuntime();
        _dropdownButtonContainer.Name = "Retro95ComboDropdownButton";
        _dropdownButtonContainer.HasEvents = false;
        _dropdownButtonContainer.X = 0;
        _dropdownButtonContainer.Y = 0;
        _dropdownButtonContainer.XUnits = GeneralUnitType.PixelsFromLarge;
        _dropdownButtonContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
        _dropdownButtonContainer.XOrigin = HorizontalAlignment.Right;
        _dropdownButtonContainer.YOrigin = VerticalAlignment.Center;
        _dropdownButtonContainer.Width = DropdownButtonWidth;
        _dropdownButtonContainer.Height = 0f;
        _dropdownButtonContainer.WidthUnits = DimensionUnitType.Absolute;
        _dropdownButtonContainer.HeightUnits = DimensionUnitType.RelativeToParent;
        AddChild(_dropdownButtonContainer);

        _dropdownBevel = Retro95Bevel.AddTo(_dropdownButtonContainer, BevelMode.Raised);

        _dropdownGlyph = new TextRuntime();
        _dropdownGlyph.Name = "Retro95ComboGlyph";
        _dropdownGlyph.X = 0; _dropdownGlyph.Y = 0;
        _dropdownGlyph.XUnits = GeneralUnitType.PixelsFromMiddle;
        _dropdownGlyph.YUnits = GeneralUnitType.PixelsFromMiddle;
        _dropdownGlyph.XOrigin = HorizontalAlignment.Center;
        _dropdownGlyph.YOrigin = VerticalAlignment.Center;
        _dropdownGlyph.Width = DropdownButtonWidth;
        _dropdownGlyph.Height = DropdownButtonWidth;
        _dropdownGlyph.WidthUnits = DimensionUnitType.Absolute;
        _dropdownGlyph.HeightUnits = DimensionUnitType.Absolute;
        _dropdownGlyph.HorizontalAlignment = HorizontalAlignment.Center;
        _dropdownGlyph.VerticalAlignment = VerticalAlignment.Center;
        _dropdownGlyph.Font = Retro95Styling.ActiveStyle.Text.IconFontFamily;
        _dropdownGlyph.FontSize = 8;
        _dropdownGlyph.Text = "▼";
        _dropdownGlyph.Color = Retro95Styling.ActiveStyle.Colors.Text;
        _dropdownButtonContainer.AddChild(_dropdownGlyph);

        AddChild(TextInstance);
        TextInstance.X = TextLeftPadding;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -(TextLeftPadding + DropdownButtonWidth + 4f);

        // Dotted focus rect inside the field area (inset 2 from the bevel).
        _focusRect = new Retro95DottedFocusRect(this, inset: 0f);
        _focusRect.Container.X = 2f;
        _focusRect.Container.XUnits = GeneralUnitType.PixelsFromSmall;
        _focusRect.Container.XOrigin = HorizontalAlignment.Left;
        _focusRect.Container.Width = -(DropdownButtonWidth + 4f);
        _focusRect.Container.WidthUnits = DimensionUnitType.RelativeToParent;
        _focusRect.Container.Y = 0f;
        _focusRect.Container.YUnits = GeneralUnitType.PixelsFromMiddle;
        _focusRect.Container.YOrigin = VerticalAlignment.Center;
        _focusRect.Container.Height = -4f;
        _focusRect.Container.HeightUnits = DimensionUnitType.RelativeToParent;

        WireStates();
    }

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(
            fieldFill: Retro95Styling.ActiveStyle.Colors.WhiteFill, text: Retro95Styling.ActiveStyle.Colors.Text,
            glyph: Retro95Styling.ActiveStyle.Colors.Text, dropdownMode: BevelMode.Raised, focus: false);

        States.Highlighted.Apply = () => Apply(
            fieldFill: Retro95Styling.ActiveStyle.Colors.WhiteFill, text: Retro95Styling.ActiveStyle.Colors.Text,
            glyph: Retro95Styling.ActiveStyle.Colors.Text, dropdownMode: BevelMode.Raised, focus: false);

        States.Focused.Apply = () => Apply(
            fieldFill: Retro95Styling.ActiveStyle.Colors.WhiteFill, text: Retro95Styling.ActiveStyle.Colors.Text,
            glyph: Retro95Styling.ActiveStyle.Colors.Text, dropdownMode: BevelMode.Raised, focus: true);

        States.HighlightedFocused.Apply = () => Apply(
            fieldFill: Retro95Styling.ActiveStyle.Colors.WhiteFill, text: Retro95Styling.ActiveStyle.Colors.Text,
            glyph: Retro95Styling.ActiveStyle.Colors.Text, dropdownMode: BevelMode.Raised, focus: true);

        States.Pushed.Apply = () => Apply(
            fieldFill: Retro95Styling.ActiveStyle.Colors.WhiteFill, text: Retro95Styling.ActiveStyle.Colors.Text,
            glyph: Retro95Styling.ActiveStyle.Colors.Text, dropdownMode: BevelMode.Sunken, focus: false);

        States.Disabled.Apply = () => Apply(
            fieldFill: Retro95Styling.ActiveStyle.Colors.Surface, text: Retro95Styling.ActiveStyle.Colors.DisabledText,
            glyph: Retro95Styling.ActiveStyle.Colors.DisabledText, dropdownMode: BevelMode.Raised, focus: false);

        States.DisabledFocused.Apply = () => Apply(
            fieldFill: Retro95Styling.ActiveStyle.Colors.Surface, text: Retro95Styling.ActiveStyle.Colors.DisabledText,
            glyph: Retro95Styling.ActiveStyle.Colors.DisabledText, dropdownMode: BevelMode.Raised, focus: true);
    }

    private void Apply(Color fieldFill, Color text, Color glyph, BevelMode dropdownMode, bool focus)
    {
        _fieldBevel.SetFill(fieldFill);
        TextInstance.Color = text;
        _dropdownGlyph.Color = glyph;
        _dropdownBevel.SetMode(dropdownMode);
        if (focus) _focusRect.Show(); else _focusRect.Hide();
    }
}
