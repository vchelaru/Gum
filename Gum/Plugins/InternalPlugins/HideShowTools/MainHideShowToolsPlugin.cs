using Gum.Controls;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace Gum.Plugins.InternalPlugins.HideShowTools;

// As of ADR-0005 Phase 3, the toggle decision lives in HideShowToolsLogic (Gum.Presentation) so it
// can be unit tested headlessly. MainPanelViewModel itself is WPF-typed, so it's narrowed to
// IToolsVisibility for that logic.
[Export(typeof(PluginBase))]
internal class MainHideShowToolsPlugin : PriorityPlugin
{
    private MenuItem _hideShowMenuItem;
    private readonly HideShowToolsLogic _hideShowToolsLogic;

    [ImportingConstructor]
    public MainHideShowToolsPlugin(MainPanelViewModel mainPanelViewModel)
    {
        _hideShowToolsLogic = new HideShowToolsLogic(mainPanelViewModel);
    }

    public override void StartUp()
    {
        _hideShowMenuItem = AddMenuItem("View", "Hide Tools");
        _hideShowMenuItem.Click += HandleMenuItemClick;
    }

    private void HandleMenuItemClick(object? sender, System.Windows.RoutedEventArgs e)
    {
        bool isVisible = _hideShowToolsLogic.ToggleToolsVisibility();

        _hideShowMenuItem.Header = isVisible ? "Hide Tools" : "Show Tools";
    }
}
