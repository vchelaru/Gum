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

    /// <summary>
    /// The loaded source project; null until LoadPreviewCommand succeeds.
    /// </summary>
    private GumProjectSave? _sourceProject;

    /// <summary>
    /// The base path or URL for fetching element files relative to the source .gumx.
    /// </summary>
    private string _sourceBase = string.Empty;

    /// <summary>
    /// True after a successful import. When true, clicking the affirmative button closes
    /// the dialog (allows the user to read any post-import warnings first).
    /// </summary>
    private bool _isImportComplete;

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
        set { if (value) SourceType = SourceType.LocalFile; }
    }

    public bool IsUrl
    {
        get => SourceType == SourceType.Url;
        set { if (value) SourceType = SourceType.Url; }
    }

    public string DestinationSubfolder
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    /// <summary>
    /// True once LoadPreviewCommand has successfully loaded the source project.
    /// Controls visibility of Phase 2 (selection panel).
    /// </summary>
    public bool IsPreviewLoaded
    {
        get => Get<bool>();
        set
        {
            Set(value);
            AffirmativeCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// True while a background operation (load or import) is running.
    /// </summary>
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

    /// <summary>
    /// Error message to display in the UI, if any.
    /// </summary>
    public string? ErrorMessage
    {
        get => Get<string>();
        set => Set(value);
    }

    /// <summary>
    /// Warning shown after a successful import when some referenced asset files
    /// could not be found in the source and were not copied.
    /// </summary>
    public string? WarningMessage
    {
        get => Get<string>();
        set => Set(value);
    }

    /// <summary>
    /// Flat list of all importable items (components, screens, behaviors, standards).
    /// </summary>
    public ObservableCollection<ImportPreviewItemViewModel> Items { get; } = new();

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

    /// <summary>
    /// Called when the user clicks Import (or Close after a completed import).
    /// Uses async void to remain compatible with the base class's synchronous RelayCommand
    /// while keeping the dialog open during the import.
    /// </summary>
    public override async void OnAffirmative()
    {
        // Second click after a successful import — just close the dialog.
        if (_isImportComplete)
        {
            base.OnAffirmative();
            return;
        }

        if (_sourceProject == null) return;

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
                // Stay open so the user can read the warning before dismissing.
                AffirmativeText = "Close";
                string assetList = string.Join(", ", result.MissingAssets.Take(5));
                if (result.MissingAssets.Count > 5)
                    assetList += $" (and {result.MissingAssets.Count - 5} more)";
                WarningMessage =
                    $"{result.MissingAssets.Count} asset file(s) could not be found in the source " +
                    $"and were not copied: {assetList}";
            }
            else
            {
                base.OnAffirmative(); // Close dialog immediately — no warnings to show
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
        foreach (var item in Items)
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
        if (string.IsNullOrWhiteSpace(SourcePath)) return;

        IsLoading = true;
        IsPreviewLoaded = false;
        ErrorMessage = null;
        Items.Clear();
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

            // Default destination subfolder to the source project's filename without extension
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
        Items.Clear();

        // Components first — most commonly imported
        foreach (var component in project.Components.OrderBy(c => c.Name))
        {
            var item = new ImportPreviewItemViewModel
            {
                Name = component.Name,
                ElementType = ElementItemType.Component,
                InclusionState = InclusionState.NotIncluded
            };
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
        }

        // Screens
        foreach (var screen in project.Screens.OrderBy(s => s.Name))
        {
            var item = new ImportPreviewItemViewModel
            {
                Name = screen.Name,
                ElementType = ElementItemType.Screen,
                InclusionState = InclusionState.NotIncluded
            };
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
        }

        // Standards (shown in a separate section at the bottom)
        var destination = _projectState.GumProjectSave;
        foreach (var standard in project.StandardElements.OrderBy(s => s.Name))
        {
            var item = new ImportPreviewItemViewModel
            {
                Name = standard.Name,
                ElementType = ElementItemType.Standard,
                InclusionState = InclusionState.NotIncluded
            };
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
        }

        // Behaviors do not appear directly — they appear as transitive items only.
        // Recompute transitive dependencies after populating.
        RecomputeTransitiveDependencies();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImportPreviewItemViewModel.InclusionState))
        {
            RecomputeTransitiveDependencies();
        }
    }

    private void RecomputeTransitiveDependencies()
    {
        if (_sourceProject == null) return;

        // Gather directly-selected elements (Explicit state only)
        var directComponents = Items
            .Where(i => i.ElementType == ElementItemType.Component && i.InclusionState == InclusionState.Explicit)
            .Select(i => _sourceProject.Components.FirstOrDefault(c => c.Name == i.Name))
            .Where(c => c != null)
            .Cast<ElementSave>()
            .ToList();

        var directScreens = Items
            .Where(i => i.ElementType == ElementItemType.Screen && i.InclusionState == InclusionState.Explicit)
            .Select(i => _sourceProject.Screens.FirstOrDefault(s => s.Name == i.Name))
            .Where(s => s != null)
            .Cast<ElementSave>()
            .ToList();

        var directSelected = directComponents.Concat(directScreens).ToList();

        var destination = _projectState.GumProjectSave;
        var deps = _dependencyResolver.ComputeTransitive(directSelected, _sourceProject, destination);

        // Build a set of auto-included component names
        var autoComponentNames = new HashSet<string>(deps.TransitiveComponents.Select(c => c.Name));
        var behaviorNames = new HashSet<string>(deps.Behaviors.Select(b => b.Name));

        // Reset all AutoIncluded items to NotIncluded first (unless they're Explicit)
        foreach (var item in Items)
        {
            if (item.InclusionState == InclusionState.AutoIncluded)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.InclusionState = InclusionState.NotIncluded;
                item.AutoIncludedReason = string.Empty;
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        // Mark transitive components as AutoIncluded
        foreach (var item in Items.Where(i => i.ElementType == ElementItemType.Component))
        {
            if (autoComponentNames.Contains(item.Name) && item.InclusionState == InclusionState.NotIncluded)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.InclusionState = InclusionState.AutoIncluded;
                item.AutoIncludedReason = BuildAutoIncludeReason(item.Name, directSelected, _sourceProject);
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        // Remove existing behavior items and re-add based on current deps
        var existingBehaviorItems = Items
            .Where(i => i.ElementType == ElementItemType.Behavior)
            .ToList();
        foreach (var b in existingBehaviorItems)
        {
            b.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(b);
        }

        // Find the insertion index: before the first Standard item
        int standardIndex = Items.IndexOf(Items.FirstOrDefault(i => i.ElementType == ElementItemType.Standard)!);
        int insertAt = standardIndex >= 0 ? standardIndex : Items.Count;

        foreach (var behavior in deps.Behaviors.OrderBy(b => b.Name))
        {
            var item = new ImportPreviewItemViewModel
            {
                Name = behavior.Name,
                ElementType = ElementItemType.Behavior,
                InclusionState = InclusionState.AutoIncluded,
                AutoIncludedReason = "required by a selected component"
            };
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Insert(insertAt++, item);
        }

        // Update standards: ☑ if referenced and differing, ☐ otherwise
        var differingStandardNames = new HashSet<string>(deps.DifferingStandards.Select(s => s.Name));
        foreach (var item in Items.Where(i => i.ElementType == ElementItemType.Standard))
        {
            if (item.InclusionState != InclusionState.Explicit)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                item.InclusionState = differingStandardNames.Contains(item.Name)
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
        // Find which directly selected element first references this component
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

        var directComponents = Items
            .Where(i => i.ElementType == ElementItemType.Component && i.InclusionState == InclusionState.Explicit)
            .Select(i => componentsByName.TryGetValue(i.Name, out var c) ? c : null)
            .Where(c => c != null)
            .Cast<ComponentSave>()
            .ToList();

        var directScreens = Items
            .Where(i => i.ElementType == ElementItemType.Screen && i.InclusionState == InclusionState.Explicit)
            .Select(i => screensByName.TryGetValue(i.Name, out var s) ? s : null)
            .Where(s => s != null)
            .Cast<ScreenSave>()
            .ToList();

        var transitiveComponents = Items
            .Where(i => i.ElementType == ElementItemType.Component && i.InclusionState == InclusionState.AutoIncluded)
            .Select(i => componentsByName.TryGetValue(i.Name, out var c) ? c : null)
            .Where(c => c != null)
            .Cast<ComponentSave>()
            .ToList();

        var behaviors = Items
            .Where(i => i.ElementType == ElementItemType.Behavior
                        && (i.InclusionState == InclusionState.AutoIncluded || i.InclusionState == InclusionState.Explicit))
            .Select(i => behaviorsByName.TryGetValue(i.Name, out var b) ? b : null)
            .Where(b => b != null)
            .Cast<BehaviorSave>()
            .ToList();

        var standards = Items
            .Where(i => i.ElementType == ElementItemType.Standard
                        && (i.InclusionState == InclusionState.Explicit || i.InclusionState == InclusionState.AutoIncluded))
            .Select(i => standardsByName.TryGetValue(i.Name, out var s) ? s : null)
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
}
