namespace GumFormsPlugin.Services;

/// <summary>
/// Copies a Forms theme's content into the currently-loaded project and registers its
/// elements. Used both by the Add Forms dialog and by new-project creation, so the two
/// paths import identically.
/// </summary>
public interface IFormsThemeImporter
{
    /// <summary>
    /// Applies the theme's project-level prerequisites, copies its files next to the project,
    /// and imports the resulting screens, components and behaviors. Requires a project that has
    /// already been saved to disk, since the destination paths are relative to the gumx.
    /// </summary>
    /// <param name="themeName">Theme to import, as named by <see cref="IFormsFileService.GetAvailableThemes"/>.</param>
    /// <param name="isIncludeDemoScreenGum">Whether the theme's demo screen is imported alongside its controls.</param>
    /// <returns>True when the content was imported; false when the user declined an overwrite prompt.</returns>
    bool ImportTheme(string themeName, bool isIncludeDemoScreenGum);
}
