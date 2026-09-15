using System;
using System.Collections.Generic;
using System.IO;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Finds the GumPreview runtime host executable (issue #4697). Checked in priority order: the
/// published layout ships it in a <c>Preview/</c> (ReadyToRun, .gumx-safe) or <c>Preview-Aot/</c>
/// (Native AOT, .gumj-only - issue #4706) folder next to the head executable; a dev build falls
/// back to the project's own build output under <c>Tool/GumPreview</c>, found by walking up from
/// the head's base directory.
/// </summary>
public static class PreviewExecutableLocator
{
    /// <summary>Folder name the release publish step copies the ReadyToRun preview host into, next to the head.</summary>
    public const string PreviewFolderName = "Preview";

    /// <summary>
    /// Folder name the release publish step copies the Native AOT preview host into, next to the
    /// head. Only safe for .gumj projects - see <see cref="Resolve"/>.
    /// </summary>
    public const string AotPreviewFolderName = "Preview-Aot";

    /// <summary>Repo-relative path to the preview host project, used for the dev-build fallback.</summary>
    public const string DevBuildProjectPath = "Tool/GumPreview";

    // Release first: a local dev build should default to real (non-Debug-JIT) performance when both
    // configurations are present, rather than silently always picking up a stale Debug build.
    private static readonly string[] DevBuildConfigurations = { "Release", "Debug" };
    private static readonly string[] DevBuildTargetFrameworks = { "net10.0" };

    /// <summary>
    /// Returns the full path to the preview executable, or null if none of the candidate locations
    /// (relative to <paramref name="headBaseDirectory"/>, the running head's <c>AppContext.BaseDirectory</c>) exist.
    /// </summary>
    /// <param name="isJsonFormat">
    /// The project's format (<see cref="Gum.DataTypes.GumProjectSave.IsJsonFormat"/>). Only a .gumj
    /// project may prefer the Native AOT build - a .gumx project's XmlSerializer load path is not
    /// Native-AOT-safe (see <c>IConvertProjectToJsonService</c>), so it always uses ReadyToRun.
    /// </param>
    public static string? Resolve(string headBaseDirectory, bool isJsonFormat)
    {
        foreach (string candidate in Candidates(headBaseDirectory, isJsonFormat))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }

    internal static IEnumerable<string> Candidates(string headBaseDirectory, bool isJsonFormat)
    {
        string exeName = OperatingSystem.IsWindows() ? "GumPreview.exe" : "GumPreview";

        // The Native AOT build is preferred for .gumj (faster startup), but is additive, not a
        // replacement - a package/platform that has not published it (or an older published head)
        // falls back to the ReadyToRun build, which loads .gumj projects fine too.
        if (isJsonFormat)
        {
            yield return Path.Combine(headBaseDirectory, AotPreviewFolderName, exeName);
        }
        yield return Path.Combine(headBaseDirectory, PreviewFolderName, exeName);

        string? repoRoot = FindRepoRoot(headBaseDirectory);
        if (repoRoot != null)
        {
            string devBinRoot = Path.Combine(repoRoot, "Tool", "GumPreview", "bin");
            foreach (string configuration in DevBuildConfigurations)
            {
                foreach (string targetFramework in DevBuildTargetFrameworks)
                {
                    yield return Path.Combine(devBinRoot, configuration, targetFramework, exeName);
                }
            }
        }
    }

    // Walks up from the head's base directory looking for the repo root, identified by the presence
    // of the GumPreview project itself.
    private static string? FindRepoRoot(string startDirectory)
    {
        DirectoryInfo? directory = new DirectoryInfo(startDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Tool", "GumPreview")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return null;
    }
}
