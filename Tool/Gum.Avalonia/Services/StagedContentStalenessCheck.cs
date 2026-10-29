using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gum.Avalonia.Services;

/// <summary>
/// Finds the plugins and staged Forms themes next to the head that a build of just the head left
/// behind, so the tool does not run old plugin or theme content without saying so.
/// </summary>
/// <remarks>
/// Plugins and themes are not built by the head's own project, only by a <c>Gum.slnx</c> build, so
/// <c>dotnet run --project</c> or an IDE run leaves them at whatever the last solution build wrote.
/// The comparison is against the shared libraries those plugins and themes are built from, not the
/// head assembly: editing only head code rewrites the head and leaves every plugin correct. It relies
/// on each plugin project touching its copied dll and the theme staging touching <c>.staged</c> on every
/// build that includes them (their post-build steps), because a plain copy keeps the older timestamp.
/// </remarks>
internal static class StagedContentStalenessCheck
{
    internal const string FixCommand = "dotnet build Gum.slnx --no-incremental -p:SkipPreviewVariantPublish=true";

    private const string PluginsFolderName = "Plugins";
    private const string ThemesFolderName = "FormsThemes";
    private const string ThemeStampFileName = ".staged";

    // What a plugin compiles against.
    private static readonly string[] PluginSharedAssemblies = ["Gum.Presentation.dll", "Gum.ProjectServices.dll", "GumCommon.dll"];

    // What produces the staged themes (the staging service lives here).
    private static readonly string[] ThemeSharedAssemblies = ["Gum.ProjectServices.dll"];

    /// <summary>
    /// The Output line to show for the content under <paramref name="baseDirectory"/>, or null when
    /// nothing is stale.
    /// </summary>
    internal static string? FindWarning(string baseDirectory)
    {
        List<string> stalePlugins = FindStalePlugins(baseDirectory);
        string? themeProblem = FindThemeProblem(baseDirectory);
        if (stalePlugins.Count == 0 && themeProblem == null)
        {
            return null;
        }

        List<string> problems = [];
        if (stalePlugins.Count > 0)
        {
            problems.Add($"plugins older than the tool build: {string.Join(", ", stalePlugins)}");
        }
        if (themeProblem != null)
        {
            problems.Add(themeProblem);
        }

        return "WARNING: This run has stale staged content (" + string.Join("; ", problems) + "). "
            + $"Building only the head leaves these behind. Run `{FixCommand}`.";
    }

    private static List<string> FindStalePlugins(string baseDirectory)
    {
        string pluginsDirectory = Path.Combine(baseDirectory, PluginsFolderName);
        DateTime? newestSharedWrite = NewestWrite(baseDirectory, PluginSharedAssemblies);
        if (newestSharedWrite == null || !Directory.Exists(pluginsDirectory))
        {
            return [];
        }

        // Only a plugin's own assembly (Plugins/X/X.dll) counts; its dependencies keep their
        // package timestamps and are never rebuilt.
        return Directory.EnumerateDirectories(pluginsDirectory)
            .Select(directory => Path.Combine(directory, Path.GetFileName(directory) + ".dll"))
            .Where(File.Exists)
            .Where(assembly => File.GetLastWriteTimeUtc(assembly) < newestSharedWrite)
            .Select(assembly => Path.GetFileNameWithoutExtension(assembly))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? FindThemeProblem(string baseDirectory)
    {
        string themesDirectory = Path.Combine(baseDirectory, "Content", ThemesFolderName);
        if (!Directory.Exists(themesDirectory))
        {
            return null;
        }

        string stamp = Path.Combine(themesDirectory, ThemeStampFileName);
        if (!File.Exists(stamp))
        {
            return $"staged Forms themes have no {ThemeStampFileName} stamp";
        }

        DateTime? newestSharedWrite = NewestWrite(baseDirectory, ThemeSharedAssemblies);
        return newestSharedWrite != null && File.GetLastWriteTimeUtc(stamp) < newestSharedWrite
            ? "staged Forms themes older than the tool build"
            : null;
    }

    private static DateTime? NewestWrite(string baseDirectory, IEnumerable<string> fileNames)
    {
        DateTime? newest = null;
        foreach (string fileName in fileNames)
        {
            string path = Path.Combine(baseDirectory, fileName);
            if (File.Exists(path) && File.GetLastWriteTimeUtc(path) is DateTime written && (newest == null || written > newest))
            {
                newest = written;
            }
        }

        return newest;
    }
}
