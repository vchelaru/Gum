using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// Where a new project's folders and main files sit. The creators and the New Project dialog both read these
/// rules; <c>NewProjectLayoutTests</c> runs the real creation for every platform and checks they still describe it.
/// </summary>
public class NewProjectLayout
{
    /// <summary>The folder a code project puts the Gum project's folder in.</summary>
    public const string ContentFolderName = "Content";

    /// <summary>The Gum project's folder, and the name of its project file, in a code project.</summary>
    public const string GumProjectFolderName = "GumProject";

    /// <summary>The startup file every code project has.</summary>
    public const string ProgramFileName = "Program.cs";

    /// <summary>The extension of the solution file a code project has beside its .csproj.</summary>
    public const string SolutionExtension = "slnx";

    /// <summary>The game class file of the platforms that run a <c>Game1</c>.</summary>
    public const string Game1FileName = "Game1.cs";

    /// <summary>The folder of generated bitmap fonts; the Forms template does not create it, so it is not in <see cref="GumFolders"/>.</summary>
    public const string FontCacheFolderName = "FontCache";

    /// <summary>The folders in a Gum project's folder that every template creates.</summary>
    public static IReadOnlyList<string> GumFolders { get; } = new[] { "Screens", "Components", "Standards", "Behaviors", "Fonts" };

    /// <summary>Folders some templates create beside <see cref="GumFolders"/> (a theme's exported events, the font cache).</summary>
    public static IReadOnlyList<string> OptionalGumFolders { get; } = new[] { FontCacheFolderName, "EventExport" };

    private NewProjectLayout(
        string rootDirectory, IReadOnlyList<string> folders, string gumProjectFile, IReadOnlyList<string> hostFiles)
    {
        RootDirectory = rootDirectory;
        Folders = folders;
        GumProjectFile = gumProjectFile;
        HostFiles = hostFiles;
    }

    /// <summary>The game folder with a code project; the Gum project's folder without one.</summary>
    public string RootDirectory { get; }

    /// <summary>Folders under <see cref="RootDirectory"/>, relative with <c>/</c> separators, parents before their children.</summary>
    public IReadOnlyList<string> Folders { get; }

    /// <summary>The Gum project file, relative to <see cref="RootDirectory"/> with <c>/</c> separators.</summary>
    public string GumProjectFile { get; }

    /// <summary>The code project's files in <see cref="RootDirectory"/>: the .csproj, the .slnx, Program.cs and, where the platform writes one, Game1.cs. Empty without a code project.</summary>
    public IReadOnlyList<string> HostFiles { get; }

    /// <summary>The Gum project's folder relative to <see cref="RootDirectory"/>; empty when it is the root.</summary>
    public string GumFolder => GumProjectFile.Contains('/') ? GumProjectFile[..GumProjectFile.LastIndexOf('/')] : string.Empty;

    /// <summary>The solution file's name in a code project named <paramref name="projectName"/>.</summary>
    public static string GetSolutionFileName(string projectName) => projectName + "." + SolutionExtension;

    /// <summary>Whether a code project for <paramref name="platform"/> has a Game1.cs.</summary>
    public static bool WritesGame1(HostPlatform platform) =>
        platform is HostPlatform.MonoGame or HostPlatform.Kni or HostPlatform.KniSkia;

    /// <summary>The layout of a Gum project without a code project: its file in <paramref name="rootDirectory"/>.</summary>
    /// <param name="rootDirectory">The folder holding the project file.</param>
    /// <param name="name">The project file's name without its extension.</param>
    /// <param name="useXmlFormat">Whether the file is a .gumx instead of a .gumj.</param>
    public static NewProjectLayout ForGumOnly(string rootDirectory, string name, bool useXmlFormat) =>
        new NewProjectLayout(
            rootDirectory,
            GumFolders.OrderBy(folder => folder, StringComparer.Ordinal).ToList(),
            name + "." + GetExtension(useXmlFormat),
            Array.Empty<string>());

    /// <summary>The layout of a code project: the host files in <paramref name="rootDirectory"/> and the Gum project under <c>Content/GumProject</c>.</summary>
    /// <param name="rootDirectory">The game folder; its name is the project name.</param>
    /// <param name="name">The project name.</param>
    /// <param name="platform">The platform the code project runs on.</param>
    /// <param name="useXmlFormat">Whether the Gum project is a .gumx instead of a .gumj.</param>
    public static NewProjectLayout ForCodeProject(string rootDirectory, string name, HostPlatform platform, bool useXmlFormat)
    {
        string gumFolder = ContentFolderName + "/" + GumProjectFolderName;
        List<string> folders = new List<string> { ContentFolderName, gumFolder };
        folders.AddRange(GumFolders.OrderBy(folder => folder, StringComparer.Ordinal).Select(folder => gumFolder + "/" + folder));

        List<string> hostFiles = new List<string> { name + ".csproj", GetSolutionFileName(name), ProgramFileName };
        if (WritesGame1(platform))
        {
            hostFiles.Add(Game1FileName);
        }

        return new NewProjectLayout(
            rootDirectory, folders, gumFolder + "/" + GumProjectFolderName + "." + GetExtension(useXmlFormat), hostFiles);
    }

    private static string GetExtension(bool useXmlFormat) =>
        useXmlFormat ? GumProjectSave.ProjectExtension : GumProjectSave.ProjectJsonExtension;
}
