using CommunityToolkit.Mvvm.Input;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Mvvm;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.ViewModel;

public abstract partial class ImportBaseDialogViewModel : DialogViewModel
{
    private readonly IDialogService _dialogService;

    public abstract string Title { get; }
    public abstract string BrowseFileFilter { get; }

    public string? SearchText 
    { 
        get => Get<string?>(); 
        set
        {
            if (Set(value))
            {
                FilteredFiles.Refresh();
            }
        }
    }

    public ObservableCollection<string> UnfilteredFiles { get; } = [];
    public ICollectionView FilteredFiles { get; }
    public ObservableCollection<string> SelectedFiles { get; } = [];

    public override bool CanExecuteAffirmative() => SelectedFiles.Any();

    protected ImportBaseDialogViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        AffirmativeText = "Import";
        NegativeText = "Cancel";
        FilteredFiles = CollectionViewSource.GetDefaultView(UnfilteredFiles);
        FilteredFiles.Filter = Filter;
        SelectedFiles.CollectionChanged += (_, _) => AffirmativeCommand.NotifyCanExecuteChanged();
    }

    private bool Filter(object item) =>
        item is string val &&
        (string.IsNullOrWhiteSpace(SearchText) ||
        val.ToLowerInvariant().Contains(SearchText!.ToLowerInvariant()));

    [RelayCommand]
    private void Browse()
    {
        OpenFileDialogOptions options = new()
        {
            // false for now. Components support it, but screens don't
            //Multiselect = true,
            Filter = BrowseFileFilter
        };

        if (_dialogService.OpenFile(options) is { Count: > 0 } files)
        {
            SelectedFiles.Clear();
            SelectedFiles.AddRange(files);
            AffirmativeCommand.Execute(null);
        }
    }
}

public class ImportBehaviorDialog : ImportBaseDialogViewModel
{
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;

    public override string Title => "Import Behavior";
    public override string BrowseFileFilter => "Behavior Files (*.behaviors)|*.behaviors";

    public ImportBehaviorDialog(
        IFileCommands fileCommands, 
        IGuiCommands guiCommands, 
        ISelectedState selectedState,
        IDialogService dialogService) : base(dialogService)
    {
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _selectedState = selectedState;

        List<FilePath> behaviorFilesNotInProject = FileManager.GetAllFilesInDirectory(
            GumState.Self.ProjectState.BehaviorFilePath.FullPath, "behx")
            .Select(item => new FilePath(item))
            .ToList();

        FilePath[] behaviorFilesInProject = GumState.Self.ProjectState.GumProjectSave
            .Behaviors
            .Select(item => new FilePath(GumState.Self.ProjectState.BehaviorFilePath + item.Name + ".behx"))
            .ToArray();

        behaviorFilesNotInProject = behaviorFilesNotInProject
            .Except(behaviorFilesInProject)
            .ToList();

        UnfilteredFiles.AddRange(behaviorFilesNotInProject.Select(item => item.FullPath));
    }

    protected override void OnAffirmative()
    {
        BehaviorSave lastImportedBehavior = null;

        string desiredDirectory = FileManager.GetDirectory(
            ProjectManager.Self.GumProjectSave.FullFileName) + "Behaviors/";

        foreach (string file in SelectedFiles)
        {
            lastImportedBehavior = ImportLogic.ImportBehavior(file, desiredDirectory, saveProject: false);
        }

        if (lastImportedBehavior != null)
        {
            _guiCommands.RefreshElementTreeView();
            _selectedState.SelectedBehavior = lastImportedBehavior;
            _fileCommands.TryAutoSaveProject();
        }
        base.OnAffirmative();
    }
}

public class ImportComponentDialog : ImportBaseDialogViewModel
{
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;

    public override string Title => "Import Component";
    public override string BrowseFileFilter => "Gum Component (*.gucx)|*.gucx";

    public ImportComponentDialog(
        IFileCommands fileCommands,
        IGuiCommands guiCommands,
        ISelectedState selectedState, 
        IDialogService dialogService) : base(dialogService)
    {
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _selectedState = selectedState;

        List<FilePath> componentFilesNotInProject = FileManager.GetAllFilesInDirectory(
            GumState.Self.ProjectState.ComponentFilePath.FullPath, "gucx")
            .Select(item => new FilePath(item))
            .ToList();

        FilePath[] componentFilesInProject = GumState.Self.ProjectState.GumProjectSave
            .Components
            .Select(item => new FilePath(GumState.Self.ProjectState.ComponentFilePath + item.Name + ".gucx"))
            .ToArray();

        componentFilesNotInProject = componentFilesNotInProject
            .Except(componentFilesInProject)
            .ToList();

        UnfilteredFiles.AddRange(componentFilesNotInProject.Select(item => item.FullPath));
    }

    protected override void OnAffirmative()
    {
        ComponentSave lastImportedComponent = null;

        string desiredDirectory = FileManager.GetDirectory(
            ProjectManager.Self.GumProjectSave.FullFileName) + "Components/";
        foreach (var file in SelectedFiles)
        {
            lastImportedComponent = ImportLogic.ImportComponent(file, desiredDirectory,
                // dont' save - we'll do it below:
                saveProject: false);
        }

        if (lastImportedComponent != null)
        {
            _guiCommands.RefreshElementTreeView();
            _selectedState.SelectedComponent = lastImportedComponent;
            _fileCommands.TryAutoSaveProject();
        }

        base.OnAffirmative();
    }
}

public class ImportScreenDialog : ImportBaseDialogViewModel
{
    public override string Title => "Import Screen";
    public override string BrowseFileFilter => "Gum Screen (*.gusx)|*.gusx";
    public ImportScreenDialog(IDialogService dialogService) : base(dialogService)
    {
        List<FilePath> screenFilesNotInProject = FileManager.GetAllFilesInDirectory(
            GumState.Self.ProjectState.ScreenFilePath.FullPath, "gusx")
            .Select(item => new FilePath(item))
            .ToList();

        FilePath[] screenFilesInProject = GumState.Self.ProjectState.GumProjectSave
            .Screens
            .Select(item => new FilePath(GumState.Self.ProjectState.ComponentFilePath + item.Name + ".gusx"))
            .ToArray();

        screenFilesNotInProject = screenFilesNotInProject
            .Except(screenFilesInProject)
            .ToList();

        UnfilteredFiles.AddRange(screenFilesNotInProject.Select(item => item.FullPath));
    }

    protected override void OnAffirmative()
    {

        foreach (var file in SelectedFiles)
        {
            ImportLogic.ImportScreen(file);
        }

        base.OnAffirmative();
    }
}