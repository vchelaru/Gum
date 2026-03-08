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
/// Gum project. Walks up from the .gumx directory to find the nearest .csproj, then derives
/// <c>CodeProjectRoot</c>, <c>RootNamespace</c>, and <c>OutputLibrary</c> from it.
/// </summary>
public class CodeGenerationAutoSetupService : ICodeGenerationAutoSetupService
{
    /// <inheritdoc/>
    public AutoSetupResult Run(string gumxFilePath)
    {
        string gumxDirectory = Path.GetDirectoryName(Path.GetFullPath(gumxFilePath))
            ?? Path.GetFullPath(gumxFilePath);

        if (!gumxDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            gumxDirectory += Path.DirectorySeparatorChar;
        }

        string? csprojDirectory = FindCsprojDirectory(gumxDirectory);

        if (csprojDirectory == null)
        {
            return new AutoSetupResult
            {
                Success = false,
                ErrorMessage = "No .csproj file found in any parent directory of the .gumx file. Cannot automatically configure code generation."
            };
        }

        string codeProjectRoot = Path.GetRelativePath(gumxDirectory, csprojDirectory);
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

        string? csprojPath = Directory
            .EnumerateFiles(csprojDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        if (csprojPath != null)
        {
            string contents = File.ReadAllText(csprojPath);

            bool isMonoGameBased =
                contents.Contains("<PackageReference Include=\"MonoGame.Framework.", StringComparison.Ordinal) ||
                contents.Contains("<PackageReference Include=\"nkast.Xna.Framework", StringComparison.Ordinal);

            if (isMonoGameBased)
            {
                settings.OutputLibrary = OutputLibrary.MonoGameForms;
            }

            settings.RootNamespace = ExtractRootNamespace(contents, csprojPath);
        }

        return new AutoSetupResult
        {
            Success = true,
            Settings = settings
        };
    }

    private static string? FindCsprojDirectory(string startDirectory)
    {
        string? current = startDirectory;

        while (current != null)
        {
            bool hasCsproj = Directory
                .EnumerateFiles(current, "*.csproj", SearchOption.TopDirectoryOnly)
                .Any();

            if (hasCsproj)
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
