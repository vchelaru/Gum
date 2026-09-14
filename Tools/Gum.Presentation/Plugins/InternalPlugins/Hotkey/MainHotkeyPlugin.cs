using Gum.Menus;
using Gum.Plugins.BaseClasses;
using System;
using System.ComponentModel.Composition;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;

namespace Gum.Plugins.InternalPlugins.Hotkey
{
    [Export(typeof(PluginBase))]
    public class MainHotkeyPlugin : CorePriorityPlugin
    {
        IPluginTab pluginTab = null!;
        MenuItemModel menuItem = null!;
        private readonly HotkeyViewModel _hotkeyViewModel;

        [ImportingConstructor]
        public MainHotkeyPlugin(HotkeyViewModel hotkeyViewModel)
        {
            _hotkeyViewModel = hotkeyViewModel;
        }

        public override void StartUp()
        {
            menuItem = AddMenuEntry(HandleToggleTabVisibility, "View", "View Hotkeys");
            // Each head resolves the view model to its own view (TabViewRegistry).
            pluginTab = base.CreateTab(_hotkeyViewModel, "Hotkeys", TabLocation.CenterBottom);
            pluginTab.TabShown += HandleTabShown;
            pluginTab.TabHidden += HandleTabHidden;
            pluginTab.CanClose = true;
        }

        private void HandleTabShown()
        {
            menuItem.Header = "Hide Hotkeys";
        }

        private void HandleTabHidden()
        {
            menuItem.Header = "View Hotkeys";
        }


        private void HandleToggleTabVisibility()
        {
            pluginTab.IsVisible = !pluginTab.IsVisible;
        }
    }
}
