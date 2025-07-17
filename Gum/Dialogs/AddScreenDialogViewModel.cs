﻿using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Dialogs;

public class AddScreenDialogViewModel : GetUserStringDialogBaseViewModel
{
    public override string Title => "Add Screen";
    public override string Message => "Enter new Screen name";
    
    private readonly NameVerifier _nameVerifier;
    private readonly ISelectedState _selectedState;
    private readonly GuiCommands _guiCommands;

    public AddScreenDialogViewModel(NameVerifier nameVerifier, 
        ISelectedState selectedState, 
        GuiCommands guiCommands)
    {
        _nameVerifier = nameVerifier;
        _selectedState = selectedState;
        _guiCommands = guiCommands;
    }

    protected override void OnAffirmative()
    {
        if (string.IsNullOrWhiteSpace(Value) || Error is not null) return;

        ITreeNode nodeToAddTo = _selectedState.SelectedTreeNode;

        while (nodeToAddTo is { Tag: ScreenSave, Parent: not null })
        {
            nodeToAddTo = nodeToAddTo.Parent;
        }

        string? path = nodeToAddTo?.GetFullFilePath().FullPath;

        if (nodeToAddTo == null || !nodeToAddTo.IsPartOfScreensFolderStructure())
        {
            path = GumState.Self.ProjectState.ScreenFilePath.FullPath;
        }
        
        string relativeToScreens = FileManager.MakeRelative(path, FileLocations.Self.ScreensFolder);

        ScreenSave screenSave = GumCommands.Self.ProjectCommands.AddScreen(relativeToScreens + Value);
        
        _guiCommands.RefreshElementTreeView();

        _selectedState.SelectedScreen = screenSave;

        GumCommands.Self.FileCommands.TryAutoSaveElement(screenSave);
        GumCommands.Self.FileCommands.TryAutoSaveProject();
        
        base.OnAffirmative();
    }

    protected override string? Validate(string? value)
    {        
        if (!ObjectFinder.Self.IsProjectSaved())
        {
            return "You must first save a project before adding a Component";
        }
        
        return _nameVerifier.IsElementNameValid(value, null, null, out string? whyNotValid)
            ? base.Validate(value)
            : whyNotValid;
    }
}