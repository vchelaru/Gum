using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.HideShowTools;

[Export(typeof(PluginBase))]
internal class MainHideShowToolsPlugin : InternalPlugin
{
    private ToolStripMenuItem menuItem;
    bool areToolsVisible = true;

    public override void StartUp()
    {
        menuItem = AddMenuItem("View", "Hide Tools");
        menuItem.Click += HandleMenuItemClick;
    }

    private void HandleMenuItemClick(object sender, EventArgs e)
    {
        if (areToolsVisible)
        {
            menuItem.Text = "Show Tools";
            _guiCommands.HideTools();
            areToolsVisible = false;

        }
        else
        {
            menuItem.Text = "Hide Tools";
            _guiCommands.ShowTools();
            areToolsVisible = true;
        }
    }
}
