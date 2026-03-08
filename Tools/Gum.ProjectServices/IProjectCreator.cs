using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// Creates new blank Gum projects on disk.
/// </summary>
public interface IProjectCreator
{
    /// <summary>
    /// Creates a new blank Gum project at the specified path. The path should
    /// end in .gumx. The standard subfolder structure (Screens, Components,
    /// Standards, Behaviors) will be created alongside the project file.
    /// </summary>
    GumProjectSave Create(string filePath);
}
