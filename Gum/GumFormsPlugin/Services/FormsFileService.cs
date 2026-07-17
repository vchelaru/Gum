using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace GumFormsPlugin.Services;

public class FormsFileService : IFormsFileService
{
    // The themes folder layout on disk is populated by GumFormsPlugin's post-build step.
    // Each immediate subdirectory is a theme (e.g. "Standard", "Bubblegum"); the
    // contents mirror what would live at the root of a Gum project.
    private const string FormsThemesSubfolder = "Content/FormsThemes";

    /// <inheritdoc/>
    public string DefaultThemeName => "Standard";

    private const string FormsGumxName = "GumProject.gumx";

    private readonly IProjectState _projectState;

    public FormsFileService(IProjectState projectState)
    {
        _projectState = projectState;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAvailableThemes()
    {
        var root = GetThemesRoot();
        if (!Directory.Exists(root)) return Array.Empty<string>();

        var names = Directory.GetDirectories(root)
            .Select(d => Path.GetFileName(d.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => string.Equals(n, DefaultThemeName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return names!;
    }

    /// <summary>
    /// Returns the path to the GumProject.gumx file for the given theme.
    /// </summary>
    public string GetFormsGumxPath(string themeName) =>
        Path.Combine(GetThemeDirectory(themeName), FormsGumxName)
            .Replace('\\', '/');

    /// <inheritdoc/>
    public string GetThemeDirectory(string themeName) =>
        Path.Combine(GetThemesRoot(), themeName)
            .Replace('\\', '/') + "/";

    /// <inheritdoc/>
    /// <remarks>Extensions skipped: .gumx, .gumfcs, .ganx (animation files, deferred), .codsj</remarks>
    public Dictionary<string, FilePath> GetSourceDestinations(string themeName, bool isIncludeDemoScreenGum)
    {
        var destinationFolder = _projectState.ProjectDirectory;

        var sourceDestinations = new Dictionary<string, FilePath>();

        if (string.IsNullOrEmpty(destinationFolder)) return sourceDestinations;

        string themeDir = GetThemeDirectory(themeName);

        if (!Directory.Exists(themeDir)) return sourceDestinations;

        var allFiles = Directory.GetFiles(themeDir, "*.*", SearchOption.AllDirectories);

        // Compute the canonical path of the root-level FontCache folder once, outside the loop
        string fontCachePath = new DirectoryInfo(Path.Combine(themeDir, "FontCache")).FullName
            + Path.DirectorySeparatorChar;

        foreach (var sourceFile in allFiles)
        {
            var extension = FileManager.GetExtension(sourceFile);

            // Skip font cache — fonts are regenerated locally and should not be copied
            if (new FileInfo(sourceFile).FullName.StartsWith(fontCachePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Skip per-theme metadata files (file-list manifest and prerequisite
            // declarations). They describe the theme but aren't part of the user's project.
            var fileName = Path.GetFileName(sourceFile);
            if (string.Equals(fileName, "manifest.txt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, ThemeRequirements.ThemeRequirementsFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Skip files that are not content or not relevant to import
            if (extension is "gumx" or "gumfcs" or
                "ganx" or "codsj" or "bmfc" or "fnt" or "exe" or "setj" or "json")
            {
                continue;
            }

            // Only include the demo screen if requested
            if (extension == "gusx")
            {
                bool isDemoScreen = sourceFile.Contains("DemoScreenGum.gusx");
                if (!isDemoScreen || !isIncludeDemoScreenGum)
                {
                    continue;
                }
            }

            // Compute the relative path from the theme directory
            string relativePath = sourceFile
                .Replace('\\', '/')
                .Substring(themeDir.Length)
                .TrimStart('/');

            string absoluteDestination = destinationFolder + relativePath;
            sourceDestinations.Add(sourceFile, absoluteDestination);
        }

        return sourceDestinations;
    }

    private static string GetThemesRoot() =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FormsThemesSubfolder);
}
