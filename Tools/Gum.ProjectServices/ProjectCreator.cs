using System.IO;
using System.Linq;
using System.Reflection;
using Gum.DataTypes;
using Gum.Managers;
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
        "Behaviors",
        "Fonts",
        // Created up front (rather than lazily on first font generation) so the file watcher
        // never has to watch a directory that doesn't exist yet on a freshly-created project (#4259).
        "FontCache"
    };

    // ColoredRectangle is intentionally omitted: the v3 Rectangle carries the full
    // fill/stroke/gradient/dropshadow surface, making it redundant for new projects
    // (#2965 phase 2). It stays loadable for legacy projects that already contain it.
    private static readonly string[] StandardElementNames =
    {
        "Circle",
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

        WriteStandardElements(directory);
        ExtractExampleSpriteFrame(directory);
        new DefaultFontBundler().CopyTo(directory);

        var project = new GumProjectSave
        {
            FontGenerator = FontGeneratorType.KernSmith,
            FullFileName = filePath,
            // The default standard elements seed the latest variable surface, so stamp the
            // project at the matching version rather than the GumProjectSave ctor default.
            Version = GumProjectSave.NativeVersion
        };

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

    // Builds standards from StandardElementsManager -- the same source the editor's File > New
    // Project uses (ProjectManager.CreateNewProject) -- instead of a hand-maintained duplicate,
    // so the two paths can't drift the way #4674 did (#4676).
    private static void WriteStandardElements(string directory)
    {
        var standardsDir = Path.Combine(directory, "Standards");

        StandardElementsManager.Self.Initialize();

        var referenceProject = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(referenceProject);

        foreach (var name in StandardElementNames)
        {
            var standard = referenceProject.StandardElements.FirstOrDefault(s => s.Name == name);
            if (standard == null)
            {
                continue;
            }

            var outputPath = Path.Combine(standardsDir, $"{name}.gutx");
            standard.Save(outputPath, useCompactFormat: true);
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
