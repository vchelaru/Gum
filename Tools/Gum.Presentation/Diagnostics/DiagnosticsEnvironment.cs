using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Gum.Services;

namespace Gum.Diagnostics;

/// <summary>The graphics adapter the canvas runs on, recorded once the canvas has created its device.</summary>
public static class GraphicsAdapterInfo
{
    /// <summary>The adapter's description, or null until the device exists.</summary>
    public static string? Name { get; set; }
}

/// <inheritdoc cref="IDiagnosticsEnvironment"/>
public class DiagnosticsEnvironment : IDiagnosticsEnvironment
{
    private readonly IAppScaleProvider _appScaleProvider;

    /// <summary>Creates the environment for the running tool.</summary>
    public DiagnosticsEnvironment(IAppScaleProvider appScaleProvider)
    {
        _appScaleProvider = appScaleProvider;
    }

    /// <inheritdoc/>
    public virtual DiagnosticsSystemInfo GetSystemInfo() => new DiagnosticsSystemInfo(
        ToolVersion.Describe(Assembly.GetEntryAssembly()),
        RuntimeInformation.OSDescription,
        RuntimeInformation.FrameworkDescription,
        RuntimeInformation.OSArchitecture.ToString(),
        GetDisplayScale(),
        GraphicsAdapterInfo.Name ?? "unknown");

    /// <summary>The display scale text; a head that knows its screen's scaling overrides this.</summary>
    protected virtual string GetDisplayScale() =>
        $"UI zoom {_appScaleProvider.BaseFontSize / UiSettingsService.DefaultBaseFontSize:P0}";

    /// <inheritdoc/>
    public string SaveFolder => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    /// <inheritdoc/>
    public string UserName => Environment.UserName;

    /// <inheritdoc/>
    public string HomeFolder => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <inheritdoc/>
    public DateTime Today => DateTime.Now;
}
