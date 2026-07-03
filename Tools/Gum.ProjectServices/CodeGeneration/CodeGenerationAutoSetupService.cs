using System;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Holds the result of an auto-setup attempt performed by <see cref="CodeGenerationAutoSetupService"/>.
/// </summary>
public class AutoSetupResult
{
    /// <summary>Gets whether the auto-setup succeeded and produced valid settings.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the human-readable error message when <see cref="Success"/> is <see langword="false"/>.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Gets the configured settings when <see cref="Success"/> is <see langword="true"/>.</summary>
    public CodeOutputProjectSettings? Settings { get; init; }
}

/// <summary>
/// Inspects the file system to automatically configure <see cref="CodeOutputProjectSettings"/> for a
/// Gum project. Walks up from the .gumx directory to find the nearest project file (see
/// <see cref="RecognizedProjectFileExtensions"/>), then derives <c>CodeProjectRoot</c>,
/// <c>RootNamespace</c>, and <c>OutputLibrary</c> from it.
/// </summary>
public class CodeGenerationAutoSetupService : ICodeGenerationAutoSetupService
{
    /// <summary>
    /// File extensions (without the leading dot) recognized as marking a valid project root
    /// directory above a .gumx file. Includes <c>.csproj</c>/<c>.vbproj</c>/<c>.fsproj</c> as well as
    /// <c>.shproj</c> shared projects, which are a common container for Gum content in multi-head
    /// (e.g. Desktop + Blazor) setups.
    /// </summary>
    public static readonly string[] RecognizedProjectFileExtensions = { "csproj", "shproj", "vbproj", "fsproj" };

    /// <inheritdoc/>
    public AutoSetupResult Run(string gumxFilePath)
    {
        string gumxDirectory = Path.GetDirectoryName(Path.GetFullPath(gumxFilePath))
            ?? Path.GetFullPath(gumxFilePath);

        if (!gumxDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            gumxDirectory += Path.DirectorySeparatorChar;
        }

        string? projectDirectory = FindProjectDirectory(gumxDirectory);

        if (projectDirectory == null)
        {
            string extensionList = string.Join(", ", RecognizedProjectFileExtensions.Select(extension => $".{extension}"));
            return new AutoSetupResult
            {
                Success = false,
                ErrorMessage = $"No {extensionList} file found in any parent directory of the .gumx file. Cannot automatically configure code generation."
            };
        }

        return BuildResultFromCsprojDirectory(gumxDirectory, projectDirectory, explicitCsprojPath: null);
    }

    /// <inheritdoc/>
    public AutoSetupResult Run(string gumxFilePath, string explicitCsprojPath)
    {
        string fullCsprojPath = Path.GetFullPath(explicitCsprojPath);

        if (!File.Exists(fullCsprojPath))
        {
            return new AutoSetupResult
            {
                Success = false,
                ErrorMessage = $"Specified .csproj file not found: {fullCsprojPath}"
            };
        }

        string gumxDirectory = Path.GetDirectoryName(Path.GetFullPath(gumxFilePath))
            ?? Path.GetFullPath(gumxFilePath);

        if (!gumxDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            gumxDirectory += Path.DirectorySeparatorChar;
        }

        string csprojDirectory = Path.GetDirectoryName(fullCsprojPath)
            ?? Path.GetFullPath(fullCsprojPath);

        return BuildResultFromCsprojDirectory(gumxDirectory, csprojDirectory, explicitCsprojPath: fullCsprojPath);
    }

    private static AutoSetupResult BuildResultFromCsprojDirectory(
        string gumxDirectory, string projectDirectory, string? explicitCsprojPath)
    {
        string codeProjectRoot = Path.GetRelativePath(gumxDirectory, projectDirectory);
        if (codeProjectRoot == ".")
        {
            codeProjectRoot = "./";
        }
        else if (!codeProjectRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            && !codeProjectRoot.EndsWith("/", StringComparison.Ordinal))
        {
            codeProjectRoot += Path.DirectorySeparatorChar;
        }

        var settings = new CodeOutputProjectSettings
        {
            CodeProjectRoot = codeProjectRoot,
            ObjectInstantiationType = ObjectInstantiationType.FindByName
        };

        settings.SetDefaults();

        string? projectFilePath = explicitCsprojPath ?? RecognizedProjectFileExtensions
            .SelectMany(extension => Directory.EnumerateFiles(projectDirectory, $"*.{extension}", SearchOption.TopDirectoryOnly))
            .FirstOrDefault();

        // Shared projects (.shproj) don't carry PackageReference/RootNamespace MSBuild metadata like a
        // .csproj does, so leave OutputLibrary/RootNamespace at their defaults for the user to fill in
        // manually — same as the no-project-found fallback does today.
        bool isSharedProject = projectFilePath != null
            && string.Equals(Path.GetExtension(projectFilePath), ".shproj", StringComparison.OrdinalIgnoreCase);

        if (projectFilePath != null && !isSharedProject)
        {
            string contents = File.ReadAllText(projectFilePath);

            bool isMonoGameBased =
                contents.Contains("<PackageReference Include=\"MonoGame.Framework.", StringComparison.Ordinal) ||
                contents.Contains("<PackageReference Include=\"nkast.Xna.Framework", StringComparison.Ordinal);

            bool isRaylibBased =
                contents.Contains("<PackageReference Include=\"Raylib-cs\"", StringComparison.Ordinal);

            if (isMonoGameBased)
            {
                settings.OutputLibrary = OutputLibrary.MonoGameForms;
            }
            else if (isRaylibBased)
            {
                // Raylib codegen only supports FindByName for now (see AssertSupportedCombination) —
                // there is no "RaylibForms" OutputLibrary equivalent to MonoGameForms yet, so this is
                // the only value auto-detection can offer.
                settings.OutputLibrary = OutputLibrary.Raylib;
            }

            settings.RootNamespace = ExtractRootNamespace(contents, projectFilePath);
        }

        return new AutoSetupResult
        {
            Success = true,
            Settings = settings
        };
    }

    private static string? FindProjectDirectory(string startDirectory)
    {
        string? current = startDirectory;

        while (current != null)
        {
            bool hasProjectFile = RecognizedProjectFileExtensions
                .Any(extension => Directory.EnumerateFiles(current, $"*.{extension}", SearchOption.TopDirectoryOnly).Any());

            if (hasProjectFile)
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        return null;
    }

    private static string ExtractRootNamespace(string csprojContents, string csprojPath)
    {
        const string openTag = "<RootNamespace>";
        const string closeTag = "</RootNamespace>";

        int startIndex = csprojContents.IndexOf(openTag, StringComparison.Ordinal);
        if (startIndex >= 0)
        {
            startIndex += openTag.Length;
            int endIndex = csprojContents.IndexOf(closeTag, startIndex, StringComparison.Ordinal);
            if (endIndex > startIndex)
            {
                return csprojContents.Substring(startIndex, endIndex - startIndex);
            }
        }

        string csprojFileName = Path.GetFileNameWithoutExtension(csprojPath);
        return csprojFileName
            .Replace(".", "_")
            .Replace("-", "_")
            .Replace(" ", "_");
    }
}
