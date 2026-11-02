using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <summary>Why a <see cref="NewProjectRequest"/> was refused.</summary>
public enum NewProjectErrorKind
{
    SourceLinkedNeedsPlatform,
    UnknownTemplate,
    ThemeNeedsFormsTemplate,
    UnknownTheme,
    FnaNotSupported,
    UnknownPlatform,
    PlatformPathIsProjectFile,
    GumSourceNotFound,
    KniSkiaNeedsSourceLinked,
    ProjectAlreadyExists,
    InvalidProjectName,
    RecommendedPackagesNeedPlatform,
    GenerateCodeNeedsPlatform,
    PreviewNeedsPlatform
}

/// <summary>A refused request. <see cref="Message"/> is the exact text <c>gumcli new</c> prints.</summary>
public class NewProjectError
{
    public NewProjectError(NewProjectErrorKind kind, string message)
    {
        Kind = kind;
        Message = message;
    }

    public NewProjectErrorKind Kind { get; }

    public string Message { get; }
}

/// <summary>Everything a valid request resolved to: the full paths and the looked-up folders.</summary>
public class ResolvedNewProject
{
    /// <summary>The platform, or <see langword="null"/> for a Gum-only project.</summary>
    public HostPlatform? Platform { get; init; }

    public bool IncludeFormsTemplate { get; init; }

    /// <summary>The theme folder, or <see langword="null"/> for the plain Forms template or an empty project.</summary>
    public string? ThemeDirectory { get; init; }

    /// <summary>Which Gum packages the code project uses.</summary>
    public NewProjectPackageChannel PackageChannel { get; init; }

    /// <summary>Whether the code project's NuGet references may resolve to preview (prerelease) versions.</summary>
    public bool UsesPrerelease => PackageChannel == NewProjectPackageChannel.Preview;

    /// <summary>The Gum checkout for a source-linked project.</summary>
    public string? GumSourceDirectory { get; init; }

    /// <summary>The game folder with a platform; the folder of the Gum project without one.</summary>
    public string RootDirectory { get; init; } = "";

    /// <summary>The Gum project file that will be created.</summary>
    public string GumProjectPath { get; init; } = "";

    /// <summary>The code project file that will be created, or <see langword="null"/> without a platform.</summary>
    public string? CsprojPath { get; init; }

    /// <summary>Whether code is generated for the whole project once it is restored.</summary>
    public bool GenerateCode { get; init; }

    /// <summary>The recommended packages to add to the code project, in order; empty when not requested.</summary>
    public IReadOnlyList<string> RecommendedPackages { get; init; } = new string[0];

    /// <summary>
    /// The add-ons (package ids, see <see cref="RecommendedPackages.ForSourceLinked"/>) a source-linked project links as project
    /// references while the files are created; empty otherwise. Nothing is added from NuGet for them.
    /// </summary>
    public IReadOnlyList<string> LinkedAddOns { get; init; } = new string[0];

    /// <summary>The project name: the game folder's name with a platform, otherwise the Gum project file's name.</summary>
    public string Name { get; init; } = "";
}

/// <summary>Outcome of validating a request: either an <see cref="Error"/> or a <see cref="Resolved"/> project.</summary>
public class NewProjectValidation
{
    public NewProjectError? Error { get; init; }

    public ResolvedNewProject? Resolved { get; init; }

    public bool IsValid => Error == null;
}

/// <summary>Where <see cref="INewProjectService"/> is in its work.</summary>
public enum NewProjectStep
{
    CreatingFiles,
    AddingPackages,
    Restoring,
    GeneratingCode
}

/// <summary>Progress report from <see cref="INewProjectService.ExecuteAsync"/>.</summary>
public class NewProjectProgress
{
    public NewProjectProgress(NewProjectStep step, string message)
    {
        Step = step;
        Message = message;
    }

    public NewProjectStep Step { get; }

    public string Message { get; }
}

/// <summary>Outcome of creating the files.</summary>
public class NewProjectFilesResult
{
    public bool Success { get; init; }

    /// <summary>True when the caller cancelled; anything this call wrote was removed.</summary>
    public bool Canceled { get; init; }

    /// <summary>The failure reason when not successful.</summary>
    public string ErrorMessage { get; init; } = "";

    public string GumProjectPath { get; init; } = "";

    /// <summary>The code project file, or <see langword="null"/> for a Gum-only project.</summary>
    public string? CsprojPath { get; init; }

    /// <summary>The demo screen the host game shows, or <see langword="null"/> when there is none (no platform, empty template, demo left out).</summary>
    public DemoScreenInfo? DemoScreen { get; init; }
}

/// <summary>The demo screen a new code project shows: its element name and the class code generation makes for it.</summary>
public class DemoScreenInfo
{
    /// <summary>The element name in the Gum project, with its folder: <c>DemoScreenGum</c> or <c>Bubblegum/DemoScreenGum</c>.</summary>
    public string Name { get; init; } = "";

    /// <summary>The generated class name.</summary>
    public string ClassName { get; init; } = "";

    /// <summary>The generated class's namespace, from the code generation settings the scaffolder wrote.</summary>
    public string Namespace { get; init; } = "";
}

/// <summary>Outcome of generating code for the new project.</summary>
public class GenerateCodeResult
{
    public bool Success { get; init; }

    /// <summary>True when the caller canceled before it started.</summary>
    public bool Canceled { get; init; }

    /// <summary>What the generation printed, errors first when it failed.</summary>
    public string OutputTail { get; init; } = "";
}

/// <summary>Outcome of <see cref="INewProjectService.ExecuteAsync"/>: the files step, then the restore step if it ran.</summary>
public class NewProjectResult
{
    public NewProjectFilesResult Files { get; init; } = new NewProjectFilesResult();

    /// <summary>The package-add outcome, or <see langword="null"/> when no packages were requested or the files failed.</summary>
    public AddPackagesResult? PackageAdd { get; init; }

    /// <summary>The restore outcome, or <see langword="null"/> when it did not run (no platform, not requested, or the files failed).</summary>
    public RestoreResult? Restore { get; init; }

    /// <summary>The code generation outcome, or <see langword="null"/> when it did not run (including when the restore was off: the generator needs the restored package).</summary>
    public GenerateCodeResult? Codegen { get; init; }
}

/// <summary>How a <c>dotnet restore</c> ended.</summary>
public enum RestoreStatus
{
    Succeeded,
    Failed,
    ProcessNotFound,
    Canceled
}

/// <summary>Outcome of a restore.</summary>
public class RestoreResult
{
    public RestoreStatus Status { get; init; }

    /// <summary>The process exit code when it ran to the end.</summary>
    public int? ExitCode { get; init; }

    /// <summary>The last lines the process printed (standard output and error; restore reports its errors on output).</summary>
    public string OutputTail { get; init; } = "";

    /// <summary>The reason the process could not be started.</summary>
    public string ErrorMessage { get; init; } = "";
}

/// <summary>Outcome of adding the recommended packages with <see cref="INewProjectService.AddPackagesAsync"/>.</summary>
public class AddPackagesResult
{
    public RestoreStatus Status { get; init; }

    /// <summary>The package that failed or was being added when canceled, or empty.</summary>
    public string FailedPackage { get; init; } = "";

    /// <summary>The last lines <c>dotnet add</c> printed for the failed package.</summary>
    public string OutputTail { get; init; } = "";

    /// <summary>The reason the process could not be started.</summary>
    public string ErrorMessage { get; init; } = "";
}
