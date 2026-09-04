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
    /// <remarks>
    /// The extension is always the XML one - this helper has no access to the project's own file
    /// name, so it cannot tell a .gumx project from a .gumj one. Every caller today uses the result
    /// only via <c>RemoveExtension()</c> to reach a sibling file (.codsj settings, the Animations
    /// sidecar), where the extension is inert. Do NOT use it to read or write the element file
    /// itself: inside a .gumj project that path points at a file the project never loads back
    /// (issue #4595). Use <c>IFileCommands.GetFullPathXmlFile</c> (tool) or
    /// <see cref="ElementSave.GetFileExtension(bool)"/> with the project's format instead.
    /// </remarks>
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
