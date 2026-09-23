using System;
using System.IO;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ToolsUtilities;

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
    private readonly IFileWatchIgnoreList _fileWatchIgnoreList;

    public ConvertToJsonLogic(
        IProjectState projectState,
        IConvertProjectToJsonService convertService,
        IFileCommands fileCommands,
        IDialogService dialogService,
        IFileWatchIgnoreList fileWatchIgnoreList)
    {
        _projectState = projectState;
        _convertService = convertService;
        _fileCommands = fileCommands;
        _dialogService = dialogService;
        _fileWatchIgnoreList = fileWatchIgnoreList;
    }

    /// <summary>
    /// Whether the currently-open project can be converted right now: a project must be loaded and
    /// have no unsaved changes, since conversion is written from disk-backed element/behavior files.
    /// </summary>
    public bool CanConvert => _projectState.GumProjectSave != null && !_projectState.NeedsToSaveProject;

    /// <summary>
    /// Confirms with the user, converts the currently-open project to JSON, then reopens it from the
    /// newly-written <c>.gumj</c> so the tool is looking at the JSON version. When the user opted in,
    /// the converted XML files then go to the OS trash, but only once the JSON project is confirmed
    /// open. No-ops (with a message) if <see cref="CanConvert"/> is false when called.
    /// </summary>
    public async Task ConvertCurrentProjectAsync()
    {
        if (!CanConvert)
        {
            _dialogService.ShowMessage("You must first save the project before converting it to JSON.");
            return;
        }

        GumProjectSave project = _projectState.GumProjectSave!;
        string xmlFileName = Path.GetFileName(project.FullFileName);
        string jsonFileName = Path.ChangeExtension(xmlFileName, GumProjectSave.ProjectJsonExtension);

        ConvertToJsonDialogViewModel dialog = new ConvertToJsonDialogViewModel(xmlFileName, jsonFileName);
        if (!_dialogService.Show(dialog))
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

        await _fileCommands.LoadProjectAsync(result.ProjectFilePath);

        string recycleSummary = "The original XML files are still in the project folder.";
        if (dialog.ShouldRecycleXmlFiles)
        {
            recycleSummary = RecycleConvertedXml(result);
        }

        _dialogService.ShowMessage(
            $"Converted {result.TotalFileCount} file(s) to JSON: 1 project, {result.ScreenCount} screen(s), " +
            $"{result.ComponentCount} component(s), {result.StandardElementCount} standard(s), " +
            $"{result.BehaviorCount} behavior(s), {result.AnimationCount} animation file(s).\n\n" +
            $"{recycleSummary}\n\n" +
            $"Now editing {result.ProjectFilePath}. Update your game to load {jsonFileName} instead of {xmlFileName}.",
            "Convert to JSON");
    }

    private string RecycleConvertedXml(ConvertProjectToJsonResult result)
    {
        string trashName = ConvertToJsonDialogViewModel.TrashName;
        string? openProject = _projectState.GumProjectSave?.FullFileName;
        if (openProject == null || new FilePath(openProject) != new FilePath(result.ProjectFilePath))
        {
            return $"The JSON project did not open, so the original XML files were not moved to the {trashName}.";
        }

        // Gum is deleting these itself, so the file watcher must not react to them as external deletes.
        foreach (FilePath file in result.ConvertedXmlFiles)
        {
            _fileWatchIgnoreList.IgnoreNextChangeUntil(file);
        }

        try
        {
            _fileCommands.MoveToRecycleBin(result.ConvertedXmlFiles);
        }
        catch (Exception ex)
        {
            return $"Some or all of the original XML files could not be moved to the {trashName}: {ex.Message}";
        }
        return $"Moved {result.ConvertedXmlFiles.Count} XML file(s) to the {trashName}.";
    }
}
