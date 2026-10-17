using Gum.Plugins.BaseClasses;
using Gum.SettingsWindow;
using System.ComponentModel.Composition;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;

namespace Gum.Plugins.InternalPlugins.Hotkey
{
    /// <summary>
    /// The Keyboard shortcuts page of the Settings window, and the app-wide shortcut (Ctrl+/) that
    /// opens the window on it.
    /// </summary>
    [Export(typeof(PluginBase))]
    public class MainHotkeyPlugin : CorePriorityPlugin
    {
        private readonly HotkeyViewModel _hotkeyViewModel;

        [ImportingConstructor]
        public MainHotkeyPlugin(HotkeyViewModel hotkeyViewModel)
        {
            _hotkeyViewModel = hotkeyViewModel;
        }

        public override void StartUp()
        {
            // Each head resolves the view model to its own view (its settings page view registry).
            AddSettingsPage(new SettingsPage(
                SettingsPageIds.KeyboardShortcuts, "Keyboard shortcuts", SettingsSection.Application, _hotkeyViewModel, order: 2)
            {
                Keywords = new[] { "hotkeys", "keys", "bindings" },
            });

            this.ShowHotkeys += HandleShowHotkeysRequested;
        }

        private void HandleShowHotkeysRequested()
        {
            ShowSettingsWindow(SettingsPageIds.KeyboardShortcuts);
        }
    }
}
