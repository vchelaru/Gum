using CommunityToolkit.Mvvm.Input;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Mvvm;
using Gum.Plugins.ImportPlugin.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ImportFromGumxPlugin.ViewModels;

public enum SourceType
{
    LocalFile,
    Url
}

public class ImportFromGumxViewModel : DialogViewModel
{
    private readonly GumxSourceService _sourceService;
    private readonly GumxDependencyResolver _dependencyResolver;
    private readonly GumxImportService _importService;
    private readonly IProjectState _projectState;

    private GumProjectSave? _sourceProject;
    private string _sourceBase = string.Empty;
    private bool _isImportComplete;

    private readonly List<ImportTreeNodeViewModel> _allLeafItems = new List<ImportTreeNodeViewModel>();
    private readonly HashSet<string> _autoAddedComponentNames = new HashSet<string>();
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
    public Visibility BrowseButtonVisibility =>
        IsLocalFile ? Visibility.Visible : Visibility.Collapsed;

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
    public Visibility PreviewVisibility =>
        IsPreviewLoaded ? Visibility.Visible : Visibility.Collapsed;

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
    public Visibility LoadingStatusVisibility =>
        string.IsNullOrEmpty(LoadingStatus) ? Visibility.Collapsed : Visibility.Visible;

    public string? ErrorMessage
    {
        get => Get<string>();
        set => Set(value);
    }

    [DependsOn(nameof(ErrorMessage))]
    public Visibility ErrorMessageVisibility =>
        string.IsNullOrEmpty(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

    public string? WarningMessage
    {
        get => Get<string>();
        set => Set(value);
    }

    [DependsOn(nameof(WarningMessage))]
    public Visibility WarningMessageVisibility =>
        string.IsNullOrEmpty(WarningMessage) ? Visibility.Collapsed : Visibility.Visible;

    public ObservableCollection<ImportTreeNodeViewModel> RootNodes { get; } = new ObservableCollection<ImportTreeNodeViewModel>();

    public AsyncRelayCommand LoadPreviewCommand { get; }

    public ImportFromGumxViewModel(
        GumxSourceService sourceService,
        GumxDependencyResolver dependencyResolver,
        GumxImportService importService,
        IProjectState projectState)
    {
        _sourceService = sourceService;
        _dependencyResolver = dependencyResolver;
        _importService = importService;
        _projectState = projectState;

        AffirmativeText = "Import";
        SourceType = SourceType.LocalFile;

        LoadPreviewCommand = new AsyncRelayCommand(ExecuteLoadPreviewAsync, CanExecuteLoadPreview);
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

            _isImportComplete = true;

            if (result.SkippedElements.Count > 0)
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
            // User directly changed a component — remove it from auto-added tracking so it isn't reset
            if (sender is ImportTreeNodeViewModel vm && vm.ElementType == ElementItemType.Component)
            {
                _autoAddedComponentNames.Remove(vm.FullName);
            }
            _recomputeQueued = true;
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _recomputeQueued = false;
                RecomputeTransitiveDependencies();
            });
        }
    }

    private void RecomputeTransitiveDependencies()
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

        // Auto-check required behaviors, uncheck others
        var requiredBehaviorNames = new HashSet<string>(deps.Behaviors.Select(b => b.Name));
        foreach (ImportTreeNodeViewModel item in _allLeafItems.Where(i => i.ElementType == ElementItemType.Behavior))
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            item.InclusionState = requiredBehaviorNames.Contains(item.FullName)
                ? InclusionState.Explicit
                : InclusionState.NotIncluded;
            item.PropertyChanged += OnItemPropertyChanged;
        }

        // Auto-check differing standards, uncheck others
        var differingStandardNames = new HashSet<string>(deps.DifferingStandards.Select(s => s.Name));
        foreach (ImportTreeNodeViewModel item in _allLeafItems.Where(i => i.ElementType == ElementItemType.Standard))
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            item.InclusionState = differingStandardNames.Contains(item.FullName)
                ? InclusionState.Explicit
                : InclusionState.NotIncluded;
            item.PropertyChanged += OnItemPropertyChanged;
        }
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
