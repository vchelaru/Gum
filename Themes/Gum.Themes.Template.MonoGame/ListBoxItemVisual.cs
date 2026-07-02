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
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;

namespace Gum.Themes.Template;

/// <summary>
/// Template-styled ListBoxItem visual. Items tile flush inside the ListBox shell, so the
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
        _fill = TemplateShapes.Fill(new Color(0, 0, 0, 0), cornerRadius: 0f, "ListItemFill");
        AddChild(_fill);

        AddChild(TextInstance);
        TextInstance.Font = TemplateStyling.ActiveStyle.Text.BodyFontFamily; // row content uses the body face
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
        // tints to Surface2 so the row reads as "interactable." Selected jumps
        // to Selection (muted blue) so an unfocused list still shows what's
        // selected; selected+focused brightens to full Accent to mark which row
        // the keyboard will move from.
        States.Enabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: TemplateStyling.ActiveStyle.Colors.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.Surface2, text: TemplateStyling.ActiveStyle.Colors.Text);

        States.Selected.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.Selection, text: TemplateStyling.ActiveStyle.Colors.Text);

        States.Focused.Apply = () => ApplyPalette(
            fill: TemplateStyling.ActiveStyle.Colors.Accent, text: TemplateStyling.ActiveStyle.Colors.Text);

        States.Disabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: TemplateStyling.ActiveStyle.Colors.DisabledText);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
