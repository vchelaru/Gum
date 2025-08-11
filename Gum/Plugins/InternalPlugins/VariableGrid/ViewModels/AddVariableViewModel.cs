using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
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
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;

public enum RenameType
{
    NormalName,
    ExposedName
}

public class AddVariableViewModel : DialogViewModel
{
    #region Fields/Properties

    public string Title { get; set; } = "Add Variable";
    public string EnteredName
    {
        get => Get<string>();
        set => Set(value);
    }

    public string DetailText
    {
        get => Get<string>();
        set => Set(value);
    }

    private readonly GuiCommands _guiCommands;
    private readonly ISelectedState _selectedState;
    private readonly UndoManager _undoManager;
    private readonly ElementCommands _elementCommands;
    private readonly FileCommands _fileCommands;
    private readonly NameVerifier _nameVerifier;
    private readonly IDialogService _dialogService;

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

    public ElementSave? Element { get; set; }
    public VariableSave? Variable { get; set; }

    public RenameType RenameType { get; set; }
    public VariableChangeResponse VariableChangeResponse 
    {
        get => Get<VariableChangeResponse>();
        set => Set(value);
    }

    [DependsOn(nameof(VariableChangeResponse))]
    public string TypeChangeMessage => VariableChangeResponse?.VariableReferenceChanges.Count > 0
        ? "This variable is used in a variable reference assignment so its type cannot be changed"
        : string.Empty;

    [DependsOn(nameof(VariableChangeResponse))]
    public bool IsTypeChangeUiEnabled => (VariableChangeResponse?.VariableReferenceChanges.Count > 0) == false;

    #endregion

    public AddVariableViewModel(GuiCommands guiCommands,
        UndoManager undoManager,
        ElementCommands elementCommands,
        FileCommands fileCommands,
        NameVerifier nameVerifier,
        ISelectedState selectedState,
        IDialogService dialogService)
    {
        _guiCommands = guiCommands;
        _undoManager = undoManager;
        _elementCommands = elementCommands;
        _fileCommands = fileCommands;
        _nameVerifier = nameVerifier;
        _selectedState = selectedState;
        _dialogService = dialogService;

        AvailableTypes = new List<string>();
        AvailableTypes.Add("float");
        AvailableTypes.Add("int");
        AvailableTypes.Add("string");
        AvailableTypes.Add("bool");

        SelectedItem = "float";
    }

    protected override void OnAffirmative()
    {
        GeneralResponse response = Validate();
        if (!response.Succeeded)
        {
            _dialogService.ShowMessage(response.Message);
            NegativeCommand.Execute(null);
            return;
        }

        if (Variable is null)
        {
            AddVariableToSelectedItem();
        }
        else
        {
            DoEdit(Variable, VariableChangeResponse);
        }
        base.OnAffirmative();
    }

    public GeneralResponse Validate()
    {
        if (SelectedItem == null)
        {
            return GeneralResponse.UnsuccessfulWith("You must select a type");
        }
        if (string.IsNullOrEmpty(EnteredName))
        {
            return GeneralResponse.UnsuccessfulWith("You must enter a name");
        }

        if (!_nameVerifier.IsVariableNameValid(EnteredName, Element, Variable, out string whyNotValid))
        {
            return GeneralResponse.UnsuccessfulWith(whyNotValid);
        }

        // If it's a new variable, then it won't already exist:
        if(Variable != null)
        {
            // it must either be exposed or a custom variable to be renamed:
            if(Variable.IsCustomVariable == false && string.IsNullOrEmpty(Variable.ExposedAsName) && Element != null)
            {
                return GeneralResponse.UnsuccessfulWith(
                    $"The variable {Variable} cannot be changed because it is not an exposed or custom variable");
            }
        }

        return GeneralResponse.SuccessfulResponse;
    }

    private void AddVariableToSelectedItem()
    {
        var type = SelectedItem;
        var name = EnteredName;

        string whyNotValid;
        bool isValid = _nameVerifier.IsVariableNameValid(
            name, Element, Variable, out whyNotValid);

        if (!isValid)
        {
            _dialogService.ShowMessage(whyNotValid);
        }
        else
        {
            var behavior = _selectedState.SelectedBehavior;

            var newVariable = new VariableSave
            {
                Name = name,
                Type = type,
                Value = DefaultValue
            };

            using var undoLock = _undoManager.RequestLock();

            var element = _selectedState.SelectedElement;
            if (behavior != null)
            {
                behavior.RequiredVariables.Variables.Add(newVariable);
                _elementCommands.SortVariables(behavior);
                _fileCommands.TryAutoSaveBehavior(behavior);
            }
            else if (element != null)
            {
                newVariable.IsCustomVariable = true;
                element.DefaultState.Variables.Add(newVariable);
                _elementCommands.SortVariables(element);
                _fileCommands.TryAutoSaveElement(element);
            }
            _guiCommands.RefreshVariables(force: true);

            PluginManager.Self.VariableAdd(element, name);
        }
    }

    private void DoEdit(VariableSave variable, Logic.VariableChangeResponse changes)
    {
        var type = SelectedItem;
        var newName = EnteredName;

        string whyNotValid;
        bool isValid = _nameVerifier.IsVariableNameValid(
            newName, Element, Variable, out whyNotValid);

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

            // Variables in behaviors cannot have expoed names, so 
            // we don't need to check the RenameType
            variable.Name = newName;

            variable.Type = type;

            _fileCommands.TryAutoSaveBehavior(behavior);
        }
        else if (_selectedState.SelectedElement != null)
        {
            DoEditsToVariableOnElement(variable, _selectedState.SelectedElement, changes, type, newName);
        }
        _guiCommands.RefreshVariables(force: true);
    }

    private void DoEditsToVariableOnElement(VariableSave variable, ElementSave element, Logic.VariableChangeResponse changes, string type, string newName)
    {
        var oldName = RenameType == RenameType.NormalName 
            ? variable.Name 
            : variable.ExposedAsName;

        HashSet<ElementSave> elementsToSave = new HashSet<ElementSave>();
        if (ApplyEditVariableOnElement(element, oldName, newName, type, 
            // If we're changing the variable on this element, we should respect whether it's
            // a full rename or just an exposed rename
            RenameType))
        {
            _elementCommands.SortVariables(element);
            elementsToSave.Add(element);
        }

        ApplyChangesToInstances(element, oldName, newName, type);

        var derivedElements = ObjectFinder.Self.GetElementsInheritingFrom(element);
        // the code to apply these changes was written before we had the VariableChangeResponse. We could
        // migrate the code to using that intead of this if we ever have a problem or need to upgrade it any.
        // Note that chagnes are used below for variable references...
        foreach (var derived in derivedElements)
        {
            if (ApplyEditVariableOnElement(derived, oldName, newName, type, RenameType))
            {
                elementsToSave.Add(derived);
            }

            ApplyChangesToInstances(derived, oldName, newName, type);
        }

        ApplyVariableReferenceChanges(changes, newName, oldName, elementsToSave);

        foreach (var elementToSave in elementsToSave)
        {
            _fileCommands.TryAutoSaveElement(elementToSave);
        }

    }

    public void ApplyVariableReferenceChanges(VariableChangeResponse changes, string newName, string oldName, HashSet<ElementSave> elementsToSave)
    {
        foreach (var referenceChange in changes.VariableReferenceChanges)
        {
            var elementWithReference = referenceChange.Container;
            var variableList = referenceChange.VariableReferenceList;

            var oldLine = variableList.ValueAsIList[referenceChange.LineIndex].ToString();

            // This could be on the left or right side, so check either
            var leftAndRight = oldLine.Split('=').Select(item => item.Trim()).ToArray();

            if (referenceChange.ChangedSide is Logic.SideOfEquals.Left or Logic.SideOfEquals.Both)
            {
                if (leftAndRight[0] == oldName)
                {
                    leftAndRight[0] = newName;
                }
            }

            if (referenceChange.ChangedSide is Logic.SideOfEquals.Right or Logic.SideOfEquals.Both)
            {
                if (leftAndRight[1] == oldName)
                {
                    leftAndRight[1] = newName;
                }
                if (leftAndRight[1].EndsWith("." + oldName))
                {
                    var lengthToTrim = oldName.Length;
                    var newLength = leftAndRight[1].Length - lengthToTrim;
                    leftAndRight[1] = leftAndRight[1].Substring(0, newLength) + newName;
                }
            }
            variableList.ValueAsIList[referenceChange.LineIndex] = $"{leftAndRight[0]}={leftAndRight[1]}";

            elementsToSave.Add(referenceChange.Container);
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

            if (ApplyEditVariableOnElement(reference.OwnerOfReferencingObject, oldFullName, newFullName, type,
                // Instances treat the name as a normal variable, so do a full rename
                RenameType.NormalName))
            {
                elementsToSave.Add(reference.OwnerOfReferencingObject);
            }
        }

        foreach (var elementToSave in elementsToSave)
        {
            _fileCommands.TryAutoSaveElement(elementToSave);
        }
    }

    private bool ApplyEditVariableOnElement(ElementSave element, string oldName, string newName, string type,
        RenameType renameType)
    {
        var changed = false;
        var allStates = element.AllStates;

        foreach (var state in allStates)
        {
            foreach (var variable in state.Variables)
            {
                if ((variable.Name == oldName && renameType == RenameType.NormalName) ||
                    (variable.ExposedAsName == oldName && renameType == RenameType.ExposedName))
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
