using System;
using Gum.Commands;
using Gum.DataTypes;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;

namespace ConvertToJsonPlugin;

/// <summary>
/// Business logic behind the "Convert to JSON" menu item, kept out of the WPF-hosted
/// <c>MainConvertToJsonPlugin</c> (mirrors <c>ImportFromGumxLogic</c>, ADR-0005 Phase 3) so it can be
/// unit tested headlessly. Converts the whole currently-open project - there is no picker, unlike
/// "Import from .gumx".
/// </summary>
public class ConvertToJsonLogic
{
    private readonly IProjectState _projectState;
    private readonly IConvertProjectToJsonService _convertService;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogService _dialogService;

    public ConvertToJsonLogic(
        IProjectState projectState,
        IConvertProjectToJsonService convertService,
        IFileCommands fileCommands,
        IDialogService dialogService)
    {
        _projectState = projectState;
        _convertService = convertService;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Whether the currently-open project can be converted right now: a project must be loaded and
    /// have no unsaved changes, since conversion is written from disk-backed element/behavior files.
    /// </summary>
    public bool CanConvert => _projectState.GumProjectSave != null && !_projectState.NeedsToSaveProject;

    /// <summary>
    /// Confirms with the user, converts the currently-open project to JSON, then reopens it from the
    /// newly-written <c>.gumj</c> so the tool is looking at the JSON version. No-ops (with a message)
    /// if <see cref="CanConvert"/> is false when called.
    /// </summary>
    public void ConvertCurrentProject()
    {
        if (!CanConvert)
        {
            _dialogService.ShowMessage("You must first save the project before converting it to JSON.");
            return;
        }

        GumProjectSave project = _projectState.GumProjectSave!;

        bool confirmed = _dialogService.ShowYesNoMessage(
            "This creates new .gumj/.gusj/.gucj/.gutj/.behj/.ganj files alongside the project's " +
            "existing XML files. The existing XML files are not modified or deleted.\n\nConvert to JSON?",
            "Convert to JSON");
        if (!confirmed)
        {
            return;
        }

        ConvertProjectToJsonResult result;
        try
        {
            result = _convertService.ConvertToJson(project);
        }
        catch (InvalidOperationException ex)
        {
            _dialogService.ShowMessage(ex.Message, "Convert to JSON");
            return;
        }

        _fileCommands.LoadProject(result.ProjectFilePath);

        _dialogService.ShowMessage(
            $"Converted {result.TotalFileCount} file(s) to JSON.\n\nNow editing {result.ProjectFilePath}.",
            "Convert to JSON");
    }
}
