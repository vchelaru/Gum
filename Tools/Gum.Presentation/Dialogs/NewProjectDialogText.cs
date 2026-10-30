namespace Gum.Dialogs;

/// <summary>
/// Every label, help line, tip and note the New Project dialog shows, in one place so a wording change touches
/// one file. The wording is the approved mock's.
/// </summary>
public static class NewProjectDialogText
{
    public const string ProjectName = "Project name";
    public const string ProjectNameTip = "Names the Gum project file. With a C# project it also names the project folder and the .csproj.";
    public const string FileFormat = "File format";
    public const string FileFormatTip = "The file format of the Gum project. JSON (.gumj) is recommended. XML (.gumx) also works.";
    public const string Location = "Location";
    public const string LocationTip = "The folder to create the project in. A new folder named after the project is created inside it.";
    public const string IncludeForms = "Include Forms controls (recommended)";
    public const string IncludeFormsTip = "Forms is the set of ready-made, interactive controls: buttons, text boxes, check boxes, list boxes, sliders, menus and more. Leave it on unless you plan to build all of your own controls.";
    public const string IncludeFormsHelp = "Adds buttons, text boxes, list boxes and other standard controls to the project.";
    public const string Theme = "Theme";
    public const string ThemeTip = "How the controls look: colors, shapes and fonts.";
    public const string IncludeDemo = "Include demo screen";
    public const string IncludeDemoTip = "Adds DemoScreenGum, a sample screen that shows the controls in use. Useful for learning and as a reference. You can delete it at any time.";
    public const string CreateCodeProject = "Create C# project";
    public const string CreateCodeProjectTip = "A C# project is a normal .NET project (.csproj) with code that loads your Gum project. Without it you only get the Gum project files, and you add them to your own game yourself.";
    public const string CreateCodeProjectHelp = "Also creates a runnable C# project (.csproj) with this Gum project inside it, so you can run it and generate code.";
    public const string Platform = "Platform";
    public const string PlatformTip = "The game framework the C# project runs on. It decides how the game draws and reads input. MonoGame is the most widely used.";
    public const string UseLocalSource = "Use local Gum source";
    public const string UseLocalSourceNote = "For Gum developers: the C# project uses the Gum source code on this computer instead of the published NuGet package, to try unreleased changes. Only available when this tool was built from that source code.";
    public const string RestorePackages = "Restore NuGet packages";
    public const string RestorePackagesNote = "Runs dotnet restore after creating. The first code generation needs the packages.";
    public const string FoldersHeading = "Folders";
    public const string CommandLineHeading = "Command line";
    public const string CommandLineDescription = "Optional. Copy this to create the same project without the tool.";
    public const string WarningHeading = "Inside an existing code project";
    public const string TargetNotEmptyHeading = "This folder is not empty";
    public const string RefusedHeading = "Project already exists";

    /// <summary>The not-empty banner body.</summary>
    public static string TargetNotEmptyBody(string fullPath) => $"{fullPath} already has files. Files with the same names may be overwritten.";

    public const string StridePlatformNote = "Runs on Windows only; renders with Skia on the CPU.";
    public const string KniSkiaPlatformNote = "Gum.KniSkia is not on NuGet yet, so the local Gum source is required.";
    public const string SilkNetPlatformNote = "Needs a graphics driver with OpenGL 3.3.";

    public const string NoDemoCommandNote = "gumcli new has no option to leave out DemoScreenGum yet.";
    public const string ExtensionPicksFormatNote = "The extension in the path picks the file format.";
    public const string NoXmlWithCodeProjectNote = "gumcli new has no option for the .gumx format with a code project yet.";

    public const string EnterName = "Enter a project name.";
    public const string InvalidFolderName = "The name has characters a folder name can't have.";
    public const string InvalidCSharpName = "Not a valid C# name.";
    public const string ChooseLocation = "Choose a location.";
    public const string NameTooLong = "The name is too long.";
    public const string InvalidLocation = "The location has characters a path can't have.";
    public const string LocationNotAbsolute = "The location must be a full path.";
    public const string NeedsLocalSource = "Use local Gum source needs the Gum source code on this computer, and none was found.";

    /// <summary>The warning's hover tip.</summary>
    public static string WarningTip(bool hasGame1, string targetFolder) =>
        $"Create C# project creates a new .csproj and C# files (Program.cs{(hasGame1 ? ", Game1.cs" : "")}) in {targetFolder}. " +
        "Select a folder outside the existing project, or uncheck Create C# project to add only the Gum project.";
}
