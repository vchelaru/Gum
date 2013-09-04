using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using Gum.Wireframe;

namespace Gum.DataTypes
{
    /// <summary>
    /// Class that can find variables
    /// and values recursively.  There's
    /// so many different ways that this
    /// happens that this consolidates all
    /// logic in one place
    /// </summary>
    public class RecursiveVariableFinder
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
            ContainerType = VariableContainerType.InstanceSave;

            mInstanceSave = instanceSave;

            mElementStack = new List<ElementWithState>() { new ElementWithState( container )};
        }

        public RecursiveVariableFinder(InstanceSave instanceSave, List<ElementWithState> elementStack)
        {
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

                    var allExposed = WireframeObjectManager.Self.GetExposedVariablesForThisInstance(mInstanceSave, mElementStack.Last().InstanceName, mElementStack);

                    var found = mInstanceSave.GetVariableFromThisOrBase(mElementStack, variableName);
                    if (found != null && !string.IsNullOrEmpty(found.ExposedAsName))
                    {
                        var exposed = allExposed.FirstOrDefault(item => item.Name == variableName);

                        if (exposed != null && exposed.Value != null)
                        {
                            found = exposed;
                        }
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

    }
}
