using Gum.Commands;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services;
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

    public override string Title => "Import Behavior";
    public override string BrowseFileFilter => "Behavior Files (*.behaviors)|*.behaviors";

    public ImportBehaviorDialog(
        IFileCommands fileCommands,
        IGuiCommands guiCommands,
        ISelectedState selectedState,
        IDialogService dialogService,
        IImportLogic importLogic,
        IProjectState projectState

        ) : base(dialogService)
    {
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _selectedState = selectedState;
        _importLogic = importLogic;
        _projectState = projectState;

        List<FilePath> behaviorFilesNotInProject = FileManager.GetAllFilesInDirectory(
            _projectState.BehaviorFilePath.FullPath, "behx")
            .Select(item => new FilePath(item))
            .ToList();

        FilePath[] behaviorFilesInProject = _projectState.GumProjectSave
            .Behaviors
            .Select(item => new FilePath(_projectState.BehaviorFilePath + item.Name + ".behx"))
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
            Locator.GetRequiredService<IProjectManager>().GumProjectSave.FullFileName) + "Behaviors/";

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