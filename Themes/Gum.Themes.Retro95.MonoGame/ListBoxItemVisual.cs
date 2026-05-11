using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled ListBoxItem visual. Transparent at rest, navy <c>--sel</c> band when
/// selected or hovered, with white text (matches <c>.rc-lb-item</c>).
/// </summary>
public class ListBoxItemVisual : BaseListBoxItemVisual
{
    private readonly ColoredRectangleRuntime _fill;

    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;
        FocusedIndicator.Parent = null;
        TextInstance.Parent = null;

        _fill = new ColoredRectangleRuntime();
        _fill.Name = "Retro95ListItemFill";
        _fill.X = 0; _fill.Y = 0;
        _fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        _fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        _fill.XOrigin = HorizontalAlignment.Center;
        _fill.YOrigin = VerticalAlignment.Center;
        _fill.Width = 0; _fill.Height = 0;
        _fill.WidthUnits = DimensionUnitType.RelativeToParent;
        _fill.HeightUnits = DimensionUnitType.RelativeToParent;
        _fill.Color = Color.Transparent;
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
        States.Enabled.Apply = () => Apply(Color.Transparent, Retro95Colors.Text);
        States.Highlighted.Apply = () => Apply(Retro95Colors.Selection, Retro95Colors.SelectionText);
        States.Selected.Apply = () => Apply(Retro95Colors.Selection, Retro95Colors.SelectionText);
        States.Focused.Apply = () => Apply(Retro95Colors.Selection, Retro95Colors.SelectionText);
        States.Disabled.Apply = () => Apply(Color.Transparent, Retro95Colors.DisabledText);
    }

    private void Apply(Color fill, Color text)
    {
        _fill.Color = fill;
        TextInstance.Color = text;
    }
}
