using System.IO;
using ToolsUtilities;

namespace Gum.ProjectServices;

/// <summary>
/// How the Project Properties store a file reference: relative to the project's folder. Shared by the tool's
/// Project Properties page and <c>gumcli edit</c>'s <c>setProjectProperty</c>.
/// </summary>
public static class ProjectRelativePaths
{
    /// <summary>
    /// <paramref name="path"/> relative to <paramref name="projectDirectory"/> with forward slashes, or absolute
    /// when the project has no folder yet (an unsaved project). See <see cref="FileManager.ToStoredFilePath"/>.
    /// </summary>
    public static string ToProjectRelative(string path, string? projectDirectory) =>
        FileManager.ToStoredFilePath(path, projectDirectory);

    /// <summary>
    /// The engine profile reference as the project stores it: relative to <paramref name="projectDirectory"/> with forward
    /// slashes ("../" segments for a profile outside it). A rooted path stays as it is when the project has no folder yet
    /// or the profile is on another drive.
    /// </summary>
    public static string ToProjectRelativeReference(string reference, string? projectDirectory)
    {
        string path = reference.Replace('\\', '/');
        if (projectDirectory == null || !Path.IsPathRooted(path))
        {
            return path;
        }

        string relative = Path.GetRelativePath(projectDirectory, path).Replace('\\', '/');
        return Path.IsPathRooted(relative) ? path : relative;
    }
}
