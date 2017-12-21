using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Gui.Forms;
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
            object objectDeleted = null;
            DeleteOptionsWindow optionsWindow = null;

            if (SelectedState.Self.SelectedInstances.Count() > 1)
            {
                AskToDeleteInstances(SelectedState.Self.SelectedInstances);
            }
            else if (SelectedState.Self.SelectedInstance != null)
            {
                objectDeleted = SelectedState.Self.SelectedInstance;
                AskToDeleteInstance(SelectedState.Self.SelectedInstance);
            }
            else if (SelectedState.Self.SelectedComponent != null)
            {
                DialogResult result = ShowDeleteDialog(SelectedState.Self.SelectedComponent, out optionsWindow);

                if (result == DialogResult.Yes || result == DialogResult.OK)
                {
                    objectDeleted = SelectedState.Self.SelectedComponent;
                    // We need to remove the reference
                    EditingManager.Self.RemoveSelectedElement();
                }
            }
            else if (SelectedState.Self.SelectedScreen != null)
            {
                DialogResult result = ShowDeleteDialog(SelectedState.Self.SelectedScreen, out optionsWindow);

                if (result == DialogResult.Yes || result == DialogResult.OK)
                {
                    objectDeleted = SelectedState.Self.SelectedScreen;
                    // We need to remove the reference
                    EditingManager.Self.RemoveSelectedElement();
                }
            }
            else if (SelectedState.Self.SelectedBehavior != null)
            {
                DialogResult result = ShowDeleteDialog(SelectedState.Self.SelectedBehavior, out optionsWindow);

                if (result == DialogResult.Yes || result == DialogResult.OK)
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

        DialogResult ShowDeleteDialog(object objectToDelete, out DeleteOptionsWindow optionsWindow)
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
            optionsWindow.Text = titleText;
            optionsWindow.Message = "Are you sure you want to delete:\n" + objectToDelete.ToString();
            optionsWindow.ObjectToDelete = objectToDelete;

            PluginManager.Self.ShowDeleteDialog(optionsWindow, objectToDelete);

            DialogResult result = optionsWindow.ShowDialog();


            return result;
        }


        private static void AskToDeleteInstances(IEnumerable<InstanceSave> instances)
        {
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

        private static void AskToDeleteInstance(InstanceSave instance)
        {
            DialogResult result =
                MessageBox.Show("Are you sure you'd like to delete " + instance.Name + "?", "Delete instance?", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                ElementSave selectedElement = SelectedState.Self.SelectedElement;

                Gum.ToolCommands.ElementCommands.Self.RemoveInstance(instance, selectedElement);

                RefreshAndSaveAfterInstanceRemoval(selectedElement);
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
