using CommunityToolkit.Mvvm.Input;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Plugins.ImportPlugin.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
    private ImportTreeNodeViewModel? _standardsGroupNode;

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
        set => Set(value);
    }

    public bool IsLocalFile
    {
        get => SourceType == SourceType.LocalFile;
        set { if (value) { SourceType = SourceType.LocalFile; } }
    }

    public bool IsUrl
    {
        get => SourceType == SourceType.Url;
        set { if (value) { SourceType = SourceType.Url; } }
    }

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

    public string? ErrorMessage
    {
        get => Get<string>();
        set => Set(value);
    }

    public string? WarningMessage
    {
        get => Get<string>();
        set => Set(value);
    }

    public ObservableCollection<ImportTreeNodeViewModel> RootNodes { get; } = new ObservableCollection<ImportTreeNodeViewModel>();

    public AsyncRelayCommand LoadPreviewCommand { get; }
    public RelayCommand SelectAllComponentsCommand { get; }

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
        SelectAllComponentsCommand = new RelayCommand(ExecuteSelectAllComponents);
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

        IsLoading = true;
        ErrorMessage = null;
        WarningMessage = null;
        try
        {
            var selections = BuildSelections();
            var result = await _importService.ImportAsync(
                selections, _sourceProject, _sourceBase, DestinationSubfolder);

            _isImportComplete = true;

            if (result.MissingAssets.Count > 0)
            {
                AffirmativeText = "Close";
                string assetList = string.Join(", ", result.MissingAssets.Take(5));
                if (result.MissingAssets.Count > 5)
                {
                    assetList += $" (and {result.MissingAssets.Count - 5} more)";
                }
                WarningMessage =
                    $"{result.MissingAssets.Count} asset file(s) could not be found in the source " +
                    $"and were not copied: {assetList}";
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

    private void ExecuteSelectAllComponents()
    {
        foreach (ImportTreeNodeViewModel item in _allLeafItems)
        {
            if (item.ElementType == ElementItemType.Component || item.ElementType == ElementItemType.Screen)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.InclusionState = InclusionState.Explicit;
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }
        RecomputeTransitiveDependencies();
    }

    private bool CanExecuteLoadPreview() => !IsLoading && !string.IsNullOrWhiteSpace(SourcePath);

    private async Task ExecuteLoadPreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(SourcePath)) { return; }

        IsLoading = true;
        IsPreviewLoaded = false;
        ErrorMessage = null;
        foreach (ImportTreeNodeViewModel leaf in _allLeafItems)
        {
            leaf.PropertyChanged -= OnItemPropertyChanged;
        }
        _allLeafItems.Clear();
        RootNodes.Clear();
        _sourceProject = null;

        try
        {
            string pathOrUrl = SourcePath.Trim();
            var project = await _sourceService.LoadProjectAsync(pathOrUrl);

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
        }
    }

    private void PopulateItems(GumProjectSave project)
    {
        foreach (ImportTreeNodeViewModel leaf in _allLeafItems)
            leaf.PropertyChanged -= OnItemPropertyChanged;
        _allLeafItems.Clear();
        RootNodes.Clear();
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

        // Standards group — behaviors are inserted before this node in RecomputeTransitiveDependencies
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
        if (e.PropertyName == nameof(ImportTreeNodeViewModel.InclusionState))
        {
            RecomputeTransitiveDependencies();
        }
    }

    private void RecomputeTransitiveDependencies()
    {
        if (_sourceProject == null) { return; }

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

        var autoComponentNames = new HashSet<string>(deps.TransitiveComponents.Select(c => c.Name));

        foreach (ImportTreeNodeViewModel item in _allLeafItems)
        {
            if (item.InclusionState == InclusionState.AutoIncluded)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.InclusionState = InclusionState.NotIncluded;
                item.AutoIncludedReason = string.Empty;
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        foreach (ImportTreeNodeViewModel item in _allLeafItems.Where(i => i.ElementType == ElementItemType.Component))
        {
            if (autoComponentNames.Contains(item.FullName) && item.InclusionState == InclusionState.NotIncluded)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.InclusionState = InclusionState.AutoIncluded;
                item.AutoIncludedReason = BuildAutoIncludeReason(item.FullName, directSelected, _sourceProject);
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        var existingBehaviorItems = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Behavior)
            .ToList();
        foreach (ImportTreeNodeViewModel b in existingBehaviorItems)
        {
            b.PropertyChanged -= OnItemPropertyChanged;
            _allLeafItems.Remove(b);
            RootNodes.Remove(b);
        }

        int insertAt = _standardsGroupNode != null ? RootNodes.IndexOf(_standardsGroupNode) : RootNodes.Count;

        foreach (var behavior in deps.Behaviors.OrderBy(b => b.Name))
        {
            ImportTreeNodeViewModel item = new ImportTreeNodeViewModel(behavior.Name, behavior.Name, ElementItemType.Behavior);
            item.InclusionState = InclusionState.AutoIncluded;
            item.AutoIncludedReason = "required by a selected component";
            item.PropertyChanged += OnItemPropertyChanged;
            _allLeafItems.Add(item);
            RootNodes.Insert(insertAt++, item);
        }

        var differingStandardNames = new HashSet<string>(deps.DifferingStandards.Select(s => s.Name));
        foreach (ImportTreeNodeViewModel item in _allLeafItems.Where(i => i.ElementType == ElementItemType.Standard))
        {
            if (item.InclusionState != InclusionState.Explicit)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.InclusionState = differingStandardNames.Contains(item.FullName)
                    ? InclusionState.Explicit
                    : InclusionState.NotIncluded;
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }
    }

    private static string BuildAutoIncludeReason(
        string componentName,
        IList<ElementSave> directSelected,
        GumProjectSave source)
    {
        foreach (var element in directSelected)
        {
            if (element.Instances.Any(i => i.BaseType == componentName))
            {
                return $"used by {element.Name}";
            }
        }
        return "used by a selected component";
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

        var directComponents = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Component && i.InclusionState == InclusionState.Explicit)
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

        var transitiveComponents = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Component && i.InclusionState == InclusionState.AutoIncluded)
            .Select(i => componentsByName.TryGetValue(i.FullName, out var c) ? c : null)
            .Where(c => c != null)
            .Cast<ComponentSave>()
            .ToList();

        var behaviors = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Behavior
                        && (i.InclusionState == InclusionState.AutoIncluded || i.InclusionState == InclusionState.Explicit))
            .Select(i => behaviorsByName.TryGetValue(i.FullName, out var b) ? b : null)
            .Where(b => b != null)
            .Cast<BehaviorSave>()
            .ToList();

        var standards = _allLeafItems
            .Where(i => i.ElementType == ElementItemType.Standard
                        && (i.InclusionState == InclusionState.Explicit || i.InclusionState == InclusionState.AutoIncluded))
            .Select(i => standardsByName.TryGetValue(i.FullName, out var s) ? s : null)
            .Where(s => s != null)
            .Cast<StandardElementSave>()
            .ToList();

        return new ImportSelections
        {
            DirectComponents = directComponents,
            DirectScreens = directScreens,
            TransitiveComponents = transitiveComponents,
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
