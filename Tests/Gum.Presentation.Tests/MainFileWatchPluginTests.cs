using System;
using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.BaseClasses;
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

    public static IEnumerable<object[]> EditsThatCanChangeWhatTheProjectReferences()
    {
        ComponentSave component = new ComponentSave { Name = "Card" };
        InstanceSave instance = new InstanceSave { Name = "Item" };
        StateSave state = new StateSave { Name = "Default" };
        yield return new object[] { "ElementAdd", (Action<PluginBase>)(p => p.CallElementAdd(component)) };
        yield return new object[] { "ElementDelete", (Action<PluginBase>)(p => p.CallElementDelete(component)) };
        yield return new object[] { "ElementImported", (Action<PluginBase>)(p => p.CallElementImported(component)) };
        yield return new object[] { "ElementDuplicate", (Action<PluginBase>)(p => p.CallElementDuplicate(component, component)) };
        yield return new object[] { "InstanceAdd", (Action<PluginBase>)(p => p.CallInstanceAdd(component, instance)) };
        yield return new object[] { "InstanceDelete", (Action<PluginBase>)(p => p.CallInstanceDelete(component, instance)) };
        yield return new object[] { "InstancesDelete", (Action<PluginBase>)(p => p.CallInstancesDelete(component, new[] { instance })) };
        yield return new object[] { "StateAdd", (Action<PluginBase>)(p => p.CallStateAdd(state)) };
        yield return new object[] { "CategoryAdd", (Action<PluginBase>)(p => p.CallStateCategoryAdd(new StateSaveCategory { Name = "Mood" })) };
        yield return new object[] { "StateDelete", (Action<PluginBase>)(p => p.CallStateDelete(state)) };
        yield return new object[] { "VariableAdd", (Action<PluginBase>)(p => p.CallVariableAdd(component, "Item.SourceFile")) };
        yield return new object[] { "VariableDelete", (Action<PluginBase>)(p => p.CallVariableDelete(component, "Item.SourceFile")) };
        yield return new object[] { "VariableSetThroughReference", (Action<PluginBase>)(p => p.CallVariableSetThroughReference(component, instance, "SourceFile", null)) };
        yield return new object[] { "AfterUndo", (Action<PluginBase>)(p => p.CallAfterUndo()) };
        yield return new object[] { "WireframeRefreshed", (Action<PluginBase>)(p => p.CallWireframeRefreshed()) };
    }

    [Theory]
    [MemberData(nameof(EditsThatCanChangeWhatTheProjectReferences))]
    public void AnEditThatCanChangeWhatTheProjectReferences_ForgetsWhichFilesItReferences(string edit, Action<PluginBase> raise)
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

        raise(plugin);

        fileWatchManager.Verify(m => m.InvalidateReferencedFiles(), Times.AtLeastOnce, edit);
    }

    private class SynchronousDispatcher : IDispatcher
    {
        public void Invoke(Action action) => action();
        public void Post(Action action) => action();
    }
}
