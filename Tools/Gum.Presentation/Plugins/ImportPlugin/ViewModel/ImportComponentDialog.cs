using Gum.Commands;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.ViewModel;

public class ImportComponentDialog : ImportBaseDialogViewModel
{
    private readonly IFileCommands _fileCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;
    private readonly IImportLogic _importLogic;
    private readonly IProjectManager _projectManager;
    private readonly IProjectState _projectState;

    public override string Title => "Import Component";
    public override string BrowseFileFilter =>
        $"Gum Component (*.{GumProjectSave.ComponentExtension};*.{GumProjectSave.ComponentJsonExtension})" +
        $"|*.{GumProjectSave.ComponentExtension};*.{GumProjectSave.ComponentJsonExtension}";

    public ImportComponentDialog(
        IFileCommands fileCommands,
        IGuiCommands guiCommands,
        ISelectedState selectedState,
        IDialogService dialogService,
        IImportLogic importLogic,
        IProjectState projectState,
        IProjectManager projectManager)
        : base(dialogService)
    {
        _fileCommands = fileCommands;
        _guiCommands = guiCommands;
        _selectedState = selectedState;
        _importLogic = importLogic;
        _projectManager = projectManager;
        _projectState = projectState;

        // A JSON-converted component (issue #4182) must be offered for import the same way its
        // XML counterpart is.
        List<FilePath> componentFilesNotInProject = FileManager.GetAllFilesInDirectory(
                _projectState.ComponentFilePath.FullPath, GumProjectSave.ComponentExtension)
            .Concat(FileManager.GetAllFilesInDirectory(
                _projectState.ComponentFilePath.FullPath, GumProjectSave.ComponentJsonExtension))
            .Select(item => new FilePath(item))
            .ToList();

        FilePath[] componentFilesInProject = _projectState.GumProjectSave
            .Components
            .SelectMany(item => new[]
            {
                new FilePath(_projectState.ComponentFilePath + item.Name + "." + GumProjectSave.ComponentExtension),
                new FilePath(_projectState.ComponentFilePath + item.Name + "." + GumProjectSave.ComponentJsonExtension),
            })
            .ToArray();

        componentFilesNotInProject = componentFilesNotInProject
            .Except(componentFilesInProject)
            .ToList();

        UnfilteredFiles.AddRange(componentFilesNotInProject.Select(item => item.FullPath));
    }

    public override void OnAffirmative()
    {
        ComponentSave lastImportedComponent = null;

        string desiredDirectory = FileManager.GetDirectory(
            _projectManager.GumProjectSave.FullFileName) + "Components/";
        foreach (var file in SelectedFiles)
        {
            lastImportedComponent = _importLogic.ImportComponent(file, desiredDirectory,
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