using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gum.DataTypes;
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
    public override string BrowseFileFilter =>
        $"Gum Screen (*.{GumProjectSave.ScreenExtension};*.{GumProjectSave.ScreenJsonExtension})" +
        $"|*.{GumProjectSave.ScreenExtension};*.{GumProjectSave.ScreenJsonExtension}";
    public ImportScreenDialog(IDialogService dialogService,
        IImportLogic importLogic,
        IProjectState projectState

        ) : base(dialogService)
    {
        _importLogic = importLogic;
        _projectState = projectState;

        // A JSON-converted screen (issue #4182) must be offered for import the same way its XML
        // counterpart is.
        List<FilePath> screenFilesNotInProject = FileManager.GetAllFilesInDirectory(
                _projectState.ScreenFilePath.FullPath, GumProjectSave.ScreenExtension)
            .Concat(FileManager.GetAllFilesInDirectory(
                _projectState.ScreenFilePath.FullPath, GumProjectSave.ScreenJsonExtension))
            .Select(item => new FilePath(item))
            .ToList();

        // Boyscout (issue #4182): this previously compared against ComponentFilePath instead of
        // ScreenFilePath, so an already-imported screen never matched here and always re-appeared
        // in the "available to import" list.
        FilePath[] screenFilesInProject = _projectState.GumProjectSave
            .Screens
            .SelectMany(item => new[]
            {
                new FilePath(_projectState.ScreenFilePath + item.Name + "." + GumProjectSave.ScreenExtension),
                new FilePath(_projectState.ScreenFilePath + item.Name + "." + GumProjectSave.ScreenJsonExtension),
            })
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