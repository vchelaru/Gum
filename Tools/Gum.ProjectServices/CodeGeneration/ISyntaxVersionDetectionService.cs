namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Detects the syntax version of the Gum runtime referenced by a game project.
/// </summary>
public interface ISyntaxVersionDetectionService
{
    /// <summary>
    /// Resolves the effective syntax version for code generation, taking into account
    /// the user's <see cref="CodeOutputProjectSettings.SyntaxVersion"/> setting.
    /// Returns <c>"*"</c> for auto-detect or an explicit version number.
    /// </summary>
    SyntaxVersionResult Detect(CodeOutputProjectSettings settings, string? projectDirectory);
}
