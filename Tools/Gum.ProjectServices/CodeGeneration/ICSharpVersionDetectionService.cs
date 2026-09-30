namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Detects the C# language version the game project compiles with, so codegen can avoid
/// syntax the project can't compile (e.g. file-scoped namespaces in Unity's C# 9).
/// </summary>
public interface ICSharpVersionDetectionService
{
    /// <summary>
    /// Returns the major C# version from the game .csproj's <c>LangVersion</c> or, when absent,
    /// its target framework's default. Returns null when it can't be determined or is open-ended
    /// (<c>latest</c>, <c>preview</c>), meaning codegen can use the newest syntax it emits.
    /// </summary>
    int? Detect(CodeOutputProjectSettings settings, string? projectDirectory);
}
