using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            }
            pluginTab.Focus();

        }
    }
}
