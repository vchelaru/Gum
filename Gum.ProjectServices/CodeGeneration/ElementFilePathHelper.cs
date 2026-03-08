using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Computes full file paths for elements without relying on Gum tool services.
/// Replaces the tool-specific <c>ElementSave.GetFullPathXmlFile()</c> extension method.
/// </summary>
public static class ElementFilePathHelper
{
    /// <summary>
    /// Gets the full path to an element's XML file given the project directory.
    /// </summary>
    public static FilePath? GetFullPathXmlFile(ElementSave? element, string? projectDirectory)
    {
        if (element == null || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        return projectDirectory + element.Subfolder + "\\" + element.Name + "." + element.FileExtension;
    }
}
