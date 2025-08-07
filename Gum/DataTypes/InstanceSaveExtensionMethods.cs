using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;

namespace Gum.DataTypes
{
    class InstanceStatePair
    {
        public InstanceSave InstanceSave { get; set;}
        public string VariableName { get; set; }
    }
    public static class InstanceSaveExtensionMethods
    {

        // To prevent infinite recursion we need to keep track of states that are being looked up
        static List<InstanceStatePair> mActiveInstanceStatePairs = new List<InstanceStatePair>();

        public static bool IsParentASibling(this InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
            if (instanceSave == null)
            {
                throw new ArgumentException("InstanceSave must not be null");
            }

            RecursiveVariableFinder rvf = new RecursiveVariableFinder(instanceSave, elementStack);

            string parent = rvf.GetValue<string>("Parent");
            bool found = false;
            if (!string.IsNullOrEmpty(parent) && parent != StandardElementsManager.ScreenBoundsName)
            {
                ElementSave parentElement = instanceSave.ParentContainer;

                found = parentElement.Instances.Any(item => item.Name == parent);
            }

            return found;
        }

        public static void Initialize(this InstanceSave instanceSave, ElementSave parent, ref bool wasModified)
        {
            var baseElementType = ObjectFinder.Self.GetRootStandardElementSave(instanceSave);

            if(baseElementType != null)
            {
                StateSave? baseDefault = null;

                baseDefault = StandardElementsManager.Self.TryGetDefaultStateFor(baseElementType.Name);

                // todo - eventually we may want to look at plugins here, but for now we'll do the built-in standards
                if (baseDefault != null)
                {
                    foreach(var state in parent.AllStates)
                    {
                        foreach(var variable in state.Variables)
                        {
                            if(!variable.Name.StartsWith(instanceSave.Name + "."))
                            {
                                // This is a variable that is set by the instance, so we don't want to overwrite it
                                continue;
                            }

                            var foundBaseVariable = baseDefault.Variables.GetVariableSave(variable.GetRootName());

                            if(foundBaseVariable != null)
                            {
                                // compare the types
                                var type = foundBaseVariable.Type;
                                var assignedValue = variable.Value;
                                if(assignedValue is string)
                                {
                                    switch(type)
                                    { 
                                        case "bool":
                                            if(bool.TryParse(assignedValue as string, out bool parsedBool))
                                            {
                                                variable.Value = parsedBool;
                                                wasModified = true;
                                            }
                                            break;
                                        case "int":
                                            if(int.TryParse(assignedValue as string, out int parsedInt))
                                            {
                                                variable.Value = parsedInt;
                                                wasModified = true;
                                            }
                                            break;
                                        case "float":
                                            if(float.TryParse(assignedValue as string, out float parsedFloat))
                                            {
                                                variable.Value = parsedFloat;
                                                wasModified = true;
                                            }
                                            break;
                                        case "double":
                                            if(double.TryParse(assignedValue as string, out double parsedDouble))
                                            {
                                                variable.Value = parsedDouble;
                                                wasModified = true;
                                            }
                                            break;
                                            // add more types here if needed
                                    }
                                }

                            }

                        }
                    }
                }
            }
            // nothing to do currently?

        }

        public static bool IsComponent(this InstanceSave instanceSave)
        {
            ComponentSave baseAsComponentSave = ObjectFinder.Self.GetComponent(instanceSave.BaseType);

            return baseAsComponentSave != null;

        }


        
        public static VariableSave GetVariableFromThisOrBase(this InstanceSave instance,
            ElementWithState parent, string variable)
        {
            var elementStack = new List<ElementWithState> { parent };
            return GetVariableFromThisOrBase(instance, elementStack, new RecursiveVariableFinder(instance, elementStack), variable, false, false);
        }
                
        //public static VariableSave GetVariableFromThisOrBase(this InstanceSave instance,
        //    List<ElementWithState> elementStack, string variable)
        //{
        //    return GetVariableFromThisOrBase(instance,elementStack, new RecursiveVariableFinder(instance, elementStack), variable, false, false);
        //}

        //public static VariableSave GetVariableFromThisOrBase(this InstanceSave instance,
        //    List<ElementWithState> elementStack, string variable, bool forceDefault)
        //{
        //    return GetVariableFromThisOrBase(instance, elementStack, new RecursiveVariableFinder(instance, elementStack), variable, forceDefault, false);
        //}

        public static VariableSave GetVariableFromThisOrBase(this InstanceSave instance,
            List<ElementWithState> elementStack, RecursiveVariableFinder rvf, string variable, bool forceDefault, bool onlyIfSetsValue)
        {
            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

            List<StateSave> statesToPullFrom;
            StateSave defaultState;
            GetStatesToUse(instance, elementStack, forceDefault, instanceBase, rvf, out statesToPullFrom, out defaultState);


            VariableSave variableSave = null;

            // See if the variable is set by the container of the instance:
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
                VariableSave foundVariableSave = TryGetVariableFromStatesOnInstance(instance, variable, instanceBase, statesToPullFrom);

                if (foundVariableSave != null)
                {
                    variableSave = foundVariableSave;
                }
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
                        //variableSave = element.GetVariableFromThisOrBase(variable, forceDefault);
                        StateSave stateToPullFrom = element.DefaultState;
                        variableSave = stateToPullFrom.GetVariableRecursive(variable);
                    }
                }
            }

            return variableSave;

        }

        private static void GetStatesToUse(InstanceSave instance, List<ElementWithState> elementStack, bool forceDefault, ElementSave instanceBase, RecursiveVariableFinder rvf, out List<StateSave> statesToPullFrom, out StateSave defaultState)
        {
            statesToPullFrom = null;
            defaultState = null;

            // October 19, 2023
            // I don't know if this is actually needed anymore. I'm commenting it out so we can move this to GumCommon
            // and my simple tests seem to indicate this is not needed.

            if (elementStack.Count != 0)
            {
                if (elementStack.Last().Element == null)
                {
                    throw new InvalidOperationException("The ElementStack contains an ElementWithState with no Element");
                }
                statesToPullFrom = elementStack.Last().AllStates.ToList();
                defaultState = elementStack.Last().Element.DefaultState;
            }
        }

        private static VariableSave TryGetVariableFromStatesOnInstance(InstanceSave instance, string variable, ElementSave instanceBase, IEnumerable<StateSave> statesToPullFrom)
        {

            string stateVariableName;
            StateSave fallbackState;
            List<StateSave> statesToLoopThrough;

            VariableSave foundVariableSave = null;

            foreach (var stateCategory in instanceBase.Categories)
            {
                stateVariableName = stateCategory.Name + "State";
                fallbackState = null;
                statesToLoopThrough = stateCategory.States;

                foundVariableSave = TryGetVariableFromStateOnInstance(instance, variable, statesToPullFrom, 
                    stateVariableName, fallbackState, statesToLoopThrough);
            }

            if (foundVariableSave == null)
            {
                stateVariableName = "State";
                fallbackState = instanceBase.DefaultState;
                statesToLoopThrough = instanceBase.States;

                foundVariableSave = TryGetVariableFromStateOnInstance(instance, variable, statesToPullFrom, 
                    stateVariableName, fallbackState, statesToLoopThrough);
            }

            return foundVariableSave;
        }

        private static VariableSave TryGetVariableFromStateOnInstance(InstanceSave instance, string variable, IEnumerable<StateSave> statesToPullFrom, string stateVariableName, StateSave fallbackState, List<StateSave> statesToLoopThrough)
        {
            VariableSave foundVariableSave = null;

            // Let's see if this is in a non-default state
            string thisState = null;
            foreach (var stateToPullFrom in statesToPullFrom)
            {
                var foundStateVariable = stateToPullFrom.GetVariableSave(instance.Name + "." + stateVariableName);
                if (foundStateVariable != null && foundStateVariable.SetsValue)
                {
                    thisState = foundStateVariable.Value as string;
                }
            }
            StateSave instanceStateToPullFrom = fallbackState;

            // if thisState is not null, then the state is being explicitly set, so let's try to get that state
            if (!string.IsNullOrEmpty(thisState) && statesToLoopThrough.Any(item => item.Name == thisState))
            {
                instanceStateToPullFrom = statesToLoopThrough.First(item => item.Name == thisState);
            }

            if (instanceStateToPullFrom != null)
            {
                // Eventually use the instanceBase's current state value
                foundVariableSave = instanceStateToPullFrom.GetVariableRecursive(variable);
            }
            return foundVariableSave;
        }



        public static VariableListSave GetVariableListFromThisOrBase(this InstanceSave instance, ElementSave parentContainer, string variable)
        {
            // instanceBase can be null here because the instance could reference a type that has been deleted
            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

            VariableListSave variableListSave = parentContainer?.DefaultState.GetVariableListRecursive(instance.Name + "." + variable);
            if (variableListSave == null)
            {

                variableListSave = instanceBase?.DefaultState.GetVariableListSave(variable);
            }

            if (variableListSave != null && variableListSave.ValueAsIList == null)
            {
                // This can happen if there is a tunneled variable that is null
                VariableListSave possibleVariable = instanceBase.DefaultState.GetVariableListRecursive(variable);
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

        static object GetValueFromThisOrBase(this InstanceSave instance, List<ElementWithState> elementStack, string variable,
            bool forceDefault = false)
        {
            ElementWithState parentContainer = elementStack.Last();
            //VariableSave variableSave = instance.GetVariableFromThisOrBase(parentContainer, variable, forceDefault, true);
            var tempElementStack = new List<ElementWithState> { parentContainer };
            var variableSave = GetVariableFromThisOrBase(instance, tempElementStack, new RecursiveVariableFinder(instance, elementStack), variable, forceDefault, true);


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
