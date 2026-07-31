using Gum.Commands;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gum.DataTypes.Behaviors;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.ViewModel;

public class ImportBehaviorDialog : ImportBaseDialogViewModel
{
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;
    private readonly IImportLogic _importLogic;
    private readonly IProjectState _projectState;
    private readonly IProjectManager _projectManager;

    public override string Title => "Import Behavior";
    public override string BrowseFileFilter =>
        $"Behavior Files (*.{BehaviorReference.Extension};*.{BehaviorReference.JsonExtension})" +
        $"|*.{BehaviorReference.Extension};*.{BehaviorReference.JsonExtension}";

    public ImportBehaviorDialog(
        IFileCommands fileCommands,
        IGuiCommands guiCommands,
        ISelectedState selectedState,
        IDialogService dialogService,
        IImportLogic importLogic,
        IProjectState projectState,
        IProjectManager projectManager
        ) : base(dialogService)
    {
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _selectedState = selectedState;
        _importLogic = importLogic;
        _projectState = projectState;
        _projectManager = projectManager;

        // A JSON-converted behavior (issue #4182) must be offered for import the same way its
        // XML counterpart is.
        List<FilePath> behaviorFilesNotInProject = FileManager.GetAllFilesInDirectory(
                _projectState.BehaviorFilePath.FullPath, BehaviorReference.Extension)
            .Concat(FileManager.GetAllFilesInDirectory(
                _projectState.BehaviorFilePath.FullPath, BehaviorReference.JsonExtension))
            .Select(item => new FilePath(item))
            .ToList();

        FilePath[] behaviorFilesInProject = _projectState.GumProjectSave
            .Behaviors
            .SelectMany(item => new[]
            {
                new FilePath(_projectState.BehaviorFilePath + item.Name + "." + BehaviorReference.Extension),
                new FilePath(_projectState.BehaviorFilePath + item.Name + "." + BehaviorReference.JsonExtension),
            })
            .ToArray();

        behaviorFilesNotInProject = behaviorFilesNotInProject
            .Except(behaviorFilesInProject)
            .ToList();

        UnfilteredFiles.AddRange(behaviorFilesNotInProject.Select(item => item.FullPath));
    }

    public override void OnAffirmative()
    {
        BehaviorSave lastImportedBehavior = null;

        string desiredDirectory = FileManager.GetDirectory(
            _projectManager.GumProjectSave.FullFileName) + "Behaviors/";

        foreach (string file in SelectedFiles)
        {
            lastImportedBehavior = _importLogic.ImportBehavior(file, desiredDirectory, saveProject: false);
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