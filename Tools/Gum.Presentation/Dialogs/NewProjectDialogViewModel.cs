using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using GumFormsPlugin.ViewModels;

namespace Gum.Dialogs;

/// <summary>One entry of the platform dropdown: the <c>--platform</c> name and what the dropdown shows.</summary>
/// <param name="Name">The name <c>gumcli new --platform</c> takes.</param>
/// <param name="Label">The text shown in the dropdown.</param>
public record NewProjectPlatformOption(string Name, string Label);

/// <summary>
/// The New Project dialog: name, location, file format, the Forms options, and optionally a C# code project
/// around the Gum project. OK creates the code project's files and runs <c>dotnet restore</c> while the dialog stays
/// open (busy state, Cancel, retry on a failed restore); for a Gum-only project it only closes and the caller
/// creates the project.
/// </summary>
public class NewProjectDialogViewModel : DialogViewModel
{
    /// <summary>The label of the default file format.</summary>
    public const string JsonFormat = ".gumj (JSON, recommended)";

    /// <summary>The label of the XML file format.</summary>
    public const string XmlFormat = ".gumx (XML)";

    private const string DefaultProjectName = "MyGame";
    private const string KniSkiaPlatform = "kniskia";
    private static readonly Regex _identifier = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private static readonly HashSet<string> _inputProperties = new HashSet<string>
    {
        nameof(ProjectName), nameof(Location), nameof(SelectedFileFormat), nameof(IsIncludeFormsControls),
        nameof(IsIncludeDemoScreenGum), nameof(IncludeCodeProject), nameof(PlatformName), nameof(SourceLinkedChoice),
        nameof(RunRestore),
    };

    private static readonly Dictionary<string, string> _platformNotes = new Dictionary<string, string>
    {
        ["stride"] = NewProjectDialogText.StridePlatformNote,
        [KniSkiaPlatform] = NewProjectDialogText.KniSkiaPlatformNote,
        ["silknet"] = NewProjectDialogText.SilkNetPlatformNote,
    };

    private readonly INewProjectService _service;
    private readonly IExistingCodeProjectDetector _detector;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _cancellation;
    private NewProjectFilesResult? _createdFiles;
    private int _refreshVersion;
    private string? _existingCodeProject;
    private bool _targetNotEmpty;

    /// <summary>
    /// Theme picker, shared with the Add Forms dialog. Only relevant when
    /// <see cref="IsIncludeFormsControls"/> is checked.
    /// </summary>
    public ThemeSelectionViewModel ThemeSelection { get; }

    /// <summary>The file formats offered, JSON first.</summary>
    public IReadOnlyList<string> FileFormats { get; } = new[] { JsonFormat, XmlFormat };

    /// <summary>The platforms offered, in <see cref="NewProjectPlatforms.MainNames"/> order.</summary>
    public IReadOnlyList<NewProjectPlatformOption> Platforms { get; }

    /// <summary>Browse button: picks the Location folder.</summary>
    public RelayCommand BrowseCommand { get; }

    /// <summary>Failed restore: runs only the restore again.</summary>
    public RelayCommand RetryRestoreCommand { get; }

    /// <summary>Failed restore: keeps the created files and finishes without restoring.</summary>
    public RelayCommand ContinueWithoutRestoreCommand { get; }

    /// <summary>Failed restore: returns to the editable dialog; the created files stay.</summary>
    public RelayCommand BackCommand { get; }

    /// <summary>Asks the view to copy <see cref="CommandLine"/> to the clipboard.</summary>
    public RelayCommand CopyCommandLineCommand { get; }

    /// <summary>Raised when the user asks to copy the command line; the view puts it on the clipboard.</summary>
    public event Action<string>? CommandLineCopyRequested;

    /// <summary>Creates the dialog; the plan for the default choices starts computing right away.</summary>
    public NewProjectDialogViewModel(
        ThemeSelectionViewModel themeSelection,
        INewProjectService service,
        IExistingCodeProjectDetector detector,
        IDialogService dialogService)
    {
        ThemeSelection = themeSelection;
        _service = service;
        _detector = detector;
        _dialogService = dialogService;

        Platforms = NewProjectPlatforms.MainNames
            .Select(entry => new NewProjectPlatformOption(entry.Name, GetPlatformLabel(entry.Name)))
            .ToList();

        ProjectName = DefaultProjectName;
        Location = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        SelectedFileFormat = JsonFormat;
        IsIncludeFormsControls = true;
        IsIncludeDemoScreenGum = false;
        PlatformName = Platforms.FirstOrDefault()?.Name ?? "monogame";
        RunRestore = true;

        BrowseCommand = new RelayCommand(Browse);
        RetryRestoreCommand = new RelayCommand(() => _ = RunRestoreStepAsync());
        ContinueWithoutRestoreCommand = new RelayCommand(FinishCodeProject);
        BackCommand = new RelayCommand(() => IsRestoreFailed = false);
        CopyCommandLineCommand = new RelayCommand(() => CommandLineCopyRequested?.Invoke(CommandLine));

        PropertyChanged += (_, e) => HandlePropertyChanged(e.PropertyName);
        ThemeSelection.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ThemeSelectionViewModel.SelectedTheme))
            {
                HandlePropertyChanged(nameof(ThemeSelection));
            }
        };

        RefreshDerived();
        RefreshPlan();
    }

    #region Inputs

    /// <summary>The project's name; also the new folder's name and, with a code project, the .csproj name.</summary>
    public string ProjectName
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    /// <summary>The folder the project's folder is created in.</summary>
    public string Location
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    /// <summary>One of <see cref="FileFormats"/>.</summary>
    public string SelectedFileFormat
    {
        get => Get<string>() ?? JsonFormat;
        set => Set(value);
    }

    /// <summary>Whether the Gum project is a .gumx (XML) instead of a .gumj (JSON).</summary>
    public bool UseXmlFormat => SelectedFileFormat == XmlFormat;

    /// <summary>
    /// Whether the selected Forms theme (<see cref="ThemeSelection"/>) is imported into the new
    /// project.
    /// </summary>
    public bool IsIncludeFormsControls
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>
    /// Whether the selected theme's DemoScreenGum is imported alongside its controls. Only
    /// relevant when <see cref="IsIncludeFormsControls"/> is checked.
    /// </summary>
    public bool IsIncludeDemoScreenGum
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>Whether a C# code project is created around the Gum project. Off by default.</summary>
    public bool IncludeCodeProject
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>The <c>--platform</c> name of the code project.</summary>
    public string PlatformName
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    /// <summary>What the user ticked for Link Gum source; see <see cref="SourceLinked"/> for what applies.</summary>
    public bool SourceLinkedChoice
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>
    /// Whether the code project references the Gum checkout. Always on for KNI with Skia (its package is not on
    /// NuGet), and off when no checkout is available.
    /// </summary>
    public bool SourceLinked
    {
        get => IsSourceLinkedForced || (SourceLinkedChoice && _service.IsSourceLinkedAvailable);
        set => SourceLinkedChoice = value;
    }

    /// <summary>Whether Run dotnet restore is ticked.</summary>
    public bool RunRestore
    {
        get => Get<bool>();
        set => Set(value);
    }

    #endregion

    #region Derived display state

    /// <summary>Whether the platform forces source-linked on.</summary>
    public bool IsSourceLinkedForced => IncludeCodeProject && PlatformName == KniSkiaPlatform;

    /// <summary>Whether the Link Gum source box can be changed.</summary>
    public bool IsSourceLinkedEnabled => IncludeCodeProject && !IsSourceLinkedForced && _service.IsSourceLinkedAvailable;

    /// <summary>The warning-colored note under the platform dropdown, or empty.</summary>
    public string PlatformNote
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>The <c>gumcli new</c> command that reproduces the current choices.</summary>
    public string CommandLine
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>What the command cannot express, or empty.</summary>
    public string CommandLineNote
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>Why OK is disabled, or empty.</summary>
    public string ValidationMessage
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>The Gum project file the choices create.</summary>
    public string GumProjectPath
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>Whether the amber banner for an existing code project at the location shows.</summary>
    public bool IsExistingCodeProjectWarningVisible
    {
        get => Get<bool>();
        private set => Set(value);
    }

    /// <summary>The banner's body, such as "Found MyOther.csproj in C:\Projects."</summary>
    public string ExistingCodeProjectText
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>The banner's info icon hover text.</summary>
    public string ExistingCodeProjectTip
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>The reason <c>gumcli new</c> would refuse these choices (the project file already exists), or empty. OK is off while it shows.</summary>
    public string RefusalText
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>Whether <see cref="RefusalText"/> has text.</summary>
    public bool IsRefused => RefusalText.Length > 0;

    /// <summary>Whether the banner for a target folder that already holds files shows.</summary>
    public bool IsTargetNotEmptyWarningVisible
    {
        get => Get<bool>();
        private set => Set(value);
    }

    /// <summary>The not-empty banner's body.</summary>
    public string TargetNotEmptyText
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    #endregion

    #region Plan

    /// <summary>The Files tree: one root folder, or empty while no plan exists.</summary>
    public IReadOnlyList<NewProjectPlanNode> PlanTree
    {
        get => Get<IReadOnlyList<NewProjectPlanNode>>() ?? Array.Empty<NewProjectPlanNode>();
        private set => Set(value);
    }

    /// <summary>The number of files the creation writes, or 0 while there is no plan.</summary>
    public int PlanFileCount
    {
        get => Get<int>();
        private set => Set(value);
    }

    /// <summary>The Files heading's count, such as "(105)", or empty while there is no plan.</summary>
    public string FilesCountText
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>The line under the tree, such as "105 files".</summary>
    public string PlanTotalText
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>
    /// The plan computation for the latest inputs. Starts at construction and after every input change; typing
    /// never waits for it. Completes when the Files tree reflects the inputs (or a newer computation took over).
    /// </summary>
    public Task PlanRefresh { get; private set; } = Task.CompletedTask;

    /// <summary>Opens or closes every folder in the Files tree.</summary>
    public void SetPlanExpanded(bool expanded)
    {
        foreach (NewProjectPlanNode root in PlanTree)
        {
            root.SetExpandedRecursive(expanded);
        }
    }

    #endregion

    #region Run state

    /// <summary>True while files are being written or restore is running; the inputs are disabled.</summary>
    public bool IsBusy
    {
        get => Get<bool>();
        private set => Set(value);
    }

    /// <summary>What the busy state is doing, such as "Creating files...".</summary>
    public string ProgressText
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>The last line <c>dotnet restore</c> printed, shown under <see cref="ProgressText"/>.</summary>
    public string ProgressDetail
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>The error from a failed validation or file creation; the dialog stays open on it.</summary>
    public string ErrorMessage
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>Whether <see cref="ErrorMessage"/> has text.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>True after the restore failed or was canceled: Retry, Continue without restore and Back are offered.</summary>
    public bool IsRestoreFailed
    {
        get => Get<bool>();
        private set
        {
            if (Set(value))
            {
                AffirmativeCommand.NotifyCanExecuteChanged();
                NotifyPropertyChanged(nameof(IsEditable));
            }
        }
    }

    /// <summary>Why the restore failed.</summary>
    public string RestoreFailureText
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>Whether the inputs can be changed (not busy and not showing a restore failure).</summary>
    public bool IsEditable => !IsBusy && !IsRestoreFailed;

    /// <summary>The running creation, so a caller or test can wait for it.</summary>
    public Task CreationTask { get; private set; } = Task.CompletedTask;

    /// <summary>Whether the OK that closed the dialog created a code project (the files are already on disk).</summary>
    public bool IsCodeProjectCreated { get; private set; }

    /// <summary>The Gum project file created by a code project run; the caller loads it.</summary>
    public string? CreatedGumProjectPath => _createdFiles?.GumProjectPath;

    #endregion

    /// <inheritdoc/>
    public override bool CanExecuteAffirmative() => !IsBusy && !IsRestoreFailed && ValidationMessage.Length == 0 && RefusalText.Length == 0;

    /// <summary>
    /// OK. A Gum-only project closes the dialog and the caller creates it. With a code project the files and the
    /// restore run first, with the dialog open; it closes only when they are done.
    /// </summary>
    public override void OnAffirmative()
    {
        if (!CanExecuteAffirmative())
        {
            return;
        }

        if (IncludeCodeProject)
        {
            CreationTask = RunCreationAsync();
            return;
        }

        base.OnAffirmative();
    }

    /// <summary>Cancel. While busy it stops the work and leaves the dialog open; otherwise it closes the dialog.</summary>
    protected override void OnNegative()
    {
        if (IsBusy)
        {
            _cancellation?.Cancel();
            return;
        }

        base.OnNegative();
    }

    /// <summary>Validates, writes the code project's files, then restores. Never throws.</summary>
    public async Task RunCreationAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            NewProjectValidation validation = _service.Validate(BuildRequest());
            if (!validation.IsValid)
            {
                ErrorMessage = validation.Error!.Message;
                return;
            }

            SetBusy(true, "Creating files...");
            _cancellation = new CancellationTokenSource();
            NewProjectFilesResult files = await _service.CreateFilesAsync(validation.Resolved!, _cancellation.Token);
            if (files.Canceled)
            {
                return;
            }
            if (!files.Success)
            {
                ErrorMessage = files.ErrorMessage;
                return;
            }

            _createdFiles = files;
            if (RunRestore && files.CsprojPath != null)
            {
                await RunRestoreStepAsync();
            }
            else
            {
                FinishCodeProject();
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            SetBusy(false, string.Empty);
        }
    }

    private async Task RunRestoreStepAsync()
    {
        if (_createdFiles?.CsprojPath is not { } csproj)
        {
            return;
        }

        bool wasBusy = IsBusy;
        try
        {
            IsRestoreFailed = false;
            SetBusy(true, "Running dotnet restore...");
            ProgressDetail = string.Empty;
            _cancellation = new CancellationTokenSource();
            IProgress<string> output = new Progress<string>(line => ProgressDetail = line);
            RestoreResult result = await _service.RestoreAsync(csproj, output.Report, _cancellation.Token);
            switch (result.Status)
            {
                case RestoreStatus.Succeeded:
                    FinishCodeProject();
                    break;
                case RestoreStatus.Canceled:
                    RestoreFailureText = "Restore was canceled.";
                    IsRestoreFailed = true;
                    break;
                default:
                    RestoreFailureText = result.Status == RestoreStatus.ProcessNotFound && result.ErrorMessage.Length > 0
                        ? result.ErrorMessage
                        : (result.OutputTail.Length > 0 ? result.OutputTail : "dotnet restore failed.");
                    IsRestoreFailed = true;
                    break;
            }
        }
        catch (Exception exception)
        {
            RestoreFailureText = exception.Message;
            IsRestoreFailed = true;
        }
        finally
        {
            if (!wasBusy)
            {
                SetBusy(false, string.Empty);
            }
        }
    }

    private void FinishCodeProject()
    {
        IsCodeProjectCreated = true;
        IsRestoreFailed = false;
        base.OnAffirmative();
    }

    private void SetBusy(bool busy, string progressText)
    {
        IsBusy = busy;
        ProgressText = progressText;
        AffirmativeCommand.NotifyCanExecuteChanged();
        NotifyPropertyChanged(nameof(IsEditable));
    }

    private void Browse()
    {
        string? folder = _dialogService.OpenFolder(new OpenFolderDialogOptions
        {
            Title = "Where would you like to create the project?",
            InitialDirectory = Directory.Exists(Location) ? Location : null,
        });
        if (folder != null)
        {
            Location = NewProjectDisplayPath.Normalize(folder);
        }
    }

    private void HandlePropertyChanged(string? propertyName)
    {
        if (propertyName == nameof(ErrorMessage))
        {
            NotifyPropertyChanged(nameof(HasError));
        }

        if (propertyName != null && (_inputProperties.Contains(propertyName) || propertyName == nameof(ThemeSelection)))
        {
            if (propertyName == nameof(SelectedFileFormat))
            {
                NotifyPropertyChanged(nameof(UseXmlFormat));
            }
            if (propertyName is nameof(IncludeCodeProject) or nameof(PlatformName) or nameof(SourceLinkedChoice))
            {
                NotifyPropertyChanged(nameof(SourceLinked));
                NotifyPropertyChanged(nameof(IsSourceLinkedForced));
                NotifyPropertyChanged(nameof(IsSourceLinkedEnabled));
            }
            RefreshDerived();
            RefreshPlan();
        }
    }

    #region Request and derived state

    private string Extension => UseXmlFormat ? ".gumx" : ".gumj";

    private string EffectiveName => ProjectName.Trim().Length > 0 ? ProjectName.Trim() : DefaultProjectName;

    private string EffectiveLocation => Location.Trim().Length > 0
        ? Location.Trim()
        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    private string SelectedThemeName => ThemeSelection.GetSelectedThemeOrDefault();

    private NewProjectRequest BuildRequest()
    {
        bool game = IncludeCodeProject;
        return new NewProjectRequest
        {
            Path = NewProjectCommandLine.GetPath(EffectiveLocation, EffectiveName, Extension, game),
            Template = IsIncludeFormsControls ? "forms" : "empty",
            Theme = IsIncludeFormsControls ? SelectedThemeName : null,
            Platform = game ? PlatformName : null,
            SourceLinked = game && SourceLinked,
            Restore = RunRestore,
            UseXmlFormat = UseXmlFormat,
        };
    }

    private void RefreshDerived()
    {
        bool game = IncludeCodeProject;
        string name = EffectiveName;
        string path = NewProjectCommandLine.GetPath(EffectiveLocation, name, Extension, game);

        GumProjectPath = game
            ? Path.Combine(path, "Content", "GumProject", "GumProject" + Extension)
            : path;
        CommandLine = NewProjectCommandLine.Build(
            path,
            game ? PlatformName : null,
            IsIncludeFormsControls,
            IsIncludeFormsControls ? SelectedThemeName : null,
            game && SourceLinked,
            RunRestore);

        List<string> notes = new List<string>();
        if (IsIncludeFormsControls && !IsIncludeDemoScreenGum)
        {
            notes.Add(NewProjectDialogText.NoDemoCommandNote);
        }
        if (!game && UseXmlFormat)
        {
            notes.Add(NewProjectDialogText.ExtensionPicksFormatNote);
        }
        if (game && UseXmlFormat)
        {
            notes.Add(NewProjectDialogText.NoXmlWithCodeProjectNote);
        }
        CommandLineNote = string.Join(" ", notes);

        PlatformNote = game && _platformNotes.TryGetValue(PlatformName, out string? note) ? note : string.Empty;

        ValidationMessage = GetValidationMessage();
        RefreshWarnings();
        AffirmativeCommand.NotifyCanExecuteChanged();
    }

    private string GetValidationMessage()
    {
        string name = ProjectName.Trim();
        if (name.Length == 0)
        {
            return NewProjectDialogText.EnterName;
        }
        if (!NewProjectNameRules.IsValidFolderName(name))
        {
            return NewProjectDialogText.InvalidFolderName;
        }
        if (IncludeCodeProject && !_identifier.IsMatch(name))
        {
            return NewProjectDialogText.InvalidCSharpName;
        }
        if (Location.Trim().Length == 0)
        {
            return NewProjectDialogText.ChooseLocation;
        }
        if (Location.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return NewProjectDialogText.InvalidLocation;
        }
        if (IncludeCodeProject && SourceLinked && !_service.IsSourceLinkedAvailable)
        {
            return NewProjectDialogText.NeedsLocalSource;
        }
        return string.Empty;
    }

    private void RefreshWarnings()
    {
        string folder = EffectiveLocation;
        bool showExisting = IncludeCodeProject && _existingCodeProject != null;
        IsExistingCodeProjectWarningVisible = showExisting;
        if (showExisting)
        {
            string found = NewProjectDisplayPath.Normalize(_existingCodeProject!);
            ExistingCodeProjectText = $"Found {Path.GetFileName(found)} in {NewProjectDisplayPath.Normalize(Path.GetDirectoryName(found) ?? string.Empty)}.";
            bool hasGame1 = PlatformName is not ("raylib" or "silknet" or "stride");
            ExistingCodeProjectTip = NewProjectDialogText.WarningTip(hasGame1, NewProjectDisplayPath.Normalize(Path.Combine(folder, EffectiveName)));
        }

        IsTargetNotEmptyWarningVisible = _targetNotEmpty;
        TargetNotEmptyText = _targetNotEmpty
            ? NewProjectDialogText.TargetNotEmptyBody(NewProjectDisplayPath.Normalize(Path.Combine(folder, EffectiveName)))
            : string.Empty;
    }

    #endregion

    #region Plan refresh

    private void RefreshPlan()
    {
        int version = Interlocked.Increment(ref _refreshVersion);
        NewProjectRequest request = BuildRequest();
        string folder = EffectiveLocation;
        string name = EffectiveName;
        bool game = IncludeCodeProject;
        string mainFile = game ? "Content/GumProject/GumProject" + Extension : name + Extension;
        PlanRefresh = RefreshPlanAsync(version, request, folder, name, game, mainFile);
    }

    private async Task RefreshPlanAsync(int version, NewProjectRequest request, string folder, string name, bool game, string mainFile)
    {
        (NewProjectPlan? plan, string? existing, bool notEmpty, string? refusal) = await Task.Run(() =>
        {
            NewProjectPlan? computedPlan = null;
            string? existingProject = null;
            bool targetNotEmpty = false;
            string? refused = null;
            try
            {
                NewProjectValidation validation = _service.Validate(request);
                if (validation.Error?.Kind == NewProjectErrorKind.ProjectAlreadyExists)
                {
                    refused = NewProjectDisplayPath.Normalize(validation.Error.Message);
                }
                else if (!game)
                {
                    // A project in the other file format at the same place would be overwritten too.
                    string otherExtension = Path.GetExtension(request.Path) == ".gumx" ? ".gumj" : ".gumx";
                    NewProjectValidation other = _service.Validate(new NewProjectRequest
                    {
                        Path = Path.ChangeExtension(request.Path, otherExtension),
                        Template = request.Template,
                        Theme = request.Theme,
                        UseXmlFormat = request.UseXmlFormat,
                    });
                    if (other.Error?.Kind == NewProjectErrorKind.ProjectAlreadyExists)
                    {
                        refused = NewProjectDisplayPath.Normalize(other.Error.Message);
                    }
                }
            }
            catch (Exception)
            {
                // Reported by OK when it runs.
            }
            try
            {
                computedPlan = _service.GetPlan(request);
            }
            catch (Exception)
            {
                // Bad characters in the path or a failed temporary run: no tree, and OK reports the real reason.
            }
            try
            {
                existingProject = _detector.FindExistingCodeProject(folder);
                targetNotEmpty = _detector.TargetExistsAndIsNotEmpty(folder, name);
            }
            catch (Exception)
            {
                // A location that cannot be read has nothing to warn about.
            }
            return (computedPlan, existingProject, targetNotEmpty, refused);
        });

        if (version != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        _existingCodeProject = existing;
        _targetNotEmpty = notEmpty;
        RefusalText = refusal ?? string.Empty;
        NotifyPropertyChanged(nameof(IsRefused));
        AffirmativeCommand.NotifyCanExecuteChanged();
        RefreshWarnings();

        if (plan == null)
        {
            PlanTree = Array.Empty<NewProjectPlanNode>();
            PlanFileCount = 0;
            FilesCountText = string.Empty;
            PlanTotalText = string.Empty;
            return;
        }

        plan = WithoutUncheckedDemoScreen(plan);
        PlanTree = new[] { NewProjectPlanNode.Build(plan, NewProjectDisplayPath.AsFolder(Path.Combine(folder, name)), mainFile, openDepth: game ? 3 : 1) };
        PlanFileCount = plan.Files.Count;
        // The plan is the template the shared creators write. The tool adds more after creating (the starting screen,
        // its generated code, the event log), so no total is shown: a count that is too low is worse than none.
        FilesCountText = string.Empty;
        PlanTotalText = NewProjectDialogText.TemplateFilesNote;
    }

    #endregion

    // The tool leaves DemoScreenGum out unless it is checked, though the template always contains it.
    private NewProjectPlan WithoutUncheckedDemoScreen(NewProjectPlan plan)
    {
        if (!IsIncludeFormsControls || IsIncludeDemoScreenGum)
        {
            return plan;
        }

        string[] files = plan.Files
            .Where(file => !DemoScreenRule.IsDemoScreenFile(file))
            .ToArray();
        return new NewProjectPlan(plan.RootDirectory, files, plan.Directories);
    }

    private static string GetPlatformLabel(string name) => name switch
    {
        "monogame" => "MonoGame",
        "kni" => "KNI",
        KniSkiaPlatform => "KNI with Skia",
        "raylib" => "raylib",
        "stride" => "Stride (Windows only)",
        "silknet" => "Silk.NET",
        _ => name,
    };
}
