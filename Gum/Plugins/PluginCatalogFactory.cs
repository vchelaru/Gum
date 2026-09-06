using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Gum.Plugins;

/// <summary>
/// Turns the files in the plugin folder into MEF catalogs. The folder holds more than plugins —
/// it is scanned recursively, so it also turns up each plugin's managed dependencies and the
/// native DLLs those ship in <c>runtimes/&lt;rid&gt;/native</c>. Neither can host a plugin, and
/// both fail to load in their own way, so this class separates a failure the user can act on
/// (a plugin that will not appear) from the expected noise.
/// </summary>
internal class PluginCatalogFactory
{
    /// <summary>
    /// Cap on how many distinct loader errors are listed. A missing dependency produces one
    /// loader exception per type that referenced it, so the raw list can run to hundreds.
    /// </summary>
    private const int MaxReportedLoaderErrors = 10;

    private readonly IOutputManager _outputManager;

    public PluginCatalogFactory(IOutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    /// <summary>
    /// Loads <paramref name="dllPath"/> and returns a catalog over it, or null if the file holds
    /// no usable types. Reports anything the user could act on to the Output tab.
    /// </summary>
    public ComposablePartCatalog? CreateCatalogForFile(string dllPath)
    {
        Assembly assembly;

        try
        {
            assembly = Assembly.LoadFrom(dllPath);
        }
        catch (BadImageFormatException)
        {
            // A native DLL, which is what this means, is expected in a plugin folder and can
            // never be loaded as a managed assembly. Nothing to report.
            return null;
        }
        catch (Exception exception)
        {
            _outputManager.AddError($"Failed to load plugin assembly '{dllPath}':\n{exception}");
            return null;
        }

        return CreateResilientCatalog(assembly);
    }

    /// <summary>
    /// Returns a catalog over <paramref name="assembly"/>, falling back to only its loadable types
    /// (reporting the rest) if type enumeration fails. Returns null if nothing loadable remains.
    /// </summary>
    /// <remarks>
    /// <see cref="AssemblyCatalog"/> enumerates types lazily during composition, so an assembly
    /// holding a type that can't be reflection-loaded would throw a
    /// <see cref="ReflectionTypeLoadException"/> later, outside any per-file try/catch, and take
    /// down every plugin. Enumerating here surfaces that eagerly so it can be contained.
    /// </remarks>
    public ComposablePartCatalog? CreateResilientCatalog(Assembly assembly)
    {
        try
        {
            assembly.GetTypes();
            return new AssemblyCatalog(assembly);
        }
        catch (ReflectionTypeLoadException exception)
        {
            return CreateCatalogForLoadableTypes(assembly.FullName ?? assembly.ToString(), exception,
                CouldContainPlugins(assembly));
        }
    }

    /// <summary>
    /// Whether <paramref name="assembly"/> could hold a plugin at all. A plugin exports
    /// <see cref="PluginBase"/>, so it must reference the assembly declaring that type; a
    /// dependency sitting in the same folder does not.
    /// </summary>
    internal bool CouldContainPlugins(Assembly assembly)
    {
        string? pluginBaseAssemblyName = typeof(PluginBase).Assembly.GetName().Name;

        return assembly.GetReferencedAssemblies()
            .Any(x => string.Equals(x.Name, pluginBaseAssemblyName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reports the types that couldn't be loaded and returns a catalog over the ones that could.
    /// Without the report, a plugin among the skipped types just silently never appears.
    /// </summary>
    internal ComposablePartCatalog? CreateCatalogForLoadableTypes(string assemblyName,
        ReflectionTypeLoadException exception, bool couldContainPlugins)
    {
        Type[] loadableTypes = exception.Types.OfType<Type>().ToArray();
        int skippedCount = exception.Types.Length - loadableTypes.Length;
        string report = BuildReport(assemblyName, exception, skippedCount, exception.Types.Length,
            couldContainPlugins);

        // An assembly that can't hold a plugin has nothing the user can act on - Vortice.Direct3D12
        // is the standing example, with two types that never load - so it stays out of the errors.
        if (couldContainPlugins)
        {
            _outputManager.AddError(report);
        }
        else
        {
            _outputManager.AddOutput(report);
        }

        return loadableTypes.Length > 0 ? new TypeCatalog(loadableTypes) : null;
    }

    private static string BuildReport(string assemblyName, ReflectionTypeLoadException exception,
        int skippedCount, int totalCount, bool couldContainPlugins)
    {
        StringBuilder report = new();
        report.AppendLine($"Assembly '{assemblyName}' loaded, but {skippedCount} of its {totalCount} " +
            "types could not be loaded and were skipped." +
            (couldContainPlugins ? " Any plugin among them will not appear in Gum:" : ""));

        // Deduplicated - a missing dependency reports the same message once per referencing type.
        List<string> messages = (exception.LoaderExceptions ?? [])
            .OfType<Exception>()
            .Select(x => x.Message)
            .Distinct()
            .ToList();

        foreach (string message in messages.Take(MaxReportedLoaderErrors))
        {
            report.AppendLine("    " + message);
        }

        if (messages.Count > MaxReportedLoaderErrors)
        {
            report.AppendLine($"    ...and {messages.Count - MaxReportedLoaderErrors} more.");
        }

        return report.ToString();
    }
}
