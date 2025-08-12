using System;
using System.IO;
using System.Windows.Forms;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Dialogs;

public class RenameFolderDialogViewModel : GetUserStringDialogBaseViewModel
{
    public override string Title => "Rename Folder";
    public override string Message => "Enter new folder name:";
    
    private readonly INameVerifier _nameVerifier;
    private readonly IRenameLogic _renameLogic;
    private readonly IGuiCommands _guiCommands;
    public TreeNode? FolderNode { get; set; }
    
    public RenameFolderDialogViewModel(
        INameVerifier nameVerifier, 
        IRenameLogic renameLogic,
        IGuiCommands guiCommands)
    {
        _nameVerifier = nameVerifier;
        _renameLogic = renameLogic;
        _guiCommands = guiCommands;
    }

    protected override void OnAffirmative()
    {
        if (FolderNode is null ||
            Value is null ||
            Error is not null)
        {
            return;
        }
        
        // see if it already exists:
        FilePath newFullPath = FileManager.GetDirectory(FolderNode.GetFullFilePath().FullPath) + Value + "\\";
        if (Directory.Exists(newFullPath.FullPath))
        {
            Error = $"Folder {Value} already exists.";
            return;
        }
        
        string rootForElement;
        if (FolderNode.IsScreensFolderTreeNode())
        {
            rootForElement = FileLocations.Self.ScreensFolder;
        }
        else if (FolderNode.IsComponentsFolderTreeNode())
        {
            rootForElement = FileLocations.Self.ComponentsFolder;
        }
        else
        {
            Error = "Invalid root node.";
            return;
        }

        var oldFullPath = FolderNode.GetFullFilePath();

        string oldPathRelativeToElementsRoot = FileManager.MakeRelative(FolderNode.GetFullFilePath().FullPath, rootForElement, preserveCase: true);
        FolderNode.Text = Value;
        string newPathRelativeToElementsRoot = FileManager.MakeRelative(FolderNode.GetFullFilePath().FullPath, rootForElement, preserveCase: true);

        if (FolderNode.IsScreensFolderTreeNode())
        {
            foreach (var screen in ProjectState.Self.GumProjectSave.Screens)
            {
                if (screen.Name.StartsWith(oldPathRelativeToElementsRoot))
                {
                    string oldVaue = screen.Name;
                    string newName = newPathRelativeToElementsRoot + screen.Name.Substring(oldPathRelativeToElementsRoot.Length);

                    screen.Name = newName;
                    _renameLogic.HandleRename(screen, (InstanceSave)null, oldVaue, NameChangeAction.Move, askAboutRename: false);
                }
            }
        }
        else if (FolderNode.IsComponentsFolderTreeNode())
        {
            foreach (var component in ProjectState.Self.GumProjectSave.Components)
            {
                if (component.Name.ToLowerInvariant().StartsWith(oldPathRelativeToElementsRoot.ToLowerInvariant()))
                {
                    string oldVaue = component.Name;
                    string newName = newPathRelativeToElementsRoot + component.Name.Substring(oldPathRelativeToElementsRoot.Length);
                    component.Name = newName;

                    _renameLogic.HandleRename(component, (InstanceSave)null, oldVaue, NameChangeAction.Move, askAboutRename: false);
                }
            }
        }

        try
        {
            Directory.Move(oldFullPath.FullPath, newFullPath.FullPath);
            _guiCommands.RefreshElementTreeView();
        }
        catch (Exception e)
        {
            Error = "Could not move the old folder." +
                $" Additional information: \n{e}";
            return;
        }
        
        base.OnAffirmative();
    }

    protected override string? Validate(string? value)
    {
        if (FolderNode is null)
        {
            return "No folder selected";
        }

        if (FolderNode.Text == value) return null;

        return _nameVerifier.IsFolderNameValid(value, out var whyNotValid) ? base.Validate(value) : whyNotValid;
    }
}