using System;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Settings;
using Gum.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Services;

/// <summary>
/// The WPF head's implementation of the framework-specific startup steps: legacy settings
/// migration into the writable options store, the WPF tree view and property grid, and the MEF
/// plugin host.
/// </summary>
public class WpfHeadStartup : IHeadStartup
{
    private readonly IServiceProvider _services;

    /// <summary>Creates the steps over the built container.</summary>
    public WpfHeadStartup(IServiceProvider services)
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
            if (config.GetSection(typeof(T).Name) is { } section &&
                section.Exists())
            {
                return;
            }
            _services.GetRequiredService<IWritableOptions<T>>().Update(applyAction);
        }
    }

    /// <inheritdoc/>
    public void InitializeElementTreeView() =>
        _services.GetRequiredService<ElementTreeViewManager>().Initialize();

    /// <inheritdoc/>
    public void InitializePropertyGrid() =>
        _services.GetRequiredService<PropertyGridManager>().InitializeEarly();

    /// <inheritdoc/>
    public void InitializePlugins()
    {
        _services.GetRequiredService<PluginManager>().Initialize();
        VariableSaveExtensionMethods.CustomFixEnumerations = VariableSaveExtensionMethodsGumTool.FixEnumerationsWithReflection;
    }

    /// <inheritdoc/>
    public void RenderSurfaceReady() =>
        _services.GetRequiredService<PluginManager>().XnaInitialized();
}
