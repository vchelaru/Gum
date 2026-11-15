using Gum.Converters;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseTabControlVisual = Gum.Forms.DefaultVisuals.V3.TabControlVisual;

namespace Gum.Themes.Meadow;

/// <summary>
/// Meadow styled TabControl visual: the content panel is a Cream2 fill with a 2px PeachDark outline.
/// </summary>
public class TabControlVisual : BaseTabControlVisual
{
    public TabControlVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // The tab strip (InnerPanelInstance) stays first; the theme's shapes replace the NineSlice behind the
        // pages, and the page container is reattached last so it stays above them.
        ContentBackground.Parent = null;
        ContentPanelInstance.Parent = null;

        AddBelowStrip(TabItemVisual.CreateFill("MeadowTabControlFill", MeadowStyling.ActiveStyle.Colors.Cream2));
        AddBelowStrip(TabItemVisual.CreateBorder("MeadowTabControlBorder"));
        AddChild(ContentPanelInstance);
    }

    private void AddBelowStrip(RectangleRuntime shape)
    {
        shape.XUnits = GeneralUnitType.PixelsFromSmall;
        shape.XOrigin = HorizontalAlignment.Left;
        shape.X = 0;
        shape.YUnits = GeneralUnitType.PixelsFromSmall;
        shape.YOrigin = VerticalAlignment.Top;
        shape.Y = StripHeight;
        shape.Height = -StripHeight;
        AddChild(shape);
    }
}
