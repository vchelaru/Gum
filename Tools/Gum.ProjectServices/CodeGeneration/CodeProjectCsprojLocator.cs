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

        string codeProjectRoot = settings.CodeProjectRoot;
        if (FileManager.IsRelative(codeProjectRoot))
        {
            // Combine through the Path APIs rather than string concatenation: projectDirectory
            // may or may not end in a separator, and a raw concat like "dir" + "./" produces
            // "dir./", a nonexistent directory on macOS/Linux. A root saved on Windows uses
            // backslashes, which are file-name characters on macOS/Linux, so they become the
            // native separator first.
            codeProjectRoot = Path.GetFullPath(Path.Combine(projectDirectory,
                codeProjectRoot.Replace('\\', Path.DirectorySeparatorChar)));
        }

        return codeProjectRoot;
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
    /// Resolves the code project root and returns its .csproj, or null.
    /// </summary>
    public static string? FindCsproj(CodeOutputProjectSettings settings, string? projectDirectory)
    {
        string? codeProjectRoot = ResolveCodeProjectRoot(settings, projectDirectory);
        return codeProjectRoot == null ? null : FindCsproj(codeProjectRoot);
    }
}
