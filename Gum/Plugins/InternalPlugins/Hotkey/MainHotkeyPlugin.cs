using Gum.Menus;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.Hotkey.Views;
using System;
using System.ComponentModel.Composition;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;

namespace Gum.Plugins.InternalPlugins.Hotkey
{
    [Export(typeof(PluginBase))]
    public class MainHotkeyPlugin : PriorityPlugin
    {
        IPluginTab pluginTab;
        HotkeyView hotkeyView;
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
            hotkeyView = new Views.HotkeyView()
            {
                DataContext = _hotkeyViewModel
            };
            pluginTab = base.CreateTab(hotkeyView, "Hotkeys", TabLocation.CenterBottom);
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
