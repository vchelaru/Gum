using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.ToolStates;

namespace Gum.ToolCommands
{
    public class ElementCommands
    {
        #region Fields

        static ElementCommands mSelf;

        #endregion

        #region Properties

        public static ElementCommands Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ElementCommands();
                }
                return mSelf;
            }
        }

        #endregion

        #region Methods

        public InstanceSave AddInstance(ElementSave elementToAddTo, string name)
        {
            if (elementToAddTo == null)
            {
                throw new Exception("Could not add instance named " + name + " because no element is selected");
            }

            InstanceSave instanceSave = new InstanceSave();
            instanceSave.Name = name;
            instanceSave.ParentContainer = elementToAddTo;
            instanceSave.BaseType = StandardElementsManager.Self.DefaultType;
            elementToAddTo.Instances.Add(instanceSave);

            return instanceSave;
        }

        public StateSave AddState(ElementSave elementToAddTo, string name)
        {
            if (elementToAddTo == null)
            {
                throw new Exception("Could not add state named " + name + " because no element is selected");
            }

            StateSave stateSave = new StateSave();
            stateSave.Name = name;
            stateSave.ParentContainer = elementToAddTo;

            elementToAddTo.States.Add(stateSave);

            return stateSave;
        }

        public void RemoveState(StateSave stateSave, ElementSave elementToRemoveFrom)
        {
            elementToRemoveFrom.States.Remove(stateSave);
        }

        public void RemoveInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom)
        {
            if (!elementToRemoveFrom.Instances.Contains(instanceToRemove))
            {
                throw new Exception("Could not find the instance " + instanceToRemove.Name + " in " + elementToRemoveFrom.Name);
            }

            elementToRemoveFrom.Instances.Remove(instanceToRemove);

            foreach (StateSave stateSave in elementToRemoveFrom.States)
            {
                for (int i = stateSave.Variables.Count - 1; i > -1; i--)
                {
                    if (stateSave.Variables[i].SourceObject == instanceToRemove.Name)
                    {
                        stateSave.Variables.RemoveAt(i);
                    }
                }
                for (int i = stateSave.VariableLists.Count - 1; i > -1; i--)
                {
                    if (stateSave.VariableLists[i].SourceObject == instanceToRemove.Name)
                    {
                        stateSave.VariableLists.RemoveAt(i);
                    }
                }
            }

            PluginManager.Self.InstanceDelete(elementToRemoveFrom, instanceToRemove);

            if (SelectedState.Self.SelectedInstance == instanceToRemove)
            {
                SelectedState.Self.SelectedInstance = null;
            }
        }

        #endregion
    }
}
