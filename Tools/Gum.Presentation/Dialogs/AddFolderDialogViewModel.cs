using Gum.Commands;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Dialogs;

public class AddFolderDialogViewModel : GetUserStringDialogBaseViewModel
{
    public override string Title => "Add Folder";
    public override string Message => "Enter new folder name";

    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IGuiCommands _guiCommands;

    public AddFolderDialogViewModel(
        ISelectedState selectedState,
        INameVerifier nameVerifier, 
        IGuiCommands guiCommands)
    {
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _guiCommands = guiCommands;
    }

    public override void OnAffirmative()
    {
        if (Value is null || Error is not null) return;

        // The path is null when the project has not been saved, so there is no folder to add to.
        FilePath? parentFolder = _selectedState.SelectedTreeNode?.GetFullFilePath();
        if (parentFolder is null)
        {
            Error = "You must first save the project before adding a folder";
            return;
        }

        string folder = parentFolder + Value + "\\";

        // If the path is relative
        // that means that the root
        // hasn't been set yet.
        if (!FileManager.IsRelative(folder))
        {
            System.IO.Directory.CreateDirectory(folder);
        }

        _guiCommands.RefreshElementTreeView();
        base.OnAffirmative();
    }

    protected override string? Validate(string? value)
    {
        if(!_nameVerifier.IsFolderNameValid(value, out string whyNotValid))
        {
            return whyNotValid;
        }
            
        if(value?.Contains(" ") is true)
        {
            return "Folders with spaces are not recommended since they can break variable references";
        }

        return base.Validate(value);
    }
}