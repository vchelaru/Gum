using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Turning a plugin off and on in Manage Plugins (<see cref="PluginManager.DisableUserPlugin"/> /
/// <see cref="PluginManager.TryEnablePlugin"/>) must leave it as it started: StartUp runs once per
/// instance, and the menu entries it added are disabled while it is off.
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

    private static (PluginManager, PluginContainer) StartUp(TogglePlugin plugin, bool isDisabledInStore)
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
    }
}
