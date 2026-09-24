using System.Reflection;
using System.Text;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

[assembly: Gum.Avalonia.Tests.PluginFailureGuard]

namespace Gum.Avalonia.Tests;

/// <summary>
/// Fails the test that disables a guarded plugin. <see cref="PluginManager"/> catches a plugin's
/// exception, disables that plugin for the rest of the process and carries on, so without this a
/// headless test can drive a real gesture and pass while the plugin it means to exercise has not
/// handled an event since the first project load.
/// </summary>
/// <remarks>
/// Guards only the plugins whose views <see cref="ToolStartup"/> builds - a plugin that needs a
/// window the headless lifetime never opens is expected to fail here. Extend
/// <see cref="GuardedPluginTypeNames"/> as startup covers more of them.
/// </remarks>
public sealed class PluginFailureGuardAttribute : BeforeAfterTestAttribute
{
    private static readonly string[] GuardedPluginTypeNames =
    [
        "MainTreeViewPlugin",
        "MainVariableGridPlugin",
        "MainErrorsPlugin",
    ];

    private readonly HashSet<string> _failedBefore = new();

    /// <summary>Records which guarded plugins an earlier test already disabled.</summary>
    public override void Before(MethodInfo methodUnderTest)
    {
        foreach ((string name, _) in FailedGuardedPlugins())
        {
            _failedBefore.Add(name);
        }
    }

    /// <summary>Throws if this test's body disabled a guarded plugin, with the plugin's exception.</summary>
    public override void After(MethodInfo methodUnderTest)
    {
        StringBuilder message = new();
        foreach ((string name, PluginContainer container) in FailedGuardedPlugins())
        {
            if (!_failedBefore.Add(name))
            {
                continue;
            }
            message.AppendLine(
                $"{methodUnderTest.Name} disabled the {name} plugin ({container.FailureDetails}). " +
                "Every later test in this run goes through PluginManager without it.");
            message.AppendLine(container.FailureException?.ToString());
        }

        if (message.Length > 0)
        {
            throw new InvalidOperationException(message.ToString());
        }
    }

    private static IEnumerable<(string Name, PluginContainer Container)> FailedGuardedPlugins()
    {
        // The container is built once per assembly, so a test that runs before any plugin loads
        // simply sees none.
        PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();
        foreach (PluginContainer container in pluginManager.PluginContainers.Values)
        {
            string name = container.Plugin.GetType().Name;
            if (!container.IsEnabled && GuardedPluginTypeNames.Contains(name))
            {
                yield return (name, container);
            }
        }
    }
}
