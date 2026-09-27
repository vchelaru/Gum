using System;
using System.IO;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Services;
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
    private readonly IFileLocations _fileLocations;
    private readonly IFileCommands _fileCommands;
    private readonly IProjectState _projectState;
    private readonly IPathCaseSensitivity _pathCaseSensitivity;

    public ITreeNode? FolderNode { get => Get<ITreeNode?>(); set => Set(value); }

    public RenameFolderDialogViewModel(
        INameVerifier nameVerifier,
        IRenameLogic renameLogic,
        IGuiCommands guiCommands,
        IFileLocations fileLocations,
        IFileCommands fileCommands,
        IProjectState projectState,
        IPathCaseSensitivity pathCaseSensitivity)
    {
        _nameVerifier = nameVerifier;
        _renameLogic = renameLogic;
        _guiCommands = guiCommands;
        _fileLocations = fileLocations;
        _fileCommands = fileCommands;
        _projectState = projectState;
        _pathCaseSensitivity = pathCaseSensitivity;
        PreSelect = true;

        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(FolderNode))
            {
                Value = FolderNode?.Text;
            }
        };
    }

    public override void OnAffirmative()
    {
        // Folder nodes only have a path, and the project only exists, once the project is saved.
        if (FolderNode is null ||
            Value is null ||
            Error is not null ||
            FolderNode.GetFullFilePath() is not { } oldFullPath ||
            _projectState.GumProjectSave is not { } project)
        {
            return;
        }

        // see if it already exists. Where the file system ignores case (Windows, default macOS), a
        // rename that only changes casing (e.g. "GameMenuScreens" -> "gamemenuscreens") finds the
        // folder itself, so it isn't a collision. Where it doesn't (Linux), foo is its own folder.
        FilePath newFullPath = FileManager.GetDirectory(oldFullPath.FullPath) + Value + "\\";
        StringComparison pathComparison = _pathCaseSensitivity.GetComparison(oldFullPath.FullPath);
        bool isSameFolderDifferentCase =
            string.Equals(oldFullPath.FullPath, newFullPath.FullPath, pathComparison);
        if (!isSameFolderDifferentCase && Directory.Exists(newFullPath.FullPath))
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

        string oldPathRelativeToElementsRoot = FileManager.MakeRelative(oldFullPath.FullPath, rootForElement, preserveCase: true);
        FolderNode.Text = Value;
        // The node had a path before the rename, so it still has one; newFullPath is the same folder.
        FilePath renamedFullPath = FolderNode.GetFullFilePath() ?? newFullPath;
        string newPathRelativeToElementsRoot = FileManager.MakeRelative(renamedFullPath.FullPath, rootForElement, preserveCase: true);

        if (FolderNode.IsScreensFolderTreeNode())
        {
            // rename logic may adjust the order so let's get a copy:
            var screensCopy = project.Screens.ToArray();
            foreach (var screen in screensCopy)
            {
                if (screen.Name.StartsWith(oldPathRelativeToElementsRoot.Replace("\\", "/"), pathComparison))
                {
                    string oldVaue = screen.Name;
                    string newName = (newPathRelativeToElementsRoot + screen.Name.Substring(oldPathRelativeToElementsRoot.Length)).Replace("\\", "/");

                    screen.Name = newName;
                    _renameLogic.HandleRename(screen, (InstanceSave?)null, oldVaue, NameChangeAction.Move, askAboutRename: false);
                }
            }
        }
        else if (FolderNode.IsComponentsFolderTreeNode())
        {
            var componentsCopy = project.Components.ToArray();
            foreach (var component in componentsCopy)
            {
                if (component.Name.StartsWith(oldPathRelativeToElementsRoot.Replace("\\", "/"), pathComparison))
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