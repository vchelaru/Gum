using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
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
    private readonly IElementCommands _elementCommands;
    private readonly SetVariableLogic _setVariableLogic;
    
    public bool IsAddingAsParentToSelectedInstance { get; set; }

    public AddInstanceDialogViewModel(
        ISelectedState selectedState,
        INameVerifier nameVerifier, 
        IElementCommands elementCommands,
        SetVariableLogic setVariableLogic)
    {
        _selectedState = selectedState;
        _nameVerifier = nameVerifier;
        _elementCommands = elementCommands;
        _setVariableLogic = setVariableLogic;
    }

    public override void OnAffirmative()
    {
        if (Value is null || Error is not null) return;
        
        ElementSave selectedElement = _selectedState.SelectedElement;
        InstanceSave? focusedInstance = _selectedState.SelectedInstance;
        InstanceSave newInstance =
            _elementCommands.AddInstance(selectedElement, Value, TypeToCreate);
        
        if (IsAddingAsParentToSelectedInstance)
        {
            System.Diagnostics.Debug.Assert(focusedInstance != null);
        }
        
        if (focusedInstance != null)
        {
            if (IsAddingAsParentToSelectedInstance)
            {
                SetInstanceParentWrapper(selectedElement, newInstance, focusedInstance);
            }
            else
            {
                SetInstanceParent(selectedElement, newInstance, focusedInstance);
            }
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

    public void SetInstanceParent(ElementSave targetElement, InstanceSave child, InstanceSave parent)
    {
        // From DragDropManager:
        // "Since the Parent property can only be set in the default state, we will
        // set the Parent variable on that instead of the _selectedState.SelectedStateSave"
        var stateToAssignOn = targetElement.DefaultState;
        var variableName = child.Name + ".Parent";
        var oldValue = stateToAssignOn.GetValue(variableName) as string;        // This will always be empty anyway...

        string newParent = parent.Name;
        var suffix = ObjectFinder.Self.GetDefaultChildName(parent);
        if (!string.IsNullOrEmpty(suffix))
        {
            newParent = parent.Name + "." + suffix;
        }

        stateToAssignOn.SetValue(variableName, newParent, "string");
        _setVariableLogic.PropertyValueChanged("Parent", oldValue, child, targetElement.DefaultState);
    }
}