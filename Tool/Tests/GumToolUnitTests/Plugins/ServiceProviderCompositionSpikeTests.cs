using System;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace GumToolUnitTests.Plugins;

/// <summary>
/// Companion to <see cref="AllPluginsCompositionTests"/> that exercises the OTHER half of plugin startup:
/// the real dependency-injection graph in <c>Gum.Services.Builder.cs</c> (<c>AddGum</c>).
/// <see cref="AllPluginsCompositionTests"/> stubs the bridged services so it can run headlessly; this builds
/// them for real and resolves the full <see cref="PluginBridgedServiceTypes"/> set, so a missing
/// registration or a construction cycle introduced by an upcoming service drain fails here rather than at
/// the tool's startup.
///
/// Spike result (the headless-boundary finding the task asked to establish and document): the entire
/// bridged set resolves on the MTA test thread with no STA thread and no live WPF <c>Application</c> —
/// including the WinForms/WPF-coupled host singletons (<c>ElementTreeViewManager</c>,
/// <c>PropertyGridManager</c>, <c>MainPanelViewModel</c>, <c>MainOutputViewModel</c>, <c>HotkeyViewModel</c>),
/// because each defers its control creation to the <c>Initialize()</c>/<c>StartUp()</c> second stage rather
/// than its DI constructor. The STA boundary therefore sits ABOVE this set, at the genuine WPF roots
/// (<c>MainWindow</c>, theming/dispatcher usage) which this spike intentionally does not resolve. If a future
/// drain makes a bridged service require STA/WPF at construction time, this test turns red — which is the
/// correct signal, since it would also break headless tooling (CLI, codegen) and CI.
/// </summary>
public class ServiceProviderCompositionSpikeTests
{
    [Fact]
    public void RealContainer_ResolvesEveryBridgedService()
    {
        using IHost host = GumBuilder.CreateHostBuilder().Build();
        IServiceProvider serviceProvider = host.Services;

        foreach (Type serviceType in PluginBridgedServiceTypes.All)
        {
            serviceProvider.GetService(serviceType)
                .ShouldNotBeNull($"{serviceType.Name} should resolve from the real Builder.cs container");
        }
    }
}
