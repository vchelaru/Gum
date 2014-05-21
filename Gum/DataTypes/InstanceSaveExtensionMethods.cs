using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;

#if GUM
using Gum.PropertyGridHelpers.Converters;
using Gum.ToolStates;
#endif

namespace Gum.DataTypes
{
    public static class InstanceSaveExtensionMethods
    {
#if GUM
        public static bool IsParentASibling(this InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
            if (instanceSave == null)
            {
                throw new ArgumentException("InstanceSave must not be null");
            }

            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instanceSave, elementStack);

            string parent = rvf.GetValue<string>("Parent");
            bool found = false;
            if (!string.IsNullOrEmpty(parent) && parent != AvailableInstancesConverter.ScreenBoundsName)
            {
                ElementSave parentElement = instanceSave.ParentContainer;

                found = parentElement.Instances.Any(item => item.Name == parent);
            }

            return found;
        }
#endif
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

            IEnumerable<StateSave> statesToPullFrom = null;
            StateSave defaultState = null;

#if GUM
            if (SelectedState.Self.SelectedElement != null)
            {
                statesToPullFrom = new List<StateSave>{SelectedState.Self.SelectedElement.DefaultState};
                defaultState = SelectedState.Self.SelectedElement.DefaultState;
            }
#endif

            if (elementStack.Count != 0)
            {
                if (elementStack.Last().Element == null)
                {
                    throw new InvalidOperationException("The ElementStack contains an ElementWithState with no Element");
                }
                statesToPullFrom = elementStack.Last().AllStates;
                defaultState = elementStack.Last().Element.DefaultState;
            }


#if GUM
            if (elementStack.Count != 0 && elementStack.Last().Element == SelectedState.Self.SelectedElement && 
                SelectedState.Self.SelectedStateSave != null &&
                !forceDefault)
            {
                statesToPullFrom = new List<StateSave> { SelectedState.Self.SelectedStateSave };
            }
#endif


            VariableSave variableSave = null;
            foreach (var stateToPullFrom in statesToPullFrom)
            {
                var possibleVariable = stateToPullFrom.GetVariableSave(instance.Name + "." + variable);
                if (possibleVariable != null)
                {
                    variableSave = possibleVariable;
                }
            }
            // non-default states can override the default state, so first
            // let's see if the selected state is non-default and has a value
            // for a given variable.  If not, we'll fall back to the default.
            if ((variableSave == null || (onlyIfSetsValue && variableSave.SetsValue == false)) && !statesToPullFrom.Contains(defaultState))
            {
                variableSave = defaultState.GetVariableSave(instance.Name + "." + variable);
            }

            // Still haven't found a variable yet, so look in the instanceBase if one exists
            if ((variableSave == null || 
                (onlyIfSetsValue && (variableSave.SetsValue == false || variableSave.Value == null))) && instanceBase != null)
            {
                // create a new recursive variable finder:
                RecursiveVariableFinder rvf = new RecursiveVariableFinder(instance, elementStack);

                // Let's see if this is in a non-default state
                string thisState = null;
                foreach (var stateToPullFrom in statesToPullFrom)
                {
                    var foundStateVariable = stateToPullFrom.GetVariableSave(instance.Name + ".State");
                    if (foundStateVariable != null && foundStateVariable.SetsValue)
                    {
                        thisState = foundStateVariable.Value as string;
                    }
                }
                StateSave instanceStateToPullFrom = instanceBase.DefaultState;

                // if thisState is not null, then the state is being explicitly set, so let's try to get that state
                if (!string.IsNullOrEmpty(thisState) && instanceBase.AllStates.Any(item => item.Name == thisState))
                {
                    instanceStateToPullFrom = instanceBase.AllStates.First(item => item.Name == thisState);
                }
                // Eventually use the instanceBase's current state value
                variableSave = instanceStateToPullFrom.GetVariableRecursive(variable);

            }

            // I don't think we have to do this because we're going to copy over
            // the variables to all components on load.
            //if (variableSave == null && instanceBase != null && instanceBase is ComponentSave)
            //{
            //    variableSave = StandardElementsManager.Self.DefaultStates["Component"].GetVariableSave(variable);
            //}

            if (variableSave != null && variableSave.Value == null && instanceBase != null && onlyIfSetsValue)
            {
                // This can happen if there is a tunneled variable that is null
                VariableSave possibleVariable = instanceBase.DefaultState.GetVariableSave(variable);
                if (possibleVariable != null && possibleVariable.Value != null && (!onlyIfSetsValue || possibleVariable.SetsValue))
                {
                    variableSave = possibleVariable;
                }
                else if (!string.IsNullOrEmpty(instanceBase.BaseType))
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
            return GetValueFromThisOrBase(instance, new List<ElementWithState>() { new ElementWithState(parent) }, variable, forceDefault);
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

        public static bool IsOfType(this InstanceSave instance, string elementName)
        {
            if (instance.BaseType == elementName)
            {
                return true;
            }
            else
            {
                var baseElement = instance.GetBaseElementSave();

                if (baseElement != null)
                {
                    return baseElement.IsOfType(elementName);

                }
            }

            return false;

        }

    }


}
