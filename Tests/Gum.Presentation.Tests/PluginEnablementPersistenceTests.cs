using System;
using System.Collections.Generic;
using System.IO;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The plugin enablement store records what actually happened to a plugin, so the next launch
/// starts the same plugins the user saw running when they closed the tool.
/// </summary>
public class PluginEnablementPersistenceTests : IDisposable
{
    private readonly string _fileName = Path.Combine(Path.GetTempPath(), $"GumPluginSettings_{Guid.NewGuid():N}.xml");

    public void Dispose()
    {
        if (File.Exists(_fileName))
        {
            File.Delete(_fileName);
        }
    }

    [Fact]
    public void DisableUserPlugin_PluginThatRefusesShutDown_StaysEnabledAndIsNotRecordedDisabled()
    {
        TestPlugin plugin = new TestPlugin { AcceptsShutDown = false };
        (PluginManager manager, PluginContainer container) = StartUp(plugin, new PluginEnablementStore(_fileName));

        PluginSummary summary = manager.DisableUserPlugin(container);

        summary.IsEnabled.ShouldBeTrue();
        Reload().IsDisabled(plugin.UniqueId).ShouldBeFalse("the plugin never turned off, so the next launch must start it");
    }

    [Fact]
    public void DisableUserPlugin_PluginThatShutsDown_IsRecordedDisabled()
    {
        TestPlugin plugin = new TestPlugin { AcceptsShutDown = true };
        (PluginManager manager, PluginContainer container) = StartUp(plugin, new PluginEnablementStore(_fileName));

        PluginSummary summary = manager.DisableUserPlugin(container);

        summary.IsEnabled.ShouldBeFalse();
        Reload().IsDisabled(plugin.UniqueId).ShouldBeTrue();
    }

    [Fact]
    public void DisableUserPlugin_PluginThatCannotBeUserDisabled_IsNotAskedToShutDown()
    {
        TestPlugin plugin = new TestPlugin { AcceptsShutDown = true, AllowsUserDisable = false };
        (PluginManager manager, PluginContainer container) = StartUp(plugin, new PluginEnablementStore(_fileName));

        PluginSummary summary = manager.DisableUserPlugin(container);

        summary.IsEnabled.ShouldBeTrue();
        summary.CanBeDisabled.ShouldBeFalse();
        plugin.ShutDownCount.ShouldBe(0);
        Reload().IsDisabled(plugin.UniqueId).ShouldBeFalse();
    }

    [Fact]
    public void PluginThatCannotBeUserDisabled_KeepsItsMenuEntriesWhenUnchecked_ButLosesThemWhenItFails()
    {
        TestPlugin plugin = new TestPlugin { AllowsUserDisable = false };
        (PluginManager manager, PluginContainer container) = StartUp(plugin, new PluginEnablementStore(_fileName));
        MenuItemModel entry = plugin.AddMenuEntry(() => { }, "Content", "Core Item");

        manager.DisableUserPlugin(container);
        entry.IsEnabled.ShouldBeTrue("unchecking a plugin that cannot be turned off must not suspend it");

        container.Fail(new InvalidOperationException("handler threw"), "Failed in ProjectLoad");
        entry.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void StartupPlugin_StaleDisabledEntryForPluginThatCannotBeUserDisabled_StartsItAndClearsTheEntry()
    {
        TestPlugin plugin = new TestPlugin { AllowsUserDisable = false };
        PluginEnablementStore stale = new PluginEnablementStore(_fileName);
        stale.Load();
        stale.Disable(typeof(TestPlugin).FullName!);

        PluginEnablementStore store = new PluginEnablementStore(_fileName);
        store.Load();
        (_, PluginContainer container) = StartUp(plugin, store);

        container.IsEnabled.ShouldBeTrue();
        plugin.StartUpCount.ShouldBe(1);
        Reload().IsDisabled(plugin.UniqueId).ShouldBeFalse("a plugin that cannot be turned off must not stay recorded as off");
    }

    [Fact]
    public void TryEnablePlugin_StartUpThrows_KeepsThePluginRecordedDisabled()
    {
        TestPlugin plugin = new TestPlugin { ThrowOnStartUp = true };
        PluginEnablementStore disabled = new PluginEnablementStore(_fileName);
        disabled.Load();
        disabled.Disable(typeof(TestPlugin).FullName!);
        PluginEnablementStore store = new PluginEnablementStore(_fileName);
        store.Load();
        (PluginManager manager, PluginContainer container) = StartUp(plugin, store);

        PluginSummary summary = manager.TryEnablePlugin(container);

        summary.IsEnabled.ShouldBeFalse();
        Reload().IsDisabled(plugin.UniqueId).ShouldBeTrue("the plugin failed to start, so it is still off");
    }

    private PluginEnablementStore Reload()
    {
        PluginEnablementStore reloaded = new PluginEnablementStore(_fileName);
        reloaded.Load();
        return reloaded;
    }

    private static (PluginManager, PluginContainer) StartUp(TestPlugin plugin, IPluginEnablementStore store)
    {
        PluginManager manager = new PluginManager(store, new Mock<IPluginHostConfiguration>().Object);
        manager.Plugins = new List<PluginBase> { plugin };
        PluginManager.StartupPlugin(plugin, manager);
        return (manager, manager.PluginContainers[plugin]);
    }

    private sealed class TestPlugin : PluginBase
    {
        public TestPlugin()
        {
            Menu = new MenuModel();
        }

        public bool AcceptsShutDown { get; set; } = true;

        public bool AllowsUserDisable { get; set; } = true;

        public bool ThrowOnStartUp { get; set; }

        public int StartUpCount { get; private set; }

        public int ShutDownCount { get; private set; }

        public override string FriendlyName => "Test";

        public override bool CanUserDisable => AllowsUserDisable;

        public override void StartUp()
        {
            if (ThrowOnStartUp)
            {
                throw new InvalidOperationException("StartUp failed");
            }
            StartUpCount++;
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            ShutDownCount++;
            return AcceptsShutDown;
        }
    }
}
