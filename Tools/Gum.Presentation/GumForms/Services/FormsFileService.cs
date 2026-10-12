using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    private const string PreviewImageFileName = "preview.png";

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

    /// <inheritdoc/>
    public string GetFormsGumxPath(string themeName) =>
        Path.Combine(GetThemeDirectory(themeName), FormsGumxName)
            .Replace('\\', '/');

    /// <inheritdoc/>
    public string GetThemeDirectory(string themeName) =>
        Path.Combine(GetThemesRoot(), themeName)
            .Replace('\\', '/') + "/";

    /// <inheritdoc/>
    public bool ProjectHasElementsOf(string themeName)
    {
        string? projectDirectory = _projectState.ProjectDirectory;
        string themeDirectory = GetThemeDirectory(themeName);
        if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(themeDirectory))
        {
            return false;
        }

        return ProjectHasFilesOf(projectDirectory, themeDirectory, "Components",
            GumProjectSave.ComponentExtension, GumProjectSave.ComponentJsonExtension)
            || ProjectHasFilesOf(projectDirectory, themeDirectory, "Behaviors",
                BehaviorReference.Extension, BehaviorReference.JsonExtension);
    }

    // A theme's own files are always XML; the project may keep the same element in either format.
    private static bool ProjectHasFilesOf(
        string projectDirectory, string themeDirectory, string subfolder, string xmlExtension, string jsonExtension)
    {
        string themeSubfolder = Path.Combine(themeDirectory, subfolder);
        if (!Directory.Exists(themeSubfolder))
        {
            return false;
        }

        foreach (string themeFile in Directory.GetFiles(themeSubfolder, "*." + xmlExtension, SearchOption.AllDirectories))
        {
            string relativeWithoutExtension = Path.ChangeExtension(Path.GetRelativePath(themeDirectory, themeFile), null);
            string projectPathWithoutExtension = Path.Combine(projectDirectory, relativeWithoutExtension);
            if (File.Exists(projectPathWithoutExtension + "." + xmlExtension)
                || File.Exists(projectPathWithoutExtension + "." + jsonExtension))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public string? GetThemePreviewImagePath(string themeName)
    {
        string path = Path.Combine(GetThemeDirectory(themeName), PreviewImageFileName);
        return File.Exists(path) ? path : null;
    }

    private static string GetThemesRoot() =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FormsThemesSubfolder);
}
