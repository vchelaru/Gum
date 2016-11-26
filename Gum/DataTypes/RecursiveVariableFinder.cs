using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using GumDataTypes.Variables;
using System.Collections.ObjectModel;

namespace Gum.DataTypes
{
    /// <summary>
    /// Class that can find variables
    /// and values recursively.  There's
    /// so many different ways that this
    /// happens that this consolidates all
    /// logic in one place
    /// </summary>
    public class RecursiveVariableFinder : IVariableFinder
    {
        #region Enums

        public enum VariableContainerType
        {
            InstanceSave,
            StateSave
        }

        #endregion

        #region Fields

        InstanceSave mInstanceSave;
        public List<ElementWithState> ElementStack { get; private set; }
        StateSave mStateSave;

        #endregion

        #region Properties


        public VariableContainerType ContainerType
        {
            get;
            private set;
        }


        #endregion

        public RecursiveVariableFinder(InstanceSave instanceSave, ElementSave container)
        {
            if (instanceSave == null)
            {
                throw new ArgumentException("InstanceSave must not be null", "instanceSave");
            }

            ContainerType = VariableContainerType.InstanceSave;

            mInstanceSave = instanceSave;

            ElementStack = new List<ElementWithState>() { new ElementWithState(container) };
        }

        public RecursiveVariableFinder(InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
            if (instanceSave == null)
            {
                throw new ArgumentException("InstanceSave must not be null", "instanceSave");
            }

            ContainerType = VariableContainerType.InstanceSave;

            mInstanceSave = instanceSave;
            ElementStack = elementStack;
        }

        public RecursiveVariableFinder(StateSave stateSave)
        {
            ContainerType = VariableContainerType.StateSave;
            mStateSave = stateSave;

            ElementStack = new List<ElementWithState>();

#if DEBUG
            if (stateSave.ParentContainer == null)
            {
                throw new NullReferenceException("The state passed in to the RecursiveVariableFinder has a null ParentContainer and it shouldn't");
            }
#endif

            ElementStack.Add(new ElementWithState(stateSave.ParentContainer) { StateName = stateSave.Name });
        }

        public object GetValue(string variableName)
        {
            switch (ContainerType)
            {
                case VariableContainerType.InstanceSave:

#if DEBUG
                    if (ElementStack.Count != 0)
                    {
                        if (ElementStack.Last().Element == null)
                        {
                            throw new InvalidOperationException("The ElementStack contains an ElementWithState with no Element");
                        }
                    }
#endif

                    VariableSave variable = GetVariable(variableName);
                    if (variable != null)
                    {
                        return variable.Value;
                    }
                    else
                    {
                        return null;
                    }

                //return mInstanceSave.GetValueFromThisOrBase(mElementStack, variableName);
                //break;
                case VariableContainerType.StateSave:
                    return mStateSave.GetValueRecursive(variableName);
                //break;
            }

            throw new NotImplementedException();
        }

        public T GetValue<T>(string variableName)
        {
#if DEBUG
            if ( ElementStack.Count != 0)
            {
                if (ElementStack.Last().Element == null)
                {
                    throw new InvalidOperationException("The ElementStack contains an ElementWithState with no Element");
                }
            }
#endif


            object valueAsObject = GetValue(variableName);
            if (valueAsObject is T)
            {
                return (T)valueAsObject;
            }
            else
            {
                return default(T);
            }
        }

        public VariableSave GetVariable(string variableName)
        {
            switch (ContainerType)
            {
                case VariableContainerType.InstanceSave:

                    string instanceName = null;
                    if (ElementStack.Count != 0)
                    {
                        instanceName = ElementStack.Last().InstanceName;
                    }

                    bool onlyIfSetsValue = false;

#if DEBUG
                    if (ElementStack.Count != 0)
                    {
                        if (ElementStack.Last().Element == null)
                        {
                            throw new InvalidOperationException("The ElementStack contains an ElementWithState with no Element");
                        }
                    }
#endif

                    var found = mInstanceSave.GetVariableFromThisOrBase(ElementStack, this, variableName, false, onlyIfSetsValue);
                    if (found != null && !string.IsNullOrEmpty(found.ExposedAsName))
                    {
                        var allExposed = GetExposedVariablesForThisInstance(mInstanceSave, instanceName, ElementStack, variableName);
                        var exposed = allExposed.FirstOrDefault();

                        if (exposed != null && exposed.Value != null)
                        {
                            found = exposed;
                        }
                    }

                    if (found == null || found.SetsValue == false || found.Value == null)
                    {
                        onlyIfSetsValue = true;
                        found = mInstanceSave.GetVariableFromThisOrBase(ElementStack, this, variableName, false, true);
                    }

                    return found;
                case VariableContainerType.StateSave:
                    return mStateSave.GetVariableRecursive(variableName);
                //break;
            }
            throw new NotImplementedException();
        }

        public VariableListSave GetVariableList(string variableName)
        {
            switch (ContainerType)
            {
                case VariableContainerType.InstanceSave:
                    return mInstanceSave.GetVariableListFromThisOrBase(ElementStack.Last().Element, variableName);
                case VariableContainerType.StateSave:
                    return mStateSave.GetVariableListRecursive(variableName);
                //break;
            }
            throw new NotImplementedException();
        }

        public List<VariableSave> GetExposedVariablesForThisInstance(DataTypes.InstanceSave instance, string parentInstanceName, 
            List<ElementWithState> elementStack, string requiredName)
        {
            List<VariableSave> exposedVariables = new List<VariableSave>();
            if (elementStack.Count > 1)
            {
                ElementWithState containerOfVariables = elementStack[elementStack.Count - 2];
                ElementWithState definerOfVariables = elementStack[elementStack.Count - 1];

                foreach (VariableSave variable in definerOfVariables.Element.DefaultState.Variables.Where(
                    item => !string.IsNullOrEmpty(item.ExposedAsName) && item.GetRootName() == requiredName))
                {
                    if (variable.SourceObject == instance.Name)
                    {
                        // This variable is exposed, let's see if the container does anything with it

                        VariableSave foundVariable = containerOfVariables.StateSave.GetVariableRecursive(
                            parentInstanceName + "." + variable.ExposedAsName);

                        if (foundVariable != null)
                        {
                            if (!string.IsNullOrEmpty(foundVariable.ExposedAsName))
                            {
                                // This variable is itself exposed, so we should go up one level to see 
                                // what's going on.
                                var instanceInParent = containerOfVariables.Element.GetInstance(parentInstanceName);
                                var parentparentInstanceName = containerOfVariables.InstanceName;

                                List<ElementWithState> stackWithLastRemoved = new List<ElementWithState>();
                                stackWithLastRemoved.AddRange(elementStack);
                                stackWithLastRemoved.RemoveAt(stackWithLastRemoved.Count - 1);

                                var exposedExposed = GetExposedVariablesForThisInstance(instanceInParent, parentparentInstanceName, 
                                    stackWithLastRemoved,
                                    // This used to be this:
                                    //foundVariable.ExposedAsName
                                    // But it should be this:
                                    variable.ExposedAsName
                                    );

                                if (exposedExposed.Count != 0)
                                {
                                    foundVariable = exposedExposed.First();
                                }

                            }
                            
                            VariableSave variableToAdd = new VariableSave();
                            variableToAdd.Type = variable.Type;
                            variableToAdd.Value = foundVariable.Value;
                            variableToAdd.SetsValue = foundVariable.SetsValue;
                            variableToAdd.Name = variable.Name.Substring(variable.Name.IndexOf('.') + 1);
                            exposedVariables.Add(variableToAdd);
                        }
                    }
                }
            }

            return exposedVariables;
        }
    }
}
