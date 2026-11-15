using Gum.GueDeriving;
using BaseTabControlVisual = Gum.Forms.DefaultVisuals.V3.TabControlVisual;

namespace Gum.Themes.Retro95;

/// <summary>
/// Retro95 styled TabControl visual: the content panel is a raised bevel in the Surface gray.
/// </summary>
public class TabControlVisual : BaseTabControlVisual
{
    public TabControlVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // The tab strip (InnerPanelInstance) stays first; the bevel replaces the NineSlice behind the pages,
        // and the page container is reattached last so it stays above it.
        ContentBackground.Parent = null;
        ContentPanelInstance.Parent = null;

        // The bevel fills its parent, so it gets a holder covering the area below the strip.
        ContainerRuntime bevelHolder = new ContainerRuntime();
        bevelHolder.Name = "Retro95TabControlBevelHolder";
        bevelHolder.HasEvents = false;
        bevelHolder.Y = StripHeight;
        bevelHolder.Width = 0;
        bevelHolder.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        bevelHolder.Height = -StripHeight;
        bevelHolder.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        AddChild(bevelHolder);
        Retro95Bevel.AddTo(bevelHolder, BevelMode.Raised);

        AddChild(ContentPanelInstance);
    }
}
