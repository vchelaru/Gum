using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Dialogs;

public class AddComponentDialogViewModel : GetUserStringDialogBaseViewModel
{
    public override string Title => "Add Component";
    public override string Message => "Enter new Component name:";

    private readonly INameVerifier _nameVerifier;
    private readonly ISelectedState _selectedState;
    private readonly IProjectCommands _projectCommands;

    public AddComponentDialogViewModel(INameVerifier nameVerifier, 
        ISelectedState selectedState, 
        IProjectCommands projectCommands)
    {
        _nameVerifier = nameVerifier;
        _selectedState = selectedState;
        _projectCommands = projectCommands;
    }

    protected override void OnAffirmative()
    {
        if (Value is null || Error is not null) return;

        ITreeNode nodeToAddTo = _selectedState.SelectedTreeNode;

        while (nodeToAddTo is { Tag: ComponentSave, Parent: { } parent })
        {
            nodeToAddTo = parent;
        }

        FilePath? path = nodeToAddTo?.GetFullFilePath();
        if (nodeToAddTo == null || !nodeToAddTo.IsPartOfComponentsFolderStructure())
        {
            path = GumState.Self.ProjectState.ComponentFilePath;
        }

        if (path != null)
        {
            string relativeToComponents = FileManager.MakeRelative(path.StandardizedCaseSensitive,
                FileLocations.Self.ComponentsFolder, preserveCase: true);

            relativeToComponents = relativeToComponents.Replace('\\', '/');

            ComponentSave componentSave = new ComponentSave();

            _projectCommands.PrepareNewComponentSave(componentSave, relativeToComponents + Value);

            _projectCommands.AddComponent(componentSave);
        }
        base.OnAffirmative();
    }

    protected override string? Validate(string? value)
    {
        if (!ObjectFinder.Self.IsProjectSaved())
        {
            return "You must first save a project before adding a Component";
        }
        
        return _nameVerifier.IsElementNameValid(value, null, null, out var whyNotValid)
            ? base.Validate(value)
            : whyNotValid;
    }
}