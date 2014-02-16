using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using GumDataTypes.Variables;

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

        enum VariableContainerType
        {
            InstanceSave,
            StateSave
        }

        #endregion

        InstanceSave mInstanceSave;
        List<ElementWithState> mElementStack;
        StateSave mStateSave;

        VariableContainerType ContainerType
        {
            get;
            set;
        }

        public RecursiveVariableFinder(InstanceSave instanceSave, ElementSave container)
        {
            if (instanceSave == null)
            {
                throw new ArgumentException("InstanceSave must not be null", "instanceSave");
            }

            ContainerType = VariableContainerType.InstanceSave;
            
            mInstanceSave = instanceSave;

            mElementStack = new List<ElementWithState>() { new ElementWithState( container )};
        }

        public RecursiveVariableFinder(InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
            if (instanceSave == null)
            {
                throw new ArgumentException("InstanceSave must not be null", "instanceSave");
            }

            ContainerType = VariableContainerType.InstanceSave;

            mInstanceSave = instanceSave;
            mElementStack = elementStack;
        }




        public RecursiveVariableFinder(StateSave stateSave)
        {
            ContainerType = VariableContainerType.StateSave;
            mStateSave = stateSave;
        }

        public object GetValue(string variableName)
        {
            switch (ContainerType)
            {
                case VariableContainerType.InstanceSave:
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
                    if (mElementStack.Count != 0)
                    {
                        instanceName = mElementStack.Last().InstanceName;
                    }
                    var allExposed = GetExposedVariablesForThisInstance(mInstanceSave, instanceName, mElementStack);

                    bool onlyIfSetsValue = false;
                    var found = mInstanceSave.GetVariableFromThisOrBase(mElementStack, variableName, false, onlyIfSetsValue);
                    if (found != null && !string.IsNullOrEmpty(found.ExposedAsName))
                    {
                        var exposed = allExposed.FirstOrDefault(item => item.Name == variableName);

                        if (exposed != null && exposed.Value != null)
                        {
                            found = exposed;
                        }
                    }

                    if (found == null || found.SetsValue == false || found.Value == null)
                    {
                        onlyIfSetsValue = true;
                        found = mInstanceSave.GetVariableFromThisOrBase(mElementStack, variableName, false, true);
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
                    return mInstanceSave.GetVariableListFromThisOrBase(mElementStack.Last().Element, variableName);
                case VariableContainerType.StateSave:
                    return mStateSave.GetVariableListRecursive(variableName);
                //break;
            }
            throw new NotImplementedException();
        }

        public List<VariableSave> GetExposedVariablesForThisInstance(DataTypes.InstanceSave instance, string parentInstanceName, List<ElementWithState> elementStack)
        {
            List<VariableSave> exposedVariables = new List<VariableSave>();
            if (elementStack.Count > 1)
            {
                ElementWithState containerOfVariables = elementStack[elementStack.Count - 2];
                ElementWithState definerOfVariables = elementStack[elementStack.Count - 1];

                foreach (VariableSave variable in definerOfVariables.Element.DefaultState.Variables)
                {
                    if (!string.IsNullOrEmpty(variable.ExposedAsName) && variable.SourceObject == instance.Name)
                    {
                        // This variable is exposed, let's see if the container does anything with it

                        VariableSave foundVariable = containerOfVariables.StateSave.GetVariableRecursive(parentInstanceName + "." + variable.ExposedAsName);

                        if (foundVariable != null)
                        {
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
