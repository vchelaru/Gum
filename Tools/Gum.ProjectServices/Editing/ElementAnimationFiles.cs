using System.IO;
using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Gum.StateAnimation.SaveClasses;
using ToolsUtilities;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Where an element's animation sidecar (<c>.ganx</c>, or <c>.ganj</c> in a JSON project) lives and how it is
/// written. The tool and the CLI both go through it.
/// </summary>
public static class ElementAnimationFiles
{
    /// <summary>
    /// The sidecar path for <paramref name="element"/>, in the project's own format, or null when the project has no
    /// file name yet. The suffix follows the project so a <c>.gumj</c> project finds <c>.ganj</c>.
    /// </summary>
    public static FilePath? GetPath(ElementSave element, GumProjectSave project)
    {
        if (string.IsNullOrEmpty(project.FullFileName))
        {
            return null;
        }

        string projectDirectory = FileManager.GetDirectory(project.FullFileName);
        FilePath? elementXmlPath = ElementFilePathHelper.GetFullPathXmlFile(element, projectDirectory);
        if (elementXmlPath == null)
        {
            return null;
        }

        return elementXmlPath.RemoveExtension() +
            ElementAnimationsSave.GetFileNameSuffix(GumProjectSave.IsJsonFormat(project.FullFileName));
    }

    /// <summary>
    /// Writes <paramref name="save"/> to <paramref name="path"/>. A save with no animations deletes the file instead,
    /// so an element with none has no sidecar.
    /// </summary>
    public static void WriteOrDelete(string path, ElementAnimationsSave save)
    {
        if (save.Animations.Count > 0)
        {
            save.Save(path);
        }
        else if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
