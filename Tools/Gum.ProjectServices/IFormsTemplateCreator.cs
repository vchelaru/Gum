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
}
