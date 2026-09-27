using System.Reflection;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

public class PluginFailureGuardTests
{
    [AvaloniaFact]
    public void After_APluginTheTestTurnedOff_IsOnAgainWithoutRunningStartUpAgain()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();
        PluginContainer container = pluginManager.PluginContainers.Values
            .Single(candidate => candidate.Name.StartsWith("State Animation Plugin", StringComparison.Ordinal));
        MethodInfo method = typeof(PluginFailureGuardTests).GetMethod(nameof(After_APluginTheTestTurnedOff_IsOnAgainWithoutRunningStartUpAgain))!;
        PluginFailureGuardAttribute guard = new PluginFailureGuardAttribute();

        guard.Before(method);
        pluginManager.DisableUserPlugin(container);
        guard.After(method);

        PluginContainer restored = pluginManager.PluginContainers[container.Plugin];
        restored.IsEnabled.ShouldBeTrue();
        restored.HasStartedUp.ShouldBeTrue("a later off/on must not run StartUp on the same instance again");
    }
}
