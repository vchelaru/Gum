using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <summary>
/// Finds the Forms themes (Bubblegum, Meadow, ...) that <c>gumcli new --theme</c> can create a project from.
/// </summary>
public interface IFormsThemeLocator
{
    /// <summary>
    /// The names of every theme found, sorted alphabetically. Does not include the default "Standard" theme,
    /// which is the plain Forms template.
    /// </summary>
    IReadOnlyList<string> GetAvailableThemes();

    /// <summary>
    /// Returns the folder holding the theme's <c>GumProject.gumx</c>, or <see langword="null"/> when no theme
    /// has that name. The name is matched without regard to case.
    /// </summary>
    string? FindThemeDirectory(string themeName);
}
