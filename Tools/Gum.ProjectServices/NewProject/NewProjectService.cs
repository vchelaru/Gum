using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class NewProjectService : INewProjectService
{
    private const string DefaultProjectName = "GumProject";
    private const string DefaultPlatformProjectName = "MyGumGame";

    private readonly IFormsThemeLocator _themeLocator;
    private readonly IRestoreRunner _restoreRunner;
    private readonly IPackageAddRunner _packageAddRunner;
    private readonly IReadOnlyList<string> _gumSourceSearchDirectories;

    /// <param name="themeLocator">Finds the bundled Forms themes.</param>
    /// <param name="restoreRunner">Runs <c>dotnet restore</c>.</param>
    /// <param name="gumSourceSearchDirectories">Where to look for a Gum checkout, walking up from each, in order.</param>
    public NewProjectService(
        IFormsThemeLocator themeLocator, IRestoreRunner restoreRunner, IReadOnlyList<string> gumSourceSearchDirectories)
        : this(themeLocator, restoreRunner, new DotnetPackageAddRunner(), gumSourceSearchDirectories)
    {
    }

    /// <param name="themeLocator">Finds the bundled Forms themes.</param>
    /// <param name="restoreRunner">Runs <c>dotnet restore</c>.</param>
    /// <param name="packageAddRunner">Runs <c>dotnet add package</c>.</param>
    /// <param name="gumSourceSearchDirectories">Where to look for a Gum checkout, walking up from each, in order.</param>
    public NewProjectService(
        IFormsThemeLocator themeLocator, IRestoreRunner restoreRunner, IPackageAddRunner packageAddRunner,
        IReadOnlyList<string> gumSourceSearchDirectories)
    {
        _themeLocator = themeLocator;
        _restoreRunner = restoreRunner;
        _packageAddRunner = packageAddRunner;
        _gumSourceSearchDirectories = gumSourceSearchDirectories;
    }

    /// <inheritdoc/>
    public bool IsSourceLinkedAvailable => FindGumSourceDirectory() != null;

    /// <inheritdoc/>
    public string? FindGumSourceDirectory()
    {
        foreach (string directory in _gumSourceSearchDirectories)
        {
            string? found = GumSourceLocator.Find(directory);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    #region Validation

    /// <inheritdoc/>
    public NewProjectValidation Validate(NewProjectRequest request)
    {
        NewProjectError? optionsError = ResolveOptions(request, out Options options);
        if (optionsError != null)
        {
            return new NewProjectValidation { Error = optionsError };
        }

        if (request.IncludeRecommendedPackages)
        {
            if (options.Platform == null)
            {
                return Fail(NewProjectErrorKind.RecommendedPackagesNeedPlatform,
                    "Recommended packages need a code project: they are added to its .csproj.");
            }

            if (request.SourceLinked)
            {
                return Fail(NewProjectErrorKind.RecommendedPackagesNeedNuGetRuntime,
                    "Recommended packages need the NuGet Gum runtime: they would pull in a second copy of Gum next to the linked source.");
            }
        }

        string? gumSourceDirectory = null;
        if (options.Platform != null)
        {
            if (!string.IsNullOrEmpty(request.Path) && GumProjectSave.IsProjectFile(request.Path))
            {
                return Fail(NewProjectErrorKind.PlatformPathIsProjectFile,
                    "With --platform, <path> names the project folder, not a Gum project file.");
            }

            if (request.SourceLinked)
            {
                gumSourceDirectory = FindGumSourceDirectory();
                if (gumSourceDirectory == null)
                {
                    return Fail(NewProjectErrorKind.GumSourceNotFound,
                        "--source-linked needs a Gum checkout: run a gumcli built from one, or run this from inside one.");
                }
            }
        }

        Targets targets = ComputeTargets(request.Path, options.Platform != null, GetGumExtension(request));

        // Only a bare file system root resolves to a folder with no name.
        if (string.IsNullOrEmpty(targets.Name))
        {
            return Fail(NewProjectErrorKind.InvalidProjectName,
                $"'{request.Path}' has no folder name to name the project after. Give a folder or project name.");
        }

        if (options.Platform == null)
        {
            // Either format counts: a .gumx beside a new .gumj would leave two Gum projects in one folder.
            foreach (string extension in new[] { GumProjectSave.ProjectJsonExtension, GumProjectSave.ProjectExtension })
            {
                string existing = Path.ChangeExtension(targets.GumProjectPath, extension);
                if (File.Exists(existing))
                {
                    return Fail(NewProjectErrorKind.ProjectAlreadyExists, $"Project already exists: {existing}");
                }
            }
        }
        else
        {
            NewProjectError? preflightError = PlatformProjectScaffolder.GetPreflightError(targets.RootDirectory, options.Platform.Value);
            if (preflightError != null)
            {
                return new NewProjectValidation { Error = preflightError };
            }
        }

        return new NewProjectValidation
        {
            Resolved = new ResolvedNewProject
            {
                Platform = options.Platform,
                IncludeFormsTemplate = options.IncludeFormsTemplate,
                ThemeDirectory = options.ThemeDirectory,
                GumSourceDirectory = gumSourceDirectory,
                RootDirectory = targets.RootDirectory,
                GumProjectPath = targets.GumProjectPath,
                CsprojPath = targets.CsprojPath,
                Name = targets.Name,
                RecommendedPackages = request.IncludeRecommendedPackages
                    ? RecommendedPackages.For(options.Platform!.Value)
                    : Array.Empty<string>()
            }
        };
    }

    private static NewProjectValidation Fail(NewProjectErrorKind kind, string message) =>
        new NewProjectValidation { Error = new NewProjectError(kind, message) };

    private class Options
    {
        public bool IncludeFormsTemplate { get; init; }

        public string? ThemeDirectory { get; init; }

        public HostPlatform? Platform { get; init; }
    }

    // The checks that do not need the path: platform, template and theme names.
    private NewProjectError? ResolveOptions(NewProjectRequest request, out Options options)
    {
        options = new Options();

        if (request.SourceLinked && request.Platform == null)
        {
            return new NewProjectError(NewProjectErrorKind.SourceLinkedNeedsPlatform,
                "--source-linked needs --platform: it links a platform's Gum runtime project.");
        }

        bool isForms = string.Equals(request.Template, "forms", StringComparison.OrdinalIgnoreCase);
        if (!isForms && !string.Equals(request.Template, "empty", StringComparison.OrdinalIgnoreCase))
        {
            return new NewProjectError(NewProjectErrorKind.UnknownTemplate,
                $"Unknown template '{request.Template}'. Valid values are: forms, empty.");
        }

        string? themeDirectory = null;
        if (request.Theme != null)
        {
            if (!isForms)
            {
                return new NewProjectError(NewProjectErrorKind.ThemeNeedsFormsTemplate,
                    "--theme needs the 'forms' template: themes style the Forms controls.");
            }

            // The default theme is the plain Forms template, so it resolves to no folder.
            if (!string.Equals(request.Theme, "Standard", StringComparison.OrdinalIgnoreCase))
            {
                themeDirectory = _themeLocator.FindThemeDirectory(request.Theme);
                if (themeDirectory == null)
                {
                    string available = string.Join(", ", new[] { "Standard" }.Concat(_themeLocator.GetAvailableThemes()));
                    return new NewProjectError(NewProjectErrorKind.UnknownTheme,
                        $"Unknown theme '{request.Theme}'. Available themes: {available}.");
                }
            }
        }

        HostPlatform? platform = null;
        if (request.Platform != null)
        {
            if (string.Equals(request.Platform, "fna", StringComparison.OrdinalIgnoreCase))
            {
                return new NewProjectError(NewProjectErrorKind.FnaNotSupported,
                    "FNA projects can't be scaffolded: FNA is not published to NuGet, so it has to be linked from source.");
            }

            if (!NewProjectPlatforms.TryParse(request.Platform, out HostPlatform parsed))
            {
                return new NewProjectError(NewProjectErrorKind.UnknownPlatform,
                    $"Unknown platform '{request.Platform}'. Valid values are: {NewProjectPlatforms.ValidNames}.");
            }

            platform = parsed;
        }

        options = new Options { IncludeFormsTemplate = isForms, ThemeDirectory = themeDirectory, Platform = platform };
        return null;
    }

    #endregion

    #region Paths

    private class Targets
    {
        public string RootDirectory { get; init; } = "";

        public string GumProjectPath { get; init; } = "";

        public string? CsprojPath { get; init; }

        public string Name { get; init; } = "";
    }

    private static string GetGumExtension(NewProjectRequest request)
    {
        if (!string.IsNullOrEmpty(request.Path) && GumProjectSave.IsProjectFile(request.Path))
        {
            return Path.GetExtension(request.Path).TrimStart('.').ToLowerInvariant();
        }

        return request.UseXmlFormat ? GumProjectSave.ProjectExtension : GumProjectSave.ProjectJsonExtension;
    }

    // The one place the path rules live, for both the Gum-only and the platform layout.
    private static Targets ComputeTargets(string? path, bool isPlatform, string gumExtension)
    {
        if (isPlatform)
        {
            string root = PlatformProjectScaffolder.GetFullDirectory(
                string.IsNullOrEmpty(path) ? DefaultPlatformProjectName : path);
            (string csproj, string gumProject) = PlatformProjectScaffolder.GetProjectPaths(
                root, useXmlFormat: gumExtension != GumProjectSave.ProjectJsonExtension);
            return new Targets
            {
                RootDirectory = root,
                GumProjectPath = gumProject,
                CsprojPath = csproj,
                Name = Path.GetFileName(root)
            };
        }

        string fullPath;
        if (string.IsNullOrEmpty(path))
        {
            fullPath = Path.GetFullPath(Path.Combine(DefaultProjectName, DefaultProjectName + "." + gumExtension));
        }
        else if (GumProjectSave.IsProjectFile(path))
        {
            fullPath = Path.GetFullPath(path);
        }
        else
        {
            // Treat as a project name: create <name>/<name>.<ext>. The name comes from the resolved
            // folder, so a trailing separator or a "." / ".." segment cannot leave it empty or wrong.
            string directory = PlatformProjectScaffolder.GetFullDirectory(path);
            fullPath = Path.Combine(directory, Path.GetFileName(directory) + "." + gumExtension);
        }

        return new Targets
        {
            RootDirectory = Path.GetDirectoryName(fullPath) ?? "",
            GumProjectPath = fullPath,
            Name = Path.GetFileNameWithoutExtension(fullPath)
        };
    }

    #endregion

    #region Creation

    /// <inheritdoc/>
    public Task<NewProjectFilesResult> CreateFilesAsync(ResolvedNewProject project, CancellationToken cancellationToken)
    {
        return Task.Run(() => CreateFiles(project, cancellationToken), CancellationToken.None);
    }

    private NewProjectFilesResult CreateFiles(ResolvedNewProject project, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new NewProjectFilesResult { Canceled = true };
        }

        Options options = new Options
        {
            IncludeFormsTemplate = project.IncludeFormsTemplate,
            ThemeDirectory = project.ThemeDirectory,
            Platform = project.Platform
        };
        Targets targets = new Targets
        {
            RootDirectory = project.RootDirectory,
            GumProjectPath = project.GumProjectPath,
            CsprojPath = project.CsprojPath,
            Name = project.Name
        };

        // What exists now is not ours to remove.
        NewProjectJournal journal = NewProjectJournal.Begin(
            project.RootDirectory, Path.GetDirectoryName(project.GumProjectPath) ?? project.RootDirectory);

        NewProjectFilesResult result;
        try
        {
            result = WriteFiles(options, targets, project.GumSourceDirectory);

            if (result.Success && cancellationToken.IsCancellationRequested)
            {
                result = new NewProjectFilesResult { Canceled = true };
            }
        }
        catch (Exception)
        {
            journal.Undo();
            throw;
        }

        if (!result.Success)
        {
            journal.Undo();
        }

        return result;
    }

    // Writes the Gum project (and the code project around it).
    private static NewProjectFilesResult WriteFiles(Options options, Targets targets, string? gumSourceDirectory)
    {
        try
        {
            if (options.Platform is HostPlatform platform)
            {
                PlatformProjectResult result = new PlatformProjectScaffolder().Create(
                    targets.RootDirectory, platform, options.IncludeFormsTemplate, gumSourceDirectory, options.ThemeDirectory,
                    useXmlFormat: !GumProjectSave.IsJsonFormat(targets.GumProjectPath));
                if (!result.Success)
                {
                    return new NewProjectFilesResult { ErrorMessage = result.ErrorMessage };
                }

                return new NewProjectFilesResult
                {
                    Success = true,
                    GumProjectPath = result.GumProjectPath,
                    CsprojPath = result.CsprojPath
                };
            }

            Directory.CreateDirectory(targets.RootDirectory);
            GumProjectTemplateWriter.Write(targets.GumProjectPath, options.IncludeFormsTemplate, options.ThemeDirectory);
            return new NewProjectFilesResult { Success = true, GumProjectPath = targets.GumProjectPath };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new NewProjectFilesResult { ErrorMessage = exception.Message };
        }
    }

    #endregion

    #region Restore and Execute

    /// <inheritdoc/>
    public Task<RestoreResult> RestoreAsync(
        string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
        _restoreRunner.RestoreAsync(csprojPath, onOutputLine, cancellationToken);

    /// <inheritdoc/>
    public async Task<AddPackagesResult> AddPackagesAsync(
        string csprojPath, IReadOnlyList<string> packages, Action<string>? onOutputLine, CancellationToken cancellationToken)
    {
        foreach (string package in packages)
        {
            RestoreResult result = await _packageAddRunner.AddAsync(csprojPath, package, onOutputLine, cancellationToken);
            if (result.Status != RestoreStatus.Succeeded)
            {
                return new AddPackagesResult
                {
                    Status = result.Status,
                    FailedPackage = package,
                    OutputTail = result.OutputTail,
                    ErrorMessage = result.ErrorMessage
                };
            }
        }

        return new AddPackagesResult { Status = RestoreStatus.Succeeded };
    }

    /// <inheritdoc/>
    public async Task<NewProjectResult> ExecuteAsync(
        NewProjectRequest request, IProgress<NewProjectProgress>? progress, CancellationToken cancellationToken)
    {
        NewProjectValidation validation = Validate(request);
        if (validation.Resolved == null)
        {
            return new NewProjectResult
            {
                Files = new NewProjectFilesResult { ErrorMessage = validation.Error?.Message ?? "" }
            };
        }

        progress?.Report(new NewProjectProgress(NewProjectStep.CreatingFiles, "Creating project files..."));
        NewProjectFilesResult files = await CreateFilesAsync(validation.Resolved, cancellationToken);
        if (!files.Success || files.CsprojPath == null)
        {
            return new NewProjectResult { Files = files };
        }

        AddPackagesResult? packageAdd = null;
        if (validation.Resolved.RecommendedPackages.Count > 0)
        {
            progress?.Report(new NewProjectProgress(NewProjectStep.AddingPackages, "Adding recommended packages..."));
            packageAdd = await AddPackagesAsync(files.CsprojPath, validation.Resolved.RecommendedPackages, null, cancellationToken);
            if (packageAdd.Status != RestoreStatus.Succeeded)
            {
                return new NewProjectResult { Files = files, PackageAdd = packageAdd };
            }
        }

        if (!request.Restore)
        {
            return new NewProjectResult { Files = files, PackageAdd = packageAdd };
        }

        progress?.Report(new NewProjectProgress(NewProjectStep.Restoring, "Restoring NuGet packages..."));
        RestoreResult restore = await RestoreAsync(files.CsprojPath, null, cancellationToken);
        return new NewProjectResult { Files = files, PackageAdd = packageAdd, Restore = restore };
    }

    #endregion
}
