using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;

namespace Gum.Themes.Hazard;

/// <summary>
/// Hazard-styled ListBoxItem visual. Items tile flush inside the ListBox shell, so the
/// background is a square (CornerRadius=0) fill that paints the row according to state:
/// transparent when idle, Surface2 on hover, Selection when selected, and the brighter
/// Accent when selected and the list has keyboard focus.
/// </summary>
public class ListBoxItemVisual : BaseListBoxItemVisual
{
    private readonly RectangleRuntime _fill;

    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // Detach the base NineSlice background and underline focus indicator;
        // the text label is reparented last so the new fill renders behind it.
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        // Square-cornered fill so the row tiles flush inside the rounded ListBox
        // shell (the shell's border masks the corners). Starts transparent.
        _fill = HazardShapes.Fill(Color.Transparent, cornerRadius: 0f, "HazardListItemFill");
        AddChild(_fill);

        AddChild(TextInstance);
        // Left-pad the text the same amount the V3 default does (-8 width split),
        // but nudge it in from the row edge so the selected-row fill reads as a
        // band rather than text mashed against the list border.
        TextInstance.X = 6f;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -12f;

        WireStates();
    }

    private void WireStates()
    {
        // Idle items show no fill — the ListBox's Surface1 reads through. Hover
        // tints to Surface2 with brightened text so the row reads as
        // "interactable." Selected fills with full hazard Accent and flips the
        // text to black Ink (.sv-lb-item.sel) — the same treatment as a selected
        // ComboBox option, so an unfocused list still shows what's selected.
        States.Enabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: HazardPalette.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: HazardPalette.Surface2, text: HazardPalette.TextBright);

        States.Selected.Apply = () => ApplyPalette(
            fill: HazardPalette.Selection, text: HazardPalette.PressedText);

        States.Focused.Apply = () => ApplyPalette(
            fill: HazardPalette.Accent, text: HazardPalette.PressedText);

        States.Disabled.Apply = () => ApplyPalette(
            fill: Color.Transparent, text: HazardPalette.DisabledText);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
