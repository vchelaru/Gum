using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.ViewModel;

public class ImportScreenDialog : ImportBaseDialogViewModel
{
    private readonly IImportLogic _importLogic;
    private readonly IProjectState _projectState;

    public override string Title => "Import Screen";
    public override string BrowseFileFilter => "Gum Screen (*.gusx)|*.gusx";
    public ImportScreenDialog(IDialogService dialogService,
        IImportLogic importLogic,
        IProjectState projectState

        ) : base(dialogService)
    {
        _importLogic = importLogic;
        _projectState = projectState;

        List<FilePath> screenFilesNotInProject = FileManager.GetAllFilesInDirectory(
                _projectState.ScreenFilePath.FullPath, "gusx")
            .Select(item => new FilePath(item))
            .ToList();

        FilePath[] screenFilesInProject = _projectState.GumProjectSave
            .Screens
            .Select(item => new FilePath(_projectState.ComponentFilePath + item.Name + ".gusx"))
            .ToArray();

        screenFilesNotInProject = screenFilesNotInProject
            .Except(screenFilesInProject)
            .ToList();

        UnfilteredFiles.AddRange(screenFilesNotInProject.Select(item => item.FullPath));
    }

    public override void OnAffirmative()
    {

        foreach (var file in SelectedFiles)
        {
            _importLogic.ImportScreen(file);
        }

        base.OnAffirmative();
    }
}