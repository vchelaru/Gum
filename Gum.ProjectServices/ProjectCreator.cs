using System.IO;
using System.Reflection;
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

    private static readonly string[] StandardElementNames =
    {
        "Circle",
        "ColoredRectangle",
        "Component",
        "Container",
        "NineSlice",
        "Polygon",
        "Rectangle",
        "Sprite",
        "Text"
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

        ExtractStandardElements(directory);
        ExtractExampleSpriteFrame(directory);

        var project = new GumProjectSave();
        project.FullFileName = filePath;

        foreach (var name in StandardElementNames)
        {
            project.StandardElementReferences.Add(new ElementReference
            {
                Name = name,
                ElementType = ElementType.Standard
            });
        }

        project.Save(filePath, saveElements: false);

        return project;
    }

    private static void ExtractStandardElements(string directory)
    {
        var standardsDir = Path.Combine(directory, "Standards");
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var name in StandardElementNames)
        {
            var resourceName = $"Gum.ProjectServices.Templates.Default.Standards.{name}.gutx";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    continue;
                }

                var outputPath = Path.Combine(standardsDir, $"{name}.gutx");
                using (var fileStream = File.Create(outputPath))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
    }

    private static void ExtractExampleSpriteFrame(string directory)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Gum.ProjectServices.Templates.Default.ExampleSpriteFrame.png";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                return;
            }

            var outputPath = Path.Combine(directory, "ExampleSpriteFrame.png");
            using (var fileStream = File.Create(outputPath))
            {
                stream.CopyTo(fileStream);
            }
        }
    }
}
