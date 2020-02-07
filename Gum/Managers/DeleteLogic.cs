using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Gui.Windows;
using Gum.Plugins;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gum.Managers
{
    public class DeleteLogic : Singleton<DeleteLogic>
    {
        public void HandleDelete()
        {
            var handled = SelectionManager.Self.TryHandleDelete();
            if(!handled)
            {
                DoDeletingLogic();
            }

        }

        private void DoDeletingLogic()
        {
            object objectDeleted = null;
            DeleteOptionsWindow optionsWindow = null;

            var selectedElement = SelectedState.Self.SelectedElement;
            var selectedInstance = SelectedState.Self.SelectedInstance;

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
                        // This will delete all references to this, meaning, all 
                        // instances attached to the deleted object will be detached, 
                        // but we don't want that, we want to only do that if the user wants to do it, which 
                        // will be handled in a plugin
                        //Gum.ToolCommands.ElementCommands.Self.RemoveInstance(instance, selectedElement);
                        selectedElement.Instances.Remove(selectedInstance);
                        var instanceName = selectedInstance.Name;

                        selectedElement.Events.RemoveAll(item => item.GetSourceObject() == instanceName);

                        foreach(var state in selectedElement.AllStates)
                        {
                            state.Variables.RemoveAll(item => item.SourceObject == instanceName);
                        }

                        // March 17, 2019
                        // Let's also delete
                        // any variables referencing
                        // this object
                        var objectName = selectedInstance.Name;
                        

                        PluginManager.Self.InstanceDelete(selectedElement, selectedInstance);
                        if (SelectedState.Self.SelectedInstance == selectedInstance)
                        {
                            SelectedState.Self.SelectedInstance = null;
                        }

                        RefreshAndSaveAfterInstanceRemoval(selectedElement);
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
            if (objectDeleted != null)
            {
                PluginManager.Self.DeleteConfirm(optionsWindow, objectDeleted);
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

            if(deletableInstances.Any() && instancesFromBase.Any())
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

                    RefreshAndSaveAfterInstanceRemoval(selectedElement);
                }
            }
            else if(instancesFromBase.Any())
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

                    foreach (var instance in instancesToRemove)
                    {
                        Gum.ToolCommands.ElementCommands.Self.RemoveInstance(instance,
                            selectedElement);
                    }

                    RefreshAndSaveAfterInstanceRemoval(selectedElement);
                }
            }
        }

        private static void RefreshAndSaveAfterInstanceRemoval(ElementSave selectedElement)
        {
            if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
            {
                ProjectManager.Self.SaveElement(selectedElement);
            }
            ElementSave elementToReselect = selectedElement;
            // Deselect before selecting the new
            // selected element and before refreshing everything
            SelectionManager.Self.Deselect();

            SelectedState.Self.SelectedInstance = null;
            SelectedState.Self.SelectedElement = elementToReselect;


            GumCommands.Self.GuiCommands.RefreshElementTreeView(selectedElement);
            WireframeObjectManager.Self.RefreshAll(true);

            SelectionManager.Self.Refresh();
        }

        public void RemoveStateCategory(StateSaveCategory category, IStateCategoryListContainer stateCategoryListContainer)
        {
            // This category can only be removed if no behaviors require it
            var behaviorsNeedingCategory = GetBehaviorsNeedingCategory(category, stateCategoryListContainer as ComponentSave);

            if (behaviorsNeedingCategory.Any())
            {
                string message =
                    "This category cannot be removed because it is needed by the following behavior(s):";

                foreach (var behavior in behaviorsNeedingCategory)
                {
                    message += "\n" + behavior.Name;
                }

                MessageBox.Show(message);
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
            var stateCategoryListContainer = 
                SelectedState.Self.SelectedStateContainer as IStateCategoryListContainer;

            stateCategoryListContainer.Categories.Remove(category);

            if(SelectedState.Self.SelectedElement != null)
            {
                var element = SelectedState.Self.SelectedElement;

                foreach (var state in element.AllStates)
                {
                    for(int i= state.Variables.Count - 1; i > -1; i--)
                    {
                        var variable = state.Variables[i];

                        if(variable.Type == category.Name + "State")
                        {
                            state.Variables.RemoveAt(i);
                        }
                    }
                }
            }

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

            StateTreeViewManager.Self.RefreshUI(SelectedState.Self.SelectedStateContainer);
            PropertyGridManager.Self.RefreshUI();
            WireframeObjectManager.Self.RefreshAll(true);
            SelectionManager.Self.Refresh();
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
    }
}
