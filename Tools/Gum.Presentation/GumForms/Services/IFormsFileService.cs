using System.Collections.Generic;
using ToolsUtilities;

namespace GumFormsPlugin.Services;

/// <summary>
/// Reads the Forms theme content shipped with the tool (<c>Content/FormsThemes</c>) and maps
/// it onto destination paths within the user's Gum project.
/// </summary>
public interface IFormsFileService
{
    /// <summary>
    /// The theme that is preselected in the Add Forms dialog when present on disk.
    /// </summary>
    string DefaultThemeName { get; }

    /// <summary>
    /// Returns the names of themes shipped with the tool (folders present under
    /// <c>Content/FormsThemes</c>), with the default theme listed first when found.
    /// </summary>
    IReadOnlyList<string> GetAvailableThemes();

    /// <summary>
    /// Returns the path to the given theme's own <c>GumProject.gumx</c>, which loads as a normal
    /// import source.
    /// </summary>
    string GetFormsGumxPath(string themeName);

    /// <summary>
    /// Returns the base directory of the given theme's files (trailing slash, forward slashes).
    /// </summary>
    string GetThemeDirectory(string themeName);

    /// <summary>
    /// Whether the open project already has any component or behavior file the theme ships, in
    /// either project format. Only the theme's component and behavior files count: a project always
    /// has Standards, and bundled fonts or images say nothing about Forms.
    /// </summary>
    bool ProjectHasElementsOf(string themeName);

    /// <summary>
    /// Returns the absolute path to the theme's preview image (<c>preview.png</c> in its folder),
    /// or null when the theme ships none.
    /// </summary>
    string? GetThemePreviewImagePath(string themeName);
}
