using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Services.Dialogs;
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
    private readonly ICopyPasteProjectCommands _projectCommands;
    private readonly IFileLocations _fileLocations;
    private readonly IProjectState _projectState;

    public AddScreenDialogViewModel(INameVerifier nameVerifier,
        ISelectedState selectedState,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ICopyPasteProjectCommands projectCommands,
        IFileLocations fileLocations,
        IProjectState projectState)
    {
        _nameVerifier = nameVerifier;
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _projectCommands = projectCommands;
        _fileLocations = fileLocations;
        _projectState = projectState;
    }

    public override void OnAffirmative()
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
            path = _projectState.ScreenFilePath.FullPath;
        }
        
        string relativeToScreens = FileManager.MakeRelative(path, _fileLocations.ScreensFolder);

        // Prevent issues with any code that's looking for a '/' instead of a '\' slash
        relativeToScreens = relativeToScreens.Replace('\\', '/');

        ScreenSave screenSave = new ScreenSave { Name = relativeToScreens + Value };
        _projectCommands.AddScreen(screenSave);

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