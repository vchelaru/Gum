using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using Gum.Wireframe;

namespace Gum.DataTypes
{
    public static class InstanceSaveExtensionMethods
    {
        public static bool IsParentASibling(this InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instanceSave, elementStack);

            string parent = rvf.GetValue<string>("Parent");
            bool found = false;
            if (!string.IsNullOrEmpty(parent))
            {
                ElementSave parentElement = instanceSave.ParentContainer;

                found = parentElement.Instances.Any(item => item.Name == parent);
            }

            return found;
        }

        public static void Initialize(this InstanceSave instanceSave)
        {
            // nothing to do currently?

        }

        public static bool IsComponent(this InstanceSave instanceSave)
        {
            ComponentSave baseAsComponentSave = ObjectFinder.Self.GetComponent(instanceSave.BaseType);

            return baseAsComponentSave != null;

        }

        public static VariableSave GetVariableFromThisOrBase(this InstanceSave instance,
            ElementWithState parent, string variable, bool forceDefault = false, bool onlyIfSetsValue = false)
        {
            return GetVariableFromThisOrBase(instance, new List<ElementWithState> { parent }, variable, forceDefault, onlyIfSetsValue);
        }

        public static VariableSave GetVariableFromThisOrBase(this InstanceSave instance, 
            List<ElementWithState> elementStack, string variable, bool forceDefault = false, bool onlyIfSetsValue = false)
        {
            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

            StateSave stateToPullFrom = elementStack.Last().StateSave;
            StateSave defaultState = elementStack.Last().Element.DefaultState;
            if (elementStack.Last().Element == SelectedState.Self.SelectedElement && 
                SelectedState.Self.SelectedStateSave != null &&
                !forceDefault)
            {
                stateToPullFrom = SelectedState.Self.SelectedStateSave;
            }


            VariableSave variableSave = stateToPullFrom.GetVariableSave(instance.Name + "." + variable);
            // non-default states can override the default state, so first
            // let's see if the selected state is non-default and has a value
            // for a given variable.  If not, we'll fall back to the default.
            if ((variableSave == null || (onlyIfSetsValue && variableSave.SetsValue == false))&& defaultState != stateToPullFrom)
            {
                variableSave = defaultState.GetVariableSave(instance.Name + "." + variable);
            }
            if ( (variableSave == null  || (onlyIfSetsValue && variableSave.SetsValue == false)) && instanceBase != null)
            {
                // Eventually use the instanceBase's current state value
                variableSave = instanceBase.DefaultState.GetVariableRecursive(variable);
            }

            // I don't think we have to do this because we're going to copy over
            // the variables to all components on load.
            //if (variableSave == null && instanceBase != null && instanceBase is ComponentSave)
            //{
            //    variableSave = StandardElementsManager.Self.DefaultStates["Component"].GetVariableSave(variable);
            //}

            if (variableSave != null && variableSave.Value == null && instanceBase != null)
            {
                // This can happen if there is a tunneled variable that is null
                VariableSave possibleVariable = instanceBase.DefaultState.GetVariableSave(variable);
                if (possibleVariable != null && possibleVariable.Value != null && (!onlyIfSetsValue || possibleVariable.SetsValue))
                {
                    variableSave = possibleVariable;
                }
                else if(!string.IsNullOrEmpty(instanceBase.BaseType))
                {
                    ElementSave element = ObjectFinder.Self.GetElementSave(instanceBase.BaseType);

                    if (element != null)
                    {
                        variableSave = element.GetVariableFromThisOrBase(variable, forceDefault);
                    }
                }
            }

            return variableSave;

        }

        public static VariableListSave GetVariableListFromThisOrBase(this InstanceSave instance, ElementSave parentContainer, string variable)
        {
            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

            VariableListSave variableListSave = parentContainer.DefaultState.GetVariableListSave(instance.Name + "." + variable);
            if (variableListSave == null)
            {
                variableListSave = instanceBase.DefaultState.GetVariableListSave(variable);
            }

            if (variableListSave != null && variableListSave.ValueAsIList == null)
            {
                // This can happen if there is a tunneled variable that is null
                VariableListSave possibleVariable = instanceBase.DefaultState.GetVariableListSave(variable);
                if (possibleVariable != null && possibleVariable.ValueAsIList != null)
                {
                    variableListSave = possibleVariable;
                }
            }

            return variableListSave;

        }

        public static object GetValueFromThisOrBase(this InstanceSave instance, ElementSave parent, string variable,
            bool forceDefault = false)
        {
            return GetValueFromThisOrBase(instance, new List < ElementWithState >(){ new ElementWithState( parent )}, variable, forceDefault);
        }

        public static object GetValueFromThisOrBase(this InstanceSave instance, List<ElementWithState> elementStack, string variable,
            bool forceDefault = false)
        {
            ElementWithState parentContainer = elementStack.Last();
            VariableSave variableSave = instance.GetVariableFromThisOrBase(parentContainer, variable, forceDefault, true);


            if (variableSave != null)
            {

                return variableSave.Value;
            }
            else
            {
                VariableListSave variableListSave = parentContainer.Element.DefaultState.GetVariableListSave(instance.Name + "." + variable);

                if (variableListSave == null)
                {
                    ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

                    if (instanceBase != null)
                    {
                        variableListSave = instanceBase.DefaultState.GetVariableListSave(variable);
                    }
                }

                if (variableListSave != null)
                {
                    return variableListSave.ValueAsIList;
                }
            }

            // If we get ehre that means there isn't any VariableSave or VariableListSave
            return null;

        }

    }


}
