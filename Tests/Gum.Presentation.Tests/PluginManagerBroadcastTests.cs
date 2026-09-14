using System.Collections.Generic;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class PluginManagerBroadcastTests
{
    [Fact]
    public void Broadcast_SkipsPluginsThatHaveNotStartedYet()
    {
        // A plugin's StartUp can pump the UI message loop (the Avalonia head's canvas creates its
        // graphics device there), so a pointer event can broadcast before later plugins have started.
        PluginManager manager = new PluginManager(
            new Mock<IPluginEnablementStore>().Object,
            new Mock<IPluginHostConfiguration>().Object);
        CountingPlugin started = new CountingPlugin();
        CountingPlugin notYetStarted = new CountingPlugin();
        manager.Plugins = new List<PluginBase> { started, notYetStarted };
        PluginManager.StartupPlugin(started, manager);

        manager.FocusSearch();

        started.FocusSearchCount.ShouldBe(1);
        notYetStarted.FocusSearchCount.ShouldBe(0);
    }

    private sealed class CountingPlugin : PluginBase
    {
        public int FocusSearchCount { get; private set; }

        public override string FriendlyName => "Counting";

        public override void StartUp() => FocusSearch += () => FocusSearchCount++;

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
