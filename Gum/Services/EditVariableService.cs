using CommonFormsAndControls;
using ExCSS;
using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.VariableGrid;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.DataTypes;

namespace Gum.Services;


internal interface IEditVariableService
{
    void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateCategoryListContainer stateListCategoryContainer);
}


internal class EditVariableService : IEditVariableService
{
    enum EditMode
    {
        None,
        ExposedName,
        FullEdit
    }

    public void TryAddEditVariableOptions(InstanceMember instanceMember, VariableSave variableSave, IStateCategoryListContainer stateListCategoryContainer)
    {
        if (ShouldAddEditVariableOptions(variableSave, stateListCategoryContainer))
        {
            instanceMember.ContextMenuEvents.Add("Edit Variable", (sender, e) =>
            {
                ShowEditVariableWindow(variableSave, stateListCategoryContainer);
            });
        }
    }

    bool ShouldAddEditVariableOptions(VariableSave variableSave, IStateCategoryListContainer stateListCategoryContainer)
    {
        return GetAvailableEditModeFor(variableSave, stateListCategoryContainer) != EditMode.None;
    }

    EditMode GetAvailableEditModeFor(VariableSave variableSave, IStateCategoryListContainer stateCategoryListContainer)
    {
        if (variableSave == null)
        {
            return EditMode.None;
        }

        var behaviorSave = stateCategoryListContainer as BehaviorSave;
        // for now only edit variables inside of behaviors:
        if (behaviorSave != null)
        {
            return EditMode.FullEdit;
        }

        if (stateCategoryListContainer is ElementSave elementSave)
        {
            //var rootVariable = ObjectFinder.Self.GetRootVariable(variableSave.Name, stateListCategoryContainer as ElementSave);

            var isExposed = !string.IsNullOrEmpty(variableSave.ExposedAsName);

            return isExposed ? EditMode.ExposedName : EditMode.None;
        }

        return EditMode.None;
    }


    private void ShowEditVariableWindow(VariableSave variable, IStateCategoryListContainer container)
    {
        var editmode = GetAvailableEditModeFor(variable, container);

        if (editmode == EditMode.ExposedName)
        {
            ShowEditExposedUi(variable, container);
        }
        else if (editmode == EditMode.FullEdit)
        {
            ShowFullEditUi(variable, container);
        }

    }

    private void ShowEditExposedUi(VariableSave variable, IStateCategoryListContainer container)
    {
        var tiw = new CustomizableTextInputWindow();
        tiw.Message = "Enter new exposed variable name.";
        tiw.Width = 600;

        var changes = RenameLogic.GetVariableChangesForRenamedVariable(container, variable, variable.ExposedAsName);
        if (changes.Count > 0)
        {
            tiw.Message += "\n\nThis will also rename the following variables:";
            foreach (var change in changes)
            {
                var containerName = change.Container.ToString();
                if (change.Container is ElementSave elementSave)
                {
                    containerName = elementSave.Name;
                }
                tiw.Message += $"\n{change.Variable.Name} in {containerName}";
            }

        }
        tiw.Result = variable.ExposedAsName;

        if (tiw.ShowDialog() == true)
        {
            RenameExposedVariable(variable, tiw.Result, container, changes);
        }
    }

    private void RenameExposedVariable(VariableSave variable, string newName, IStateCategoryListContainer container, List<VariableChange> changes)
    {
        var oldName = variable.ExposedAsName;

        variable.ExposedAsName = newName;

        HashSet<ElementSave> changedElements = new HashSet<ElementSave>();

        if(container is ElementSave containerElement)
        {
            changedElements.Add(containerElement);
        }

        foreach (var change in changes)
        {
            var element = change.Container as ElementSave;
            if (element != null)
            {
                changedElements.Add(element);
            }

            if(change.Variable.ExposedAsName == oldName)
            {
                change.Variable.ExposedAsName = newName;
            }
            else if(change.Variable.GetRootName() == oldName)
            {
                var prefix = string.Empty;
                if(change.Variable.SourceObject != null)
                {
                    prefix = change.Variable.SourceObject + ".";
                }

                change.Variable.Name = prefix + newName;
            }
        }

        GumCommands.Self.GuiCommands.RefreshPropertyGrid(force:true);
        foreach(var element in changedElements)
        {
            GumCommands.Self.FileCommands.TryAutoSaveElement(element);
        }

    }

    private void ShowFullEditUi(VariableSave variable, IStateCategoryListContainer container)
    {
        var window = new AddVariableWindow();

        window.SelectedType = variable.Type;
        window.EnteredName = variable.Name;

        var result = window.ShowDialog();

        if (result == true)
        {
            var type = window.SelectedType;
            if (type == null)
            {
                throw new InvalidOperationException("Type cannot be null");
            }
            var newName = window.EnteredName;

            string whyNotValid;
            bool isValid = NameVerifier.Self.IsVariableNameValid(
                newName, out whyNotValid);

            if (!isValid)
            {
                GumCommands.Self.GuiCommands.ShowMessage(whyNotValid);
            }
            else
            {
                var behavior = SelectedState.Self.SelectedBehavior;


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

                    GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
                }
                else if (SelectedState.Self.SelectedElement != null)
                {
                    var oldName = variable.Name;
                    var element = SelectedState.Self.SelectedElement;
                    if (ApplyEditVariableOnElement(element, oldName, newName, type))
                    {
                        GumCommands.Self.FileCommands.TryAutoSaveElement(element);
                    }

                    ApplyChangesToInstances(element, oldName, newName, type);

                    var derivedElements = ObjectFinder.Self.GetElementsInheritingFrom(element);
                    foreach (var derived in derivedElements)
                    {
                        if (ApplyEditVariableOnElement(derived, oldName, newName, type))
                        {
                            GumCommands.Self.FileCommands.TryAutoSaveElement(derived);
                        }

                        ApplyChangesToInstances(derived, oldName, newName, type);
                    }
                }
                GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
            }
        }
    }

    private void ApplyChangesToInstances(ElementSave element, string oldName, string newName, string type)
    {
        var references = ObjectFinder.Self.GetElementReferences(element)
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
