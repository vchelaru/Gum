using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Dialogs;

public class AddScreenDialogViewModel : GetUserStringDialogBaseViewModel
{
    public override string Title => "Add Screen";
    public override string Message => "Enter new Screen name";
    
    private readonly INameVerifier _nameVerifier;
    private readonly ISelectedState _selectedState;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ProjectCommands _projectCommands;

    public AddScreenDialogViewModel(INameVerifier nameVerifier, 
        ISelectedState selectedState, 
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ProjectCommands projectCommands)
    {
        _nameVerifier = nameVerifier;
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _projectCommands = projectCommands;
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

        ScreenSave screenSave = _projectCommands.AddScreen(relativeToScreens + Value);
        
        _guiCommands.RefreshElementTreeView();

        _selectedState.SelectedScreen = screenSave;

        _fileCommands.TryAutoSaveElement(screenSave);
        _fileCommands.TryAutoSaveProject();
        
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