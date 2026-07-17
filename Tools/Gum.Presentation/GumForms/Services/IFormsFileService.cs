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
    /// Returns the base directory of the given theme's files (trailing slash, forward slashes).
    /// </summary>
    string GetThemeDirectory(string themeName);

    /// <summary>
    /// Returns a mapping of source file paths (in the selected theme's folder) to destination
    /// file paths (in the user's Gum project directory).
    /// </summary>
    Dictionary<string, FilePath> GetSourceDestinations(string themeName, bool isIncludeDemoScreenGum);
}
