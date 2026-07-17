using Gum.Plugins;
using Gum.Wireframe;

namespace Gum.Commands;

public class WireframeCommands : IWireframeCommands
{
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IPluginManager _pluginManager;
    public WireframeCommands(IWireframeObjectManager wireframeObjectManager,
        IPluginManager pluginManager)
    {
        _wireframeObjectManager = wireframeObjectManager;
        _pluginManager = pluginManager;
    }

    public void Refresh(bool forceLayout = true, bool forceReloadContent = false)
    {
        _wireframeObjectManager.RefreshAll(forceLayout, forceReloadContent);
    }

    bool areRulersVisible = true;
    public bool AreRulersVisible
    {
        get => areRulersVisible;
        set
        {
            areRulersVisible = value;
            _pluginManager.WireframePropertyChanged(nameof(AreRulersVisible));
        }
    }

    bool areCanvasBoundsVisible = true;
    public bool AreCanvasBoundsVisible
    {
        get => areCanvasBoundsVisible;
        set
        {
            areCanvasBoundsVisible = value;
            _pluginManager.WireframePropertyChanged(nameof(AreCanvasBoundsVisible));
        }
    }

    bool isBackgroundGridVisible = true;
    public bool IsBackgroundGridVisible
    {
        get => isBackgroundGridVisible;
        set
        {
            isBackgroundGridVisible = value;
            _pluginManager.WireframePropertyChanged(nameof(IsBackgroundGridVisible));
        }
    }

    bool areHighlightsVisible;

    public bool AreHighlightsVisible
    {
        get => areHighlightsVisible;
        set
        {
            areHighlightsVisible = value;
            _pluginManager.WireframePropertyChanged(nameof(AreHighlightsVisible));
        }
    }
}
