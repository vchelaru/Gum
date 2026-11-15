using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Gum.Plugins;

/// <summary>
/// Binds the Gum assemblies a plugin references to the copies the tool already loaded. The tool's
/// assemblies are stamped with the build date (2026.11.9.0) while the NuGet packages a plugin is
/// built against carry a revision too (2026.11.9.1). The runtime never binds a reference to a lower
/// assembly version, so without this the plugin's types are skipped (#7959). Only a revision
/// difference is tolerated: a plugin built from a later day's package, or one that needs a newer
/// third-party assembly, still fails to bind and says why.
/// </summary>
internal class PluginAssemblyResolver
{
    private readonly Action<string> _printOutput;
    private readonly string _toolDirectory;

    public PluginAssemblyResolver(Action<string> printOutput)
    {
        _printOutput = printOutput;
        _toolDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
    }

    /// <summary>
    /// Subscribes one resolver to <see cref="AppDomain.AssemblyResolve"/> for the rest of the process,
    /// or only updates where it prints when one is already subscribed. It stays on because a plugin
    /// binds Gum types lazily too (the first call of a method that uses one, long after load).
    /// </summary>
    public static void Install(Action<string> printOutput)
    {
        lock (_installLock)
        {
            if (_installed == null)
            {
                _installed = new PluginAssemblyResolver(message => _installedOutput?.Invoke(message));
                AppDomain.CurrentDomain.AssemblyResolve += _installed.Resolve;
            }

            _installedOutput = printOutput;
        }
    }

    private static readonly object _installLock = new();
    private static PluginAssemblyResolver? _installed;
    private static Action<string>? _installedOutput;

    /// <summary>Handler for <see cref="AppDomain.AssemblyResolve"/>; returns null when the reference stays unbound.</summary>
    public Assembly? Resolve(object? sender, ResolveEventArgs args)
    {
        AssemblyName requested = new AssemblyName(args.Name);
        Assembly? loaded = FindLoadedToolAssembly(requested);

        if (loaded != null && IsRevisionOnlyDifference(requested.Version, loaded.GetName().Version))
        {
            return loaded;
        }

        if (args.RequestingAssembly != null)
        {
            string reason = loaded != null && IsNewerBuild(requested.Version, loaded.GetName().Version)
                ? $" (the plugin was built against a newer build of Gum than this tool, {loaded.GetName().Version}; rebuild it against this tool's version)"
                : string.Empty;
            _printOutput("Couldn't find assembly: " + args.Name + " for " + args.RequestingAssembly + reason);
        }

        return null;
    }

    /// <summary>
    /// The already-loaded copy of <paramref name="requested"/> that the tool itself shipped: in the
    /// default load context, in the tool's own folder (not a plugin's), and carrying a build-date
    /// version, which every Gum assembly is stamped with and a third-party one is not.
    /// </summary>
    private Assembly? FindLoadedToolAssembly(AssemblyName requested)
    {
        foreach (Assembly assembly in AssemblyLoadContext.Default.Assemblies)
        {
            AssemblyName name = assembly.GetName();
            if (!string.Equals(name.Name, requested.Name, StringComparison.OrdinalIgnoreCase) || assembly.IsDynamic)
            {
                continue;
            }

            string? directory = string.IsNullOrEmpty(assembly.Location) ? null : Path.GetDirectoryName(assembly.Location);
            if (directory != null && IsBuildDateVersion(name.Version) &&
                string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)), _toolDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return assembly;
            }
        }

        return null;
    }

    private static bool IsBuildDateVersion(Version? version)
    {
        return version != null && version.Major >= 2020 && version.Minor is >= 1 and <= 12 && version.Build is >= 1 and <= 31;
    }

    private static bool IsRevisionOnlyDifference(Version? requested, Version? loaded)
    {
        return requested != null && loaded != null &&
            requested.Major == loaded.Major && requested.Minor == loaded.Minor && requested.Build == loaded.Build &&
            requested.Revision > loaded.Revision;
    }

    private static bool IsNewerBuild(Version? requested, Version? loaded)
    {
        return requested != null && loaded != null &&
            (requested.Major, requested.Minor, requested.Build).CompareTo((loaded.Major, loaded.Minor, loaded.Build)) > 0;
    }
}
