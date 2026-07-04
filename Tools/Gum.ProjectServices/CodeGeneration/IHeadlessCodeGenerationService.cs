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

    /// <summary>
    /// Writes the per-project Standard Elements fallback-registration file
    /// (<c>StandardElements.Generated.cs</c>) so Standard-Element-owned category/state assignments
    /// still work in code-only games (issue #3505). See
    /// <see cref="Gum.ProjectServices.CodeGeneration.CodeGenerator.GenerateStandardElementsFallbackCode"/>
    /// for what the file contains. Called automatically by <see cref="GenerateCodeForAllElements"/>;
    /// exposed separately so callers that generate elements individually (e.g. the CLI's
    /// <c>codegen</c> command) can still produce the fallback file for the whole project.
    /// </summary>
    /// <returns>
    /// True if the file was written; false if generation was skipped (unsupported
    /// <see cref="CodeOutputProjectSettings.OutputLibrary"/> or no
    /// <see cref="CodeOutputProjectSettings.CodeProjectRoot"/> configured).
    /// </returns>
    bool GenerateStandardElementsFallbackFile(GumProjectSave project, CodeOutputProjectSettings projectSettings);
}
