using System;
using Gum.Plugins;
using Gum.Settings;
using Gum.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Services;

/// <summary>
/// This head's framework-specific startup steps. Settings migration is the same as the WPF head's;
/// the view and plugin steps are placeholders until phases 40 through 70 bring those subsystems
/// across.
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
    public void InitializePropertyGrid() { }

    /// <inheritdoc/>
    public void InitializePlugins() => _services.GetRequiredService<PluginManager>().Initialize();

    /// <inheritdoc/>
    public void RenderSurfaceReady() => _services.GetRequiredService<PluginManager>().XnaInitialized();
}
