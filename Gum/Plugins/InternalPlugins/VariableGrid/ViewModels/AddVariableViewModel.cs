using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Mvvm;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
public class AddVariableViewModel : ViewModel
{
    #region Fields/Properties

    public string EnteredName
    {
        get => Get<string>();
        set => Set(value);
    }

    private readonly Commands.GuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;
    private readonly UndoManager _undoManager;
    private readonly ElementCommands _elementCommands;
    private readonly FileCommands _fileCommands;
    private readonly NameVerifier _nameVerifier;

    public List<string> AvailableTypes
    {
        get; private set;
    }

    public string SelectedItem
    {
        get => Get<string>();
        set => Set(value);
    }

    [DependsOn(nameof(SelectedItem))]
    public object DefaultValue
    {
        get
        {
            switch (SelectedItem)
            {
                case "float":
                    return 1.0f;
                case "int":
                    return 10;
                case "string":
                    return "Hello";
                case "bool":
                    return true;
                default:
                    return null;
            }
        }
    }

    public ElementSave Element { get; set; }
    public VariableSave Variable { get; set; }

    #endregion

    public AddVariableViewModel(Commands.GuiCommands guiCommands,
        ISelectedState selectedState,
        UndoManager undoManager,
        ElementCommands elementCommands,
        FileCommands fileCommands,
        NameVerifier nameVerifier)
    {
        _guiCommands = guiCommands;
        _selectedState = selectedState;
        _undoManager = undoManager;
        _elementCommands = elementCommands;
        _fileCommands = fileCommands;
        _nameVerifier = nameVerifier;

        AvailableTypes = new List<string>();
        AvailableTypes.Add("float");
        AvailableTypes.Add("int");
        AvailableTypes.Add("string");
        AvailableTypes.Add("bool");

        SelectedItem = "float";
    }

    public GeneralResponse Validate()
    {
        if(SelectedItem == null)
        {
            return GeneralResponse.UnsuccessfulWith("You must select a type");
        }
        if(string.IsNullOrEmpty(EnteredName))
        {
            return GeneralResponse.UnsuccessfulWith("You must enter a name");
        }

        if(!_nameVerifier.IsVariableNameValid(EnteredName, Element, Variable, out string whyNotValid))
        {
            return GeneralResponse.UnsuccessfulWith(whyNotValid);
        }

        return GeneralResponse.SuccessfulResponse;
    }

    public void AddVariableToSelectedItem()
    {
        var type = SelectedItem;
        var name = EnteredName;

        string whyNotValid;
        bool isValid = NameVerifier.Self.IsVariableNameValid(
            name, Element, Variable, out whyNotValid);

        if (!isValid)
        {
            _guiCommands.ShowMessage(whyNotValid);
        }
        else
        {
            var behavior = _selectedState.SelectedBehavior;

            var newVariable = new VariableSave();

            newVariable.Name = name;
            newVariable.Type = type;
            newVariable.Value = DefaultValue;

            using var undoLock = _undoManager.RequestLock();

            if (behavior != null)
            {
                behavior.RequiredVariables.Variables.Add(newVariable);
                _elementCommands.SortVariables(behavior);
                _fileCommands.TryAutoSaveBehavior(behavior);
            }
            else if (_selectedState.SelectedElement != null)
            {
                var element = _selectedState.SelectedElement;
                newVariable.IsCustomVariable = true;
                element.DefaultState.Variables.Add(newVariable);
                _elementCommands.SortVariables(element);
                _fileCommands.TryAutoSaveElement(element);
            }
            _guiCommands.RefreshVariables(force: true);

        }
    }

    internal void DoEdit(VariableSave variable)
    {
        var type = SelectedItem;
        var newName = EnteredName;

        string whyNotValid;
        bool isValid = NameVerifier.Self.IsVariableNameValid(
            newName, Element, Variable, out whyNotValid);

        if (!isValid)
        {
            _guiCommands.ShowMessage(whyNotValid);
        }
        else
        {
            var behavior = _selectedState.SelectedBehavior;

            using var undoLock = _undoManager.RequestLock();

            if (behavior != null)
            {
                var changedType = variable.Type != type;
                if (changedType)
                {
                    // todo - need to fix this by converting?
                    variable.Value = null;
                }
                variable.Name = newName;
                variable.Type = type;

                _fileCommands.TryAutoSaveBehavior(behavior);
            }
            else if (_selectedState.SelectedElement != null)
            {
                var oldName = variable.Name;
                var element = SelectedState.Self.SelectedElement;
                if (ApplyEditVariableOnElement(element, oldName, newName, type))
                {
                    _elementCommands.SortVariables(element);
                    _fileCommands.TryAutoSaveElement(element);
                }

                ApplyChangesToInstances(element, oldName, newName, type);

                var derivedElements = ObjectFinder.Self.GetElementsInheritingFrom(element);
                foreach (var derived in derivedElements)
                {
                    if (ApplyEditVariableOnElement(derived, oldName, newName, type))
                    {
                        _fileCommands.TryAutoSaveElement(derived);
                    }

                    ApplyChangesToInstances(derived, oldName, newName, type);
                }
            }
            _guiCommands.RefreshVariables(force: true);
        }
    }

    private void ApplyChangesToInstances(ElementSave element, string oldName, string newName, string type)
    {
        var references = ObjectFinder.Self.GetElementReferencesToThis(element)
            .Where(item => item.ReferenceType == ReferenceType.InstanceOfType)
            .ToArray();

        ////////////////////////// Early Out ///////////////////////////
        if (references.Length == 0) return;
        /////////////////////// End Early Out /////////////////////////

        HashSet<ElementSave> elementsToSave = new HashSet<ElementSave>();

        foreach (var reference in references)
        {
            var instance = reference.ReferencingObject as InstanceSave;

            var oldFullName = instance.Name + "." + oldName;
            var newFullName = instance.Name + "." + newName;

            if (ApplyEditVariableOnElement(reference.OwnerOfReferencingObject, oldFullName, newFullName, type))
            {
                elementsToSave.Add(reference.OwnerOfReferencingObject);
            }
        }

        foreach (var elementToSave in elementsToSave)
        {
            GumCommands.Self.FileCommands.TryAutoSaveElement(elementToSave);
        }
    }

    private bool ApplyEditVariableOnElement(ElementSave element, string oldName, string newName, string type)
    {
        var changed = false;
        var allStates = element.AllStates;

        foreach (var state in allStates)
        {
            foreach (var variable in state.Variables)
            {
                if (variable.Name == oldName)
                {
                    variable.Name = newName;
                    if (variable.Type != type)
                    {
                        variable.Type = type;
                        // todo - convert:
                        variable.Value = null;
                    }
                    changed = true;
                }
            }
        }



        return changed;
    }


}
