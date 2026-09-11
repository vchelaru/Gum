using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Gum.Avalonia.Shell;
using Gum.Input;
using Gum.Menus;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Services;

/// <summary>
/// The Avalonia head's contribution to the plugin host: its own built-in plugins, its shell
/// services and menu model as exports, and a filter that keeps WPF/WinForms plugin assemblies out
/// since they cannot run here.
/// </summary>
public class AvaloniaPluginHostConfiguration : IPluginHostConfiguration
{
    private static readonly string[] WindowsOnlyAssemblies =
    {
        "PresentationFramework", "PresentationCore", "WindowsBase", "System.Windows.Forms", "System.Xaml",
    };

    private readonly IServiceProvider _services;

    /// <summary>
    /// Creates the configuration. Shell services are resolved when exports are added, not here:
    /// the plugin host is constructed early and several shell services depend on it.
    /// </summary>
    public AvaloniaPluginHostConfiguration(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc/>
    public IEnumerable<Assembly> InternalPluginAssemblies => new[] { typeof(AvaloniaPluginHostConfiguration).Assembly };

    /// <inheritdoc/>
    public void AddHeadExports(CompositionBatch batch)
    {
        batch.AddExportedValue<MenuModel>(_services.GetRequiredService<MenuModel>());
        batch.AddExportedValue<ShellViewModel>(_services.GetRequiredService<ShellViewModel>());
        batch.AddExportedValue<AvaloniaTabManager>(_services.GetRequiredService<AvaloniaTabManager>());
    }

    /// <inheritdoc/>
    public bool CanHostExternalAssembly(Assembly assembly, out string? reason)
    {
        string? windowsOnly = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .FirstOrDefault(name => name != null && WindowsOnlyAssemblies.Contains(name));
        if (windowsOnly != null)
        {
            reason = $"references {windowsOnly}, which only exists on Windows; this plugin needs an Avalonia build";
            return false;
        }

        reason = null;
        return true;
    }

    /// <inheritdoc/>
    public IGumCursorState? CursorState => null;
}
