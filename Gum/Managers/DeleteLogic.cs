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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Gum.Managers
{
    public class DeleteLogic : Singleton<DeleteLogic>
    {
        private readonly IProjectCommands _projectCommands;
        private readonly ISelectedState _selectedState;
        private readonly IElementCommands _elementCommands;
        private readonly IUndoManager _undoManager;
        private readonly IDialogService _dialogService;
        private readonly IGuiCommands _guiCommands;
        private readonly IFileCommands _fileCommands;

        public DeleteLogic()
        {
            _projectCommands = Locator.GetRequiredService<IProjectCommands>();
            _selectedState = Locator.GetRequiredService<ISelectedState>();
            _undoManager = Locator.GetRequiredService<IUndoManager>();
            _elementCommands = Locator.GetRequiredService<IElementCommands>();
            _dialogService = Locator.GetRequiredService<IDialogService>();
            _guiCommands = Locator.GetRequiredService<IGuiCommands>();
            _fileCommands = Locator.GetRequiredService<IFileCommands>();
        }


        public void HandleDeleteCommand()
        {
            var handled = PluginManager.Self.TryHandleDelete();
            if (!handled)
            {
                DoDeletingLogic();
            }

        }

        private void DoDeletingLogic()
        {
            using (var undoLock = _undoManager.RequestLock())
            {
                Array objectsDeleted = null;
                DeleteOptionsWindow optionsWindow = null;

                var selectedElements = _selectedState.SelectedElements;
                var selectedInstance = _selectedState.SelectedInstance;
                var selectedBehavior = _selectedState.SelectedBehavior;

                var selectedStateContainer = (IStateContainer)selectedElements.FirstOrDefault() ?? selectedBehavior;

                if (_selectedState.SelectedInstances.Count() > 1)
                {
                    AskToDeleteInstances(_selectedState.SelectedInstances);
                }
                else if (selectedInstance != null)
                {
                    var array = new InstanceSave[] { selectedInstance };
                    objectsDeleted = array;

                    if (selectedInstance.DefinedByBase)
                    {
                        MessageBox.Show($"The instance {selectedInstance.Name} cannot be deleted becuase it is defined in a base object.");
                    }
                    else
                    {
                        //DialogResult result =
                        //    MessageBox.Show("Are you sure you'd like to delete " + instance.Name + "?", "Delete instance?", MessageBoxButtons.YesNo);
                        var result = ShowDeleteDialog(array, out optionsWindow);


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
                            var selectedElement = selectedElements.FirstOrDefault();
                            if (selectedElement != null)
                            {
                                selectedElement.Instances.Remove(selectedInstance);
                                selectedElement.Events.RemoveAll(item => item.GetSourceObject() == instanceName);
                            }
                            else if (selectedBehavior != null)
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

                            var deletedSelection = _selectedState.SelectedInstance == selectedInstance;

                            RefreshAndSaveAfterInstanceRemoval(selectedElement, selectedBehavior);

                            if (deletedSelection)
                            {
                                var index = siblings.IndexOf(selectedInstance);
                                if (index + 1 < siblings.Count)
                                {
                                    _selectedState.SelectedInstance = siblings[index + 1];
                                }
                                else if (index > 0)
                                {
                                    _selectedState.SelectedInstance = siblings[index - 1];
                                }
                                else
                                {
                                    // no siblings so select the container or null if none exists:
                                    _selectedState.SelectedInstance = parentInstance;
                                }
                            }
                        }
                        else
                        {
                            objectsDeleted = null;
                        }
                    }
                }
                else if (_selectedState.SelectedElements.Count() > 0)
                {
                    var array = _selectedState.SelectedElements
                        .Where(item => item is not StandardElementSave).ToArray();

                    if (array.Length > 0)
                    {

                        var result = ShowDeleteDialog(array, out optionsWindow);

                        if (result == true)
                        {
                            objectsDeleted = array;

                            foreach(var item in array)
                            {
                                _projectCommands.RemoveElement(item);
                            }
                            _selectedState.SelectedElement = null;
                        }
                    }
                }
                else if (selectedBehavior != null)
                {
                    var array = new[] { selectedBehavior };
                    var result = ShowDeleteDialog(array, out optionsWindow);

                    if (result == true)
                    {
                        objectsDeleted = array;
                        // We need to remove the reference
                        var behavior = _selectedState.SelectedBehavior;
                        _projectCommands.RemoveBehavior(behavior);
                    }
                }

                var shouldDelete = objectsDeleted?.Length > 0;

                if (shouldDelete && selectedInstance != null)
                {
                    shouldDelete = selectedInstance.DefinedByBase == false;
                }

                if (shouldDelete)
                {
                    PluginManager.Self.DeleteConfirm(optionsWindow, objectsDeleted);
                }
            }
        }

        //bool? ShowDeleteMultiple(Array array)
        //{
        //    if(array.Length == 1)
        //    {
        //        ShowDeleteDialog(array[0]);
        //    }
        //}
        bool? ShowDeleteDialog(Array objectsToDelete, out DeleteOptionsWindow optionsWindow)
        {

            string titleText;

            titleText = "Delete?";
            if(objectsToDelete.Length == 1)
            {
                var objectToDelete = objectsToDelete.GetValue(0);
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
            }

            optionsWindow = new DeleteOptionsWindow();
            optionsWindow.Title = titleText;
            optionsWindow.Message = "Are you sure you want to delete:\n";
            foreach(var item in objectsToDelete)
            {
                optionsWindow.Message += item.ToString() + "\n";

            }
                
            optionsWindow.ObjectsToDelete = objectsToDelete;

            // do this in Loaded so it has height
            //_guiCommands.MoveToCursor(optionsWindow);

            PluginManager.Self.ShowDeleteDialog(optionsWindow, objectsToDelete);

            var result = optionsWindow.ShowDialog();


            return result;
        }

        private void AskToDeleteInstances(IEnumerable<InstanceSave> instances)
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
                    ElementSave selectedElement = _selectedState.SelectedElement;
                    foreach (var instance in deletableInstances)
                    {
                        _elementCommands.RemoveInstance(instance, selectedElement);
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
                    ElementSave selectedElement = _selectedState.SelectedElement;

                    // Just in case the argument is a reference to the selected instances:
                    var instancesToRemove = instances.ToList();

                    _elementCommands.RemoveInstances(instancesToRemove, selectedElement);

                    RefreshAndSaveAfterInstanceRemoval(selectedElement, null);
                }
            }
        }

        private void RefreshAndSaveAfterInstanceRemoval(ElementSave selectedElement, BehaviorSave behavior)
        {
            if(selectedElement != null)
            {
                _fileCommands.TryAutoSaveElement(selectedElement);
            }
            else if(behavior != null)
            {
                _fileCommands.TryAutoSaveBehavior(behavior);
            }

            ElementSave elementToReselect = selectedElement;
            BehaviorSave behaviorToReselect = behavior;


            _selectedState.SelectedInstance = null;
            if(selectedElement != null)
            {
                _selectedState.SelectedElement = elementToReselect;
                _guiCommands.RefreshElementTreeView(selectedElement);
            }
            else if(behavior != null)
            {
                _selectedState.SelectedBehavior = behaviorToReselect;
                _guiCommands.RefreshElementTreeView(behavior);
            }

            WireframeObjectManager.Self.RefreshAll(true);
        }

        public void RemoveStateCategory(StateSaveCategory category, IStateContainer stateCategoryListContainer)
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
                    _dialogService.ShowMessage(deleteResponse.Message);
                }
            }
            else
            {
                var response = _dialogService.ShowYesNoMessage($"Are you sure you want to delete the category {category.Name}?", "Delete category?");

                if (response)
                {
                    Remove(category);
                }

            }
        }

        private void Remove(StateSaveCategory category)
        {
            using (_undoManager.RequestLock())
            {

                var stateCategoryListContainer =
                    _selectedState.SelectedStateContainer;

                var isRemovingSelectedCategory = _selectedState.SelectedStateCategorySave == category;

                stateCategoryListContainer.Categories.Remove(category);

                if (_selectedState.SelectedElement != null)
                {
                    var element = _selectedState.SelectedElement;

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
                                _fileCommands.TryAutoSaveElement(ownerOfInstance);
                            }
                        }
                    }
                }

                if(isRemovingSelectedCategory)
                {
                    if(_selectedState.SelectedElement != null)
                    {
                        _selectedState.SelectedStateSave = _selectedState.SelectedElement.DefaultState;
                    }
                }

                _fileCommands.TryAutoSaveCurrentElement();

                _guiCommands.RefreshStateTreeView();
                _guiCommands.RefreshVariables();
                WireframeObjectManager.Self.RefreshAll(true);

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
            bool shouldProgress = TryAskForRemovalConfirmation(stateSave, _selectedState.SelectedElement);
            if (shouldProgress)
            {
                using (_undoManager.RequestLock())
                {
                    var stateCategory = _selectedState.SelectedStateCategorySave;
                    var shouldSelectAfterRemoval = stateSave == _selectedState.SelectedStateSave;
                    int index = stateCategory?.States.IndexOf(stateSave) ?? -1;

                    _elementCommands.RemoveState(stateSave, _selectedState.SelectedStateContainer);
                    PluginManager.Self.StateDelete(stateSave);

                    _guiCommands.RefreshVariables();
                    WireframeObjectManager.Self.RefreshAll(true);

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
                            _selectedState.SelectedStateCategorySave = stateCategory;
                            _selectedState.SelectedStateSave = null;
                        }
                        else if(newIndex != null)
                        {
                            _selectedState.SelectedStateSave = stateCategory.States[newIndex.Value];
                        }
                        else if(_selectedState.SelectedElement != null)
                        {
                            _selectedState.SelectedStateSave = _selectedState.SelectedElement.DefaultState;
                        }
                        else
                        {
                            _selectedState.SelectedStateSave = null;
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

                        mbmb.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

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
