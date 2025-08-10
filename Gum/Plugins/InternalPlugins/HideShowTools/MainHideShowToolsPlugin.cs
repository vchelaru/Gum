using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gum.Controls;
using Gum.Services;

namespace Gum.Plugins.InternalPlugins.HideShowTools;

[Export(typeof(PluginBase))]
internal class MainHideShowToolsPlugin : InternalPlugin
{
    private ToolStripMenuItem menuItem;
    private readonly MainPanelViewModel _mainPanelViewModel;

    public MainHideShowToolsPlugin()
    {
        _mainPanelViewModel = Locator.GetRequiredService<MainPanelViewModel>();
    }
    
    public override void StartUp()
    {
        menuItem = AddMenuItem("View", "Hide Tools");
        menuItem.Click += HandleMenuItemClick;
    }

    private void HandleMenuItemClick(object sender, EventArgs e)
    {
        _mainPanelViewModel.IsToolsVisible = !_mainPanelViewModel.IsToolsVisible;
        menuItem.Text = _mainPanelViewModel.IsToolsVisible ? "Hide Tools" : "Show Tools";
    }
}
