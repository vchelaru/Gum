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
    /// The one place a batch of elements is generated, for both <c>gumcli codegen</c> and the tool's
    /// Generate All. Skips elements set to never generate, runs the optional gate, generates the rest,
    /// and, only when <see cref="CodeGenerationRunOptions.Elements"/> is null (the whole project),
    /// writes the per-project Standard Elements fallback-registration file
    /// (<c>StandardElements.Generated.cs</c>, issue #3505; see
    /// <see cref="CodeGenerator.GenerateStandardElementsFallbackCode"/>). The file is skipped when
    /// the output library doesn't use it or no
    /// <see cref="CodeOutputProjectSettings.CodeProjectRoot"/> is configured.
    /// </summary>
    CodeGenerationRunResult GenerateCodeForElements(GumProjectSave project,
        CodeOutputProjectSettings projectSettings, CodeGenerationRunOptions? options = null);
}
