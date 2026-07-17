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
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled ListBoxItem visual. Items tile flush inside the ListBox shell, so the
/// background is a square (CornerRadius=0) fill that paints the row according to state:
/// transparent when idle, Surface2 on hover, AccentDark when selected, and the brighter
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

        _fill = CreateFill();
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

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "DarkProListItemFill";
        fill.X = 0;
        fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.CornerRadius = 0f;
        fill.IsFilled = true;
        fill.FillColor = new Color(0, 0, 0, 0);
        fill.StrokeWidth = 0;
        return fill;
    }

    private void WireStates()
    {
        // Idle items show no fill — the ListBox's Surface1 reads through. Hover
        // tints to Surface2 so the row reads as "interactable." Selected jumps
        // to AccentDark (muted blue) so an unfocused list still shows what's
        // selected; selected+focused brightens to full Accent to mark which row
        // the keyboard will move from.
        States.Enabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: DarkProStyling.ActiveStyle.Colors.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface2, text: DarkProStyling.ActiveStyle.Colors.Text);

        States.Selected.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.AccentDark, text: DarkProStyling.ActiveStyle.Colors.Text);

        States.Focused.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Accent, text: DarkProStyling.ActiveStyle.Colors.Text);

        States.Disabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: DarkProStyling.ActiveStyle.Colors.DisabledText);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
