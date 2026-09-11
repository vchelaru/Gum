using Gum.Menus;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

namespace Gum.Plugins.InternalPlugins.HideShowTools;

// As of ADR-0005 Phase 3, the toggle decision lives in HideShowToolsLogic (Gum.Presentation) so it
// can be unit tested headlessly. Each head exports its tab host as IToolsVisibility
// (MainPanelViewModel in WPF, AvaloniaTabManager in Avalonia).
[Export(typeof(PluginBase))]
internal class MainHideShowToolsPlugin : CorePriorityPlugin
{
    private MenuItemModel _hideShowMenuItem = null!;
    private readonly HideShowToolsLogic _hideShowToolsLogic;

    [ImportingConstructor]
    public MainHideShowToolsPlugin(IToolsVisibility toolsVisibility)
    {
        _hideShowToolsLogic = new HideShowToolsLogic(toolsVisibility);
    }

    public override void StartUp()
    {
        _hideShowMenuItem = AddMenuEntry(HandleMenuItemClick, "View", "Hide Tools");
    }

    private void HandleMenuItemClick()
    {
        bool isVisible = _hideShowToolsLogic.ToggleToolsVisibility();

        _hideShowMenuItem.Header = isVisible ? "Hide Tools" : "Show Tools";
    }
}
