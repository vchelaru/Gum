using Gum.DataTypes;
using ToolsUtilities;

namespace StateAnimationPlugin.Managers;

/// <summary>
/// Resolves the absolute path of an element's animation (.ganx) file.
/// </summary>
public interface IAnimationFilePathService
{
    /// <summary>
    /// Returns the absolute animation file path for the currently selected element, named after
    /// <paramref name="elementName"/>. Returns null when no element is selected.
    /// </summary>
    FilePath? GetAbsoluteAnimationFileNameFor(string elementName);

    /// <summary>
    /// Returns the absolute animation file path for the given element, or null when the element's
    /// XML path cannot be resolved (e.g. no project is loaded).
    /// </summary>
    FilePath? GetAbsoluteAnimationFileNameFor(ElementSave elementSave);
}
