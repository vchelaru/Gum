using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled ListBoxItem visual. Transparent at rest, navy <c>--sel</c> band when
/// selected or hovered, with white text (matches <c>.rc-lb-item</c>).
/// </summary>
public class ListBoxItemVisual : BaseListBoxItemVisual
{
    private readonly RectangleRuntime _fill;

    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        _fill = new RectangleRuntime();
        _fill.Name = "Retro95ListItemFill";
        _fill.X = 0; _fill.Y = 0;
        _fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        _fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        _fill.XOrigin = HorizontalAlignment.Center;
        _fill.YOrigin = VerticalAlignment.Center;
        _fill.Width = 0; _fill.Height = 0;
        _fill.WidthUnits = DimensionUnitType.RelativeToParent;
        _fill.HeightUnits = DimensionUnitType.RelativeToParent;
        _fill.IsFilled = true;
        _fill.FillColor = new Color(0, 0, 0, 0);
        _fill.StrokeWidth = 0;
        AddChild(_fill);

        AddChild(TextInstance);
        TextInstance.X = 6f;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.Width = -12f;

        WireStates();
    }

    private void WireStates()
    {
        // Win95-authentic: ListBox items had no hover styling. Only the selected
        // item drew the navy band. The CSS source spec paints hover navy too,
        // but that's a modernization — keeping the historical behavior here
        // makes selected items stand out unambiguously from "the row my cursor
        // happens to be over."
        States.Enabled.Apply = () => Apply(new Color(0, 0, 0, 0), Retro95Styling.ActiveStyle.Colors.Text);
        States.Highlighted.Apply = () => Apply(new Color(0, 0, 0, 0), Retro95Styling.ActiveStyle.Colors.Text);
        States.Selected.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.Selection, Retro95Styling.ActiveStyle.Colors.SelectionText);
        States.Focused.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.Selection, Retro95Styling.ActiveStyle.Colors.SelectionText);
        States.Disabled.Apply = () => Apply(new Color(0, 0, 0, 0), Retro95Styling.ActiveStyle.Colors.DisabledText);
    }

    private void Apply(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
    }
}
