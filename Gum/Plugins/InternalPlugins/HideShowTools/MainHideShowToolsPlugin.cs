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
    private ToolStripMenuItem _hideShowMenuItem;
    private readonly MainPanelViewModel _mainPanelViewModel;

    public MainHideShowToolsPlugin()
    {
        _mainPanelViewModel = Locator.GetRequiredService<MainPanelViewModel>();
    }
    
    public override void StartUp()
    {
        _hideShowMenuItem = AddMenuItem("View", "Hide Tools");
        _hideShowMenuItem.Click += HandleMenuItemClick;
    }

    private void HandleMenuItemClick(object sender, EventArgs e)
    {
        _mainPanelViewModel.IsToolsVisible = !_mainPanelViewModel.IsToolsVisible;

        if(_mainPanelViewModel.IsToolsVisible)
        {
            _mainPanelViewModel.EnsureMinimumWidth();
        }

        _hideShowMenuItem.Text = _mainPanelViewModel.IsToolsVisible ? "Hide Tools" : "Show Tools";
    }
}
