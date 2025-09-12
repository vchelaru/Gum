using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Gum.Dialogs;

public class DisplayReferencesDialog : DialogViewModel
{
    private readonly ISelectedState _selectedState;

    public ElementSave? ElementSave 
    {
        get => Get<ElementSave?>();
        set
        {
            if (Set(value))
            {
                References.Clear();
                if (value is null)
                {
                    return;
                }
                References.AddRange(ObjectFinder.Self.GetElementReferencesToThis(value));
            }
        }
    }

    public TypedElementReference? SelectedReference
    {
        get => Get<TypedElementReference?>();
        set
        {
            if (Set(value))
            {
                OnSelectedReferenceChanged(value);
            }
        }
    }

    public ObservableCollection<TypedElementReference> References { get; } = [];

    [DependsOn(nameof(ElementSave))]
    [DependsOn(nameof(References))]
    public string Message => References.Count > 0 ? $"The following files reference {ElementSave}" : $"{ElementSave} is not referenced by any other Screen/Component";

    public DisplayReferencesDialog(ISelectedState selectedState)
    {
        _selectedState = selectedState;
        NegativeText = null;
    }

    void OnSelectedReferenceChanged(TypedElementReference? newValue)
    {
        object? selectedItem = newValue?.ReferencingObject;

        if (selectedItem is InstanceSave instance)
        {
            _selectedState.SelectedInstance = instance;
        }
        else if (selectedItem is ElementSave selectedElement)
        {
            _selectedState.SelectedElement = selectedElement;
        }
        else if (selectedItem is VariableSave variable)
        {
            ElementSave? foundElement = ObjectFinder.Self.GumProjectSave?.Screens
                .FirstOrDefault(item => item.DefaultState.Variables.Contains(variable));
            if (foundElement == null)
            {
                foundElement = ObjectFinder.Self.GumProjectSave?.Components
                    .FirstOrDefault(item => item.DefaultState.Variables.Contains(variable));
            }
            if (foundElement != null)
            {
                // what's the instance?
                InstanceSave instanceWithVariable = foundElement.GetInstance(variable.SourceObject);

                if (instanceWithVariable != null)
                {
                    _selectedState.SelectedInstance = instanceWithVariable;
                }
            }
        }
        else if (selectedItem is VariableListSave variableListSave && newValue?.OwnerOfReferencingObject is { } foundElement)
        {

            if (string.IsNullOrEmpty(variableListSave.SourceObject))
            {
                _selectedState.SelectedElement = foundElement;
            }
            else
            {
                InstanceSave instanceWithVariable = foundElement.GetInstance(variableListSave.SourceObject);

                if (instanceWithVariable != null)
                {
                    _selectedState.SelectedInstance = instanceWithVariable;
                }
            }
        }
    }
}
