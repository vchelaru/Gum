using System;
using System.IO;

namespace Gum.ProjectServices;

/// <summary>
/// Where the New Project dialog creates a project, given its Location and name, and the folder it remembers for next time.
/// A Location whose last folder is the project's own name is the project folder itself (no nested <c>Name</c> folder) and
/// its parent is the remembered one; any other Location is that parent, and the project goes in <c>Location\Name</c>. The
/// dialog's command line, folder tree, checks, creation and remember step all read this one result so they cannot disagree.
/// </summary>
public sealed class NewProjectFolder
{
    private NewProjectFolder(string projectFolder, string parentFolder, bool locationIsProjectFolder)
    {
        ProjectFolder = projectFolder;
        ParentFolder = parentFolder;
        LocationIsProjectFolder = locationIsProjectFolder;
    }

    /// <summary>The folder the project is created in: the game folder with a code project, the Gum project's folder without one.</summary>
    public string ProjectFolder { get; }

    /// <summary>
    /// The folder that holds <see cref="ProjectFolder"/>, and the one remembered as the next dialog's Location. A file system or
    /// UNC share root has no folder name, so it never matches and is always this parent: there is no "root's parent" to find.
    /// </summary>
    public string ParentFolder { get; }

    /// <summary>Whether Location already names the project folder, so nothing nests under it.</summary>
    public bool LocationIsProjectFolder { get; }

    /// <summary>Resolves with the OS's path comparison: ignoring case on Windows and macOS, exact on Linux.</summary>
    public static NewProjectFolder Resolve(string location, string name) =>
        Resolve(location, name, OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

    /// <param name="location">The Location field, already trimmed; a full path.</param>
    /// <param name="name">The project name.</param>
    /// <param name="comparison">How the last folder of <paramref name="location"/> is compared with <paramref name="name"/>.</param>
    public static NewProjectFolder Resolve(string location, string name, StringComparison comparison)
    {
        // A root such as "C:\" keeps its separator: "C:" combined with a name is the drive-relative "C:MyGame".
        string trimmed = location.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string folder = trimmed.Length < (Path.GetPathRoot(location)?.Length ?? 0) ? location : trimmed;

        string lastFolder = Path.GetFileName(folder);
        if (lastFolder.Length == 0 || !string.Equals(lastFolder, name, comparison))
        {
            return new NewProjectFolder(Path.Combine(folder, name), location, locationIsProjectFolder: false);
        }

        // A folder with a name is never a root, so it has a parent.
        return new NewProjectFolder(folder, Path.GetDirectoryName(folder)!, locationIsProjectFolder: true);
    }
}
