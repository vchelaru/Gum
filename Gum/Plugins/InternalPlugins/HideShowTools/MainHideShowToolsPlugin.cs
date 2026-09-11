using Gum.Menus;
using Gum.Controls;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;

namespace Gum.Plugins.InternalPlugins.HideShowTools;

// As of ADR-0005 Phase 3, the toggle decision lives in HideShowToolsLogic (Gum.Presentation) so it
// can be unit tested headlessly. MainPanelViewModel itself is WPF-typed, so it's narrowed to
// IToolsVisibility for that logic.
[Export(typeof(PluginBase))]
internal class MainHideShowToolsPlugin : PriorityPlugin
{
    private MenuItemModel _hideShowMenuItem = null!;
    private readonly HideShowToolsLogic _hideShowToolsLogic;

    [ImportingConstructor]
    public MainHideShowToolsPlugin(MainPanelViewModel mainPanelViewModel)
    {
        _hideShowToolsLogic = new HideShowToolsLogic(mainPanelViewModel);
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
