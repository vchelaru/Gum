using Gum.Plugins;
using Gum.Wireframe;

namespace Gum.Commands;

public class WireframeCommands
{
    public void Refresh(bool forceLayout = true, bool forceReloadContent = false)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout, forceReloadContent);
    }

    bool areRulersVisible = true;
    public bool AreRulersVisible
    {
        get => areRulersVisible;
        set
        {
            areRulersVisible = value;
            PluginManager.Self.WireframePropertyChanged(nameof(AreRulersVisible));
        }
    }

    bool areCanvasBoundsVisible = true;
    public bool AreCanvasBoundsVisible
    {
        get => areCanvasBoundsVisible;
        set
        {
            areCanvasBoundsVisible = value;
            PluginManager.Self.WireframePropertyChanged(nameof(AreCanvasBoundsVisible));
        }
    }

    bool isBackgroundGridVisible = true;
    public bool IsBackgroundGridVisible
    {
        get => isBackgroundGridVisible;
        set
        {
            isBackgroundGridVisible = value;
            PluginManager.Self.WireframePropertyChanged(nameof(IsBackgroundGridVisible));
        }
    }

    bool areHighlightsVisible;
    public bool AreHighlightsVisible
    {
        get => areHighlightsVisible;
        set
        {
            areHighlightsVisible = value;
            PluginManager.Self.WireframePropertyChanged(nameof(AreHighlightsVisible));
        }
    }
}
