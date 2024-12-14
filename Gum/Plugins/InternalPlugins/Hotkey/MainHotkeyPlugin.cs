using Gum.Plugins.BaseClasses;
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.Hotkey
{
    [Export(typeof(PluginBase))]
    public class MainHotkeyPlugin : InternalPlugin
    {
        PluginTab pluginTab;

        public override void StartUp()
        {
            this.AddMenuItemTo("View Hotkeys", HandleViewHotkeys, "View");
        }

        private void HandleViewHotkeys(object sender, EventArgs e)
        {
            if(pluginTab == null)
            {
                var view = new Views.HotkeyView();
                pluginTab = base.AddControl(view, "Hotkeys", TabLocation.CenterBottom);
                pluginTab.Focus();

                ToolStripMenuItem viewMenuItem = this.GetChildMenuItem("View", "View Hotkeys");
                if (viewMenuItem != null)
                {
                    viewMenuItem.Text = "Hide Hotkeys";
                }
            } 
            else
            {
                ToolStripMenuItem viewMenuItem = this.GetChildMenuItem("View", "Hide Hotkeys");
                if (viewMenuItem != null)
                {
                    viewMenuItem.Text = "View Hotkeys";
                    System.Windows.Controls.UserControl panelControl = (System.Windows.Controls.UserControl)pluginTab.Page.Content;
                    base.RemoveControl(panelControl);
                    pluginTab = null;
                }
            }
        }
    }
}
