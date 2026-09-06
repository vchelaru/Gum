using Gum.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Gum.Plugins;

/// <summary>
/// Builds a MEF catalog for a plugin-folder assembly without letting a single unloadable type
/// abort plugin loading. <see cref="AssemblyCatalog"/> enumerates an assembly's types lazily
/// during composition, so an assembly that contains a type which can't be reflection-loaded —
/// e.g. a plugin's native-interop dependency such as Vortice.Direct3D12, whose explicit-layout
/// <c>Union</c> struct overlaps object and non-object fields — would throw a
/// <see cref="ReflectionTypeLoadException"/> later, outside the per-DLL try/catch in
/// <c>PluginManager.CreateCatalog</c>, and take down every plugin. Forcing the type enumeration
/// here surfaces that failure eagerly so it can be contained, and the catalog is then built from
/// only the types that did load.
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
    /// Returns a catalog over <paramref name="assembly"/>, falling back to only its loadable types
    /// (reporting the rest) if type enumeration fails. Returns null if nothing loadable remains.
    /// </summary>
    public ComposablePartCatalog? CreateResilientCatalog(Assembly assembly)
    {
        try
        {
            // Surface any unloadable types now rather than during deferred MEF composition.
            assembly.GetTypes();
            return new AssemblyCatalog(assembly);
        }
        catch (ReflectionTypeLoadException exception)
        {
            return CreateCatalogForLoadableTypes(assembly.FullName ?? assembly.ToString(), exception);
        }
    }

    /// <summary>
    /// Reports the types that couldn't be loaded and returns a catalog over the ones that could.
    /// Without the report, a plugin among the skipped types just silently never appears.
    /// </summary>
    internal ComposablePartCatalog? CreateCatalogForLoadableTypes(string assemblyName,
        ReflectionTypeLoadException exception)
    {
        Type[] loadableTypes = exception.Types.OfType<Type>().ToArray();
        int skippedCount = exception.Types.Length - loadableTypes.Length;

        _outputManager.AddError(BuildReport(assemblyName, exception, skippedCount, exception.Types.Length));

        return loadableTypes.Length > 0 ? new TypeCatalog(loadableTypes) : null;
    }

    private static string BuildReport(string assemblyName, ReflectionTypeLoadException exception,
        int skippedCount, int totalCount)
    {
        StringBuilder report = new();
        report.AppendLine($"Plugin assembly '{assemblyName}' loaded, but {skippedCount} of its " +
            $"{totalCount} types could not be loaded and were skipped. Any plugin among them will " +
            "not appear in Gum:");

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
