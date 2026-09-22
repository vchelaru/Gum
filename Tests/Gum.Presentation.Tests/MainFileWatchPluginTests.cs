using System;
using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.FileWatchPlugin;
using Gum.Services;
using Gum.ToolStates;
using Gum.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// The File Watch tab's display-refresh timer. It is a thread-pool timer that hops to the UI thread
/// every 200 ms, and it only feeds the tab's own display, so it must run while the tab is shown and
/// not otherwise: left running with the tab hidden, its pool thread touches the UI dispatcher for
/// nothing, and in the headless test host that races the per-test dispatcher reset (a test's
/// windows then never render or hit-test).
/// </summary>
public class MainFileWatchPluginTests
{
    [Fact]
    public void TheRefreshTimer_RunsOnlyWhileTheFileWatchTabIsShown()
    {
        Mock<IPluginTab> tab = new Mock<IPluginTab>();
        tab.SetupAllProperties();
        Mock<ITabManager> tabManager = new Mock<ITabManager>();
        tabManager.Setup(manager => manager.AddControl(It.IsAny<object>(), "File Watch", It.IsAny<TabLocation>())).Returns(tab.Object);
        Mock<IFileWatchManager> fileWatchManager = new Mock<IFileWatchManager>();
        FileWatchLogic logic = new FileWatchLogic(fileWatchManager.Object, Mock.Of<IGuiCommands>(), Mock.Of<IProjectState>(), Mock.Of<IProjectManager>(), new SynchronousDispatcher());
        using PeriodicUiTimer timer = new PeriodicUiTimer(new SynchronousDispatcher(), Mock.Of<ILogger<PeriodicUiTimer>>());
        MainFileWatchPlugin plugin = new MainFileWatchPlugin(fileWatchManager.Object, logic, timer)
        {
            TabManager = tabManager.Object,
            Menu = new MenuModel(),
        };

        plugin.StartUp();

        timer.Enabled.ShouldBeFalse("the tab starts hidden, so nothing shows what the timer refreshes");

        tab.Raise(t => t.TabShown += null);

        timer.Enabled.ShouldBeTrue();

        tab.Raise(t => t.TabHidden += null);

        timer.Enabled.ShouldBeFalse();
    }

    private class SynchronousDispatcher : IDispatcher
    {
        public void Invoke(Action action) => action();
        public void Post(Action action) => action();
    }
}
