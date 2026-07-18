using CommunityToolkit.Mvvm.Input;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Mvvm;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ProjectServices;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportFromGumxPlugin.ViewModels;

public enum SourceType
{
    LocalFile,
    Url
}

public class ImportFromGumxViewModel : DialogViewModel
{
    private readonly IGumxSourceService _sourceService;
    private readonly IGumxDependencyResolver _dependencyResolver;
    private readonly IGumxImportService _importService;
    private readonly IProjectState _projectState;
    private readonly IDialogService _dialogService;
    private readonly IDispatcher _dispatcher;

    private GumProjectSave? _sourceProject;
    private string _sourceBase = string.Empty;
    private bool _isImportComplete;

    private readonly List<ImportTreeNodeViewModel> _allLeafItems = new List<ImportTreeNodeViewModel>();
    private readonly HashSet<string> _autoAddedComponentNames = new HashSet<string>();
    // Behaviors and Standards aren't pulled in by the dependency resolver from a user's perspective —
    // the user picks them directly. Track explicit user picks so RecomputeTransitiveDependencies
    // doesn't clobber them when it rebuilds those groups from resolver output. (#2642)
    private readonly HashSet<string> _userExplicitBehaviorNames = new HashSet<string>();
    private readonly HashSet<string> _userExplicitStandardNames = new HashSet<string>();
    private ImportTreeNodeViewModel? _behaviorsGroupNode;
    private ImportTreeNodeViewModel? _standardsGroupNode;
    private bool _recomputeQueued = false;

    public string SourcePath
    {
        get => Get<string>() ?? string.Empty;
        set
        {
            Set(value);
            LoadPreviewCommand?.NotifyCanExecuteChanged();
        }
    }

    public SourceType SourceType
    {
        get => Get<SourceType>();
        set
        {
            if (Set(value))
            {
                ClearPreview();
            }
        }
    }

    [DependsOn(nameof(SourceType))]
    public bool IsLocalFile
    {
        get => SourceType == SourceType.LocalFile;
        set { if (value) { SourceType = SourceType.LocalFile; } }
    }

    [DependsOn(nameof(SourceType))]
    public bool IsUrl
    {
        get => SourceType == SourceType.Url;
        set { if (value) { SourceType = SourceType.Url; } }
    }

    [DependsOn(nameof(IsLocalFile))]
    public bool IsBrowseButtonVisible => IsLocalFile;

    public string DestinationSubfolder
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    public bool IsPreviewLoaded
    {
        get => Get<bool>();
        set
        {
            Set(value);
            AffirmativeCommand.NotifyCanExecuteChanged();
        }
    }

    [DependsOn(nameof(IsPreviewLoaded))]
    public bool IsPreviewVisible => IsPreviewLoaded;

    public bool IsLoading
    {
        get => Get<bool>();
        set
        {
            Set(value);
            AffirmativeCommand.NotifyCanExecuteChanged();
            LoadPreviewCommand.NotifyCanExecuteChanged();
        }
    }

    public string? LoadingStatus
    {
        get => Get<string>();
        set => Set(value);
    }

    [DependsOn(nameof(LoadingStatus))]
    public bool IsLoadingStatusVisible => !string.IsNullOrEmpty(LoadingStatus);

    public string? ErrorMessage
    {
        get => Get<string>();
        set => Set(value);
    }

    [DependsOn(nameof(ErrorMessage))]
    public bool IsErrorMessageVisible => !string.IsNullOrEmpty(ErrorMessage);

    public string? WarningMessage
    {
        get => Get<string>();
        set => Set(value);
    }

    [DependsOn(nameof(WarningMessage))]
    public bool IsWarningMessageVisible => !string.IsNullOrEmpty(WarningMessage);

    public ObservableCollection<ImportTreeNodeViewModel> RootNodes { get; } = new ObservableCollection<ImportTreeNodeViewModel>();

    public AsyncRelayCommand LoadPreviewCommand { get; }

    /// <summary>
    /// Opens a read-only modal showing the per-variable diff for a flagged Standard row
    /// (#2779). Bound to the row's "Details..." button via RelativeSource; the parameter
    /// is the row's <see cref="ImportTreeNodeViewModel"/>.
    /// </summary>
    public RelayCommand<ImportTreeNodeViewModel> ShowStandardDiffCommand { get; }

    /// <summary>
    /// Prompts the user (via <see cref="IDialogService"/>) for a source <c>.gumx</c> file, then
    /// sets <see cref="SourcePath"/> and loads its preview. Bound to the "Browse..." button;
    /// a no-op if the user cancels the picker.
    /// </summary>
    public AsyncRelayCommand BrowseCommand { get; }

    public ImportFromGumxViewModel(
        IGumxSourceService sourceService,
        IGumxDependencyResolver dependencyResolver,
        IGumxImportService importService,
        IProjectState projectState,
        IDialogService dialogService,
        IDispatcher dispatcher)
    {
        _sourceService = sourceService;
        _dependencyResolver = dependencyResolver;
        _importService = importService;
        _projectState = projectState;
        _dialogService = dialogService;
        _dispatcher = dispatcher;

        AffirmativeText = "Import";
        SourceType = SourceType.LocalFile;

        LoadPreviewCommand = new AsyncRelayCommand(ExecuteLoadPreviewAsync, CanExecuteLoadPreview);
        ShowStandardDiffCommand = new RelayCommand<ImportTreeNodeViewModel>(ExecuteShowStandardDiff);
        BrowseCommand = new AsyncRelayCommand(ExecuteBrowseAsync);
    }

    private void ExecuteShowStandardDiff(ImportTreeNodeViewModel? row)
    {
        if (row?.StandardDiffRows is null || row.StandardDiffRows.Count == 0) { return; }

        StandardDiffDetailsViewModel detailsVm =
            new StandardDiffDetailsViewModel(row.DisplayName, row.StandardDiffRows);
        _dialogService.Show(detailsVm);
    }

    private async Task ExecuteBrowseAsync()
    {
        List<string>? selectedFiles = _dialogService.OpenFile(new OpenFileDialogOptions
        {
            Title = "Open Gum Project",
            Filter = "Gum Project Files (*.gumx)|*.gumx|All Files (*.*)|*.*"
        });

        string? selectedPath = selectedFiles?.FirstOrDefault();
        if (string.IsNullOrEmpty(selectedPath)) { return; }

        SourcePath = selectedPath;
        if (LoadPreviewCommand.CanExecute(null))
        {
            await LoadPreviewCommand.ExecuteAsync(null);
        }
    }

    public override bool CanExecuteAffirmative() => IsPreviewLoaded && !IsLoading;

    public override async void OnAffirmative()
    {
        if (_isImportComplete)
        {
            base.OnAffirmative();
            return;
        }

        if (_sourceProject == null) { return; }

        if (!_allLeafItems.Any(i => i.InclusionState == InclusionState.Explicit))
        {
            WarningMessage = "Please select at least one item to import.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        WarningMessage = null;
        try
        {
            var selections = BuildSelections();
            var result = await _importService.ImportAsync(
                selections, _sourceProject, _sourceBase, DestinationSubfolder);

            // If conflicts came back, ask the user once how to handle the whole batch and retry.
            if (result.ConflictingElements.Count > 0)
            {
                ConflictResolution? resolution = PromptConflictResolution(result.ConflictingElements);
                if (resolution == null || resolution == ConflictResolution.Cancel)
                {
                    string elementList = FormatConflictList(result.ConflictingElements);
                    _isImportComplete = true;
                    ErrorMessage =
                        $"Import cancelled: {result.ConflictingElements.Count} element(s) already exist " +
                        $"in this project: {elementList}";
                    return;
                }

                result = await _importService.ImportAsync(
                    selections, _sourceProject, _sourceBase, DestinationSubfolder, resolution.Value);
            }

            _isImportComplete = true;

            if (result.ConflictingElements.Count > 0)
            {
                // Defensive: a retry pass should not produce new conflicts, but report them if it does.
                ErrorMessage =
                    $"Import cancelled: {result.ConflictingElements.Count} element(s) already exist " +
                    $"in this project: {FormatConflictList(result.ConflictingElements)}";
            }
            else if (result.SkippedElements.Count > 0)
            {
                AffirmativeText = "Close";
                string elementList = string.Join(", ", result.SkippedElements.Take(5));
                if (result.SkippedElements.Count > 5)
                {
                    elementList += $" (and {result.SkippedElements.Count - 5} more)";
                }
                WarningMessage =
                    $"{result.SkippedElements.Count} element(s) were not imported because required " +
                    $"asset files could not be found: {elementList}";
            }
            else
            {
                base.OnAffirmative();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Asks the user how to handle existing destination files. Returns the chosen resolution,
    /// or <see cref="ConflictResolution.Cancel"/> if the user dismisses the dialog.
    /// </summary>
    /// <remarks>Virtual so tests can override without spinning up a WPF dialog.</remarks>
    protected internal virtual ConflictResolution? PromptConflictResolution(IReadOnlyList<string> conflictingElements)
    {
        string elementList = FormatConflictList(conflictingElements, max: 10);
        string message =
            $"The following {conflictingElements.Count} element(s) already exist in this project:\n\n" +
            $"{elementList}\n\n" +
            "How would you like to proceed?";

        // ShowChoices<T> can't return Nullable<T> when T is a value type, so use string keys
        // (which can come back as null on cancel) and map to the enum locally.
        var options = new Dictionary<string, string>
        {
            ["skip"] = "Skip Existing",
            ["overwrite"] = "Overwrite All",
        };

        string? key = _dialogService.ShowChoices(message, options, title: "Import Conflicts", canCancel: true);
        return key switch
        {
            "skip" => ConflictResolution.Skip,
            "overwrite" => ConflictResolution.Overwrite,
            _ => null,
        };
    }

    private static string FormatConflictList(IReadOnlyList<string> names, int max = 5)
    {
        string list = string.Join(", ", names.Take(max));
        if (names.Count > max)
        {
            list += $" (and {names.Count - max} more)";
        }
        return list;
    }

    private bool CanExecuteLoadPreview() => !IsLoading && !string.IsNullOrWhiteSpace(SourcePath);

    private void ClearPreview()
    {
        IsPreviewLoaded = false;
        ErrorMessage = null;
        LoadingStatus = null;
        foreach (ImportTreeNodeViewModel leaf in _allLeafItems)
        {
            leaf.PropertyChanged -= OnItemPropertyChanged;
        }
        _allLeafItems.Clear();
        RootNodes.Clear();
        _sourceProject = null;
    }

    private async Task ExecuteLoadPreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(SourcePath)) { return; }

        IsLoading = true;
        ClearPreview();

        try
        {
            string pathOrUrl = SourcePath.Trim();

            if (SourceType == SourceType.LocalFile && IsSameFileAsCurrentProject(pathOrUrl))
            {
                ErrorMessage = "The selected file is the currently open project. Please choose a different .gumx file to import from.";
                return;
            }

            IProgress<(int loaded, int total)>? progress = null;
            if (SourceType == SourceType.Url)
            {
                LoadingStatus = "Loading...";
                progress = new Progress<(int loaded, int total)>(p =>
                    LoadingStatus = $"Loading file {p.loaded} of {p.total}...");
            }
            else
            {
                LoadingStatus = "Loading...";
            }

            var project = await _sourceService.LoadProjectAsync(pathOrUrl, progress);

            if (project == null)
            {
                ErrorMessage = "Could not load the .gumx file. Check the path or URL and try again.";
                return;
            }

            _sourceProject = project;
            _sourceBase = _sourceService.GetSourceBase(pathOrUrl);

            if (string.IsNullOrWhiteSpace(DestinationSubfolder))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(pathOrUrl.TrimEnd('/'));
                DestinationSubfolder = fileName;
            }

            PopulateItems(project);
            IsPreviewLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load source: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            LoadingStatus = null;
        }
    }

    private bool IsSameFileAsCurrentProject(string path)
    {
        var currentFile = _projectState.GumProjectSave?.FullFileName;
        if (string.IsNullOrEmpty(currentFile)) return false;

        try
        {
            var fullSource = System.IO.Path.GetFullPath(path);
            var fullCurrent = System.IO.Path.GetFullPath(currentFile);
            return string.Equals(fullSource, fullCurrent, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void PopulateItems(GumProjectSave project)
    {
        foreach (ImportTreeNodeViewModel leaf in _allLeafItems)
            leaf.PropertyChanged -= OnItemPropertyChanged;
        _allLeafItems.Clear();
        _autoAddedComponentNames.Clear();
        _userExplicitBehaviorNames.Clear();
        _userExplicitStandardNames.Clear();
        RootNodes.Clear();
        _behaviorsGroupNode = null;
        _standardsGroupNode = null;

        // Components group
        var componentsGroup = new ImportTreeNodeViewModel("Components", "Components");
        var componentLeaves = new List<ImportTreeNodeViewModel>();
        foreach (var component in project.Components.OrderBy(c => c.Name))
        {
            int lastSlash = component.Name.LastIndexOf('/');
            string displayName = lastSlash < 0 ? component.Name : component.Name[(lastSlash + 1)..];
            var leaf = new ImportTreeNodeViewModel(displayName, component.Name, ElementItemType.Component);
            _allLeafItems.Add(leaf);
            componentLeaves.Add(leaf);
        }
        BuildTree(componentLeaves, componentsGroup.Children);
        RootNodes.Add(componentsGroup);

        // Screens group
        var screensGroup = new ImportTreeNodeViewModel("Screens", "Screens");
        var screenLeaves = new List<ImportTreeNodeViewModel>();
        foreach (var screen in project.Screens.OrderBy(s => s.Name))
        {
            int lastSlash = screen.Name.LastIndexOf('/');
            string displayName = lastSlash < 0 ? screen.Name : screen.Name[(lastSlash + 1)..];
            var leaf = new ImportTreeNodeViewModel(displayName, screen.Name, ElementItemType.Screen);
            _allLeafItems.Add(leaf);
            screenLeaves.Add(leaf);
        }
        BuildTree(screenLeaves, screensGroup.Children);
        RootNodes.Add(screensGroup);

        // Behaviors group
        _behaviorsGroupNode = new ImportTreeNodeViewModel("Behaviors", "Behaviors");
        foreach (var behavior in project.Behaviors.OrderBy(b => b.Name))
        {
            var leaf = new ImportTreeNodeViewModel(behavior.Name, behavior.Name, ElementItemType.Behavior);
            _allLeafItems.Add(leaf);
            _behaviorsGroupNode.Children.Add(leaf);
        }
        RootNodes.Add(_behaviorsGroupNode);

        // Standards group
        _standardsGroupNode = new ImportTreeNodeViewModel("Standards", "Standards");
        foreach (var standard in project.StandardElements.OrderBy(s => s.Name))
        {
            var leaf = new ImportTreeNodeViewModel(standard.Name, standard.Name, ElementItemType.Standard);
            _allLeafItems.Add(leaf);
            _standardsGroupNode.Children.Add(leaf);
        }
        RootNodes.Add(_standardsGroupNode);

        foreach (ImportTreeNodeViewModel leaf in _allLeafItems)
            leaf.PropertyChanged += OnItemPropertyChanged;

        RecomputeTransitiveDependencies();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImportTreeNodeViewModel.InclusionState) && !_recomputeQueued)
        {
            if (sender is ImportTreeNodeViewModel vm)
            {
                // User directly changed a component — remove it from auto-added tracking so it isn't reset
                if (vm.ElementType == ElementItemType.Component)
                {
                    _autoAddedComponentNames.Remove(vm.FullName);
                }
                // Behaviors / Standards: track explicit user picks so the recompute pass
                // doesn't wipe them out (#2642).
                else if (vm.ElementType == ElementItemType.Behavior)
                {
                    if (vm.InclusionState == InclusionState.Explicit) { _userExplicitBehaviorNames.Add(vm.FullName); }
                    else { _userExplicitBehaviorNames.Remove(vm.FullName); }
                }
                else if (vm.ElementType == ElementItemType.Standard)
                {
                    if (vm.InclusionState == InclusionState.Explicit) { _userExplicitStandardNames.Add(vm.FullName); }
                    else { _userExplicitStandardNames.Remove(vm.FullName); }
                }
            }
            _recomputeQueued = true;
            _dispatcher.Post(() =>
            {
                _recomputeQueued = false;
                RecomputeTransitiveDependencies();
            });
        }
    }

    /// <summary>
    /// Test seam: directly load a source project without the file/URL fetch step.
    /// Production callers go through <see cref="LoadPreviewCommand"/>.
    /// </summary>
    internal void InitializeFromProjectForTesting(GumProjectSave project)
    {
        _sourceProject = project;
        PopulateItems(project);
    }

    internal void RecomputeTransitiveDependencies()
    {
        if (_sourceProject == null) { return; }

        // Reset components that were auto-added in the previous pass
        foreach (var name in _autoAddedComponentNames)
        {
            var item = _allLeafItems.FirstOrDefault(i => i.ElementType == ElementItemType.Component && i.FullName == name);
            if (item != null)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.InclusionState = InclusionState.NotIncluded;
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }
        _autoAddedComponentNames.Clear();

        // Find user-explicitly checked components and screens
        var directComponents = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Component && i.InclusionState == InclusionState.Explicit)
            .Select(i => _sourceProject.Components.FirstOrDefault(c => c.Name == i.FullName))
            .Where(c => c != null)
            .Cast<ElementSave>()
            .ToList();

        var directScreens = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Screen && i.InclusionState == InclusionState.Explicit)
            .Select(i => _sourceProject.Screens.FirstOrDefault(s => s.Name == i.FullName))
            .Where(s => s != null)
            .Cast<ElementSave>()
            .ToList();

        var directSelected = directComponents.Concat(directScreens).ToList();
        var destination = _projectState.GumProjectSave;
        var deps = _dependencyResolver.ComputeTransitive(directSelected, _sourceProject, destination);

        // Auto-check transitive components (deps of selected elements not directly selected)
        foreach (var comp in deps.TransitiveComponents)
        {
            var item = _allLeafItems.FirstOrDefault(i => i.ElementType == ElementItemType.Component && i.FullName == comp.Name);
            if (item != null && item.InclusionState == InclusionState.NotIncluded)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.InclusionState = InclusionState.Explicit;
                item.PropertyChanged += OnItemPropertyChanged;
                _autoAddedComponentNames.Add(item.FullName);
            }
        }

        // Auto-check required behaviors, plus any the user explicitly picked; uncheck others
        var requiredBehaviorNames = new HashSet<string>(deps.Behaviors.Select(b => b.Name));
        foreach (ImportTreeNodeViewModel item in _allLeafItems.Where(i => i.ElementType == ElementItemType.Behavior))
        {
            bool shouldBeExplicit = requiredBehaviorNames.Contains(item.FullName)
                || _userExplicitBehaviorNames.Contains(item.FullName);
            item.PropertyChanged -= OnItemPropertyChanged;
            item.InclusionState = shouldBeExplicit ? InclusionState.Explicit : InclusionState.NotIncluded;
            item.PropertyChanged += OnItemPropertyChanged;
        }

        // Auto-check differing standards, plus any the user explicitly picked; uncheck others
        var differingStandardNames = new HashSet<string>(deps.DifferingStandards.Select(s => s.Name));
        var diffsByName = deps.DifferingStandardDiffs
            .ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);
        foreach (ImportTreeNodeViewModel item in _allLeafItems.Where(i => i.ElementType == ElementItemType.Standard))
        {
            bool shouldBeExplicit = differingStandardNames.Contains(item.FullName)
                || _userExplicitStandardNames.Contains(item.FullName);
            item.PropertyChanged -= OnItemPropertyChanged;
            item.InclusionState = shouldBeExplicit ? InclusionState.Explicit : InclusionState.NotIncluded;
            item.StandardDiffRows = diffsByName.TryGetValue(item.FullName, out StandardComparisonResult? comparison)
                ? BuildDiffRows(comparison)
                : null;
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    /// <summary>
    /// Flattens a <see cref="StandardComparisonResult"/> into display rows: one per added/removed
    /// category, then one per variable diff with <c>ChangedFields</c> joined into the summary
    /// (e.g. <c>"Rotation · SetsValue: True → False"</c>).
    /// </summary>
    private static IReadOnlyList<StandardDiffRowViewModel>? BuildDiffRows(StandardComparisonResult comparison)
    {
        var rows = new List<StandardDiffRowViewModel>();

        foreach (string categoryName in comparison.CategoryNamesOnlyInSource)
        {
            rows.Add(new StandardDiffRowViewModel("Category added", categoryName));
        }
        foreach (string categoryName in comparison.CategoryNamesOnlyInDestination)
        {
            rows.Add(new StandardDiffRowViewModel("Category removed", categoryName));
        }

        foreach (StandardVariableDiff diff in comparison.VariableDifferences)
        {
            string kind = diff.Kind switch
            {
                StandardVariableDiffKind.AddedInProject => "Added",
                StandardVariableDiffKind.RemovedFromProject => "Removed",
                _ => "Changed",
            };
            rows.Add(new StandardDiffRowViewModel(kind, FormatVariableSummary(diff)));
        }

        return rows.Count == 0 ? null : rows;
    }

    private static string FormatVariableSummary(StandardVariableDiff diff)
    {
        var sb = new StringBuilder(diff.VariableName);

        if (diff.Kind == StandardVariableDiffKind.Changed
            && diff.ChangedFields.Count == 0
            && (diff.ProjectValue.Length > 0 || diff.DefaultValue.Length > 0))
        {
            sb.Append(": ").Append(diff.DefaultValue).Append(" → ").Append(diff.ProjectValue);
        }

        foreach (VariableFieldDiff field in diff.ChangedFields)
        {
            sb.Append(" · ")
              .Append(field.FieldName).Append(": ")
              .Append(field.DefaultValue).Append(" → ").Append(field.ProjectValue);
        }

        return sb.ToString();
    }

    private ImportSelections BuildSelections()
    {
        if (_sourceProject == null)
        {
            return new ImportSelections();
        }

        var componentsByName = _sourceProject.Components.ToDictionary(c => c.Name);
        var screensByName = _sourceProject.Screens.ToDictionary(s => s.Name);
        var behaviorsByName = _sourceProject.Behaviors.ToDictionary(b => b.Name);
        var standardsByName = _sourceProject.StandardElements.ToDictionary(s => s.Name);

        // User-directly-checked components (not auto-added by the dependency resolver)
        var directComponents = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Component
                        && i.InclusionState == InclusionState.Explicit
                        && !_autoAddedComponentNames.Contains(i.FullName))
            .Select(i => componentsByName.TryGetValue(i.FullName, out var c) ? c : null)
            .Where(c => c != null)
            .Cast<ComponentSave>()
            .ToList();

        var directScreens = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Screen && i.InclusionState == InclusionState.Explicit)
            .Select(i => screensByName.TryGetValue(i.FullName, out var s) ? s : null)
            .Where(s => s != null)
            .Cast<ScreenSave>()
            .ToList();

        // Re-run the resolver on user-direct elements to get topologically-sorted transitive components
        var directElements = directComponents.Cast<ElementSave>()
            .Concat(directScreens.Cast<ElementSave>())
            .ToList();
        var destination = _projectState.GumProjectSave;
        var deps = _dependencyResolver.ComputeTransitive(directElements, _sourceProject, destination);

        var behaviors = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Behavior && i.InclusionState == InclusionState.Explicit)
            .Select(i => behaviorsByName.TryGetValue(i.FullName, out var b) ? b : null)
            .Where(b => b != null)
            .Cast<BehaviorSave>()
            .ToList();

        var standards = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Standard && i.InclusionState == InclusionState.Explicit)
            .Select(i => standardsByName.TryGetValue(i.FullName, out var s) ? s : null)
            .Where(s => s != null)
            .Cast<StandardElementSave>()
            .ToList();

        return new ImportSelections
        {
            DirectComponents = directComponents,
            DirectScreens = directScreens,
            TransitiveComponents = deps.TransitiveComponents,
            Behaviors = behaviors,
            Standards = standards
        };
    }

    private static void BuildTree(List<ImportTreeNodeViewModel> leaves, ObservableCollection<ImportTreeNodeViewModel> rootNodes)
    {
        var foldersByPath = new Dictionary<string, ImportTreeNodeViewModel>(StringComparer.OrdinalIgnoreCase);
        foreach (ImportTreeNodeViewModel leaf in leaves)
        {
            int lastSlash = leaf.FullName.LastIndexOf('/');
            if (lastSlash < 0)
            {
                rootNodes.Add(leaf);
            }
            else
            {
                ImportTreeNodeViewModel folder = EnsureFolder(rootNodes, foldersByPath, leaf.FullName[..lastSlash]);
                folder.Children.Add(leaf);
            }
        }
    }

    private static ImportTreeNodeViewModel EnsureFolder(
        ObservableCollection<ImportTreeNodeViewModel> rootNodes,
        Dictionary<string, ImportTreeNodeViewModel> foldersByPath,
        string folderPath)
    {
        if (foldersByPath.TryGetValue(folderPath, out ImportTreeNodeViewModel? existing))
        {
            return existing;
        }

        int lastSlash = folderPath.LastIndexOf('/');
        string displayName = lastSlash < 0 ? folderPath : folderPath[(lastSlash + 1)..];
        ImportTreeNodeViewModel folder = new ImportTreeNodeViewModel(displayName, folderPath);
        foldersByPath[folderPath] = folder;

        if (lastSlash < 0)
        {
            rootNodes.Add(folder);
        }
        else
        {
            ImportTreeNodeViewModel parent = EnsureFolder(rootNodes, foldersByPath, folderPath[..lastSlash]);
            parent.Children.Add(folder);
        }

        return folder;
    }
}
