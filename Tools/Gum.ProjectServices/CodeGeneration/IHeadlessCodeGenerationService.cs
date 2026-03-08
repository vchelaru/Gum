using Gum.DataTypes;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Generates code for Gum elements without requiring the Gum tool UI.
/// </summary>
public interface IHeadlessCodeGenerationService
{
    /// <summary>
    /// Generates code for a single element.
    /// </summary>
    /// <returns>True if generation succeeded, false otherwise.</returns>
    bool GenerateCodeForElement(ElementSave element, CodeOutputElementSettings elementSettings,
        CodeOutputProjectSettings projectSettings, bool checkForMissing = true);

    /// <summary>
    /// Generates code for all screens and components in the project.
    /// </summary>
    /// <returns>The number of elements for which code was generated.</returns>
    int GenerateCodeForAllElements(GumProjectSave project, CodeOutputProjectSettings projectSettings);
}
