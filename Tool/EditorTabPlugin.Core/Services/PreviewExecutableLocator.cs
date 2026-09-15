using System;
using System.Collections.Generic;
using System.IO;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Finds the GumPreview runtime host executable (issue #4697). Checked in priority order: the
/// published layout ships it in a <c>Preview/</c> folder next to the head executable; a dev build
/// falls back to the sample's own build output under <c>Samples/GumPreview</c>, found by walking up
/// from the head's base directory.
/// </summary>
public static class PreviewExecutableLocator
{
    /// <summary>Folder name the release publish step copies the preview host into, next to the head.</summary>
    public const string PreviewFolderName = "Preview";

    /// <summary>Repo-relative path to the preview host sample project, used for the dev-build fallback.</summary>
    public const string SampleProjectPath = "Samples/GumPreview/GumPreview";

    private static readonly string[] DevBuildConfigurations = { "Debug", "Release" };
    private static readonly string[] DevBuildTargetFrameworks = { "net8.0" };

    /// <summary>
    /// Returns the full path to the preview executable, or null if none of the candidate locations
    /// (relative to <paramref name="headBaseDirectory"/>, the running head's <c>AppContext.BaseDirectory</c>) exist.
    /// </summary>
    public static string? Resolve(string headBaseDirectory)
    {
        foreach (string candidate in Candidates(headBaseDirectory))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }

    internal static IEnumerable<string> Candidates(string headBaseDirectory)
    {
        string exeName = OperatingSystem.IsWindows() ? "GumPreview.exe" : "GumPreview";

        yield return Path.Combine(headBaseDirectory, PreviewFolderName, exeName);

        string? repoRoot = FindRepoRoot(headBaseDirectory);
        if (repoRoot != null)
        {
            string sampleBinRoot = Path.Combine(repoRoot, "Samples", "GumPreview", "GumPreview", "bin");
            foreach (string configuration in DevBuildConfigurations)
            {
                foreach (string targetFramework in DevBuildTargetFrameworks)
                {
                    yield return Path.Combine(sampleBinRoot, configuration, targetFramework, exeName);
                }
            }
        }
    }

    // Walks up from the head's base directory looking for the repo root, identified by the presence
    // of the GumPreview sample project itself.
    private static string? FindRepoRoot(string startDirectory)
    {
        DirectoryInfo? directory = new DirectoryInfo(startDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Samples", "GumPreview")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return null;
    }
}
