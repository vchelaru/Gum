using System.Linq;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins.InternalPlugins.Hotkey;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using Gum.SettingsWindow;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.Hotkey;

/// <summary>The hotkey list is the Keyboard shortcuts page of the Settings window; Ctrl+/ opens it there.</summary>
public class MainHotkeyPluginTests
{
    private readonly SettingsPageRegistry _registry;
    private readonly Mock<ISettingsWindowService> _settingsWindow;
    private readonly MenuModel _menu;
    private readonly MainHotkeyPlugin _plugin;

    public MainHotkeyPluginTests()
    {
        _registry = new SettingsPageRegistry();
        _settingsWindow = new Mock<ISettingsWindowService>();
        _menu = new MenuModel();

        HotkeyViewModel viewModel = new(Mock.Of<IHotkeyManager>(), Mock.Of<IKeyCombinationFormatter>());

        _plugin = new MainHotkeyPlugin(viewModel)
        {
            Menu = _menu,
            SettingsPageRegistry = _registry,
            SettingsWindowService = _settingsWindow.Object,
        };
        _plugin.StartUp();
    }

    [Fact]
    public void StartUp_RegistersAKeyboardShortcutsPageInTheApplicationSection()
    {
        SettingsPage page = _registry.Pages.ShouldHaveSingleItem();

        page.Id.ShouldBe(SettingsPageIds.KeyboardShortcuts);
        page.Title.ShouldBe("Keyboard shortcuts");
        page.Section.ShouldBe(SettingsSection.Application);
        page.Content.ShouldBeOfType<HotkeyViewModel>();
    }

    [Fact]
    public void StartUp_AddsNoViewHotkeysMenuItem()
    {
        _menu.GetItem("View").ShouldBeNull();
    }

    [Fact]
    public void ShowHotkeysRequested_OpensTheSettingsWindowOnTheKeyboardShortcutsPage()
    {
        // Simulates the app-wide Ctrl+/ hotkey, which broadcasts through PluginManager.ShowHotkeys().
        _plugin.CallShowHotkeys();

        _settingsWindow.Verify(x => x.Show(SettingsPageIds.KeyboardShortcuts), Times.Once);
    }
}
