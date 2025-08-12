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
    private readonly GuiCommands _guiCommands;

    public AddFolderDialogViewModel(
        ISelectedState selectedState,
        INameVerifier nameVerifier, 
        GuiCommands guiCommands)
    {
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _guiCommands = guiCommands;
    }

    protected override void OnAffirmative()
    {
        if (Value is null || Error is not null) return;

        string folder = _selectedState.SelectedTreeNode.GetFullFilePath() + Value + "\\";

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