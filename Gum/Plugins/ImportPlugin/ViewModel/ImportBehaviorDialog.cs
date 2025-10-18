using Gum.Commands;
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
    private readonly ImportLogic _importLogic;

    public override string Title => "Import Behavior";
    public override string BrowseFileFilter => "Behavior Files (*.behaviors)|*.behaviors";

    public ImportBehaviorDialog(
        IFileCommands fileCommands,
        IGuiCommands guiCommands,
        ISelectedState selectedState,
        IDialogService dialogService,
        ImportLogic importLogic

        ) : base(dialogService)
    {
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _selectedState = selectedState;
        _importLogic = importLogic;

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