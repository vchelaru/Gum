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

    /// <summary>
    /// A change applied through a variable reference reaches a plugin's VariableSetThroughReference
    /// handler when it has one, and its VariableSet handler otherwise, so plugins that never opted in
    /// see the same VariableSet as before (#5541). VariableSetLate fires for both.
    /// </summary>
    [Fact]
    public void VariableSetThroughReference_GoesToVariableSetUnlessThePluginHandlesItSeparately()
    {
        PluginManager manager = new PluginManager(
            new Mock<IPluginEnablementStore>().Object,
            new Mock<IPluginHostConfiguration>().Object);
        VariableSetPlugin plain = new VariableSetPlugin(handlesThroughReference: false);
        VariableSetPlugin optedIn = new VariableSetPlugin(handlesThroughReference: true);
        manager.Plugins = new List<PluginBase> { plain, optedIn };
        PluginManager.StartupPlugin(plain, manager);
        PluginManager.StartupPlugin(optedIn, manager);

        manager.VariableSetThroughReference(new Gum.DataTypes.ComponentSave(), null, "Red", 0, true);

        plain.VariableSetCount.ShouldBe(1);
        plain.ThroughReferenceCount.ShouldBe(0);
        optedIn.VariableSetCount.ShouldBe(0);
        optedIn.ThroughReferenceCount.ShouldBe(1);
        plain.LateCount.ShouldBe(1);
        optedIn.LateCount.ShouldBe(1);
    }

    private sealed class VariableSetPlugin : PluginBase
    {
        private readonly bool _handlesThroughReference;

        public VariableSetPlugin(bool handlesThroughReference)
        {
            _handlesThroughReference = handlesThroughReference;
        }

        public int VariableSetCount { get; private set; }
        public int ThroughReferenceCount { get; private set; }
        public int LateCount { get; private set; }

        public override string FriendlyName => "VariableSetCounting";

        public override void StartUp()
        {
            VariableSet += (_, _, _, _, _) => VariableSetCount++;
            VariableSetLate += (_, _, _, _, _) => LateCount++;
            if (_handlesThroughReference)
            {
                VariableSetThroughReference += (_, _, _, _, _) => ThroughReferenceCount++;
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }

    private sealed class CountingPlugin : PluginBase
    {
        public int FocusSearchCount { get; private set; }

        public override string FriendlyName => "Counting";

        public override void StartUp() => FocusSearch += () => FocusSearchCount++;

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
