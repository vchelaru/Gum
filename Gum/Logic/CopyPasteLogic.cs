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

    public enum TopOrRecursive
    {
        Top,
        Recursive
    }

    public class CopiedData
    {
        /// <summary>
        /// The instances which were selected when the user pressed COPY
        /// </summary>
        public List<InstanceSave> CopiedInstancesSelected = new List<InstanceSave>();
        /// <summary>
        /// The instances which were selected along with all children of the selected instances.
        /// </summary>
        public List<InstanceSave> CopiedInstancesRecursive = new List<InstanceSave>();
        public List<StateSave> CopiedStates = new List<StateSave>();
        public ElementSave CopiedElement = null;
    }

    #endregion

    static class CopyPasteLogic
    {
        #region Fields/Properties

        public static CopiedData CopiedData { get; private set; } = new CopiedData();

        static CopyType mCopyType;

        // If the user copies/pastes, the pasted object
        // becomes the newly-selected instance. A user may
        // want to copy/paste multiple times to create multiple
        // (sibling) instances, but by default this would cause each
        // new instance to be the child of the previously-pasted instance.
        // This is not desirable, so we'll special-case pasting. If the user
        // is pasting on the last pasted object, paste on the parent of the last 
        // pasted object
        static List<InstanceSave> LastPastedInstances = new List<InstanceSave>();

        #endregion

        #region Copy
        public static void OnCopy(CopyType copyType)
        {
            StoreCopiedObject(copyType);
        }


        private static void StoreCopiedObject(CopyType copyType)
        {
            mCopyType = copyType;
            CopiedData.CopiedElement = null;
            CopiedData.CopiedInstancesRecursive.Clear();
            CopiedData.CopiedStates.Clear();

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
                CopiedData.CopiedStates.Clear();
                CopiedData.CopiedStates.Add(SelectedState.Self.SelectedStateSave.Clone());
            }
        }

        private static void StoreCopiedInstances()
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                var element = SelectedState.Self.SelectedElement;

                var state = SelectedState.Self.SelectedStateSave;

                // a state may not be selected if the user selected a category.
                if(state == null)
                {
                    state = element.DefaultState;
                }
                CopiedData.CopiedStates.Clear();
                CopiedData.CopiedStates.Add(state.Clone());

                if(SelectedState.Self.SelectedStateCategorySave != null && SelectedState.Self.SelectedStateSave != null)
                {
                    // it's categorized, so add the default:
                    CopiedData.CopiedStates.Add(element.DefaultState.Clone());
                }

                RecursiveVariableFinder rfv = new RecursiveVariableFinder(state);

                List<InstanceSave> selected = new List<InstanceSave>();
                // When copying we want to grab all instances in the order that they are in their container.
                // That way when they're pasted they are pasted in the right order
                selected.AddRange(SelectedState.Self.SelectedInstances);

                CopiedData.CopiedInstancesSelected.Clear();
                CopiedData.CopiedInstancesSelected.AddRange(SelectedState.Self.SelectedInstances);

                CopiedData.CopiedInstancesRecursive = GetAllInstancesAndChildrenOf(selected, selected.FirstOrDefault()?.ParentContainer)
                    // Sort by index in parent at the end so the children are sorted properly:
                            .OrderBy(item =>
                            {
                                return element.Instances.IndexOf(item);
                            })
                            // clone after doing OrderBy
                            .Select(item => item.Clone())
                            .ToList();


                // Clear out any variables that don't pertain to the selected instance:
                foreach(var copiedState in CopiedData.CopiedStates)
                {
                    for (int i = copiedState.Variables.Count - 1; i > -1; i--)
                    {
                        if (CopiedData.CopiedInstancesRecursive.Any(item => item.Name == copiedState.Variables[i].SourceObject) == false)
                        {
                            copiedState.Variables.RemoveAt(i);
                        }
                    }

                    // And also any VariableLists:
                    for (int i = copiedState.VariableLists.Count - 1; i > -1; i--)
                    {
                        if (CopiedData.CopiedInstancesRecursive.Any(item => item.Name == copiedState.VariableLists[i].SourceObject) == false)
                        {
                            copiedState.VariableLists.RemoveAt(i);
                        }
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
                    CopiedData.CopiedElement = ((ScreenSave)SelectedState.Self.SelectedElement).Clone();
                }
                else if (SelectedState.Self.SelectedElement is ComponentSave)
                {
                    CopiedData.CopiedElement = ((ComponentSave)SelectedState.Self.SelectedElement).Clone();
                }
            }
        }

        #endregion



        public static void OnCut(CopyType copyType)
        {
            StoreCopiedObject(copyType);

            ElementSave sourceElement = SelectedState.Self.SelectedElement;

            if(CopiedData.CopiedInstancesRecursive.Any())
            {
                foreach(var clone in CopiedData.CopiedInstancesRecursive)
                {
                    // copied instances is a clone, so need to find by name:
                    var originalForCopy = sourceElement.Instances.FirstOrDefault(item => item.Name == clone.Name);
                    if (sourceElement.Instances.Contains(originalForCopy))
                    {
                        ElementCommands.Self.RemoveInstance(originalForCopy, sourceElement);
                    }
                }

                GumCommands.Self.FileCommands.TryAutoSaveElement(sourceElement);
                WireframeObjectManager.Self.RefreshAll(true);
                PropertyGridManager.Self.RefreshUI();
                GumCommands.Self.GuiCommands.RefreshElementTreeView();
            }

            // todo: need to handle cut Element saves, but I don't want to do it yet due to the danger of losing valid data...


        }

        public static void OnPaste(CopyType copyType, TopOrRecursive topOrRecursive = TopOrRecursive.Recursive)
        {
            // To make sure we didn't copy one type and paste another
            if (mCopyType == copyType)
            {
                if (mCopyType == CopyType.InstanceOrElement)
                {
                    if (CopiedData.CopiedElement != null)
                    {
                        PasteCopiedElement();

                    }
                    // We need to both duplicate the InstanceSave, but we also need to duplicate all of the variables
                    // that use the copied InstanceSave.
                    else if (CopiedData.CopiedInstancesRecursive.Count != 0)
                    {
                        PasteCopiedInstanceSaves(topOrRecursive);
                    }
                }
                else if (mCopyType == CopyType.State && CopiedData.CopiedStates?.Count > 0)
                {
                    PastedCopiedState();
                }
            }

        }


        private static void PasteCopiedInstanceSaves(TopOrRecursive topOrRecursive)
        {
            if(topOrRecursive == TopOrRecursive.Recursive)
            {
                PasteInstanceSaves(CopiedData.CopiedInstancesRecursive, CopiedData.CopiedStates, SelectedState.Self.SelectedElement, SelectedState.Self.SelectedInstance);
            }
            else
            {
                PasteInstanceSaves(CopiedData.CopiedInstancesSelected, CopiedData.CopiedStates, SelectedState.Self.SelectedElement, SelectedState.Self.SelectedInstance);
            }
        }

        private static void PastedCopiedState()
        {
            ElementSave container = SelectedState.Self.SelectedElement;
            var targetCategory = SelectedState.Self.SelectedStateCategorySave;

            /////////////////////Early Out//////////////////
            if (container == null)
            {
                return;
            }
            //////////////////End Early Out////////////////

            StateSave newStateSave = CopiedData.CopiedStates.First().Clone();

            newStateSave.Variables.RemoveAll(item => item.CanOnlyBeSetInDefaultState);


            newStateSave.ParentContainer = container;

            string name = CopiedData.CopiedStates.First().Name + "Copy";


            if(targetCategory != null)
            {
                name = StringFunctions.MakeStringUnique(name, targetCategory.States.Select(item => item.Name));
                newStateSave.Name = name;

                targetCategory.States.Add(newStateSave);
            }
            else
            {
                name = StringFunctions.MakeStringUnique(name, container.States.Select(item => item.Name));
                newStateSave.Name = name;
                container.States.Add(newStateSave);
            }

            StateTreeViewManager.Self.RefreshUI(container);



            //SelectedState.Self.SelectedInstance = targetInstance;
            SelectedState.Self.SelectedStateSave = newStateSave;

            GumCommands.Self.FileCommands.TryAutoSaveElement(container);
        }


        public static void PasteInstanceSaves(List<InstanceSave> instancesToCopy, List<StateSave> copiedStates, ElementSave targetElement, InstanceSave selectedInstance)
        {
            if(LastPastedInstances.Contains(selectedInstance))
            {
                selectedInstance = selectedInstance?.GetParentInstance();
            }
            Dictionary<string, string> oldNewNameDictionary = new Dictionary<string, string>();

            List<InstanceSave> newInstances = new List<InstanceSave>();

            #region Create the new instance and add it to the target element

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

                    if(targetElement == sourceElement)
                    {
                        var original = sourceElement.Instances.FirstOrDefault(item => item.Name == sourceInstance.Name);
                        int newIndex = -1;
                        if(original != null)
                        {
                            newIndex = sourceElement.Instances.IndexOf(original);
                        }

                        // The user is pasting again, so let's use the index of that:
                        if(LastPastedInstances?.Count > 0 && sourceElement.Instances.ContainsAny(LastPastedInstances))
                        {
                            newIndex = LastPastedInstances.Select(item => sourceElement.Instances.IndexOf(item)).Max();
                        }
                        if(newIndex != -1)
                        {
                            targetElement.Instances.Insert(newIndex+1, newInstance);
                        }
                        else
                        {
                            targetElement.Instances.Add(newInstance);
                        }
                    }
                    else
                    {
                        targetElement.Instances.Add(newInstance);
                    }

                }
            }

            #endregion

            foreach (var sourceInstance in instancesToCopy)
            {
                ElementSave sourceElement = sourceInstance.ParentContainer;

                var isPastingInNewElement = sourceElement != targetElement;
                var isSelectedInstancePartOfCopied = selectedInstance != null && instancesToCopy.Any(item => 
                    item.Name == selectedInstance.Name &&
                    item.ParentContainer == selectedInstance.ParentContainer);

                var shouldAttachToSelectedInstance = isPastingInNewElement || !isSelectedInstancePartOfCopied;
                var newParentName = selectedInstance?.Name;


                var newInstance = newInstances.First(item => item.Name == oldNewNameDictionary[sourceInstance.Name]);

                if (targetElement != null)
                { 
                    foreach(var stateSave in copiedStates)
                    {

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
                            var selectedElement = SelectedState.Self.SelectedElement;

                            targetState = selectedElement.AllStates.FirstOrDefault(item => item.Name == stateSave.Name) ?? 
                                SelectedState.Self.SelectedElement.DefaultState;
                                //SelectedState.Self.SelectedStateSave ?? SelectedState.Self.SelectedElement.DefaultState;

                        }

                        var variablesOnSourceInstance = stateSave.Variables.Where(item => item.SourceObject == sourceInstance.Name).ToArray();
                        // why reverse loop?
                        for (int i = variablesOnSourceInstance.Length - 1; i > -1; i--)
                        {

                            // We may have copied over a group of instances.  If so
                            // the copied state may have variables for multiple instances.
                            // We only want to apply the variables that work for the selected
                            // object.
                            VariableSave sourceVariable = variablesOnSourceInstance[i];

                            VariableSave copiedVariable = sourceVariable.Clone();
                            copiedVariable.Name = newInstance.Name + "." + copiedVariable.GetRootName();

                            var valueAsString = copiedVariable.Value as string;

                            if (copiedVariable.GetRootName() == "Parent" && 
                                string.IsNullOrWhiteSpace(valueAsString) == false)
                            {
                                var valueWithoutSubItem = valueAsString;
                                if(valueWithoutSubItem.Contains("."))
                                {
                                    valueWithoutSubItem = valueWithoutSubItem.Substring(0, valueWithoutSubItem.IndexOf("."));
                                }
                                    
                                if(oldNewNameDictionary.ContainsKey(valueWithoutSubItem))
                                {
                                    // this is a parent and it may be attached to a copy, so update the value
                                    var newValue = oldNewNameDictionary[valueWithoutSubItem];
                                    if(valueAsString.Contains("."))
                                    {
                                        newValue += valueAsString.Substring(valueAsString.IndexOf("."));
                                    }
                                    copiedVariable.Value = newValue;
                                    shouldAttachToSelectedInstance = false;

                                }

                            }
                            // Not sure why we are doing this. If the old contains the key, then attach to that every time (see above)
                            //if(copiedVariable.GetRootName() == "Parent" && shouldAttachToSelectedInstance && selectedInstance == null)
                            //{
                            //    // don't assign it because we're not pasting onto a particular instance and
                            //    // the copied instance already has a parent.
                            //    shouldAttachToSelectedInstance = false;
                            //}

                            // We don't want to copy exposed variables.
                            // If we did, the user would have 2 variables exposed with the same.
                            copiedVariable.ExposedAsName = null;

                            targetState.Variables.Add(copiedVariable);
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

                        if(shouldAttachToSelectedInstance)
                        {
                            targetState.SetValue($"{newInstance.Name}.Parent", newParentName, "string");
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

            LastPastedInstances.Clear();
            LastPastedInstances.AddRange(newInstances);
        }

        private static List<InstanceSave> GetAllInstancesAndChildrenOf(List<InstanceSave> explicitlySelectedInstances, ElementSave container)
        {
            List<InstanceSave> listToFill = new List<InstanceSave>();

            foreach(var instance in explicitlySelectedInstances)
            {
                if(listToFill.Any(item => item.Name == instance.Name) == false)
                {
                    listToFill.Add(instance);

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

                    if(!string.IsNullOrEmpty(value) && (value == instance.Name || value.StartsWith(instance.Name + ".") ))
                    {
                        var foundObject = container.GetInstance(variable.SourceObject);

                        if(foundObject != null && listToFill.Any(item => item.Name == foundObject.Name) == false)
                        {
                            listToFill.Add(foundObject);
                            FillWithChildrenOf(foundObject, listToFill, container);
                        }
                    }
                }
            }
        }

        private static void PasteCopiedElement()
        {
            ElementSave toAdd;

            if (CopiedData.CopiedElement is ScreenSave)
            {
                toAdd = ((ScreenSave)CopiedData.CopiedElement).Clone();
                toAdd.Initialize(null);
            }
            else
            {
                toAdd = ((ComponentSave)CopiedData.CopiedElement).Clone();
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

            PluginManager.Self.ElementDuplicate(CopiedData.CopiedElement, toAdd);

            GumCommands.Self.FileCommands.TryAutoSaveElement(toAdd);
            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }


    }
}
