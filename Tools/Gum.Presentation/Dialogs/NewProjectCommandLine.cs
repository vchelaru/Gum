using System.IO;

namespace Gum.Dialogs;

/// <summary>
/// Builds the <c>gumcli new</c> command that reproduces the New Project dialog's choices.
/// </summary>
public static class NewProjectCommandLine
{
    /// <summary>The theme that needs no <c>--theme</c> option.</summary>
    public const string DefaultTheme = "Standard";

    /// <summary>
    /// The command for the given choices. <paramref name="path"/> is quoted; the other options appear only when
    /// they differ from what <c>gumcli new</c> does by default.
    /// </summary>
    /// <param name="path">The path argument: the game folder with a platform, otherwise the Gum project file.</param>
    /// <param name="platform">The <c>--platform</c> name, or null for a Gum-only project.</param>
    /// <param name="includeForms">False adds <c>--template empty</c>.</param>
    /// <param name="theme">Added as <c>--theme</c> when Forms are included and it is not <see cref="DefaultTheme"/>.</param>
    /// <param name="sourceLinked">Adds <c>--source-linked</c>; only with a platform.</param>
    /// <param name="restore">False adds <c>--no-restore</c>; only with a platform.</param>
    public static string Build(string path, string? platform, bool includeForms, string? theme, bool sourceLinked, bool restore)
    {
        string command = $"gumcli new \"{path}\"";
        if (platform != null)
        {
            command += $" --platform {platform}";
        }
        if (!includeForms)
        {
            command += " --template empty";
        }
        if (includeForms && !string.IsNullOrEmpty(theme) && theme != DefaultTheme)
        {
            command += $" --theme {theme}";
        }
        if (platform != null && sourceLinked)
        {
            command += " --source-linked";
        }
        if (platform != null && !restore)
        {
            command += " --no-restore";
        }
        return command;
    }

    /// <summary>The path argument for a Gum-only project, or for a game folder when <paramref name="withCodeProject"/>.</summary>
    public static string GetPath(string location, string name, string extension, bool withCodeProject)
    {
        string trimmed = location.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        // A root such as "C:\" must keep its separator: "C:" combined with a name is the drive-relative "C:MyGame".
        string folder = trimmed.Length < (Path.GetPathRoot(location)?.Length ?? 0) ? location : trimmed;
        return NewProjectDisplayPath.Normalize(withCodeProject
            ? Path.Combine(folder, name)
            : Path.Combine(folder, name, name + extension));
    }
}
