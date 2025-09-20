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
    private readonly ImportLogic _importLogic;

    public override string Title => "Import Screen";
    public override string BrowseFileFilter => "Gum Screen (*.gusx)|*.gusx";
    public ImportScreenDialog(IDialogService dialogService,
        ImportLogic importLogic

        ) : base(dialogService)
    {
        _importLogic = importLogic;

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
            _importLogic.ImportScreen(file);
        }

        base.OnAffirmative();
    }
}