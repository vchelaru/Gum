using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;

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
        ElementSave mInstanceContainer;
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
            mInstanceContainer = container;
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

                    return mInstanceSave.GetValueFromThisOrBase(mInstanceContainer, variableName);
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
                    return mInstanceSave.GetVariableFromThisOrBase(mInstanceContainer, variableName);
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
                    return mInstanceSave.GetVariableListFromThisOrBase(mInstanceContainer, variableName);
                case VariableContainerType.StateSave:
                    return mStateSave.GetVariableListRecursive(variableName);
                //break;
            }
            throw new NotImplementedException();
        }

    }
}
