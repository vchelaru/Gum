using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Logic;
using Gum.ToolStates;

namespace Gum.Dialogs;

public class AddInstanceDialogViewModel : GetUserStringDialogBaseViewModel
{
    public string TypeToCreate
    {
        get;
        set;
    } = StandardElementsManager.Self.DefaultType;

    public override string Title => "New Object";
    public override string Message => "Enter new object name";

    private readonly ISelectedState _selectedState;
    private readonly INameVerifier _nameVerifier;
    private readonly IAddInstanceLogic _addInstanceLogic;
    private readonly ISetVariableLogic _setVariableLogic;
    
    public bool IsAddingAsParentToSelectedInstance { get; set; }

    public AddInstanceDialogViewModel(
        ISelectedState selectedState,
        INameVerifier nameVerifier, 
        IAddInstanceLogic addInstanceLogic,
        ISetVariableLogic setVariableLogic)
    {
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _addInstanceLogic = addInstanceLogic;
        _setVariableLogic = setVariableLogic;
    }

    public override void OnAffirmative()
    {
        if (Value is null || Error is not null) return;

        if (ObjectFinder.Self.GetElementSave(TypeToCreate) is not { } elementToAdd)
        {
            return;
        }

        if (IsAddingAsParentToSelectedInstance)
        {
            ElementSave selectedElement = _selectedState.SelectedElement;
            InstanceSave? focusedInstance = _selectedState.SelectedInstance;
            System.Diagnostics.Debug.Assert(focusedInstance != null);

            // The new parent is created at the root, then takes the focused instance's place.
            InstanceSave? newInstance = _addInstanceLogic.AddInstance(elementToAdd, selectedElement, Value);
            if (newInstance != null && focusedInstance != null)
            {
                SetInstanceParentWrapper(selectedElement, newInstance, focusedInstance);
            }
        }
        else
        {
            _addInstanceLogic.AddInstanceAtDestination(elementToAdd, Value);
        }

        base.OnAffirmative();
    }

    protected override string? Validate(string? value)
    {
        if (!_nameVerifier.IsInstanceNameValid(value, null, _selectedState.SelectedElement, out string whyNotValid))
        {
            return  whyNotValid;
        }

        return base.Validate(value);
    }
    
    public void SetInstanceParentWrapper(ElementSave targetElement, InstanceSave newInstance, InstanceSave existingInstance)
    {
        // Vic October 13, 2023
        // Currently new parents can
        // only be created as Containers,
        // so they won't have Default Child 
        // Containers. In the future we will
        // probably add the ability to select
        // the type of parent to add, and when
        // that happens we'll want to add assignment
        // of the parent's default child container.

        // From DragDropManager:
        // "Since the Parent property can only be set in the default state, we will
        // set the Parent variable on that instead of the _selectedState.SelectedStateSave"
        var stateToAssignOn = targetElement.DefaultState;

        var variableName = newInstance.Name + ".Parent";
        var existingInstanceVar = existingInstance.Name + ".Parent";
        var oldValue = stateToAssignOn.GetValue(variableName) as string;        // This will always be empty anyway...
        var oldParentValue = stateToAssignOn.GetValue(existingInstanceVar) as string;

        stateToAssignOn.SetValue(variableName, oldParentValue, "string");
        stateToAssignOn.SetValue(existingInstanceVar, newInstance.Name, "string");

        _setVariableLogic.PropertyValueChanged("Parent", oldValue, newInstance, targetElement.DefaultState);
        _setVariableLogic.PropertyValueChanged("Parent", oldParentValue, existingInstance, targetElement.DefaultState);
    }
}
