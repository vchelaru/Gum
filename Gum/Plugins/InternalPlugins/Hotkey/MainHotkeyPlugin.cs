using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.Hotkey.Views;
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.Hotkey
{
    [Export(typeof(PluginBase))]
    public class MainHotkeyPlugin : InternalPlugin
    {
        PluginTab pluginTab;
        HotkeyView hotkeyView;
        ToolStripMenuItem menuItem;

        public override void StartUp()
        {
            menuItem = this.AddMenuItemTo("View Hotkeys", HandleToggleTabVisibility, "View");
            hotkeyView = new Views.HotkeyView();
            pluginTab = base.CreateTab(hotkeyView, "Hotkeys", TabLocation.CenterBottom);
            pluginTab.TabShown += HandleTabShown;
            pluginTab.TabHidden += HandleTabHidden;
            pluginTab.CanClose = true;
        }

        private void HandleTabShown()
        {
            menuItem.Text = "Hide Hotkeys";
        }

        private void HandleTabHidden()
        {
            menuItem.Text = "View Hotkeys";
        }


        private void HandleToggleTabVisibility(object sender, EventArgs e)
        {
            if(!_guiCommands.IsTabVisible(pluginTab))
            {
                pluginTab.Show();
            } 
            else
            {
                pluginTab.Hide();


            }
        }
    }
}
