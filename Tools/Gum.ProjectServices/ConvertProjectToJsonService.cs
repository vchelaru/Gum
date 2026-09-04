using System;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Serialization.Json;
using Gum.Logic.FileWatch;
using Gum.StateAnimation.SaveClasses;
using ToolsUtilities;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class ConvertProjectToJsonService : IConvertProjectToJsonService
{
    private readonly IFileWatchIgnoreList _fileWatchIgnoreList;

    public ConvertProjectToJsonService(IFileWatchIgnoreList fileWatchIgnoreList)
    {
        _fileWatchIgnoreList = fileWatchIgnoreList;
    }

    /// <inheritdoc/>
    public ConvertProjectToJsonResult ConvertToJson(GumProjectSave project)
    {
        if (project == null)
        {
            throw new ArgumentNullException(nameof(project));
        }
        if (string.IsNullOrEmpty(project.FullFileName))
        {
            throw new InvalidOperationException(
                "The project must be saved to a .gumx file before it can be converted to JSON.");
        }
        if (string.Equals(FileManager.GetExtension(project.FullFileName), GumProjectSave.ProjectJsonExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The project is already in JSON format.");
        }

        string projectDirectory = FileManager.GetDirectory(project.FullFileName);
        string jsonProjectPath = FileManager.RemoveExtension(project.FullFileName) + "." + GumProjectSave.ProjectJsonExtension;

        // GumProjectSave.Save dispatches XML vs JSON purely off the target path's extension, and
        // cascades to every Screen/Component/StandardElement (skipping IsSourceFileMissing stubs -
        // issue #3369). It does NOT cascade to Behaviors or element animations - those aren't part
        // of the project's own referenced-element save cascade, so this method converts them below.
        //
        // Every one of those cascaded writes must be registered with IgnoreNextChangeUntil first
        // (issue #4219) - otherwise the file watcher reacts to each write as an external change and
        // triggers a full element reload + tree-view refresh per file, mirroring the pattern already
        // used by FileCommands/ProjectManager when they save.
        IgnoreUpcomingElementWrites(project, projectDirectory);
        _fileWatchIgnoreList.IgnoreNextChangeUntil(jsonProjectPath);
        project.Save(jsonProjectPath, saveElements: true);

        return new ConvertProjectToJsonResult
        {
            ProjectFilePath = jsonProjectPath,
            ScreenCount = project.Screens.Count(s => !s.IsSourceFileMissing),
            ComponentCount = project.Components.Count(c => !c.IsSourceFileMissing),
            StandardElementCount = project.StandardElements.Count(s => !s.IsSourceFileMissing),
            BehaviorCount = ConvertBehaviors(project, projectDirectory),
            AnimationCount = ConvertAnimations(project, projectDirectory),
        };
    }

    private void IgnoreUpcomingElementWrites(GumProjectSave project, string projectDirectory)
    {
        foreach (ScreenSave screen in project.Screens)
        {
            IgnoreElementJsonWrite(screen, projectDirectory);
        }
        foreach (ComponentSave component in project.Components)
        {
            IgnoreElementJsonWrite(component, projectDirectory);
        }
        foreach (StandardElementSave standard in project.StandardElements)
        {
            IgnoreElementJsonWrite(standard, projectDirectory);
        }
    }

    private void IgnoreElementJsonWrite(ElementSave element, string projectDirectory)
    {
        if (element.IsSourceFileMissing)
        {
            return;
        }

        string elementXmlPath = GetElementXmlPath(element, projectDirectory);
        _fileWatchIgnoreList.IgnoreNextChangeUntil(ToJsonSiblingPath(elementXmlPath));
    }

    private int ConvertBehaviors(GumProjectSave project, string projectDirectory)
    {
        int count = 0;

        foreach (BehaviorSave behavior in project.Behaviors)
        {
            if (behavior.IsSourceFileMissing)
            {
                continue;
            }

            BehaviorReference? reference = project.BehaviorReferences
                .FirstOrDefault(r => r.Name == behavior.Name);
            if (reference == null)
            {
                continue;
            }

            string xmlPath = projectDirectory + reference.GetRelativeFilePath(isJsonFormat: false);
            string jsonPath = ToJsonSiblingPath(xmlPath);
            _fileWatchIgnoreList.IgnoreNextChangeUntil(jsonPath);
            behavior.Save(jsonPath);
            count++;
        }

        return count;
    }

    private int ConvertAnimations(GumProjectSave project, string projectDirectory)
    {
        int count = 0;

        foreach (ScreenSave screen in project.Screens)
        {
            count += ConvertAnimationIfPresent(screen, projectDirectory);
        }
        foreach (ComponentSave component in project.Components)
        {
            count += ConvertAnimationIfPresent(component, projectDirectory);
        }
        foreach (StandardElementSave standard in project.StandardElements)
        {
            count += ConvertAnimationIfPresent(standard, projectDirectory);
        }

        return count;
    }

    /// <summary>
    /// Converts <paramref name="element"/>'s <c>{ElementName}Animations.ganx</c> to
    /// <c>{ElementName}Animations.ganj</c> (see <c>GumService.LoadAnimationsFromProvider</c> for the
    /// same naming convention) when that file exists. Returns 1 when converted, 0 otherwise.
    /// </summary>
    private int ConvertAnimationIfPresent(ElementSave element, string projectDirectory)
    {
        if (element.IsSourceFileMissing)
        {
            return 0;
        }

        string elementXmlPath = GetElementXmlPath(element, projectDirectory);
        string animationXmlPath = FileManager.RemoveExtension(elementXmlPath) + ElementAnimationsSave.GetFileNameSuffix(isJsonFormat: false);

        if (!FileManager.FileExists(animationXmlPath))
        {
            return 0;
        }

        ElementAnimationsSave animations = ElementAnimationsSave.Load(animationXmlPath);
        string animationJsonPath = FileManager.RemoveExtension(elementXmlPath) + ElementAnimationsSave.GetFileNameSuffix(isJsonFormat: true);
        _fileWatchIgnoreList.IgnoreNextChangeUntil(animationJsonPath);
        GumJsonFileSerializer.WriteToFile(animationJsonPath, GumAnimationJsonFileSerializer.SerializeElementAnimations(animations));

        return 1;
    }

    private string GetElementXmlPath(ElementSave element, string projectDirectory) =>
        projectDirectory + element.Subfolder + "/" + element.Name + "." + element.FileExtension;

    /// <summary>
    /// Every JSON sibling extension in this format is its XML counterpart with the trailing "x"
    /// swapped for "j" (behx-&gt;behj), matching the convention used throughout
    /// <see cref="GumJsonFileSerializer"/>/<see cref="BehaviorSave.Save"/>.
    /// </summary>
    private string ToJsonSiblingPath(string xmlPath) =>
        xmlPath.Substring(0, xmlPath.Length - 1) + "j";
}
