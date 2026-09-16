using System;
using System.Collections.Generic;
using System.IO;

namespace Gum.Plugins.InternalPlugins.EditorTab.Services;

/// <summary>
/// Finds the GumPreview runtime host executable (issue #4697). Checked in priority order: the
/// published layout ships it in a <c>Preview-Aot/</c> (Native AOT) or <c>Preview/</c> (ReadyToRun)
/// folder next to the head executable; a dev build falls back to the project's own build output
/// under <c>Tool/GumPreview</c>, found by walking up from the head's base directory. The Native AOT
/// build is always preferred (faster startup) and now serves every project format, including
/// .gumx - PreviewLauncher converts a .gumx project to a temporary JSON copy first (issue #4748),
/// since XmlSerializer itself is not Native-AOT-safe.
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
    /// Resolves the preview executable to launch, or null if none of the candidate locations
    /// (relative to <paramref name="headBaseDirectory"/>, the running head's
    /// <c>AppContext.BaseDirectory</c>) exist. The Native AOT build is preferred whenever present
    /// (issue #4748: it now serves every project format), falling back to the ReadyToRun build, then
    /// a local dev build.
    /// </summary>
    public static ResolvedPreviewExecutable? Resolve(string headBaseDirectory)
    {
        foreach (ResolvedPreviewExecutable candidate in Candidates(headBaseDirectory))
        {
            if (File.Exists(candidate.ExecutablePath))
            {
                return candidate;
            }
        }
        return null;
    }

    internal static IEnumerable<ResolvedPreviewExecutable> Candidates(string headBaseDirectory)
    {
        string exeName = OperatingSystem.IsWindows() ? "GumPreview.exe" : "GumPreview";

        // The Native AOT build is additive, not a replacement - a package/platform that has not
        // published it (or an older published head) falls back to the ReadyToRun build.
        yield return new ResolvedPreviewExecutable(Path.Combine(headBaseDirectory, AotPreviewFolderName, exeName), IsNativeAot: true);
        yield return new ResolvedPreviewExecutable(Path.Combine(headBaseDirectory, PreviewFolderName, exeName), IsNativeAot: false);

        string? repoRoot = FindRepoRoot(headBaseDirectory);
        if (repoRoot != null)
        {
            string devBinRoot = Path.Combine(repoRoot, "Tool", "GumPreview", "bin");
            foreach (string configuration in DevBuildConfigurations)
            {
                foreach (string targetFramework in DevBuildTargetFrameworks)
                {
                    yield return new ResolvedPreviewExecutable(Path.Combine(devBinRoot, configuration, targetFramework, exeName), IsNativeAot: false);
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

/// <summary>A preview executable candidate <see cref="PreviewExecutableLocator.Resolve"/> found on disk.</summary>
/// <param name="ExecutablePath">Full path to the GumPreview executable.</param>
/// <param name="IsNativeAot">
/// Whether this is the Native AOT build (<see cref="PreviewExecutableLocator.AotPreviewFolderName"/>) as
/// opposed to the ReadyToRun build or a local dev build. A .gumx project launched against the Native
/// AOT build needs converting to JSON first - see <c>PreviewLauncher</c>.
/// </param>
public readonly record struct ResolvedPreviewExecutable(string ExecutablePath, bool IsNativeAot);
