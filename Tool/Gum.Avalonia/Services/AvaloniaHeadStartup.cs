using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Gum.Avalonia.Canvas;
using Gum.Diagnostics;
using Gum.Managers;
using Gum.Plugins;
using Gum.Settings;
using Gum.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Services;

/// <summary>
/// This head's framework-specific startup steps. Settings migration is the same as the WPF head's
/// and plugins load through the shared host.
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
    public void InitializeElementTreeView() =>
        _services.GetRequiredService<ElementTreeViewManager>().Initialize();

    /// <inheritdoc/>
    public void InitializePropertyGrid() =>
        _services.GetRequiredService<PropertyGridManager>().InitializeEarly();

    /// <inheritdoc/>
    public void InitializePlugins()
    {
        PluginManager pluginManager = _services.GetRequiredService<PluginManager>();
        pluginManager.Initialize();

        // Lists the composed plugins in the Output tab; Plugins > Manage Plugins shows the same list.
        string names = string.Join(", ", pluginManager.InitializedPlugins.Select(plugin => plugin.FriendlyName).OrderBy(name => name));
        _services.GetRequiredService<IOutputManager>().AddOutput($"Loaded {pluginManager.InitializedPlugins.Count()} plugin(s): {names}");

        // AddOutput, not AddError: a stale build is routine while developing and must not steal the tab.
        if (StagedContentStalenessCheck.FindWarning(AppContext.BaseDirectory) is string staleWarning)
        {
            _services.GetRequiredService<IOutputManager>().AddOutput(staleWarning);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Creating the device blocks the UI thread for seconds, so it is split into steps with a
    /// dispatcher pass between them: the loaded tree reaches the screen first, and clicks in it
    /// are handled between steps instead of after all of them.
    /// </remarks>
    public async Task RenderSurfaceReadyAsync()
    {
        await LetTheUiRunAsync();
        try
        {
            await GameRenderDeviceHost.CreateSharedDeviceAsync(LetTheUiRunAsync);
        }
        catch (Exception exception)
        {
            // The editor canvas asks for the device again when it initializes, and reports the
            // failure to the user there.
            ToolLog.Write("device.create.failed", detail: exception.Message);
        }
        await LetTheUiRunAsync();
        _services.GetRequiredService<PluginManager>().XnaInitialized();
        // The editor canvas builds the loaded project's wireframe in a callback it posts, so
        // startup ends once that has run.
        await LetTheUiRunAsync();
    }

    // Background is below input and rendering, so everything they have queued runs first.
    private static Task LetTheUiRunAsync() =>
        Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();
}
