using System.Collections.Generic;
using System.IO;
using Gum.ProjectServices;

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

    /// <summary>
    /// The <see cref="Build"/> command followed by one <c>dotnet add</c> per package and, when <paramref name="restore"/>, a
    /// final <c>dotnet restore</c>, chained with <c>&amp;&amp;</c> on one line. It mirrors the order the dialog runs
    /// (create, add every package, restore once), so <c>gumcli new</c> gets <c>--no-restore</c> and each add is
    /// <c>--no-restore</c>, which writes <c>Version="*"</c> without a feed lookup. The csproj path is spelled out so no
    /// <c>cd</c> is needed.
    /// </summary>
    /// <param name="packages">The package ids to add; empty gives the same as <see cref="Build"/>.</param>
    /// <param name="csprojPath">The code project file the new step writes.</param>
    /// <param name="linkedProjects">Full paths of the Gum checkout's add-on projects of a source-linked project, each added with <c>dotnet add reference</c> instead of a package.</param>
    /// <param name="gumProjectPath">The Gum project file; when set, the last step is <c>gumcli codegen "&lt;gumj&gt;"</c> (every screen and component), after the restore (the generator reads the restored runtime package). Null adds none.</param>
    public static string BuildChained(
        string path, string platform, bool includeForms, string? theme, bool restore, IReadOnlyList<string> packages, string csprojPath,
        string? gumProjectPath = null, bool sourceLinked = false, IReadOnlyList<string>? linkedProjects = null)
    {
        string codegen = gumProjectPath == null ? string.Empty : $" && gumcli codegen \"{gumProjectPath}\"";
        linkedProjects ??= new string[0];

        if (packages.Count == 0 && linkedProjects.Count == 0)
        {
            return Build(path, platform, includeForms, theme, sourceLinked, restore) + codegen;
        }

        string command = Build(path, platform, includeForms, theme, sourceLinked, restore: false);
        foreach (string package in packages)
        {
            command += $" && dotnet add \"{csprojPath}\" package {package} --no-restore";
        }
        foreach (string project in linkedProjects)
        {
            command += $" && dotnet add \"{csprojPath}\" reference \"{project}\"";
        }
        if (restore)
        {
            command += $" && dotnet restore \"{csprojPath}\"";
        }
        return command + codegen;
    }

    /// <summary>The path argument for a Gum-only project, or for a game folder when <paramref name="withCodeProject"/>.</summary>
    public static string GetPath(string location, string name, string extension, bool withCodeProject)
    {
        // The project folder is Location itself when it ends in the project's name (see NewProjectFolder).
        string projectFolder = NewProjectFolder.Resolve(location, name).ProjectFolder;
        return NewProjectDisplayPath.Normalize(withCodeProject ? projectFolder : Path.Combine(projectFolder, name + extension));
    }
}
