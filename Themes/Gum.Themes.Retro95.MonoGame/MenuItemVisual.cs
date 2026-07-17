using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseMenuItemVisual = Gum.Forms.DefaultVisuals.V3.MenuItemVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95-styled MenuItem visual. Transparent at rest, navy band with white text when
/// active / selected (matches <c>.rc-mi.act</c>). Submenu popup uses
/// <see cref="ScrollViewerVisual"/>.
/// </summary>
public class MenuItemVisual : BaseMenuItemVisual
{
    private const float HorizontalPadding = 10f;
    private const float VerticalPadding = 2f;

    private readonly RectangleRuntime _fill;

    public MenuItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        Background.Parent = null;
        ContainerInstance.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        AddChild(ContainerInstance);

        ContainerInstance.X = HorizontalPadding;
        ContainerInstance.Y = VerticalPadding;
        Width = HorizontalPadding * 2f;
        WidthUnits = DimensionUnitType.RelativeToChildren;
        Height = VerticalPadding * 2f;
        HeightUnits = DimensionUnitType.RelativeToChildren;

        WireStates();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new MenuItem(this);
        }
    }

    public override object FormsControlAsObject
    {
        get => base.FormsControlAsObject;
        set
        {
            base.FormsControlAsObject = value;
            if (value is MenuItem menuItem)
            {
                menuItem.ScrollViewerVisualTemplate = Retro95SubmenuScrollTemplate;
            }
        }
    }

    // Use the dedicated menu-popup visual (raised + gray surface) rather than
    // the regular ScrollViewerVisual (inset + white) — the inset-white look is
    // right for a content scroll area but wrong for a Win95 drop-down menu.
    private VisualTemplate Retro95SubmenuScrollTemplate => new VisualTemplate(() =>
    {
        Retro95MenuPopupVisual visual = new Retro95MenuPopupVisual(fullInstantiation: true, tryCreateFormsObject: false);
        visual.HasEvents = true;
        visual.MakeSizedToChildren();
        return visual;
    });

    private void WireStates()
    {
        States.Enabled.Apply = () => Apply(new Color(0, 0, 0, 0), Retro95Styling.ActiveStyle.Colors.Text);
        States.Highlighted.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.Selection, Retro95Styling.ActiveStyle.Colors.SelectionText);
        States.Selected.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.Selection, Retro95Styling.ActiveStyle.Colors.SelectionText);
        States.Focused.Apply = () => Apply(Retro95Styling.ActiveStyle.Colors.Selection, Retro95Styling.ActiveStyle.Colors.SelectionText);
        States.Disabled.Apply = () => Apply(new Color(0, 0, 0, 0), Retro95Styling.ActiveStyle.Colors.DisabledText);
    }

    private void Apply(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
        SubmenuIndicatorInstance.Color = text;
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "Retro95MenuItemFill";
        fill.X = 0; fill.Y = 0;
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0; fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.IsFilled = true;
        fill.FillColor = new Color(0, 0, 0, 0);
        fill.StrokeWidth = 0;
        return fill;
    }
}
