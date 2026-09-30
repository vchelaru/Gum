using System;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Finds the game .csproj that codegen reads for the runtime's syntax version and the C#
/// language version.
/// </summary>
public static class CodeProjectCsprojLocator
{
    private const string UnityGameProjectFileName = "Assembly-CSharp.csproj";

    /// <summary>
    /// Returns the absolute code project root, or null when the project directory or
    /// <see cref="CodeOutputProjectSettings.CodeProjectRoot"/> isn't set.
    /// </summary>
    public static string? ResolveCodeProjectRoot(CodeOutputProjectSettings settings, string? projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory) || string.IsNullOrEmpty(settings.CodeProjectRoot))
        {
            return null;
        }

        return ResolveAgainstProjectDirectory(settings.CodeProjectRoot, projectDirectory);
    }

    /// <summary>
    /// Returns the absolute path of <see cref="CodeOutputProjectSettings.CsprojPath"/>, whether or not the
    /// file exists, or null when it or the project directory isn't set.
    /// </summary>
    public static string? ResolveConfiguredCsproj(CodeOutputProjectSettings settings, string? projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory) || string.IsNullOrEmpty(settings.CsprojPath))
        {
            return null;
        }

        return ResolveAgainstProjectDirectory(settings.CsprojPath, projectDirectory);
    }

    private static string ResolveAgainstProjectDirectory(string path, string projectDirectory)
    {
        if (!FileManager.IsRelative(path))
        {
            return path;
        }

        // Combine through the Path APIs rather than string concatenation: projectDirectory
        // may or may not end in a separator, and a raw concat like "dir" + "./" produces
        // "dir./", a nonexistent directory on macOS/Linux. A path saved on Windows uses
        // backslashes, which are file-name characters on macOS/Linux, so they become the
        // native separator first.
        return Path.GetFullPath(Path.Combine(projectDirectory, path.Replace('\\', Path.DirectorySeparatorChar)));
    }

    /// <summary>
    /// Whether <paramref name="csprojPath"/> is Unity's game project: Assembly-CSharp.csproj next to an
    /// Assets folder. Unity compiles only code under Assets.
    /// </summary>
    public static bool IsUnityGameProject(string csprojPath)
    {
        string? directory = Path.GetDirectoryName(csprojPath);
        return directory != null
            && string.Equals(Path.GetFileName(csprojPath), UnityGameProjectFileName, StringComparison.OrdinalIgnoreCase)
            && Directory.Exists(Path.Combine(directory, "Assets"));
    }

    /// <summary>
    /// Returns the .csproj in <paramref name="directory"/> (top level only), or null if there is
    /// none. When there are several, Unity's game project (Assembly-CSharp.csproj) wins, then
    /// the shortest file name, so MyGame.csproj beats MyGame.Tests.csproj.
    /// </summary>
    public static string? FindCsproj(string directory)
    {
        try
        {
            return Directory
                .EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly)
                .OrderBy(path => string.Equals(Path.GetFileName(path), UnityGameProjectFileName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(path => Path.GetFileName(path).Length)
                .ThenBy(path => Path.GetFileName(path), StringComparer.Ordinal)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the game .csproj for these settings, or null: <see cref="CodeOutputProjectSettings.CsprojPath"/>
    /// when set (even if the file is missing, so a wrong path isn't swapped for another project), otherwise
    /// the .csproj in the nearest folder at or above the code project root that has one. Walking up lets
    /// the code project root be a subfolder, like Unity's Assets folder below Assembly-CSharp.csproj.
    /// </summary>
    public static string? FindCsproj(CodeOutputProjectSettings settings, string? projectDirectory)
    {
        string? configured = ResolveConfiguredCsproj(settings, projectDirectory);
        if (configured != null)
        {
            return configured;
        }

        string? directory = ResolveCodeProjectRoot(settings, projectDirectory);
        while (directory != null)
        {
            string? csproj = FindCsproj(directory);
            if (csproj != null)
            {
                return csproj;
            }
            directory = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(directory));
        }

        return null;
    }
}
