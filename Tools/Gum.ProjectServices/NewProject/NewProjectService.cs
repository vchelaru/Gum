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

    // The name the plan's temporary run is created under; swapped for the real name in the plan's paths.
    private const string PlanPlaceholderName = "GumPlanProjectName";

    private readonly IFormsThemeLocator _themeLocator;
    private readonly IRestoreRunner _restoreRunner;
    private readonly IReadOnlyList<string> _gumSourceSearchDirectories;

    // The creators initialize shared static state (the standard elements), so one creation runs at a time.
    private readonly object _creationLock = new object();
    private readonly Dictionary<string, PlanTemplate> _planCache = new Dictionary<string, PlanTemplate>();

    /// <param name="themeLocator">Finds the bundled Forms themes.</param>
    /// <param name="restoreRunner">Runs <c>dotnet restore</c>.</param>
    /// <param name="gumSourceSearchDirectories">Where to look for a Gum checkout, walking up from each, in order.</param>
    public NewProjectService(
        IFormsThemeLocator themeLocator, IRestoreRunner restoreRunner, IReadOnlyList<string> gumSourceSearchDirectories)
    {
        _themeLocator = themeLocator;
        _restoreRunner = restoreRunner;
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

        if (options.Platform == null)
        {
            if (File.Exists(targets.GumProjectPath))
            {
                return Fail(NewProjectErrorKind.ProjectAlreadyExists, $"Project already exists: {targets.GumProjectPath}");
            }
        }
        else
        {
            string? preflightError = PlatformProjectScaffolder.GetPreflightError(targets.RootDirectory);
            if (preflightError != null)
            {
                NewProjectErrorKind kind = preflightError.Contains("already exists")
                    ? NewProjectErrorKind.ProjectAlreadyExists
                    : NewProjectErrorKind.InvalidProjectName;
                return Fail(kind, preflightError);
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
                Name = targets.Name
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
            (string csproj, string gumProject) = PlatformProjectScaffolder.GetProjectPaths(root);
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
            // Treat as a project name: create <name>/<name>.<ext>
            string directoryName = Path.GetFileName(path);
            fullPath = Path.GetFullPath(Path.Combine(path, directoryName + "." + gumExtension));
        }

        return new Targets
        {
            RootDirectory = Path.GetDirectoryName(fullPath) ?? "",
            GumProjectPath = fullPath,
            Name = Path.GetFileNameWithoutExtension(fullPath)
        };
    }

    #endregion

    #region Plan

    private class PlanTemplate
    {
        public PlanTemplate(IReadOnlyList<string> files, IReadOnlyList<string> directories)
        {
            Files = files;
            Directories = directories;
        }

        public IReadOnlyList<string> Files { get; }

        public IReadOnlyList<string> Directories { get; }
    }

    /// <inheritdoc/>
    public NewProjectPlan? GetPlan(NewProjectRequest request)
    {
        if (ResolveOptions(request, out Options options) != null)
        {
            return null;
        }

        string extension = GetGumExtension(request);
        Targets real = ComputeTargets(request.Path, options.Platform != null, extension);
        PlanTemplate template = GetPlanTemplate(options, extension);

        string Name(string relative) => relative.Replace(PlanPlaceholderName, real.Name);
        return new NewProjectPlan(
            real.RootDirectory,
            template.Files.Select(Name).OrderBy(file => file, StringComparer.Ordinal).ToList(),
            template.Directories.Select(Name).OrderBy(directory => directory, StringComparer.Ordinal).ToList());
    }

    private PlanTemplate GetPlanTemplate(Options options, string extension)
    {
        string key = $"{options.Platform}|{options.IncludeFormsTemplate}|{options.ThemeDirectory}|{extension}";
        lock (_creationLock)
        {
            if (_planCache.TryGetValue(key, out PlanTemplate? cached))
            {
                return cached;
            }

            string tempRoot = Path.Combine(Path.GetTempPath(), "GumNewProjectPlan_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempRoot);
                Targets targets = ComputeTargets(
                    Path.Combine(tempRoot, PlanPlaceholderName), options.Platform != null, extension);

                // The same code the real creation runs, so the plan cannot drift from it.
                NewProjectFilesResult result = WriteFiles(options, targets, gumSourceDirectory: null);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Could not compute the new project plan: {result.ErrorMessage}");
                }

                PlanTemplate template = new PlanTemplate(
                    Directory.GetFiles(targets.RootDirectory, "*", SearchOption.AllDirectories)
                        .Select(path => ToRelative(targets.RootDirectory, path)).ToList(),
                    Directory.GetDirectories(targets.RootDirectory, "*", SearchOption.AllDirectories)
                        .Select(path => ToRelative(targets.RootDirectory, path)).ToList());
                _planCache[key] = template;
                return template;
            }
            finally
            {
                try
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
                catch (IOException)
                {
                    // Left in the temp folder; not worth failing the plan.
                }
            }
        }
    }

    private static string ToRelative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

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
        List<string> createdDirectories = FindDirectoriesThisRunWillCreate(project, options);
        HashSet<string> preexistingFiles = new HashSet<string>(
            Directory.Exists(project.RootDirectory)
                ? GetPlanFilePaths(project, options).Where(File.Exists)
                : Enumerable.Empty<string>());
        List<string> plannedFiles = Directory.Exists(project.RootDirectory)
            ? GetPlanFilePaths(project, options).ToList()
            : new List<string>();

        NewProjectFilesResult result;
        lock (_creationLock)
        {
            result = WriteFiles(options, targets, project.GumSourceDirectory);
        }

        if (result.Success && cancellationToken.IsCancellationRequested)
        {
            result = new NewProjectFilesResult { Canceled = true };
        }

        if (!result.Success)
        {
            RemoveWhatThisRunCreated(createdDirectories, plannedFiles, preexistingFiles);
        }

        return result;
    }

    // Writes the Gum project (and the code project around it). Shared by the real creation and the plan.
    private static NewProjectFilesResult WriteFiles(Options options, Targets targets, string? gumSourceDirectory)
    {
        try
        {
            if (options.Platform is HostPlatform platform)
            {
                PlatformProjectResult result = new PlatformProjectScaffolder().Create(
                    targets.RootDirectory, platform, options.IncludeFormsTemplate, gumSourceDirectory, options.ThemeDirectory);
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

    // The planned file paths, absolute, for the folder a request will write into.
    private IEnumerable<string> GetPlanFilePaths(ResolvedNewProject project, Options options)
    {
        PlanTemplate template = GetPlanTemplate(options, Path.GetExtension(project.GumProjectPath).TrimStart('.'));
        return template.Files.Select(file =>
            Path.Combine(project.RootDirectory, file.Replace(PlanPlaceholderName, project.Name).Replace('/', Path.DirectorySeparatorChar)));
    }

    // The top-most folders that do not exist yet and will: everything inside them is this run's to remove.
    private List<string> FindDirectoriesThisRunWillCreate(ResolvedNewProject project, Options options)
    {
        List<string> result = new List<string>();

        if (!Directory.Exists(project.RootDirectory))
        {
            string? topMissing = null;
            DirectoryInfo? directory = new DirectoryInfo(project.RootDirectory);
            while (directory != null && !directory.Exists)
            {
                topMissing = directory.FullName;
                directory = directory.Parent;
            }

            if (topMissing != null)
            {
                result.Add(topMissing);
            }

            return result;
        }

        PlanTemplate template = GetPlanTemplate(options, Path.GetExtension(project.GumProjectPath).TrimStart('.'));
        foreach (string relative in template.Directories.OrderBy(path => path.Length))
        {
            string path = Path.Combine(
                project.RootDirectory,
                relative.Replace(PlanPlaceholderName, project.Name).Replace('/', Path.DirectorySeparatorChar));
            bool isInsideCreated = result.Any(created => path.StartsWith(created + Path.DirectorySeparatorChar, StringComparison.Ordinal));
            if (!Directory.Exists(path) && !isInsideCreated)
            {
                result.Add(path);
            }
        }

        return result;
    }

    private static void RemoveWhatThisRunCreated(
        List<string> createdDirectories, List<string> plannedFiles, HashSet<string> preexistingFiles)
    {
        foreach (string file in plannedFiles)
        {
            if (!preexistingFiles.Contains(file) && File.Exists(file))
            {
                TryDelete(() => File.Delete(file));
            }
        }

        foreach (string directory in createdDirectories)
        {
            if (Directory.Exists(directory))
            {
                TryDelete(() => Directory.Delete(directory, recursive: true));
            }
        }
    }

    private static void TryDelete(Action delete)
    {
        try
        {
            delete();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Cleanup is best effort; the creation error is what the caller needs to see.
        }
    }

    #endregion

    #region Restore and Execute

    /// <inheritdoc/>
    public Task<RestoreResult> RestoreAsync(
        string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
        _restoreRunner.RestoreAsync(csprojPath, onOutputLine, cancellationToken);

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
        if (!files.Success || files.CsprojPath == null || !request.Restore)
        {
            return new NewProjectResult { Files = files };
        }

        progress?.Report(new NewProjectProgress(NewProjectStep.Restoring, "Restoring NuGet packages..."));
        RestoreResult restore = await RestoreAsync(files.CsprojPath, null, cancellationToken);
        return new NewProjectResult { Files = files, Restore = restore };
    }

    #endregion
}
