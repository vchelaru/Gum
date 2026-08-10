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
    /// Gets the full path to an element's XML file given the project directory. Pass
    /// <paramref name="forcedElementName"/> to resolve the path the element had under a different
    /// name, which rename uses to locate files still sitting at the old name.
    /// </summary>
    public static FilePath? GetFullPathXmlFile(ElementSave? element, string? projectDirectory,
        string? forcedElementName = null)
    {
        if (element == null || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        string elementName = string.IsNullOrEmpty(forcedElementName) ? element.Name : forcedElementName!;

        return projectDirectory + element.Subfolder + "\\" + elementName + "." + element.FileExtension;
    }
}
