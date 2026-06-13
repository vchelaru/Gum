using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Services.Dialogs;

namespace Gum.Dialogs;

/// <summary>
/// Dialog for the tree-view "Create Component" command. Prompts for the new component's name and,
/// via the optional checkbox, whether to replace the source instance subtree with a single instance
/// of the new component. The actual promotion is performed by
/// <see cref="ICopyPasteLogic.CreateComponentFromInstance"/>.
/// </summary>
public class CreateComponentDialogViewModel : GetUserStringDialogBaseViewModel
{
    public override string Title => "Create Component";
    public override string Message => "Name of the new component";

    private readonly INameVerifier _nameVerifier;
    private readonly ICopyPasteLogic _copyPasteLogic;
    private readonly IGuiCommands _guiCommands;

    private InstanceSave? _instance;

    /// <summary>
    /// The instance to promote into a component. Must be set (via the dialog initializer) before the
    /// dialog is shown.
    /// </summary>
    public InstanceSave? Instance
    {
        get => _instance;
        set
        {
            _instance = value;
            Value = value == null ? null : $"{value.Name}Component";
            CheckboxText = value == null
                ? null
                : $"Replace {value.Name} and all children with an instance of the new component";
        }
    }

    public CreateComponentDialogViewModel(INameVerifier nameVerifier,
        ICopyPasteLogic copyPasteLogic,
        IGuiCommands guiCommands)
    {
        _nameVerifier = nameVerifier;
        _copyPasteLogic = copyPasteLogic;
        _guiCommands = guiCommands;
    }

    public override void OnAffirmative()
    {
        if (Instance == null || string.IsNullOrEmpty(Value) || Error != null)
        {
            return;
        }

        _copyPasteLogic.CreateComponentFromInstance(Instance, Value, replaceWithInstance: IsCheckboxChecked);
        _guiCommands.RefreshElementTreeView();

        base.OnAffirmative();
    }

    protected override string? Validate(string? value)
    {
        if (!ObjectFinder.Self.IsProjectSaved())
        {
            return "You must first save the project before adding a new component";
        }

        return _nameVerifier.IsElementNameValid(value, null, null, out string? whyNotValid)
            ? base.Validate(value)
            : whyNotValid;
    }
}
