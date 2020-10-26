using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ToolsUtilities;

namespace Gum.Logic
{
    #region Copy Type

    public enum CopyType
    {
        InstanceOrElement = 1,
        State = 2,
    }

    #endregion

    static class CopyPasteLogic
    {
        #region Fields/Properties

        static List<InstanceSave> mCopiedInstances = new List<InstanceSave>();
        static StateSave mCopiedState = new StateSave();
        static ElementSave mCopiedElement = null;

        static CopyType mCopyType;

        #endregion

        public static void OnCopy(CopyType copyType)
        {
            StoreCopiedObject(copyType);
        }

        public static void OnCut(CopyType copyType)
        {
            StoreCopiedObject(copyType);

            ElementSave sourceElement = SelectedState.Self.SelectedElement;

            if(mCopiedInstances.Any())
            {
                foreach(var clone in mCopiedInstances)
                {
                    // copied instances is a clone, so need to find by name:
                    var originalForCopy = sourceElement.Instances.FirstOrDefault(item => item.Name == clone.Name);
                    if (sourceElement.Instances.Contains(originalForCopy))
                    {
                        ElementCommands.Self.RemoveInstance(originalForCopy, sourceElement);
                    }
                }

                if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
                {
                    ProjectManager.Self.SaveElement(sourceElement);
                }
                WireframeObjectManager.Self.RefreshAll(true);
                PropertyGridManager.Self.RefreshUI();
                GumCommands.Self.GuiCommands.RefreshElementTreeView();
            }

            // todo: need to handle cut Element saves, but I don't want to do it yet due to the danger of losing valid data...


        }

        public static void OnPaste(CopyType copyType)
        {
            // To make sure we didn't copy one type and paste another
            if (mCopyType == copyType)
            {
                if (mCopyType == CopyType.InstanceOrElement)
                {
                    if (mCopiedElement != null)
                    {
                        PasteCopiedElement();

                    }
                    // We need to both duplicate the InstanceSave, but we also need to duplicate all of the variables
                    // that use the copied InstanceSave.
                    else if (mCopiedInstances.Count != 0)
                    {
                        PasteCopiedInstanceSaves();
                    }
                }
                else if (mCopyType == CopyType.State && mCopiedState != null)
                {
                    PastedCopiedState();
                }
            }

        }

        private static void StoreCopiedObject(CopyType copyType)
        {
            mCopyType = copyType;
            mCopiedElement = null;
            mCopiedInstances.Clear();
            mCopiedState.Variables.Clear();
            mCopiedState.VariableLists.Clear();

            if (copyType == CopyType.InstanceOrElement)
            {
                if (ProjectState.Self.Selected.SelectedInstances.Count() != 0)
                {
                    StoreCopiedInstances();
                }
                else if (ProjectState.Self.Selected.SelectedElement != null)
                {
                    StoreCopiedElementSave();
                }
            }
            else if (copyType == CopyType.State)
            {
                StoreCopiedState();
            }
        }

        private static void StoreCopiedState()
        {
            if (SelectedState.Self.SelectedStateSave != null)
            {
                mCopiedState = SelectedState.Self.SelectedStateSave.Clone();
            }
        }

        private static void StoreCopiedInstances()
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                var element = SelectedState.Self.SelectedElement;

                List<InstanceSave> selected = new List<InstanceSave>();
                // When copying we want to grab all instances in the order that they are in their container.
                // That way when they're pasted they are pasted in the right order
                foreach (var instance in SelectedState.Self.SelectedInstances.OrderBy(item => element.Instances.IndexOf(item)))
                {
                    
                    selected.Add(instance.Clone());
                }

                mCopiedInstances =
                        GetAllInstancesAndChildrenOf(selected, selected.FirstOrDefault()?.ParentContainer);

                mCopiedState = SelectedState.Self.SelectedStateSave?.Clone() ?? SelectedState.Self.SelectedElement.DefaultState.Clone();

                // Clear out any variables that don't pertain to the selected instance:
                for (int i = mCopiedState.Variables.Count - 1; i > -1; i--)
                {
                    if (mCopiedInstances.Any(item => item.Name == mCopiedState.Variables[i].SourceObject) == false)
                    {
                        mCopiedState.Variables.RemoveAt(i);
                    }
                }

                // And also any VariableLists:
                for (int i = mCopiedState.VariableLists.Count - 1; i > -1; i--)
                {
                    if (mCopiedInstances.Any(item => item.Name == mCopiedState.VariableLists[i].SourceObject) == false)
                    {
                        mCopiedState.VariableLists.RemoveAt(i);
                    }
                }
            }
        }

        private static void StoreCopiedElementSave()
        {
            if (SelectedState.Self.SelectedElement != null)
            {
                if (SelectedState.Self.SelectedElement is ScreenSave)
                {
                    mCopiedElement = ((ScreenSave)SelectedState.Self.SelectedElement).Clone();
                }
                else if (SelectedState.Self.SelectedElement is ComponentSave)
                {
                    mCopiedElement = ((ComponentSave)SelectedState.Self.SelectedElement).Clone();
                }
            }
        }



        private static void PasteCopiedInstanceSaves()
        {
            PasteInstanceSaves(mCopiedInstances, mCopiedState, SelectedState.Self.SelectedElement);
        }

        private static void PastedCopiedState()
        {
            ElementSave container = SelectedState.Self.SelectedElement;
            /////////////////////Early Out//////////////////
            if (container == null)
            {
                return;
            }
            //////////////////End Early Out////////////////

            if (container.Categories.Count != 0)
            {
                MessageBox.Show("Pasting into elements with state categories may cause unexpected results.  Please complain on codeplex!");
            }


            StateSave newStateSave = mCopiedState.Clone();

            newStateSave.Variables.RemoveAll(item => item.CanOnlyBeSetInDefaultState);


            newStateSave.ParentContainer = container;

            string name = mCopiedState.Name + "Copy";

            name = StringFunctions.MakeStringUnique(name, container.States.Select(item => item.Name));

            newStateSave.Name = name;

            container.States.Add(newStateSave);

            StateTreeViewManager.Self.RefreshUI(container);



            //SelectedState.Self.SelectedInstance = targetInstance;
            SelectedState.Self.SelectedStateSave = newStateSave;

            GumCommands.Self.FileCommands.TryAutoSaveElement(container);
        }


        public static void PasteInstanceSaves(List<InstanceSave> instancesToCopy, StateSave copiedState, ElementSave targetElement)
        {
            Dictionary<string, string> oldNewNameDictionary = new Dictionary<string, string>();



            List<InstanceSave> newInstances = new List<InstanceSave>();
            foreach (var sourceInstance in instancesToCopy)
            {
                ElementSave sourceElement = sourceInstance.ParentContainer;

                InstanceSave newInstance = sourceInstance.Clone();

                // the original may have been defined in a base component. The new instance will not be
                // derived in the base, so let's get rid of that:
                newInstance.DefinedByBase = false;

                newInstances.Add(newInstance);

                if (targetElement != null)
                {

                    var oldName = newInstance.Name;
                    newInstance.Name = StringFunctions.MakeStringUnique(newInstance.Name, targetElement.Instances.Select(item => item.Name));
                    var newName = newInstance.Name;

                    oldNewNameDictionary[oldName] = newName;

                    targetElement.Instances.Add(newInstance);

                }
            }

            foreach (var sourceInstance in instancesToCopy)
            {
                ElementSave sourceElement = sourceInstance.ParentContainer;
                var newInstance = newInstances.First(item => item.Name == oldNewNameDictionary[sourceInstance.Name]);

                if (targetElement != null)
                { 
                    StateSave stateSave = copiedState;
                    StateSave targetState;
                    // We now have to copy over the states
                    if (targetElement != sourceElement)
                    {
                        if (sourceElement.States.Count != 1)
                        {
                            MessageBox.Show("Only the default state variables will be copied since the source and target elements differ.");
                        }

                        targetState = targetElement.DefaultState;
                    }
                    else
                    {
                        targetState = SelectedState.Self.SelectedStateSave ?? SelectedState.Self.SelectedElement.DefaultState;
                    }

                    // why reverse loop?
                    for (int i = stateSave.Variables.Count - 1; i > -1; i--)
                    {
                        // We may have copied over a group of instances.  If so
                        // the copied state may have variables for multiple instances.
                        // We only want to apply the variables that work for the selected
                        // object.
                        VariableSave sourceVariable = stateSave.Variables[i];
                        if (sourceVariable.SourceObject == sourceInstance.Name)
                        {

                            VariableSave copiedVariable = sourceVariable.Clone();
                            copiedVariable.Name = newInstance.Name + "." + copiedVariable.GetRootName();

                            var valueAsString = copiedVariable.Value as string;

                            if (copiedVariable.GetRootName() == "Parent" && 
                                string.IsNullOrWhiteSpace(valueAsString) == false &&
                                oldNewNameDictionary.ContainsKey(valueAsString))
                            {
                                // this is a parent and it may be attached to a copy, so update the value
                                var newValue = oldNewNameDictionary[valueAsString];
                                copiedVariable.Value = newValue;
                            }

                            // We don't want to copy exposed variables.
                            // If we did, the user would have 2 variables exposed with the same.
                            copiedVariable.ExposedAsName = null;

                            targetState.Variables.Add(copiedVariable);
                        }
                    }
                    // Copy over the VariableLists too
                    for (int i = stateSave.VariableLists.Count - 1; i > -1; i--)
                    {

                        VariableListSave sourceVariableList = stateSave.VariableLists[i];
                        if (sourceVariableList.SourceObject == sourceInstance.Name)
                        {
                            VariableListSave copiedList = sourceVariableList.Clone();
                            copiedList.Name = newInstance.Name + "." + copiedList.GetRootName();

                            targetState.VariableLists.Add(copiedList);
                        }
                    }

                    // This used to be done here when we paste, but now we're
                    // going to remove it when the cut happens - just like text
                    // editors.  Undo will handle this if we mess up.
                    // bool shouldSaveSource = false;
                    //if (mIsCtrlXCut)
                    //{
                    //    if (sourceElement.Instances.Contains(sourceInstance))
                    //    {
                    //        // Not sure why we weren't just using
                    //        // ElementCommands here - maybe an oversight?
                    //        // This should improve things like 
                    //        //sourceElement.Instances.Remove(sourceInstance);

                    //        ElementCommands.Self.RemoveInstance(sourceInstance, sourceElement);
                    //        shouldSaveSource = true;
                    //    }
                    //}

                    newInstance.ParentContainer = targetElement;
                    // We need to call InstanceAdd before we select the new object - the Undo manager expects it
                    // This includes before other managers refresh
                    PluginManager.Self.InstanceAdd(targetElement, newInstance);





                }
            }


            WireframeObjectManager.Self.RefreshAll(true);
            GumCommands.Self.GuiCommands.RefreshElementTreeView(targetElement);
            GumCommands.Self.FileCommands.TryAutoSaveElement(targetElement);
            SelectedState.Self.SelectedInstances = newInstances;


        }

        private static List<InstanceSave> GetAllInstancesAndChildrenOf(List<InstanceSave> explicitlySelectedInstances, ElementSave container)
        {
            List<InstanceSave> listToFill = new List<InstanceSave>();

            foreach(var instance in explicitlySelectedInstances)
            {
                if(listToFill.Any(item => item.Name == instance.Name) == false)
                {
                    listToFill.Add(instance.Clone());

                    FillWithChildrenOf(instance, listToFill, container);
                }
            }

            return listToFill;
        }

        private static void FillWithChildrenOf(InstanceSave instance, List<InstanceSave> listToFill, ElementSave container)
        {
            var defaultState = container.DefaultState;

            foreach(var variable in defaultState.Variables)
            {
                if(variable.GetRootName() == "Parent")
                {
                    var value = variable.Value as string;

                    if(!string.IsNullOrEmpty(value) && value == instance.Name)
                    {
                        var foundObject = container.GetInstance(variable.SourceObject);

                        if(foundObject != null && listToFill.Any(item => item.Name == foundObject.Name) == false)
                        {
                            listToFill.Add(foundObject.Clone());
                            FillWithChildrenOf(foundObject, listToFill, container);
                        }
                    }
                }
            }
        }

        private static void PasteCopiedElement()
        {
            ElementSave toAdd;

            if (mCopiedElement is ScreenSave)
            {
                toAdd = ((ScreenSave)mCopiedElement).Clone();
                toAdd.Initialize(null);
            }
            else
            {
                toAdd = ((ComponentSave)mCopiedElement).Clone();
                ((ComponentSave)toAdd).InitializeDefaultAndComponentVariables();
            }


            List<string> allElementNames = new List<string>();
            allElementNames.AddRange(ProjectState.Self.GumProjectSave.Screens.Select(item => item.Name.ToLowerInvariant()));
            allElementNames.AddRange(ProjectState.Self.GumProjectSave.Components.Select(item => item.Name.ToLowerInvariant()));
            allElementNames.AddRange(ProjectState.Self.GumProjectSave.StandardElements.Select(item => item.Name.ToLowerInvariant()));

            while (allElementNames.Contains(toAdd.Name.ToLowerInvariant()))
            {
                toAdd.Name = StringFunctions.IncrementNumberAtEnd(toAdd.Name);
            }

            if (toAdd is ScreenSave)
            {
                ProjectCommands.Self.AddScreen(toAdd as ScreenSave);
            }
            else
            {
                ProjectCommands.Self.AddComponent(toAdd as ComponentSave);
            }


            GumCommands.Self.GuiCommands.RefreshElementTreeView();

            SelectedState.Self.SelectedElement = toAdd;

            GumCommands.Self.FileCommands.TryAutoSaveElement(toAdd);
            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }


    }
}
