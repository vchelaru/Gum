using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif
using RenderingLibrary.Graphics;
using BaseMenuItemVisual = Gum.Forms.DefaultVisuals.V3.MenuItemVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled MenuItem visual. State coloring mirrors the ListBoxItem
/// pattern: transparent at rest, Surface2 on hover, AccentDark when the item's
/// submenu is open (Selected). Text uses Text / DisabledText.
/// <para>
/// Adds symmetric horizontal + vertical padding around the content so menu
/// items don't look cramped — V3 sizes them snug to text + submenu arrow,
/// which reads as crowded in a top menu bar.
/// </para>
/// <para>
/// The submenu popup ScrollViewer template is overridden in
/// <see cref="FormsControlAsObject"/> to use a Dark Pro ScrollViewerVisual
/// (sized to children, matching the V3 pattern) so the popup container
/// matches the rest of the theme.
/// </para>
/// </summary>
public class MenuItemVisual : BaseMenuItemVisual
{
    private const float HorizontalPadding = 12f;
    private const float VerticalPadding = 4f;

    private readonly RectangleRuntime _fill;

    public MenuItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject: false)
    {
        // V3 order is [Background, ContainerInstance]. Replace Background with
        // a Dark Pro fill, reattach ContainerInstance last so text + submenu
        // arrow render on top.
        Background.Parent = null;
        ContainerInstance.Parent = null;

        _fill = CreateFill();
        AddChild(_fill);

        AddChild(ContainerInstance);

        // Symmetric padding: shift ContainerInstance in by HorizontalPadding
        // and VerticalPadding, and grow the MenuItem visual by twice that on
        // each axis so the inset stays equal on both sides.
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

    /// <summary>
    /// Overrides V3's submenu-popup template assignment so the popup uses a
    /// Dark Pro ScrollViewerVisual instead of the V3 internal one.
    /// </summary>
    public override object FormsControlAsObject
    {
        get => base.FormsControlAsObject;
        set
        {
            base.FormsControlAsObject = value;
            if (value is MenuItem menuItem)
            {
                menuItem.ScrollViewerVisualTemplate = DarkProSubmenuScrollTemplate;
            }
        }
    }

    private VisualTemplate DarkProSubmenuScrollTemplate => new VisualTemplate(() =>
    {
        ScrollViewerVisual visual = new ScrollViewerVisual(fullInstantiation: true, tryCreateFormsObject: false);
        visual.HasEvents = true;
        visual.MakeSizedToChildren();
        return visual;
    });

    private void WireStates()
    {
        // Background.Visible is repurposed here as "should the fill paint" —
        // when false the fill stays transparent (rest state). The V3 SetValues
        // helper toggled Background.Visible directly; we replace the entire
        // state body so we don't need to keep V3's NineSlice attached.
        States.Enabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: DarkProStyling.ActiveStyle.Colors.Text);

        States.Highlighted.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface2, text: DarkProStyling.ActiveStyle.Colors.Text);

        States.Selected.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.AccentDark, text: DarkProStyling.ActiveStyle.Colors.Text);

        States.Focused.Apply = () => ApplyPalette(
            fill: DarkProStyling.ActiveStyle.Colors.Surface2, text: DarkProStyling.ActiveStyle.Colors.Text);

        States.Disabled.Apply = () => ApplyPalette(
            fill: new Color(0, 0, 0, 0), text: DarkProStyling.ActiveStyle.Colors.DisabledText);
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
        fill.Name = "DarkProMenuItemFill";
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
        fill.IsFilled = true;
        fill.FillColor = new Color(0, 0, 0, 0);
        fill.StrokeWidth = 0;
        return fill;
    }
}
