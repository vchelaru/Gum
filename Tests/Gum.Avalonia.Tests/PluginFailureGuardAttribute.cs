using System.Reflection;
using System.Text;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

[assembly: Gum.Avalonia.Tests.PluginFailureGuard]

namespace Gum.Avalonia.Tests;

/// <summary>
/// Keeps the plugin set from leaking between tests, and fails the test that disables a guarded
/// plugin. <see cref="PluginManager"/> catches a plugin's exception, disables that plugin for the
/// rest of the process and carries on, so without this a headless test can drive a real gesture
/// and pass while the plugin it means to exercise has not handled an event since an earlier test
/// broke it.
/// </summary>
/// <remarks>
/// After every test, each plugin the test disabled gets a fresh, enabled container, and a test that
/// swapped <see cref="PluginManager.Plugins"/> without putting it back has the original set restored,
/// so the next test starts with the same plugins enabled whatever ran before it. Plugins that fail
/// while the tool first starts up are left as they are.
///
/// The failure check covers only the plugins whose views <see cref="ToolStartup"/> builds - a plugin
/// that needs a window the headless lifetime never opens (the editor tab) is expected to fail inside
/// a test that shows elements. Extend <see cref="GuardedPluginTypeNames"/> as startup covers more.
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
    private readonly HashSet<PluginContainer> _enabledBefore = new();
    private List<PluginBase>? _pluginsBefore;

    /// <summary>Records the plugin set and which guarded plugins an earlier test already disabled.</summary>
    public override void Before(MethodInfo methodUnderTest)
    {
        PluginManager pluginManager = PluginManagerInstance;
        _pluginsBefore = pluginManager.Plugins?.ToList();
        _enabledBefore.Clear();
        foreach (PluginContainer container in pluginManager.PluginContainers.Values.Where(container => container.IsEnabled))
        {
            _enabledBefore.Add(container);
        }
        foreach ((string name, _) in FailedGuardedPlugins())
        {
            _failedBefore.Add(name);
        }
    }

    /// <summary>
    /// Restores the plugin set, then throws if this test's body disabled a guarded plugin, with the
    /// plugin's exception.
    /// </summary>
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
                $"{methodUnderTest.Name} disabled the {name} plugin ({container.FailureDetails}).");
            message.AppendLine(container.FailureException?.ToString());
        }

        RestorePluginSet();

        if (message.Length > 0)
        {
            throw new InvalidOperationException(message.ToString());
        }
    }

    private void RestorePluginSet()
    {
        PluginManager pluginManager = PluginManagerInstance;
        if (_pluginsBefore != null && (pluginManager.Plugins == null || !pluginManager.Plugins.ToHashSet().SetEquals(_pluginsBefore)))
        {
            pluginManager.Plugins = _pluginsBefore;
        }

        List<IPlugin> disabledByThisTest = pluginManager.PluginContainers
            .Where(pair => !pair.Value.IsEnabled && _enabledBefore.Contains(pair.Value))
            .Select(pair => pair.Key)
            .ToList();
        foreach (IPlugin plugin in disabledByThisTest)
        {
            // A fresh container is enabled and carries no failure. The plugin's ShutDown already
            // ran; for most plugins, the editor tab included, that is a no-op.
            pluginManager.PluginContainers[plugin] = new PluginContainer(plugin);
            _failedBefore.Remove(plugin.GetType().Name);
        }
    }

    private static PluginManager PluginManagerInstance => TestAppBuilder.Services.GetRequiredService<PluginManager>();

    private static IEnumerable<(string Name, PluginContainer Container)> FailedGuardedPlugins()
    {
        // The container is built once per assembly, so a test that runs before any plugin loads
        // simply sees none.
        foreach (PluginContainer container in PluginManagerInstance.PluginContainers.Values)
        {
            string name = container.Plugin.GetType().Name;
            if (!container.IsEnabled && GuardedPluginTypeNames.Contains(name))
            {
                yield return (name, container);
            }
        }
    }
}
