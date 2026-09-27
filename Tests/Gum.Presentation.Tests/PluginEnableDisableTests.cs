using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin;
using Gum.Services.Dialogs;
using Gum.Settings;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Turning a plugin off and on in Manage Plugins (<see cref="PluginManager.DisableUserPlugin"/> /
/// <see cref="PluginManager.TryEnablePlugin"/>) must leave it as it started: StartUp runs once per
/// instance, and the tabs and menu entries it added are hidden and disabled while it is off.
/// </summary>
public class PluginEnableDisableTests
{
    [Fact]
    public void DisableUserPlugin_DisablesItsMenuEntriesUntilReenabled()
    {
        MenuModel menu = new MenuModel();
        TogglePlugin plugin = new TogglePlugin { Menu = menu };
        plugin.AddEntriesOnStartUp = (p) =>
        {
            // A one-part path returns the top-level menu, which other plugins share.
            p.AddMenuEntry(() => { }, "Content");
            p.AddMenuEntry(() => { }, "Content", "Import", "Enabled Item");
            MenuItemModel selfDisabled = p.AddMenuEntry(() => { }, "Content", "Import", "Self Disabled Item");
            selfDisabled.IsEnabled = false;
        };
        (PluginManager manager, PluginContainer container) = StartUp(plugin, isDisabledInStore: false);

        manager.DisableUserPlugin(container);
        // A second turn-off must not record the already-disabled state as the plugin's own.
        container.IsEnabled = false;

        MenuItemModel enabledItem = FindItem(menu, "Enabled Item");
        MenuItemModel selfDisabledItem = FindItem(menu, "Self Disabled Item");
        enabledItem.IsEnabled.ShouldBeFalse();
        selfDisabledItem.IsEnabled.ShouldBeFalse();
        menu.GetItem("Content")!.IsEnabled.ShouldBeTrue();

        manager.TryEnablePlugin(container);

        enabledItem.IsEnabled.ShouldBeTrue();
        selfDisabledItem.IsEnabled.ShouldBeFalse("re-enabling restores the state the plugin itself set");
    }

    [Fact]
    public void DisableUserPlugin_HidesItsTabsUntilReenabled()
    {
        FakeTabManager tabManager = new FakeTabManager();
        TogglePlugin plugin = new TogglePlugin { Menu = new MenuModel(), TabManager = tabManager };
        FakeTab? visibleTab = null;
        FakeTab? hiddenTab = null;
        FakeTab? constructorManagerTab = null;
        FakeTab? removedTab = null;
        plugin.AddEntriesOnStartUp = (p) =>
        {
            TogglePlugin toggle = (TogglePlugin)p;
            visibleTab = (FakeTab)toggle.AddTabThroughInjectedField("Visible");
            hiddenTab = (FakeTab)toggle.AddTabThroughInjectedField("Hidden");
            hiddenTab.Hide();
            // Texture Coordinates receives its tab manager through its constructor instead.
            constructorManagerTab = (FakeTab)toggle.TrackTabs(tabManager).AddControl(new object(), "From constructor", TabLocation.RightBottom);
            removedTab = (FakeTab)toggle.AddTabThroughInjectedField("Removed");
            toggle.RemoveTab(removedTab);
        };
        (PluginManager manager, PluginContainer container) = StartUp(plugin, isDisabledInStore: false);

        manager.DisableUserPlugin(container);

        visibleTab!.IsVisible.ShouldBeFalse();
        hiddenTab!.IsVisible.ShouldBeFalse();
        constructorManagerTab!.IsVisible.ShouldBeFalse();
        removedTab!.IsVisible.ShouldBeTrue("a tab the plugin removed is no longer its to hide");

        manager.TryEnablePlugin(container);

        visibleTab.IsVisible.ShouldBeTrue();
        constructorManagerTab.IsVisible.ShouldBeTrue();
        hiddenTab.IsVisible.ShouldBeFalse("re-enabling restores the visibility the plugin itself set");
    }

    [Fact]
    public void DisableUserPlugin_LeavesAMenuEntryThePluginRemovedFromTheMenu()
    {
        MenuModel menu = new MenuModel();
        TogglePlugin plugin = new TogglePlugin { Menu = menu };
        MenuItemModel? removed = null;
        plugin.AddEntriesOnStartUp = (p) =>
        {
            // The Forms plugin removes its item and adds a new one on each project load.
            removed = p.AddMenuEntry(() => { }, "Content", "Add Forms Components");
            menu.GetItem("Content")!.Items.Remove(removed);
            p.AddMenuEntry(() => { }, "Content", "Add Forms Components");
        };
        (PluginManager manager, PluginContainer container) = StartUp(plugin, isDisabledInStore: false);

        manager.DisableUserPlugin(container);

        removed!.IsEnabled.ShouldBeTrue("an item that left the menu is no longer tracked");
        FindItem(menu, "Add Forms Components").IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Fail_LoadRecent_DisablesItsFileMenuItem()
    {
        MenuModel menu = new MenuModel();
        menu.AddMenuItem(new[] { "File", "New Project" });
        menu.AddMenuItem(new[] { "File", "Load Project..." });
        menu.GetItem("File")!.Items.Add(MenuItemModel.Separator());
        Mock<IProjectManager> projectManager = new Mock<IProjectManager>();
        projectManager.Setup(p => p.RecentProjects).Returns(new List<RecentProjectReference>());
        MainRecentFilesPlugin plugin = new MainRecentFilesPlugin(projectManager.Object, new Mock<IFileCommands>().Object, new Mock<IDialogService>().Object)
        {
            Menu = menu,
        };
        // Load Recent refuses a user shutdown, so a failure is how it gets turned off.
        (_, PluginContainer container) = StartUp(plugin, isDisabledInStore: false);
        MenuItemModel loadRecent = menu.GetItem("File")!.Items[2];
        loadRecent.Header.ShouldBe("Load Recent", "Load Recent sits just after Load Project...");

        container.Fail(new InvalidOperationException("handler threw"), "Failed in ProjectLoad");

        loadRecent.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Fail_DisablesTheFailedPluginsMenuEntries()
    {
        MenuModel menu = new MenuModel();
        TogglePlugin plugin = new TogglePlugin { Menu = menu };
        plugin.AddEntriesOnStartUp = (p) => p.AddMenuEntry(() => { }, "Content", "Import", "HTML…");
        (_, PluginContainer container) = StartUp(plugin, isDisabledInStore: false);

        container.Fail(new InvalidOperationException("handler threw"), "Failed in ProjectLoad");

        FindItem(menu, "HTML…").IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void TryEnablePlugin_AfterDisable_DoesNotRunStartUpAgain()
    {
        MenuModel menu = new MenuModel();
        TogglePlugin plugin = new TogglePlugin { Menu = menu };
        plugin.AddEntriesOnStartUp = (p) => p.AddMenuEntry(() => { }, "Content", "Import", "HTML…");
        (PluginManager manager, PluginContainer container) = StartUp(plugin, isDisabledInStore: false);

        manager.DisableUserPlugin(container);
        PluginSummary summary = manager.TryEnablePlugin(container);

        summary.IsEnabled.ShouldBeTrue();
        plugin.StartUpCount.ShouldBe(1);
        MenuItemModel import = menu.GetItem("Content")!.Items.Single(item => item.Header == "Import");
        import.Items.Count(item => item.Header == "HTML…").ShouldBe(1);
    }

    [Fact]
    public void TryEnablePlugin_ForPluginDisabledAtLoad_RunsStartUpOnce()
    {
        TogglePlugin plugin = new TogglePlugin { Menu = new MenuModel() };
        (PluginManager manager, PluginContainer container) = StartUp(plugin, isDisabledInStore: true);
        plugin.StartUpCount.ShouldBe(0);

        manager.TryEnablePlugin(container);
        manager.DisableUserPlugin(container);
        manager.TryEnablePlugin(container);

        plugin.StartUpCount.ShouldBe(1);
    }

    private static (PluginManager, PluginContainer) StartUp(PluginBase plugin, bool isDisabledInStore)
    {
        Mock<IPluginEnablementStore> store = new Mock<IPluginEnablementStore>();
        store.Setup(s => s.IsDisabled(It.IsAny<string>())).Returns(isDisabledInStore);
        PluginManager manager = new PluginManager(store.Object, new Mock<IPluginHostConfiguration>().Object);
        manager.Plugins = new List<PluginBase> { plugin };
        PluginManager.StartupPlugin(plugin, manager);
        return (manager, manager.PluginContainers[plugin]);
    }

    private static MenuItemModel FindItem(MenuModel menu, string header) =>
        Flatten(menu.TopLevelItems).Single(item => item.Header == header);

    private static IEnumerable<MenuItemModel> Flatten(IEnumerable<MenuItemModel> items) =>
        items.SelectMany(item => new[] { item }.Concat(Flatten(item.Items)));

    private sealed class TogglePlugin : PluginBase
    {
        public int StartUpCount { get; private set; }

        public Action<PluginBase>? AddEntriesOnStartUp { get; set; }

        public override string FriendlyName => "Toggle";

        public override void StartUp()
        {
            StartUpCount++;
            AddEntriesOnStartUp?.Invoke(this);
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

        public IPluginTab AddTabThroughInjectedField(string title) =>
            _tabManager.AddControl(new object(), title, TabLocation.RightBottom);

        public ITabManager TrackTabs(ITabManager tabManager) => TrackTabsFrom(tabManager);
    }

    private sealed class FakeTabManager : ITabManager
    {
        public IPluginTab AddControl(object element, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom) =>
            new FakeTab { Title = tabTitle, Location = tabLocation, IsVisible = true };

        public void RemoveTab(IPluginTab plugin) { }
    }

    private sealed class FakeTab : IPluginTab
    {
        public TabLocation Location { get; set; }
        public string Title { get; set; } = "";
        public bool IsVisible { get; set; }
        public bool IsSelected { get; set; }
        public bool CanClose { get; set; }

        public event Action? TabShown { add { } remove { } }
        public event Action? TabHidden { add { } remove { } }
        public event Action? GotFocus { add { } remove { } }

        public void Show() => IsVisible = true;
        public void Hide() => IsVisible = false;
    }
}
