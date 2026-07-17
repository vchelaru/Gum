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

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled MenuItem visual. Transparent at rest, sun-pale tint
/// on hover, accent-dim band when the submenu is open (Selected). Submenu
/// popup uses the Forest Glade <see cref="ScrollViewerVisual"/> via the
/// <c>ScrollViewerVisualTemplate</c> hook.
/// </summary>
public class MenuItemVisual : BaseMenuItemVisual
{
    private const float HorizontalPadding = 12f;
    private const float VerticalPadding = 6f;

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

    /// <inheritdoc/>
    public override object FormsControlAsObject
    {
        get => base.FormsControlAsObject;
        set
        {
            base.FormsControlAsObject = value;
            if (value is MenuItem menuItem)
            {
                menuItem.ScrollViewerVisualTemplate = ForestGladeSubmenuScrollTemplate;
            }
        }
    }

    private VisualTemplate ForestGladeSubmenuScrollTemplate => new VisualTemplate(() =>
    {
        ScrollViewerVisual visual = new ScrollViewerVisual(fullInstantiation: true, tryCreateFormsObject: false);
        visual.HasEvents = true;
        visual.MakeSizedToChildren();
        return visual;
    });

    private void WireStates()
    {
        Color hoverFill = new Color(232, 255, 117, 26); // ~10% alpha tint

        States.Enabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: ForestGladeStyling.ActiveStyle.Colors.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: hoverFill, text: ForestGladeStyling.ActiveStyle.Colors.Text);

        States.Selected.Apply = () => ApplyPalette(
            fill: ForestGladeStyling.ActiveStyle.Colors.SelectedRow, text: ForestGladeStyling.ActiveStyle.Colors.SunPale);

        States.Focused.Apply = () => ApplyPalette(
            fill: hoverFill, text: ForestGladeStyling.ActiveStyle.Colors.Text);

        States.Disabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: ForestGladeStyling.ActiveStyle.Colors.Disabled);
    }

    private void ApplyPalette(Color fill, Color text)
    {
        _fill.FillColor = fill;
        TextInstance.Color = text;
        SubmenuIndicatorInstance.Color = text;
    }

    private static RectangleRuntime CreateFill()
    {
        RectangleRuntime fill = new RectangleRuntime();
        fill.Name = "ForestGladeMenuItemFill";
        fill.XUnits = GeneralUnitType.PixelsFromMiddle;
        fill.YUnits = GeneralUnitType.PixelsFromMiddle;
        fill.XOrigin = HorizontalAlignment.Center;
        fill.YOrigin = VerticalAlignment.Center;
        fill.Width = 0;
        fill.Height = 0;
        fill.WidthUnits = DimensionUnitType.RelativeToParent;
        fill.HeightUnits = DimensionUnitType.RelativeToParent;
        fill.IsFilled = true;
        fill.FillColor = new Color(0, 0, 0, 0);
        fill.StrokeWidth = 0;
        return fill;
    }
}
