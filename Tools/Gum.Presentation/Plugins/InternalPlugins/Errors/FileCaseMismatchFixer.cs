using System;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Messages;
using Gum.ProjectServices;
using Gum.ProjectServices.Editing;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using ToolsUtilities;

namespace Gum.Plugins.Errors;

/// <inheritdoc/>
public class FileCaseMismatchFixer : IFileCaseMismatchFixer
{
    private readonly IProjectState _projectState;
    private readonly IDialogService _dialogService;
    private readonly IUndoManager _undoManager;
    private readonly IPluginManager _pluginManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IMessenger _messenger;
    private readonly Func<string, string, bool> _hasCaseTwin;

    public FileCaseMismatchFixer(
        IProjectState projectState,
        IDialogService dialogService,
        IUndoManager undoManager,
        IPluginManager pluginManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IMessenger messenger,
        Func<string, string, bool>? hasCaseTwin = null)
    {
        _projectState = projectState;
        _dialogService = dialogService;
        _undoManager = undoManager;
        _pluginManager = pluginManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _messenger = messenger;
        _hasCaseTwin = hasCaseTwin ?? FileReferenceCaseRewriter.HasCaseTwin;
    }

    /// <inheritdoc/>
    public bool CanFix(FileNameCaseMismatchDetails details) =>
        _projectState.GumProjectSave is { } project
        && FileReferenceCaseRewriter.Find(project, details.ReferencedPath).Any;

    /// <inheritdoc/>
    public void Fix(FileNameCaseMismatchDetails details)
    {
        if (_projectState.GumProjectSave is not { } project)
        {
            return;
        }
        FileReferenceLocations locations = FileReferenceCaseRewriter.Find(project, details.ReferencedPath);
        if (!locations.Any)
        {
            // An undo or an edit moved the reference since the row was made.
            return;
        }

        string projectDirectory = FileManager.GetDirectory(project.FullFileName);
        if (_hasCaseTwin(projectDirectory, details.OnDiskPath))
        {
            _dialogService.ShowMessage(
                $"More than one file named \"{details.OnDiskPath}\" exists, differing only by letter case, so Gum cannot tell which one \"{details.ReferencedPath}\" means. " +
                "Delete or rename one of them, then fix the reference.",
                "Cannot fix the file name");
            return;
        }

        foreach (ElementSave element in locations.Variables.Select(item => item.Element).Distinct().ToList())
        {
            // Requested before the change so the history entry covers it.
            using UndoLock undoLock = _undoManager.RequestLock(element);
            FileReferenceCaseRewriter.ApplyToElement(locations, element, details.OnDiskPath);
            foreach (var item in locations.Variables.Where(item => item.Element == element))
            {
                string name = item.Variable.Name;
                int dot = name.IndexOf('.');
                InstanceSave? instance = dot < 0 ? null : element.GetInstance(name.Substring(0, dot));
                _pluginManager.VariableSet(element, instance, dot < 0 ? name : name.Substring(dot + 1), details.ReferencedPath);
            }
            _fileCommands.TryAutoSaveElement(element);
        }

        if (locations.IsSinglePixelTexture || locations.LocalizationFileIndexes.Count > 0)
        {
            FileReferenceCaseRewriter.ApplyToProject(project, locations, details.OnDiskPath);
            _fileCommands.TryAutoSaveProject();
        }

        _guiCommands.RefreshVariableValues();
        _messenger.Send(new RequestErrorRefreshMessage { IncludesProject = true });
    }
}
