using System;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager;

/// <summary>
/// Gives an element created in the tool (added, duplicated, pasted) the code settings it should
/// start with. A duplicate keeps the settings copied from its source; any other new element starts
/// from defaults, so a .codsj already sitting at its name is left over from something else (an
/// element deleted outside the tool, or one set to Never Generate, whose files delete leaves alone)
/// and is recycled rather than applied. Loading a project never goes through here, so settings
/// files restored from source control are kept.
/// </summary>
public class NewElementCodeSettingsService
{
    private readonly CodeOutputElementSettingsManager _settingsManager;
    private readonly IFileCommands _fileCommands;
    private readonly IOutputManager _outputManager;

    // ElementDuplicate fires just before the copy's ElementAdd.
    private ElementSave? _elementWithCopiedSettings;

    public NewElementCodeSettingsService(
        CodeOutputElementSettingsManager settingsManager, IFileCommands fileCommands, IOutputManager outputManager)
    {
        _settingsManager = settingsManager;
        _fileCommands = fileCommands;
        _outputManager = outputManager;
    }

    public void HandleElementDuplicate(ElementSave source, ElementSave copy)
    {
        bool copied = _settingsManager.CopySettings(source, copy);
        _elementWithCopiedSettings = copied ? copy : null;
    }

    public void HandleElementAdd(ElementSave element)
    {
        bool hasCopiedSettings = element == _elementWithCopiedSettings;
        _elementWithCopiedSettings = null;
        if (hasCopiedSettings)
        {
            return;
        }

        FilePath? settingsFile = _settingsManager.GetCodeSettingsFilePath(element);
        if (settingsFile?.Exists() != true)
        {
            return;
        }

        try
        {
            _fileCommands.MoveToRecycleBin(settingsFile);
            _outputManager.AddOutput(
                $"Moved leftover code settings file {settingsFile.FullPath} to the recycle bin; the new element {element.Name} starts from default code settings.");
        }
        catch (Exception exception)
        {
            _outputManager.AddError(
                $"Could not remove leftover code settings file {settingsFile.FullPath}, so the new element {element.Name} uses its settings: {exception.Message}");
        }
    }
}
