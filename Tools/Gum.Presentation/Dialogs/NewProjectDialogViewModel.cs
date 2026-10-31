using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Gum.Managers;
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
        nameof(RunRestore), nameof(IncludeRecommendedPackages), nameof(GenerateCodeChoice),
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
    private readonly INewProjectLocationSettings _locationSettings;
    private CancellationTokenSource? _cancellation;
    private NewProjectFilesResult? _createdFiles;
    private IReadOnlyList<string> _createdPackages = Array.Empty<string>();
    private ResolvedNewProject? _createdResolved;
    private int _refreshVersion;
    private string? _existingCodeProject;
    private bool _targetNotEmpty;
    private bool _targetExists;

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

    /// <summary>Creates the dialog; the folder tree and the checks for the default choices start right away.</summary>
    public NewProjectDialogViewModel(
        ThemeSelectionViewModel themeSelection,
        INewProjectService service,
        IExistingCodeProjectDetector detector,
        IDialogService dialogService,
        INewProjectLocationSettings locationSettings)
    {
        ThemeSelection = themeSelection;
        _service = service;
        _detector = detector;
        _dialogService = dialogService;
        _locationSettings = locationSettings;

        Platforms = NewProjectPlatforms.MainNames
            .Select(entry => new NewProjectPlatformOption(entry.Name, GetPlatformLabel(entry.Name)))
            .ToList();

        ProjectName = DefaultProjectName;
        Location = DefaultLocation;
        SelectedFileFormat = JsonFormat;
        IsIncludeFormsControls = true;
        IsIncludeDemoScreenGum = true;
        PlatformName = Platforms.FirstOrDefault()?.Name ?? "monogame";
        RunRestore = true;
        IncludeRecommendedPackages = true;
        GenerateCodeChoice = true;

        BrowseCommand = new RelayCommand(Browse);
        RetryRestoreCommand = new RelayCommand(() => _ = RunPackagesAndRestoreAsync());
        ContinueWithoutRestoreCommand = new RelayCommand(FinishCodeProject);
        BackCommand = new RelayCommand(Back);
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
        RefreshFolderTree();
        RefreshChecks();
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

    /// <summary>
    /// What the user ticked for Include recommended packages; on by default. See <see cref="AddsRecommendedPackages"/> for
    /// what applies.
    /// </summary>
    public bool IncludeRecommendedPackages
    {
        get => Get<bool>();
        set => Set(value);
    }

    /// <summary>What the user ticked for Generate code for the demo screen; on by default. See <see cref="GeneratesCode"/> for what applies.</summary>
    public bool GenerateCodeChoice
    {
        get => Get<bool>();
        set => Set(value);
    }

    #endregion

    #region Derived display state

    /// <summary>Whether the new project will have a screen or component to generate code for: the Forms template brings both.</summary>
    private bool HasSomethingToGenerate => IsIncludeFormsControls;

    /// <summary>Whether the Generate code checkbox shows: only with a C# project.</summary>
    public bool IsGenerateCodeVisible => IncludeCodeProject;

    /// <summary>
    /// Whether the checkbox can be changed: there is something to generate, and the restore runs (code generation reads the restored
    /// runtime package to choose the syntax it emits, so without the restore it does not run).
    /// </summary>
    public bool IsGenerateCodeEnabled => IsGenerateCodeVisible && RunRestore && HasSomethingToGenerate;

    /// <summary>The grey line under the checkbox: what it does, or why it is unavailable.</summary>
    public string GenerateCodeNote =>
        !HasSomethingToGenerate ? NewProjectDialogText.GenerateCodeNothingNote
        : !RunRestore ? NewProjectDialogText.GenerateCodeNeedsRestoreNote
        : NewProjectDialogText.GenerateCodeNote;

    /// <summary>Whether creating generates code for the demo screen.</summary>
    public bool GeneratesCode => IsGenerateCodeEnabled && GenerateCodeChoice;

    /// <summary>The checkbox's state: ticked only when code will really be generated; a disabled box shows unticked.</summary>
    public bool GenerateCodeChecked
    {
        get => GeneratesCode;
        set => GenerateCodeChoice = value;
    }

    /// <summary>
    /// Called after the tool removed the demo screen the dialog's project was created with (the demo box was unticked): rewrites the
    /// game so it no longer names the screen. The files were written while the demo still existed so the shown command and the dialog
    /// create the same files.
    /// </summary>
    public void RemoveDemoScreenFromHost()
    {
        if (_createdResolved is null || _createdFiles is null || !_createdFiles.Success)
        {
            return;
        }

        NewProjectFilesResult withoutDemo = new NewProjectFilesResult
        {
            Success = true, GumProjectPath = _createdFiles.GumProjectPath, CsprojPath = _createdFiles.CsprojPath, DemoScreen = null
        };
        _service.UpdateHost(_createdResolved, withoutDemo, packagesAdded: _createdPackages.Count > 0, screenCodeGenerated: false);

        // Code generation ran over the whole project, demo screen included: its class goes with the screen.
        if (_createdFiles.DemoScreen is { } demo && _createdFiles.CsprojPath is { } csproj)
        {
            DeleteGeneratedDemoClass(Path.GetDirectoryName(csproj)!, demo);
        }
    }

    private static void DeleteGeneratedDemoClass(string gameFolder, DemoScreenInfo demo)
    {
        try
        {
            foreach (string suffix in new[] { ".Generated.cs", ".cs" })
            {
                foreach (string file in Directory.EnumerateFiles(gameFolder, demo.ClassName + suffix, SearchOption.AllDirectories))
                {
                    string relative = Path.GetRelativePath(gameFolder, file).Replace((char)92, '/');
                    if (relative.StartsWith("Screens/", StringComparison.Ordinal))
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A class that could not be removed stays as ordinary project code.
        }
    }

    /// <summary>Whether the Include recommended packages checkbox shows: only with a C# project.</summary>
    public bool IsRecommendedPackagesVisible => IncludeCodeProject;

    /// <summary>Whether the checkbox can be changed: the platform has packages and the Gum runtime comes from NuGet.</summary>
    public bool IsRecommendedPackagesEnabled => IncludeCodeProject && !SourceLinked && RecommendedPackageIds.Count > 0;

    /// <summary>Whether creating adds the platform's recommended packages.</summary>
    public bool AddsRecommendedPackages => IncludeRecommendedPackages && IsRecommendedPackagesEnabled;

    /// <summary>The checkbox's state: ticked only when the packages will really be added; a disabled box shows unticked.</summary>
    public bool RecommendedPackagesChecked
    {
        get => AddsRecommendedPackages;
        set => IncludeRecommendedPackages = value;
    }

    /// <summary>The grey line under the checkbox: the packages it adds, or why it is unavailable.</summary>
    public string RecommendedPackagesNote => IsRecommendedPackagesEnabled
        ? NewProjectDialogText.RecommendedPackagesNote(string.Join(", ", RecommendedPackageIds))
        : NewProjectDialogText.RecommendedPackagesDisabledNote;

    private IReadOnlyList<string> RecommendedPackageIds =>
        NewProjectPlatforms.TryParse(PlatformName, out HostPlatform platform)
            ? RecommendedPackages.For(platform)
            : Array.Empty<string>();

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

    #region Folder notice

    /// <summary>Whether the line under Location that names the project folder shows.</summary>
    public bool IsFolderNoticeVisible
    {
        get => Get<bool>();
        private set => Set(value);
    }

    /// <summary>What happens to the project folder: made, reused, or Location used as it is.</summary>
    public string FolderNoticeHeading
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>The full project folder, with a trailing separator as the Folders section shows it.</summary>
    public string FolderNoticePath
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>Whether a Location that is already the project folder exists yet; empty when Location is the parent.</summary>
    public string FolderNoticeSuffix
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    /// <summary>The same answer, short, for the collapsed Folders header; empty when there is no notice.</summary>
    public string FoldersSummary
    {
        get => Get<string>() ?? string.Empty;
        private set => Set(value);
    }

    #endregion

    #region Folder tree

    /// <summary>
    /// The folder tree: one root folder (the full path of the folder that will be created) with the folders
    /// beneath it and the Gum project file marked. Empty while the platform is not a known one.
    /// </summary>
    public IReadOnlyList<NewProjectLayoutNode> FolderTree
    {
        get => Get<IReadOnlyList<NewProjectLayoutNode>>() ?? Array.Empty<NewProjectLayoutNode>();
        private set => Set(value);
    }

    /// <summary>
    /// The existing-code-project and project-already-exists checks for the latest inputs. They start at construction
    /// and after every input change on a background thread, so typing never waits for a slow folder. Completes when
    /// the warnings reflect the inputs (or a newer check took over).
    /// </summary>
    public Task ChecksRefresh { get; private set; } = Task.CompletedTask;

    /// <summary>Opens or closes every folder in the folder tree.</summary>
    public void SetFolderTreeExpanded(bool expanded)
    {
        foreach (NewProjectLayoutNode root in FolderTree)
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

    /// <summary>The failure panel's heading: says whether the restore or adding the recommended packages failed.</summary>
    public string RestoreFailureHeading
    {
        get => Get<string>() ?? NewProjectDialogText.RestoreFailedHeading;
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
            if (files.Canceled || !files.Success)
            {
                // Files may have been written or removed; the checks read the disk.
                RefreshChecks();
                if (!files.Canceled)
                {
                    ErrorMessage = files.ErrorMessage;
                }
                return;
            }

            _createdFiles = files;
            _createdPackages = validation.Resolved!.RecommendedPackages;
            _createdResolved = validation.Resolved;
            if (files.CsprojPath != null && (RunRestore || _createdPackages.Count > 0 || GeneratesDemoCode(validation.Resolved, files)))
            {
                await RunPackagesAndRestoreAsync();
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

    // The files were written while the dialog was open, so what the refusal and the not-empty warning
    // read from disk has changed without an input changing.
    private void Back()
    {
        IsRestoreFailed = false;
        RefreshChecks();
    }

    // The recommended packages are added first (nothing is fetched yet), then one restore covers them all. A failure
    // in either shows the same panel; Retry runs both again, and adding a package twice is harmless.
    private async Task RunPackagesAndRestoreAsync()
    {
        if (_createdFiles?.CsprojPath is not { } csproj)
        {
            return;
        }

        bool wasBusy = IsBusy;
        try
        {
            IsRestoreFailed = false;
            ProgressDetail = string.Empty;
            IProgress<string> output = new Progress<string>(line => ProgressDetail = line);
            if (_createdPackages.Count > 0)
            {
                SetBusy(true, "Adding recommended packages...");
                _cancellation = new CancellationTokenSource();
                AddPackagesResult added = await _service.AddPackagesAsync(csproj, _createdPackages, output.Report, _cancellation.Token);
                if (added.Status != RestoreStatus.Succeeded)
                {
                    RestoreFailureHeading = NewProjectDialogText.PackagesFailedHeading;
                    RestoreFailureText = added.Status == RestoreStatus.Canceled
                        ? "Adding packages was canceled."
                        : GetPackageFailureText(added);
                    IsRestoreFailed = true;
                    return;
                }

                if (_createdResolved != null)
                {
                    _service.UpdateHost(_createdResolved, _createdFiles, packagesAdded: true, screenCodeGenerated: false);
                }
            }

            if (!RunRestore)
            {
                await RunCodegenOrFinishAsync(output);
                return;
            }

            SetBusy(true, "Running dotnet restore...");
            ProgressDetail = string.Empty;
            _cancellation = new CancellationTokenSource();
            RestoreResult result = await _service.RestoreAsync(csproj, output.Report, _cancellation.Token);
            switch (result.Status)
            {
                case RestoreStatus.Succeeded:
                    await RunCodegenOrFinishAsync(output);
                    break;
                case RestoreStatus.Canceled:
                    RestoreFailureHeading = NewProjectDialogText.RestoreFailedHeading;
                    RestoreFailureText = "Restore was canceled.";
                    IsRestoreFailed = true;
                    break;
                default:
                    RestoreFailureHeading = NewProjectDialogText.RestoreFailedHeading;
                    RestoreFailureText = result.Status == RestoreStatus.ProcessNotFound && result.ErrorMessage.Length > 0
                        ? result.ErrorMessage
                        : (result.OutputTail.Length > 0 ? result.OutputTail : "dotnet restore failed.");
                    IsRestoreFailed = true;
                    break;
            }
        }
        catch (Exception exception)
        {
            RestoreFailureHeading = NewProjectDialogText.RestoreFailedHeading;
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

    private static bool GeneratesDemoCode(ResolvedNewProject resolved, NewProjectFilesResult files) =>
        resolved.GenerateCode && resolved.IncludeFormsTemplate;

    // The last step, after the restore: the generator reads the restored runtime package to choose the syntax it emits.
    private async Task RunCodegenOrFinishAsync(IProgress<string> output)
    {
        if (_createdResolved == null || _createdFiles == null || !GeneratesDemoCode(_createdResolved, _createdFiles))
        {
            FinishCodeProject();
            return;
        }

        SetBusy(true, "Generating code for the demo screen...");
        ProgressDetail = string.Empty;
        _cancellation = new CancellationTokenSource();
        GenerateCodeResult generated = await _service.GenerateCodeAsync(
            _createdResolved, _createdFiles, output.Report, _cancellation.Token);
        if (generated.Success)
        {
            FinishCodeProject();
            return;
        }

        RestoreFailureHeading = NewProjectDialogText.CodegenFailedHeading;
        RestoreFailureText = generated.Canceled
            ? "Code generation was canceled."
            : (generated.OutputTail.Length > 0 ? generated.OutputTail : "gumcli codegen failed.");
        IsRestoreFailed = true;
    }

    private static string GetPackageFailureText(AddPackagesResult added)
    {
        string detail = added.Status == RestoreStatus.ProcessNotFound && added.ErrorMessage.Length > 0
            ? added.ErrorMessage
            : (added.OutputTail.Length > 0 ? added.OutputTail : "dotnet add package failed.");
        return $"Could not add {added.FailedPackage}. {detail}";
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
        // Starting inside the project's own folder would show its contents; its parent shows it among its siblings.
        string startFolder = Directory.Exists(Location) ? Location : DefaultLocation;
        NewProjectFolder resolved = ResolvedFolder;
        if (resolved.LocationIsProjectFolder && Directory.Exists(resolved.ParentFolder) && Directory.Exists(Location))
        {
            startFolder = NewProjectDisplayPath.Normalize(resolved.ParentFolder);
        }

        string? folder = _dialogService.OpenFolder(new OpenFolderDialogOptions
        {
            Title = "Where would you like to create the project?",
            InitialDirectory = startFolder,
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
            // An error from the last attempt describes inputs that no longer stand.
            ErrorMessage = string.Empty;
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
            if (propertyName is nameof(IncludeCodeProject) or nameof(IsIncludeFormsControls) or nameof(GenerateCodeChoice) or nameof(RunRestore))
            {
                NotifyPropertyChanged(nameof(IsGenerateCodeVisible));
                NotifyPropertyChanged(nameof(IsGenerateCodeEnabled));
                NotifyPropertyChanged(nameof(GenerateCodeNote));
                NotifyPropertyChanged(nameof(GeneratesCode));
                NotifyPropertyChanged(nameof(GenerateCodeChecked));
            }
            if (propertyName is nameof(IncludeCodeProject) or nameof(PlatformName) or nameof(SourceLinkedChoice) or nameof(IncludeRecommendedPackages))
            {
                NotifyPropertyChanged(nameof(IsRecommendedPackagesVisible));
                NotifyPropertyChanged(nameof(IsRecommendedPackagesEnabled));
                NotifyPropertyChanged(nameof(AddsRecommendedPackages));
                NotifyPropertyChanged(nameof(RecommendedPackagesChecked));
                NotifyPropertyChanged(nameof(RecommendedPackagesNote));
            }
            RefreshDerived();
            RefreshFolderTree();
            RefreshChecks();
        }
    }

    #region Request and derived state

    private string Extension => UseXmlFormat ? ".gumx" : ".gumj";

    private string EffectiveName => ProjectName.Trim().Length > 0 ? ProjectName.Trim() : DefaultProjectName;

    private string EffectiveLocation => Location.Trim().Length > 0
        ? Location.Trim()
        : DefaultLocation;

    // The parent of the last created project's folder while it still exists, otherwise Documents.
    private string DefaultLocation =>
        _locationSettings.LastNewProjectParent is { Length: > 0 } remembered && Directory.Exists(remembered)
            ? remembered
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    // Where the project goes and what is remembered: Location itself when it ends in the project's name, else Location\Name.
    private NewProjectFolder ResolvedFolder => NewProjectFolder.Resolve(EffectiveLocation, EffectiveName);

    /// <summary>
    /// The folder that holds the project folder, which the caller remembers once the project exists: Location, or its parent when
    /// Location is the project folder. A file system or UNC root is never the project folder, so it is remembered as it is.
    /// </summary>
    public string ProjectParentFolder => NewProjectDisplayPath.Normalize(ResolvedFolder.ParentFolder);

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
            IncludeRecommendedPackages = game && AddsRecommendedPackages,
            GenerateCode = game && GeneratesCode,
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
        bool generatesCode = game && GeneratesCode;
        bool chained = game && (AddsRecommendedPackages || generatesCode);
        CommandLine = chained
            ? NewProjectCommandLine.BuildChained(
                path,
                PlatformName,
                IsIncludeFormsControls,
                IsIncludeFormsControls ? SelectedThemeName : null,
                RunRestore,
                AddsRecommendedPackages ? RecommendedPackageIds : Array.Empty<string>(),
                NewProjectDisplayPath.Normalize(Path.Combine(path, name + ".csproj")),
                generatesCode ? GumProjectPath : null,
                SourceLinked)
            : NewProjectCommandLine.Build(
                path,
                game ? PlatformName : null,
                IsIncludeFormsControls,
                IsIncludeFormsControls ? SelectedThemeName : null,
                game && SourceLinked,
                RunRestore);

        List<string> notes = new List<string>();
        if (chained)
        {
            notes.Add(NewProjectDialogText.ChainedCommandNote);
        }
        if (game && IsIncludeFormsControls && !RunRestore)
        {
            notes.Add(NewProjectDialogText.NoCodegenWithoutRestoreNote);
        }
        if (game && (AddsRecommendedPackages || generatesCode))
        {
            notes.Add(NewProjectDialogText.DemoHostCommandNote);
        }
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
        if (NewProjectNameRules.IsTooLong(name, IncludeCodeProject))
        {
            return NewProjectDialogText.NameTooLong;
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
        if (!Path.IsPathFullyQualified(Location.Trim()))
        {
            return NewProjectDialogText.LocationNotAbsolute;
        }
        if (IncludeCodeProject && SourceLinked && !_service.IsSourceLinkedAvailable)
        {
            return NewProjectDialogText.NeedsLocalSource;
        }
        return string.Empty;
    }

    private void RefreshWarnings()
    {
        string projectFolder = NewProjectDisplayPath.Normalize(ResolvedFolder.ProjectFolder);
        bool showExisting = IncludeCodeProject && _existingCodeProject != null;
        IsExistingCodeProjectWarningVisible = showExisting;
        if (showExisting)
        {
            string found = NewProjectDisplayPath.Normalize(_existingCodeProject!);
            ExistingCodeProjectText = $"Found {Path.GetFileName(found)} in {NewProjectDisplayPath.Normalize(Path.GetDirectoryName(found) ?? string.Empty)}.";
            bool hasGame1 = NewProjectPlatforms.TryParse(PlatformName, out HostPlatform platform) && NewProjectLayout.WritesGame1(platform);
            ExistingCodeProjectTip = NewProjectDialogText.WarningTip(hasGame1, projectFolder);
        }

        IsTargetNotEmptyWarningVisible = _targetNotEmpty;
        TargetNotEmptyText = _targetNotEmpty
            ? NewProjectDialogText.TargetNotEmptyBody(projectFolder)
            : string.Empty;

        RefreshFolderNotice();
    }

    // Says where the project folder goes. Silent while a validation message or an existing-project refusal is up, so it never
    // contradicts them. Whether the folder exists comes from the same worker-thread check pass as the banners.
    private void RefreshFolderNotice()
    {
        if (ValidationMessage.Length > 0 || RefusalText.Length > 0)
        {
            IsFolderNoticeVisible = false;
            FolderNoticeHeading = string.Empty;
            FolderNoticePath = string.Empty;
            FolderNoticeSuffix = string.Empty;
            FoldersSummary = string.Empty;
            return;
        }

        NewProjectFolder resolved = ResolvedFolder;
        string projectFolder = NewProjectDisplayPath.AsFolder(resolved.ProjectFolder);
        string suffix = string.Empty;
        if (resolved.LocationIsProjectFolder)
        {
            FolderNoticeHeading = NewProjectDialogText.FolderUsesThis;
            suffix = _targetExists ? NewProjectDialogText.FolderAlreadyExists : NewProjectDialogText.FolderWillBeCreated;
        }
        else
        {
            FolderNoticeHeading = _targetExists ? NewProjectDialogText.FolderUsesExisting : NewProjectDialogText.FolderCreates;
        }

        string parent = NewProjectDisplayPath.Normalize(resolved.ParentFolder);
        string parentName = Path.GetFileName(parent.TrimEnd(Path.DirectorySeparatorChar));
        FoldersSummary = _targetExists
            ? NewProjectDialogText.FolderUsesExisting
            : NewProjectDialogText.FoldersSummaryCreates(Path.GetFileName(projectFolder.TrimEnd(Path.DirectorySeparatorChar)), parentName.Length > 0 ? parentName : parent);
        FolderNoticePath = projectFolder;
        FolderNoticeSuffix = suffix;
        IsFolderNoticeVisible = true;
    }

    #endregion

    #region Folder tree and checks

    private void RefreshFolderTree()
    {
        bool game = IncludeCodeProject;
        string name = EffectiveName;
        string root = ResolvedFolder.ProjectFolder;

        NewProjectLayout? layout = null;
        if (!game)
        {
            layout = NewProjectLayout.ForGumOnly(root, name, UseXmlFormat);
        }
        else if (NewProjectPlatforms.TryParse(PlatformName, out HostPlatform platform))
        {
            layout = NewProjectLayout.ForCodeProject(root, name, platform, UseXmlFormat);
        }

        FolderTree = layout == null
            ? Array.Empty<NewProjectLayoutNode>()
            : new[] { NewProjectLayoutNode.Build(layout, NewProjectDisplayPath.AsFolder(root), openDepth: game ? 3 : 1) };
    }

    private void RefreshChecks()
    {
        int version = Interlocked.Increment(ref _refreshVersion);
        ChecksRefresh = RefreshChecksAsync(version, BuildRequest(), ResolvedFolder.ParentFolder, EffectiveName);
    }

    private async Task RefreshChecksAsync(int version, NewProjectRequest request, string folder, string name)
    {
        (string? existing, bool notEmpty, bool exists, string? refusal) = await Task.Run(() =>
        {
            string? existingProject = null;
            bool targetNotEmpty = false;
            bool targetExists = false;
            string? refused = null;
            try
            {
                NewProjectValidation validation = _service.Validate(request);
                if (validation.Error?.Kind == NewProjectErrorKind.ProjectAlreadyExists)
                {
                    refused = NewProjectDisplayPath.Normalize(validation.Error.Message);
                }
            }
            catch (Exception)
            {
                // Reported by OK when it runs.
            }
            try
            {
                existingProject = _detector.FindExistingCodeProject(folder);
                targetNotEmpty = _detector.TargetExistsAndIsNotEmpty(folder, name);
                targetExists = _detector.TargetExists(folder, name);
            }
            catch (Exception)
            {
                // A location that cannot be read has nothing to warn about.
            }
            return (existingProject, targetNotEmpty, targetExists, refused);
        });

        if (version != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        _existingCodeProject = existing;
        _targetNotEmpty = notEmpty;
        _targetExists = exists || notEmpty;
        RefusalText = refusal ?? string.Empty;
        NotifyPropertyChanged(nameof(IsRefused));
        AffirmativeCommand.NotifyCanExecuteChanged();
        RefreshWarnings();
    }

    #endregion

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
