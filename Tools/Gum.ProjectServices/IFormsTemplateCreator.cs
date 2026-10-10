namespace Gum.ProjectServices;

/// <summary>
/// Creates new Gum projects pre-populated with the Forms template content
/// (behaviors, components, standards, screens, and UISpriteSheet).
/// </summary>
public interface IFormsTemplateCreator
{
    /// <summary>
    /// Creates a new Gum project with Forms template content at the specified path.
    /// The path must end in .gumx. All template files are extracted alongside the
    /// project file, and the project file is named after the .gumx file.
    /// </summary>
    /// <param name="filePath">Absolute path ending in .gumx for the new project.</param>
    void Create(string filePath);

    /// <summary>
    /// Creates a new Gum project from a Forms theme folder (see <see cref="IFormsThemeLocator"/>) instead of the
    /// embedded Forms template. The theme's files are copied alongside the project file, its shared behaviors
    /// are written into the project's own Behaviors folder, and the project file takes the name of <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">Absolute path ending in .gumx or .gumj for the new project.</param>
    /// <param name="themeDirectory">The theme folder, containing its <c>GumProject.gumx</c>.</param>
    void CreateFromTheme(string filePath, string themeDirectory);
}
