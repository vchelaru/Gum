using Gum.Converters;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using BaseTabControlVisual = Gum.Forms.DefaultVisuals.V3.TabControlVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade styled TabControl visual: the content panel is an InputFill fill with a 2px Border outline.
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

        AddBelowStrip(TabItemVisual.CreateFill("ForestGladeTabControlFill", ForestGladeStyling.ActiveStyle.Colors.InputFill));
        AddBelowStrip(TabItemVisual.CreateBorder("ForestGladeTabControlBorder"));
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
