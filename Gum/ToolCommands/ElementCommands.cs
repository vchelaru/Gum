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
            // elementToAddTo may be null if category is not null
            if (elementToAddTo == null && category == null)
            {
                throw new Exception("Could not add state named " + name + " because no element is selected");
            }

            StateSave stateSave = new StateSave();
            stateSave.Name = name;
            AddState(elementToAddTo, category, stateSave);

            var otherState = category.States.FirstOrDefault(item => item != stateSave);
            if(otherState != null)
            {
                foreach(var variable in otherState.Variables)
                {
                    PropertyGridHelpers.SetVariableLogic.Self.PropagateVariablesInCategory(variable.Name);
                }
            }

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

        public StateSaveCategory AddCategory(IStateCategoryListContainer objectToAddTo, string name)
        {
            if (objectToAddTo == null)
            {
                throw new Exception("Could not add category " + name + " because no element or behavior is selected");
            }



            StateSaveCategory category = new StateSaveCategory();
            category.Name = name;

            objectToAddTo.Categories.Add(category);

            string categoryName = category.Name + "State";

            if(objectToAddTo is ElementSave)
            {
                var elementToAddTo = objectToAddTo as ElementSave;
                elementToAddTo.DefaultState.Variables.Add(new VariableSave() { Name = categoryName, Type = categoryName, Value = null
    #if GUM
    ,             CustomTypeConverter = new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(category.Name)
    #endif    
                });

                elementToAddTo.DefaultState.Variables.Sort((first, second) => first.Name.CompareTo(second.Name));
            }



            return category;
        }

        public void RemoveState(StateSave stateSave, IStateContainer elementToRemoveFrom)
        {
            
            elementToRemoveFrom.UncategorizedStates.Remove(stateSave);

            foreach (var category in elementToRemoveFrom.Categories.Where(item => item.States.Contains(stateSave)))
            {
                category.States.Remove(stateSave);
            }

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }

        public void RemoveStateCategory(StateSaveCategory category, IStateCategoryListContainer elementToRemoveFrom)
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
