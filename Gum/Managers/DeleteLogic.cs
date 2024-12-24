using CommonFormsAndControls.Forms;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Gui.Windows;
using Gum.Plugins;
using Gum.Responses;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Gum.Managers
{
    public class DeleteLogic : Singleton<DeleteLogic>
    {
        public void HandleDeleteCommand()
        {
            var handled = SelectionManager.Self.TryHandleDelete();
            if (!handled)
            {
                DoDeletingLogic();
            }

        }

        private void DoDeletingLogic()
        {
            using (var undoLock = UndoManager.Self.RequestLock())
            {
                object objectDeleted = null;
                DeleteOptionsWindow optionsWindow = null;

                var selectedElement = SelectedState.Self.SelectedElement;
                var selectedInstance = SelectedState.Self.SelectedInstance;
                var selectedBehavior = SelectedState.Self.SelectedBehavior;

                var selectedStateContainer = (IStateContainer)selectedElement ?? selectedBehavior;

                if (SelectedState.Self.SelectedInstances.Count() > 1)
                {
                    AskToDeleteInstances(SelectedState.Self.SelectedInstances);
                }
                else if (selectedInstance != null)
                {
                    objectDeleted = selectedInstance;
                    //AskToDeleteInstance(SelectedState.Self.SelectedInstance);

                    if (selectedInstance.DefinedByBase)
                    {
                        MessageBox.Show($"The instance {selectedInstance.Name} cannot be deleted becuase it is defined in a base object.");
                    }
                    else
                    {
                        //DialogResult result =
                        //    MessageBox.Show("Are you sure you'd like to delete " + instance.Name + "?", "Delete instance?", MessageBoxButtons.YesNo);
                        var result = ShowDeleteDialog(selectedInstance, out optionsWindow);


                        if (result == true)
                        {
                            var siblings = selectedInstance.GetSiblingsIncludingThis();
                            var parentInstance = selectedInstance.GetParentInstance();

                            // This will delete all references to this, meaning, all 
                            // instances attached to the deleted object will be detached, 
                            // but we don't want that, we want to only do that if the user wants to do it, which 
                            // will be handled in a plugin
                            //Gum.ToolCommands.ElementCommands.Self.RemoveInstance(instance, selectedElement);
                            var instanceName = selectedInstance.Name;
                            if(selectedElement != null)
                            {
                                selectedElement.Instances.Remove(selectedInstance);
                                selectedElement.Events.RemoveAll(item => item.GetSourceObject() == instanceName);
                            }
                            else if(selectedBehavior != null)
                            {
                                selectedBehavior.RequiredInstances.Remove(selectedInstance as BehaviorInstanceSave);
                            }

                            // March 17, 2019
                            // Let's also delete
                            // any variables referencing
                            // this object
                            foreach (var state in selectedStateContainer.AllStates)
                            {
                                state.Variables.RemoveAll(item => item.SourceObject == instanceName);
                                state.VariableLists.RemoveAll(item => item.SourceObject == instanceName);
                            }



                            PluginManager.Self.InstanceDelete(selectedElement, selectedInstance);

                            var deletedSelection = SelectedState.Self.SelectedInstance == selectedInstance;

                            RefreshAndSaveAfterInstanceRemoval(selectedElement, selectedBehavior);

                            if (deletedSelection)
                            {
                                var index = siblings.IndexOf(selectedInstance);
                                if (index + 1 < siblings.Count)
                                {
                                    SelectedState.Self.SelectedInstance = siblings[index + 1];
                                }
                                else if (index > 0)
                                {
                                    SelectedState.Self.SelectedInstance = siblings[index - 1];
                                }
                                else
                                {
                                    // no siblings so select the container or null if none exists:
                                    SelectedState.Self.SelectedInstance = parentInstance;
                                }
                            }
                        }
                    }
                }
                else if (SelectedState.Self.SelectedComponent != null)
                {
                    var result = ShowDeleteDialog(SelectedState.Self.SelectedComponent, out optionsWindow);

                    if (result == true)
                    {
                        objectDeleted = SelectedState.Self.SelectedComponent;
                        // We need to remove the reference
                        EditingManager.Self.RemoveSelectedElement();
                    }
                }
                else if (SelectedState.Self.SelectedScreen != null)
                {
                    var result = ShowDeleteDialog(SelectedState.Self.SelectedScreen, out optionsWindow);

                    if (result == true)
                    {
                        objectDeleted = SelectedState.Self.SelectedScreen;
                        // We need to remove the reference
                        EditingManager.Self.RemoveSelectedElement();
                    }
                }
                else if (SelectedState.Self.SelectedBehavior != null)
                {
                    var result = ShowDeleteDialog(SelectedState.Self.SelectedBehavior, out optionsWindow);

                    if (result == true)
                    {
                        objectDeleted = SelectedState.Self.SelectedBehavior;
                        // We need to remove the reference
                        EditingManager.Self.RemoveSelectedBehavior();
                    }
                }

                var shouldDelete = objectDeleted != null;

                if (shouldDelete && selectedInstance != null)
                {
                    shouldDelete = selectedInstance.DefinedByBase == false;
                }

                if (shouldDelete)
                {
                    PluginManager.Self.DeleteConfirm(optionsWindow, objectDeleted);
                }
            }
        }

        bool? ShowDeleteDialog(object objectToDelete, out DeleteOptionsWindow optionsWindow)
        {
            string titleText;
            if (objectToDelete is ComponentSave)
            {
                titleText = "Delete Component?";
            }
            else if (objectToDelete is ScreenSave)
            {
                titleText = "Delete Screen?";
            }
            else if (objectToDelete is InstanceSave)
            {
                titleText = "Delete Instance?";
            }
            else if (objectToDelete is BehaviorSave)
            {
                titleText = "Delete Behavior?";
            }
            else
            {
                titleText = "Delete?";
            }

            optionsWindow = new DeleteOptionsWindow();
            optionsWindow.Title = titleText;
            optionsWindow.Message = "Are you sure you want to delete:\n" + objectToDelete.ToString();
            optionsWindow.ObjectToDelete = objectToDelete;

            GumCommands.Self.GuiCommands.PositionWindowByCursor(optionsWindow);

            PluginManager.Self.ShowDeleteDialog(optionsWindow, objectToDelete);

            var result = optionsWindow.ShowDialog();


            return result;
        }

        private static void AskToDeleteInstances(IEnumerable<InstanceSave> instances)
        {

            var deletableInstances = instances.Where(item => item.DefinedByBase == false).ToArray();
            var instancesFromBase = instances.Except(deletableInstances).ToArray();

            if (deletableInstances.Any() && instancesFromBase.Any())
            {
                // has both
                string message = "Are you sure you'd like to delete the following:";
                foreach (var instance in deletableInstances)
                {
                    message += "\n" + instance.Name;
                }

                message += "\n\nThe following instances will not be deleted because they are defined in a base object:";
                foreach (var instance in instancesFromBase)
                {
                    message += "\n" + instance.Name;
                }

                DialogResult result =
                    MessageBox.Show(message, "Delete instances?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    ElementSave selectedElement = SelectedState.Self.SelectedElement;
                    foreach (var instance in deletableInstances)
                    {
                        Gum.ToolCommands.ElementCommands.Self.RemoveInstance(instance,
                            selectedElement);
                    }

                    RefreshAndSaveAfterInstanceRemoval(selectedElement, null);
                }
            }
            else if (instancesFromBase.Any())
            {
                // only from base
                var message = "All selected instances are defined in a base object, so cannot be deleted";

                MessageBox.Show(message);
            }
            else
            {
                // all can be deleted
                string message = "Are you sure you'd like to delete the following:";
                foreach (var instance in instances)
                {
                    message += "\n" + instance.Name;
                }
                DialogResult result =
                    MessageBox.Show(message, "Delete instances?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    ElementSave selectedElement = SelectedState.Self.SelectedElement;

                    // Just in case the argument is a reference to the selected instances:
                    var instancesToRemove = instances.ToList();

                    Gum.ToolCommands.ElementCommands.Self.RemoveInstances(instancesToRemove,
                        selectedElement);

                    RefreshAndSaveAfterInstanceRemoval(selectedElement, null);
                }
            }
        }

        private static void RefreshAndSaveAfterInstanceRemoval(ElementSave selectedElement, BehaviorSave behavior)
        {
            if(selectedElement != null)
            {
                GumCommands.Self.FileCommands.TryAutoSaveElement(selectedElement);
            }
            else if(behavior != null)
            {
                GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
            }

            ElementSave elementToReselect = selectedElement;
            BehaviorSave behaviorToReselect = behavior;

            // Deselect before selecting the new
            // selected element and before refreshing everything
            SelectionManager.Self.Deselect();

            SelectedState.Self.SelectedInstance = null;
            if(selectedElement != null)
            {
                SelectedState.Self.SelectedElement = elementToReselect;
                GumCommands.Self.GuiCommands.RefreshElementTreeView(selectedElement);
            }
            else if(behavior != null)
            {
                SelectedState.Self.SelectedBehavior = behaviorToReselect;
                GumCommands.Self.GuiCommands.RefreshElementTreeView(behavior);
            }

            WireframeObjectManager.Self.RefreshAll(true);

            SelectionManager.Self.Refresh();
        }

        public void RemoveStateCategory(StateSaveCategory category, IStateCategoryListContainer stateCategoryListContainer)
        {
            var deleteResponse = new DeleteResponse();
            deleteResponse.ShouldDelete = true;
            deleteResponse.ShouldShowMessage = false;

            // This category can only be removed if no behaviors require it
            var behaviorsNeedingCategory = GetBehaviorsNeedingCategory(category, stateCategoryListContainer as ComponentSave);

            if (behaviorsNeedingCategory.Any())
            {
                deleteResponse.ShouldDelete = false;
                deleteResponse.ShouldShowMessage = true;

                string message =
                    $"The category {category.Name} cannot be removed because it is needed by the following behavior(s):";

                foreach (var behavior in behaviorsNeedingCategory)
                {
                    message += "\n" + behavior.Name;
                }

                deleteResponse.Message = message;
            }


            if (deleteResponse.ShouldDelete)
            {
                deleteResponse = PluginManager.Self.GetDeleteStateCategoryResponse(category, stateCategoryListContainer);
            }

            if (deleteResponse.ShouldDelete == false)
            {
                if (deleteResponse.ShouldShowMessage)
                {
                    GumCommands.Self.GuiCommands.ShowMessage(deleteResponse.Message);
                }
            }
            else
            {
                var response = MessageBox.Show($"Are you sure you want to delete the category {category.Name}?", "Delete category?", MessageBoxButtons.YesNo);

                if (response == DialogResult.Yes)
                {
                    Remove(category);
                }

            }
        }

        private void Remove(StateSaveCategory category)
        {
            using (UndoManager.Self.RequestLock())
            {

                var stateCategoryListContainer =
                    SelectedState.Self.SelectedStateContainer as IStateCategoryListContainer;

                var isRemovingSelectedCategory = SelectedState.Self.SelectedStateCategorySave == category;

                stateCategoryListContainer.Categories.Remove(category);

                if (SelectedState.Self.SelectedElement != null)
                {
                    var element = SelectedState.Self.SelectedElement;

                    foreach (var state in element.AllStates)
                    {
                        for (int i = state.Variables.Count - 1; i > -1; i--)
                        {
                            var variable = state.Variables[i];

                            // Modern Gum now has the type match the state
                            //if(variable.Type == category.Name + "State")
                            if (variable.Type == category.Name)
                            {
                                state.Variables.RemoveAt(i);
                            }
                        }
                    }

                    var elementReferences = ObjectFinder.Self.GetElementReferencesToThis(element);

                    foreach (var reference in elementReferences)
                    {
                        if (reference.ReferenceType == ReferenceType.InstanceOfType)
                        {
                            var shouldSave = false;

                            var ownerOfInstance = reference.OwnerOfReferencingObject;
                            var instance = reference.ReferencingObject as InstanceSave;

                            var variableToRemove = $"{instance.Name}.{category.Name}State";

                            foreach (var state in ownerOfInstance.AllStates)
                            {
                                var numberRemoved = state.Variables.RemoveAll(item => item.Name == variableToRemove);

                                if (numberRemoved > 0)
                                {
                                    shouldSave = true;
                                }
                            }

                            if (shouldSave)
                            {
                                GumCommands.Self.FileCommands.TryAutoSaveElement(ownerOfInstance);
                            }
                        }
                    }
                }

                if(isRemovingSelectedCategory)
                {
                    if(SelectedState.Self.SelectedElement != null)
                    {
                        SelectedState.Self.SelectedStateSave = SelectedState.Self.SelectedElement.DefaultState;
                    }
                }

                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

                GumCommands.Self.GuiCommands.RefreshStateTreeView();
                GumCommands.Self.GuiCommands.RefreshVariables();
                WireframeObjectManager.Self.RefreshAll(true);
                SelectionManager.Self.Refresh();

                PluginManager.Self.CategoryDelete(category);
            }
        }

        public List<BehaviorSave> GetBehaviorsNeedingCategory(StateSaveCategory category, ComponentSave componentSave)
        {
            List<BehaviorSave> behaviors = new List<BehaviorSave>();

            if (componentSave != null)
            {
                var behaviorNames = componentSave.Behaviors.Select(item => item.BehaviorName);

                foreach (var behavior in ProjectManager.Self.GumProjectSave.Behaviors.Where(item => behaviorNames.Contains(item.Name)))
                {
                    bool needsCategory = behavior.Categories.Any(item => item.Name == category.Name);

                    if (needsCategory)
                    {
                        behaviors.Add(behavior);
                    }
                }
            }

            return behaviors;
        }

        public void Remove(StateSave stateSave)
        {
            bool shouldProgress = TryAskForRemovalConfirmation(stateSave, SelectedState.Self.SelectedElement);
            if (shouldProgress)
            {
                using (UndoManager.Self.RequestLock())
                {
                    var stateCategory = SelectedState.Self.SelectedStateCategorySave;
                    var shouldSelectAfterRemoval = stateSave == SelectedState.Self.SelectedStateSave;
                    int index = stateCategory?.States.IndexOf(stateSave) ?? -1;

                    ElementCommands.Self.RemoveState(stateSave, SelectedState.Self.SelectedStateContainer);
                    PluginManager.Self.StateDelete(stateSave);
                    GumCommands.Self.GuiCommands.RefreshStateTreeView();
                    GumCommands.Self.GuiCommands.RefreshVariables();
                    WireframeObjectManager.Self.RefreshAll(true);
                    SelectionManager.Self.Refresh();

                    if (shouldSelectAfterRemoval)
                    {
                        int? newIndex = null;
                        if (index != -1)
                        {
                            if (index < stateCategory.States.Count)
                            {
                                newIndex = index;
                            }
                            else if (stateCategory.States.Count > 0)
                            {
                                newIndex = stateCategory.States.Count - 1;
                            }
                        }

                        if(newIndex == null && stateCategory != null)
                        {
                            SelectedState.Self.SelectedStateCategorySave = stateCategory;
                        }
                        else if(newIndex != null)
                        {
                            SelectedState.Self.SelectedStateSave = stateCategory.States[newIndex.Value];
                        }
                        else if(SelectedState.Self.SelectedElement != null)
                        {
                            SelectedState.Self.SelectedStateSave = SelectedState.Self.SelectedElement.DefaultState;
                        }
                        else
                        {
                            SelectedState.Self.SelectedStateSave = null;
                        }
                    }
                }
            }
        }


        private bool TryAskForRemovalConfirmation(StateSave stateSave, ElementSave elementSave)
        {
            bool shouldContinue = true;
            // See if the element is used anywhere

            List<InstanceSave> foundInstances = new List<InstanceSave>();

            if(elementSave != null)
            {
                ObjectFinder.Self.GetElementsReferencing(elementSave, null, foundInstances);
            }

            foreach (var instance in foundInstances)
            {
                // We don't want to go recursively, just top level because
                // I *think* that the lists will include copies of the instances
                // recursively
                ElementSave parent = instance.ParentContainer;

                string variableToLookFor = instance.Name + ".State";

                // loop through all of the states to see if any of the parents' states
                // reference the state that is being removed.
                foreach (var stateInContainer in parent.AllStates)
                {
                    var foundVariable = stateInContainer.Variables.FirstOrDefault(item => item.Name == variableToLookFor);

                    if (foundVariable != null && foundVariable.Value == stateSave.Name)
                    {
                        MultiButtonMessageBox mbmb = new MultiButtonMessageBox();

                        mbmb.StartPosition = System.Windows.Forms.FormStartPosition.Manual;

                        mbmb.Location = new System.Drawing.Point(MainWindow.MousePosition.X - mbmb.Width / 2,
                             MainWindow.MousePosition.Y - mbmb.Height / 2);

                        mbmb.MessageText = "The state " + stateSave.Name + " is used in the element " +
                            elementSave + " in its state " + stateInContainer + ".\n  What would you like to do?";

                        mbmb.AddButton("Do nothing - project may be in an invalid state", System.Windows.Forms.DialogResult.No);
                        mbmb.AddButton("Change variable to default", System.Windows.Forms.DialogResult.OK);
                        // eventually will want to add a cancel option

                        if (mbmb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            foundVariable.Value = "Default";
                        }
                    }
                }
            }

            return shouldContinue;
        }
    }
}
