using Gum.Avalonia.Shell;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Reflection;
using Gum.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Runs the startup steps <see cref="GumStartupSequence"/> runs before a project loads, on the
/// shared container, once per test run, in the head's order. A test that drives a real gesture
/// needs all of them - the singleton <see cref="PropertyGridManager"/> especially: until it has
/// built its grid, the first selection event that reaches the Variables tab plugin throws, and
/// <see cref="PluginManager"/> then disables that plugin for the rest of the run.
/// </summary>
internal static class ToolStartup
{
    private static bool _initialized;
    private static AvaloniaPluginTab? _variablesTab;

    /// <summary>The Variables tab the singleton grid manager owns, as in the running tool.</summary>
    internal static AvaloniaPluginTab VariablesTab
    {
        get
        {
            EnsureInitialized();
            return _variablesTab!;
        }
    }

    /// <summary>
    /// Runs the steps unless they have already run. Call from the UI thread, so an
    /// <c>[AvaloniaFact]</c> rather than a <c>[Fact]</c>: the grid manager creates its view.
    /// </summary>
    internal static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;

        IServiceProvider services = TestAppBuilder.Services;
        services.GetRequiredService<ITypeManager>().Initialize();
        AvaloniaTabManager tabManager = (AvaloniaTabManager)services.GetRequiredService<ITabManager>();

        // Builds the Project tab's tree. Until this has run, the first project load that reaches
        // the tree's plugin throws on the view it never got.
        services.GetRequiredService<ElementTreeViewManager>().Initialize();

        // Builds the Variables tab's view and grid. Until this has run, the first selection event
        // that reaches the tab's plugin throws on the grid it never got.
        services.GetRequiredService<PropertyGridManager>().InitializeEarly();
        _variablesTab = tabManager.CenterBottom.Last(tab => tab.Title == "Variables");

        PluginManager pluginManager = services.GetRequiredService<PluginManager>();
        if (!pluginManager.IsInitialized)
        {
            pluginManager.Initialize();
        }
        // The standard-state refresh goes through the plugins, so they load first.
        StandardElementsManager.Self.Initialize();
        services.GetRequiredService<IStandardElementsManagerGumTool>().Initialize();
    }
}
