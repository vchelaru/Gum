using System;
using System.Collections.Generic;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Localization;

/// <summary>
/// Loads a Gum project's <see cref="GumProjectSave.LocalizationFiles"/> into an
/// <see cref="ILocalizationService"/> during a runtime <c>GumService.Initialize</c>. Shared by every
/// runtime's GumService so the MonoGame family, raylib and Skia apply one policy.
/// </summary>
internal static class ProjectLocalizationLoader
{
    /// <summary>
    /// Loads the localization files of <paramref name="gumProject"/> into
    /// <paramref name="localizationService"/>. The policy mirrors the tool's
    /// <c>FileCommands.LoadLocalizationFile</c>: one file dispatches by extension; several files must
    /// all be .resx and are merged; mixed CSV/RESX or multiple CSVs are skipped with a warning because
    /// <see cref="ILocalizationService"/> has no merge API for them.
    /// </summary>
    /// <param name="gumProject">The loaded project; its directory anchors the relative file paths.</param>
    /// <param name="localizationService">The service to load into. Null skips loading.</param>
    /// <param name="warnings">Receives non-fatal problems (skipped files, key collisions).</param>
    internal static void Load(GumProjectSave gumProject, ILocalizationService? localizationService, ICollection<string> warnings)
    {
        var localizationFiles = gumProject.LocalizationFiles;
        if (localizationService == null || localizationFiles == null || localizationFiles.Count == 0)
        {
            return;
        }

        // A project with localization files was loaded from disk, so it has a file name.
        var projectDirectory = FileManager.GetDirectory(gumProject.FullFileName!);
        var resolvedPaths = ResolveLocalizationFilePaths(projectDirectory, localizationFiles);

        if (resolvedPaths.Count == 1)
        {
            var fileName = resolvedPaths[0];

            if (IsResx(fileName))
            {
                // RESX satellite discovery requires enumerating the directory
                // (e.g. Strings.es.resx alongside Strings.resx). On desktop platforms
                // the path-based overload handles this via Directory.GetFiles.
                // Bundled-content platforms (Android/iOS/TitleContainer) cannot
                // enumerate sibling files from a stream, so this auto-load path
                // assumes real filesystem access - matching the existing CSV behavior.
                localizationService.AddResxDatabase(fileName);
            }
            else
            {
                using var stream = FileManager.GetStreamForFile(fileName);
                localizationService.AddCsvDatabase(stream);
            }
        }
        else if (resolvedPaths.Count > 1)
        {
            if (!resolvedPaths.TrueForAll(IsResx))
            {
                warnings.Add(
                    "Localization: multiple files configured but not all are .resx. " +
                    "Mixed CSV/RESX and multi-CSV loading are not supported. Loading was skipped.");
                return;
            }

            var existingPaths = new List<string>();
            foreach (var path in resolvedPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    existingPaths.Add(path);
                }
                else
                {
                    warnings.Add($"Localization: file not found, skipping: {path}");
                }
            }

            if (existingPaths.Count > 0)
            {
                localizationService.AddResxDatabase(
                    existingPaths,
                    onWarning: message => warnings.Add("Localization warning: " + message));
            }
        }
    }

    /// <summary>
    /// Joins each non-empty project-relative localization path onto <paramref name="projectDirectory"/>,
    /// with native separators. A project saved on Windows stores these paths with backslashes, which
    /// macOS/Linux read as part of the file name.
    /// </summary>
    internal static List<string> ResolveLocalizationFilePaths(string projectDirectory, IEnumerable<string?> relativePaths)
    {
        var resolvedPaths = new List<string>();
        foreach (var relative in relativePaths)
        {
            if (!string.IsNullOrEmpty(relative))
            {
                resolvedPaths.Add(FileManager.Standardize(projectDirectory + relative, preserveCase: true));
            }
        }
        return resolvedPaths;
    }

    private static bool IsResx(string path) =>
        string.Equals(FileManager.GetExtension(path), "resx", StringComparison.OrdinalIgnoreCase);
}
