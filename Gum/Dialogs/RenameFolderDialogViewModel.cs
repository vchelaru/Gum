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
    private readonly FileLocations _fileLocations;
    private readonly IFileCommands _fileCommands;

    public ITreeNode? FolderNode { get; set; }
    
    public RenameFolderDialogViewModel(
        INameVerifier nameVerifier, 
        IRenameLogic renameLogic,
        IGuiCommands guiCommands,
        FileLocations fileLocations,
        IFileCommands fileCommands)
    {
        _nameVerifier = nameVerifier;
        _renameLogic = renameLogic;
        _guiCommands = guiCommands;
        _fileLocations = fileLocations;
        _fileCommands = fileCommands;
    }

    public override void OnAffirmative()
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
            rootForElement = _fileLocations.ScreensFolder;
        }
        else if (FolderNode.IsComponentsFolderTreeNode())
        {
            rootForElement = _fileLocations.ComponentsFolder;
        }
        else
        {
            Error = "Invalid root node.";
            return;
        }

        var oldFullPath = FolderNode.GetFullFilePath();

        string oldPathRelativeToElementsRoot = FileManager.MakeRelative(FolderNode.GetFullFilePath().FullPath, rootForElement, preserveCase: true);
        var folderNodeAsTreeNode = FolderNode as TreeNodeWrapper;
        if(folderNodeAsTreeNode?.Node != null)
        {
            folderNodeAsTreeNode.Node.Text = Value;
        }
        string newPathRelativeToElementsRoot = FileManager.MakeRelative(FolderNode.GetFullFilePath().FullPath, rootForElement, preserveCase: true);

        if (FolderNode.IsScreensFolderTreeNode())
        {
            // rename logic may adjust the order so let's get a copy:
            var screensCopy = ProjectState.Self.GumProjectSave.Screens.ToArray();
            foreach (var screen in screensCopy)
            {
                if (screen.Name.ToLowerInvariant().StartsWith(oldPathRelativeToElementsRoot.Replace("\\", "/").ToLowerInvariant()))
                {
                    string oldVaue = screen.Name;
                    string newName = newPathRelativeToElementsRoot + screen.Name.Substring(oldPathRelativeToElementsRoot.Length).Replace("\\", "/");

                    screen.Name = newName;
                    _renameLogic.HandleRename(screen, (InstanceSave?)null, oldVaue, NameChangeAction.Move, askAboutRename: false);
                }
            }
        }
        else if (FolderNode.IsComponentsFolderTreeNode())
        {
            var componentsCopy = ProjectState.Self.GumProjectSave.Components.ToArray();
            foreach (var component in componentsCopy)
            {
                if (component.Name.ToLowerInvariant().StartsWith(oldPathRelativeToElementsRoot.Replace("\\", "/").ToLowerInvariant()))
                {
                    string oldVaue = component.Name;
                    string newName = (newPathRelativeToElementsRoot + component.Name.Substring(oldPathRelativeToElementsRoot.Length)).Replace("\\", "/");
                    component.Name = newName;

                    _renameLogic.HandleRename(component, (InstanceSave?)null, oldVaue, NameChangeAction.Move, askAboutRename: false);
                }
            }
        }

        try
        {
            _fileCommands.MoveDirectory(oldFullPath.FullPath, newFullPath.FullPath);
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