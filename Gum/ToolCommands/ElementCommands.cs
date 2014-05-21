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

        public StateSave AddState(ElementSave elementToAddTo, StateSaveCategory category, string name)
        {
            if (elementToAddTo == null)
            {
                throw new Exception("Could not add state named " + name + " because no element is selected");
            }

            StateSave stateSave = new StateSave();
            stateSave.Name = name;
            AddState(elementToAddTo, category, stateSave);
            return stateSave;
        }

        public void AddState(ElementSave elementToAddTo, StateSaveCategory category, StateSave stateSave)
        {
            stateSave.ParentContainer = elementToAddTo;

            if (category == null)
            {
                elementToAddTo.States.Add(stateSave);
            }
            else
            {
                category.States.Add(stateSave);
            }
        }

        public StateSaveCategory AddCategory(ElementSave elementToAddTo, string name)
        {
            if (elementToAddTo == null)
            {
                throw new Exception("Could not add category " + name + " because no element is selected");
            }



            StateSaveCategory category = new StateSaveCategory();
            category.Name = name;

            elementToAddTo.Categories.Add(category);

            elementToAddTo.DefaultState.Variables.Add(new VariableSave() { Name = category.Name + "State", Type = "string", Value = null
#if GUM
,             CustomTypeConverter = new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(category.Name)
#endif    
            });

            return category;
        }

        public void RemoveState(StateSave stateSave, ElementSave elementToRemoveFrom)
        {
            elementToRemoveFrom.States.Remove(stateSave);

            foreach (var category in elementToRemoveFrom.Categories.Where(item => item.States.Contains(stateSave)))
            {
                category.States.Remove(stateSave);
            }

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }

        public void RemoveStateCategory(StateSaveCategory category, ElementSave elementToRemoveFrom)
        {
            elementToRemoveFrom.Categories.Remove(category);

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

        }

        public void RemoveInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom)
        {
            if (!elementToRemoveFrom.Instances.Contains(instanceToRemove))
            {
                throw new Exception("Could not find the instance " + instanceToRemove.Name + " in " + elementToRemoveFrom.Name);
            }

            elementToRemoveFrom.Instances.Remove(instanceToRemove);

            foreach (StateSave stateSave in elementToRemoveFrom.AllStates)
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

            elementToRemoveFrom.Events.RemoveAll(item => item.GetSourceObject() == instanceToRemove.Name);


            PluginManager.Self.InstanceDelete(elementToRemoveFrom, instanceToRemove);

            if (SelectedState.Self.SelectedInstance == instanceToRemove)
            {
                SelectedState.Self.SelectedInstance = null;
            }
        }

        #endregion
    }
}
