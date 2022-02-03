using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.PropertyGridHelpers;

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

            var otherState = category?.States.FirstOrDefault(item => item != stateSave);
            if(otherState != null)
            {
                foreach(var variable in otherState.Variables)
                {
                    VariableInCategoryPropagationLogic.Self
                        .PropagateVariablesInCategory(variable.Name);
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


            // September 20, 2018
            // Not sure why we have
            // this category add itself
            // as a variable to the default
            // state. States can't set other
            // states, and I don't think the rest
            // of Gum depends on this. Commenting it
            // out to see.
            // Update - even though the element can't set
            // it's own categorized state in the default state,
            // instances use this variable to determine if a variable
            // should be shown.
            if (objectToAddTo is ElementSave)
            {              
                var elementToAddTo = objectToAddTo as ElementSave;
                elementToAddTo.DefaultState.Variables.Add(new VariableSave()
                {
                    Name = category.Name + "State",
                    // We used to set the type with the word "State" appended but why? Gum seems to not do this everywhere, and this can add confusion, so let's omit the "State" suffix
                    Type = category.Name,
                    Value = null
#if GUM
    ,
                    CustomTypeConverter = new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(category.Name)
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

        /// <summary>
        /// Removes the argument instance from the argument elementToRemoveFrom, and detaches any
        /// object that was attached to this parent.
        /// </summary>
        /// <param name="instanceToRemove">The instance to remove.</param>
        /// <param name="elementToRemoveFrom">The element to remove from.</param>
        public void RemoveInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom)
        {
            if (!elementToRemoveFrom.Instances.Contains(instanceToRemove))
            {
                throw new Exception("Could not find the instance " + instanceToRemove.Name + " in " + elementToRemoveFrom.Name);
            }

            elementToRemoveFrom.Instances.Remove(instanceToRemove);

            RemoveParentReferencesToInstance(instanceToRemove, elementToRemoveFrom);

            elementToRemoveFrom.Events.RemoveAll(item => item.GetSourceObject() == instanceToRemove.Name);


            PluginManager.Self.InstanceDelete(elementToRemoveFrom, instanceToRemove);

            if (SelectedState.Self.SelectedInstance == instanceToRemove)
            {
                SelectedState.Self.SelectedInstance = null;
            }
        }

        public void RemoveParentReferencesToInstance(InstanceSave instanceToRemove, ElementSave elementToRemoveFrom)
        {
            foreach (StateSave stateSave in elementToRemoveFrom.AllStates)
            {
                for (int i = stateSave.Variables.Count - 1; i > -1; i--)
                {
                    var variable = stateSave.Variables[i];

                    if (variable.SourceObject == instanceToRemove.Name)
                    {
                        // this is a variable that assigns a value on the removed object. The object
                        // is gone, so the variable should be removed too.
                        stateSave.Variables.RemoveAt(i);
                    }
                    else if (variable.GetRootName() == "Parent" && variable.Value as string == instanceToRemove.Name)
                    {
                        // This is a variable that assigns the Parent to the removed object. Since the object is
                        // gone, the parent value shouldn't be assigned anymore.
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
        }

        #endregion
    }
}
