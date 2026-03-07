using System.IO;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class ProjectCreator : IProjectCreator
{
    private static readonly string[] StandardSubfolders =
    {
        "Screens",
        "Components",
        "Standards",
        "Behaviors"
    };

    /// <inheritdoc/>
    public GumProjectSave Create(string filePath)
    {
        var directory = FileManager.GetDirectory(filePath);

        foreach (var subfolder in StandardSubfolders)
        {
            var subfolderPath = Path.Combine(directory, subfolder);
            Directory.CreateDirectory(subfolderPath);
        }

        var project = new GumProjectSave();
        project.FullFileName = filePath;
        project.Save(filePath, saveElements: false);

        return project;
    }
}
