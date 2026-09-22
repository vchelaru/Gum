using System.Linq;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.Hotkey;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.Hotkey;

/// <summary>Ctrl+/ and the "View Hotkeys" menu item both bring the Hotkeys tab to the foreground.</summary>
public class MainHotkeyPluginTests
{
    private readonly Mock<IPluginTab> _pluginTab = new();
    private readonly MainHotkeyPlugin _plugin;

    public MainHotkeyPluginTests()
    {
        _pluginTab.SetupProperty(t => t.IsVisible, false);
        _pluginTab.SetupProperty(t => t.IsSelected, false);

        Mock<ITabManager> tabManager = new();
        tabManager.Setup(t => t.AddControl(It.IsAny<object>(), "Hotkeys", It.IsAny<TabLocation>()))
            .Returns(_pluginTab.Object);

        HotkeyViewModel viewModel = new(Mock.Of<IHotkeyManager>(), Mock.Of<IKeyCombinationFormatter>());

        _plugin = new MainHotkeyPlugin(viewModel)
        {
            TabManager = tabManager.Object,
            Menu = new MenuModel(),
        };
        _plugin.StartUp();
    }

    [Fact]
    public void ShowHotkeysRequested_ShowsAndSelectsTheTab()
    {
        // Simulates the app-wide Ctrl+/ hotkey, which broadcasts through PluginManager.ShowHotkeys().
        _plugin.CallShowHotkeys();

        _pluginTab.Object.IsVisible.ShouldBeTrue();
        _pluginTab.Object.IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void ViewHotkeysMenuItem_Click_ShowsAndSelectsTheTab()
    {
        MenuItemModel menuItem = _plugin.Menu!.GetItem("View")!.Items.Single(i => i.Header == "View Hotkeys");

        menuItem.Invoke();

        _pluginTab.Object.IsVisible.ShouldBeTrue();
        _pluginTab.Object.IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void ViewHotkeysMenuItem_ClickWhileVisible_HidesTheTab()
    {
        MenuItemModel menuItem = _plugin.Menu!.GetItem("View")!.Items.Single(i => i.Header == "View Hotkeys");
        menuItem.Invoke();

        menuItem.Invoke();

        _pluginTab.Object.IsVisible.ShouldBeFalse();
    }
}
