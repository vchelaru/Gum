namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Inspects the file system to automatically produce a <see cref="CodeOutputProjectSettings"/>
/// for a Gum project. Call <see cref="Run"/> with the path to the .gumx file.
/// </summary>
public interface ICodeGenerationAutoSetupService
{
    /// <summary>
    /// Walks up from the .gumx directory to locate a .csproj, then derives
    /// <c>CodeProjectRoot</c>, <c>RootNamespace</c>, and <c>OutputLibrary</c> from it.
    /// </summary>
    /// <param name="gumxFilePath">Absolute or relative path to the .gumx file.</param>
    /// <returns>
    /// An <see cref="AutoSetupResult"/> whose <see cref="AutoSetupResult.Success"/> is
    /// <see langword="true"/> when a .csproj was found, or <see langword="false"/> with an
    /// <see cref="AutoSetupResult.ErrorMessage"/> when none could be found.
    /// </returns>
    AutoSetupResult Run(string gumxFilePath);
}
