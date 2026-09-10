using Gum.Managers;
using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Gum.Plugins;

/// <summary>
/// Turns the files in the plugin folder into MEF catalogs. The folder holds more than plugins —
/// it is scanned recursively, so it also turns up each plugin's managed dependencies and the
/// native DLLs those ship in <c>runtimes/&lt;rid&gt;/native</c>. Neither can host a plugin, and
/// both fail to load in their own way, so this class reports only the one failure the user can
/// act on — a plugin that will not appear — and stays silent about the rest.
/// </summary>
internal class PluginCatalogFactory
{
    /// <summary>
    /// Cap on how many distinct loader errors are listed. A missing dependency produces one
    /// loader exception per type that referenced it, so the raw list can run to hundreds.
    /// </summary>
    private const int MaxReportedLoaderErrors = 10;

    private readonly IOutputManager _outputManager;
    private readonly List<PluginFileScan> _scans = [];

    public PluginCatalogFactory(IOutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    /// <summary>
    /// One entry per file passed to <see cref="CreateCatalogForFile"/>, for the scan report shown
    /// in the "Manage Plugins" dialog.
    /// </summary>
    public IReadOnlyList<PluginFileScan> Scans => _scans;

    /// <summary>
    /// Loads <paramref name="dllPath"/> and returns a catalog over it, or null if the file holds
    /// no usable types. Reports anything the user could act on to the Output tab.
    /// </summary>
    public ComposablePartCatalog? CreateCatalogForFile(string dllPath)
    {
        return CreateCatalogForFile(dllPath, null);
    }

    /// <summary>As <see cref="CreateCatalogForFile(string)"/>, with a head that can refuse an assembly it cannot run.</summary>
    public ComposablePartCatalog? CreateCatalogForFile(string dllPath, IPluginHostConfiguration? host)
    {
        string fileName = Path.GetFileName(dllPath);
        Assembly assembly;

        try
        {
            assembly = Assembly.LoadFrom(dllPath);
        }
        catch (BadImageFormatException) when (!IsManagedAssembly(dllPath))
        {
            // A native DLL, which a plugin ships alongside itself in runtimes/<rid>/native. It can
            // never load as managed, so it isn't a failure and there is nothing to report.
            _scans.Add(new PluginFileScan(fileName, PluginFileOutcome.NotManagedAssembly, false, null));
            return null;
        }
        catch (Exception exception)
        {
            _scans.Add(new PluginFileScan(fileName, PluginFileOutcome.LoadFailed, false, exception.Message));
            _outputManager.AddError($"Failed to load plugin assembly '{dllPath}':\n{exception}");
            return null;
        }
        if (host != null && !host.CanHostExternalAssembly(assembly, out string? reason))
        {
            _scans.Add(new PluginFileScan(fileName, PluginFileOutcome.NotHostable, CouldContainPlugins(assembly), reason));
            _outputManager.AddError($"Skipped plugin assembly '{fileName}': {reason}.");
            return null;
        }


        _scans.Add(new PluginFileScan(fileName, PluginFileOutcome.Loaded, CouldContainPlugins(assembly), null));

        return CreateResilientCatalog(assembly);
    }

    /// <summary>
    /// Whether the file at <paramref name="path"/> carries a CLI header, which is what separates a
    /// native DLL from a managed assembly that failed to load for some other reason (a truncated
    /// or corrupted plugin, say). Without this check a damaged plugin would be dismissed as native
    /// and vanish silently — the exact failure this scan exists to expose.
    /// </summary>
    internal static bool IsManagedAssembly(string path)
    {
        try
        {
            using FileStream stream = File.OpenRead(path);
            using PEReader peReader = new(stream);

            return peReader.HasMetadata;
        }
        catch (Exception)
        {
            // Unreadable or not a PE file at all. Treat as unmanaged; the caller stays quiet, and a
            // file Gum can't even open is not a diagnosis anyone can act on.
            return false;
        }
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

        // Only worth reporting when a plugin could have been among the skipped types. An assembly
        // that can't hold one has nothing the user can act on, now or ever - Vortice.Direct3D12 is
        // the standing example, with two types that can never load and no MEF parts.
        if (couldContainPlugins)
        {
            int skippedCount = exception.Types.Length - loadableTypes.Length;
            _outputManager.AddError(BuildReport(assemblyName, exception, skippedCount, exception.Types.Length));
        }

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
