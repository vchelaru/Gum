namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// What the New Project dialog says, written out as the user reads it. The tests use these literals, not the production
/// constants, so a changed or misspelled label or message fails a test instead of passing against itself.
/// </summary>
internal static class NewProjectUiText
{
    public const string JsonFormat = ".gumj (JSON, recommended)";
    public const string XmlFormat = ".gumx (XML)";

    public const string IncludeForms = "Include Forms controls (recommended)";
    public const string IncludeDemo = "Include demo screen";
    public const string CreateCodeProject = "Create C# project";
    public const string UseLocalSource = "Use local Gum source";
    public const string RestorePackages = "Restore NuGet packages";
    public const string RecommendedPackages = "Include recommended packages";
    public const string GenerateCode = "Generate code";

    public const string WarningHeading = "Inside an existing code project";
    public const string TargetNotEmptyHeading = "This folder is not empty";
    public const string RefusedHeading = "Project already exists";

    public const string StridePlatformNote = "Runs on Windows only; renders with Skia on the CPU.";
    public const string KniSkiaPlatformNote = "Gum.KniSkia is not on NuGet yet, so the local Gum source is required.";
    public const string SilkNetPlatformNote = "Needs a graphics driver with OpenGL 3.3.";
    public const string NoXmlWithCodeProjectNote = "gumcli new has no option for the .gumx format with a code project yet.";

    public const string EnterName = "Enter a project name.";
    public const string InvalidFolderName = "Not a valid folder name: avoid < > : \" / \\ | ? *, a trailing dot or space, and names like CON or COM1.";
    public const string InvalidCSharpName = "Not a valid C# name.";
    public const string ChooseLocation = "Choose a location.";
    public const string NameTooLong = "The name is too long.";
    public const string InvalidLocation = "The location has characters a path can't have.";
    public const string LocationNotAbsolute = "The location must be a full path.";
    public const string NeedsLocalSource = "Use local Gum source needs the Gum source code on this computer, and none was found.";

    /// <summary>A folder as the Folders tree shows its root: the path with a trailing separator.</summary>
    public static string Folder(string path) => path + Path.DirectorySeparatorChar;

    public static string TargetNotEmptyBody(string fullPath) => $"{fullPath} already has files. Files with the same names may be overwritten.";
}
