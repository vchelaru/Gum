using System;
using System.Linq;
using Gum.Managers;
using Gum.Plugins;
using Gum.Settings;
using Gum.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Services;

/// <summary>
/// This head's framework-specific startup steps. Settings migration is the same as the WPF head's
/// and plugins load through the shared host; the tree-view step is a placeholder until phase 60
/// brings it across.
/// </summary>
public class AvaloniaHeadStartup : IHeadStartup
{
    private readonly IServiceProvider _services;

    /// <summary>Creates the steps over the built container.</summary>
    public AvaloniaHeadStartup(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc/>
    public void MigrateLegacySettings(GeneralSettingsFile legacySettings)
    {
        IConfiguration config = _services.GetRequiredService<IConfiguration>();

        ApplyIfNotExists<ThemeSettings>(x => ThemeSettingsMigration.MigrateExplicitLegacyColors(legacySettings, x));
        ApplyIfNotExists<LayoutSettings>(x => LayoutSettings.MigrateLegacyLayout(legacySettings, x));

        void ApplyIfNotExists<T>(Action<T> applyAction) where T : class, new()
        {
            if (config.GetSection(typeof(T).Name) is { } section && section.Exists())
            {
                return;
            }
            _services.GetRequiredService<IWritableOptions<T>>().Update(applyAction);
        }
    }

    /// <inheritdoc/>
    public void InitializeElementTreeView() { }

    /// <inheritdoc/>
    public void InitializePropertyGrid() =>
        _services.GetRequiredService<PropertyGridManager>().InitializeEarly();

    /// <inheritdoc/>
    public void InitializePlugins()
    {
        PluginManager pluginManager = _services.GetRequiredService<PluginManager>();
        pluginManager.Initialize();

        // Until the plugin-management dialog comes across, the Output tab is where a user can see
        // which plugins this head composed.
        string names = string.Join(", ", pluginManager.Plugins.Select(plugin => plugin.FriendlyName).OrderBy(name => name));
        _services.GetRequiredService<IOutputManager>().AddOutput($"Loaded {pluginManager.Plugins.Count()} plugin(s): {names}");
    }

    /// <inheritdoc/>
    public void RenderSurfaceReady() => _services.GetRequiredService<PluginManager>().XnaInitialized();
}
