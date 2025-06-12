﻿using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Gui.Windows;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.Responses;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using StateAnimationPlugin.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using GumCommon;
using ToolsUtilities;

namespace Gum.Commands
{
    public class EditCommands
    {
        private ISelectedState _selectedState;

        public EditCommands()
        {
            _selectedState = Locator.GetRequiredService<ISelectedState>();
        }
        #region State

        public void AskToDeleteState(StateSave stateSave, IStateContainer stateContainer)
        {
            var deleteResponse = new DeleteResponse();
            deleteResponse.ShouldDelete = true;
            deleteResponse.ShouldShowMessage = false;

            var behaviorNeedingState = GetBehaviorsNeedingState(stateSave);

            if (behaviorNeedingState.Any())
            {
                deleteResponse.ShouldDelete = false;
                deleteResponse.ShouldShowMessage = true;
                string message =
                    $"The state {stateSave.Name} cannot be removed because it is needed by the following behavior(s):";

                foreach (var behavior in behaviorNeedingState)
                {
                    message += "\n" + behavior.Name;
                }

                deleteResponse.Message = message;

            }

            if (deleteResponse.ShouldDelete && stateSave.ParentContainer?.DefaultState == stateSave)
            {
                string message =
                    "This state cannot be removed because it is the default state.";

                deleteResponse.ShouldDelete = false;
                deleteResponse.Message = message;
                deleteResponse.ShouldShowMessage = false;
            }

            if (deleteResponse.ShouldDelete)
            {
                deleteResponse = PluginManager.Self.GetDeleteStateResponse(stateSave, stateContainer);
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
                var response = MessageBox.Show($"Are you sure you want to delete the state {stateSave.Name}?", "Delete state?", MessageBoxButtons.YesNo);

                if (response == DialogResult.Yes)
                {
                    DeleteLogic.Self.Remove(stateSave);
                }
            }
        }

        internal void AskToRenameState(StateSave stateSave, IStateContainer stateContainer)
        {
            var behaviorNeedingState = GetBehaviorsNeedingState(stateSave);

            if (behaviorNeedingState.Any())
            {
                string message =
                    $"The state {stateSave.Name} cannot be renamed because it is needed by the following behavior(s):";

                foreach (var behavior in behaviorNeedingState)
                {
                    message += "\n" + behavior.Name;
                }

                MessageBox.Show(message);
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new state name";
                tiw.Title = "Rename state";
                tiw.Result = _selectedState.SelectedStateSave.Name;
                var result = tiw.ShowDialog();

                if (result == DialogResult.OK)
                {
                    var category = stateContainer.Categories.FirstOrDefault(item => item.States.Contains(stateSave));
                    RenameLogic.RenameState(stateSave, category, tiw.Result);
                }
            }
        }

        public void MoveToCategory(string categoryNameToMoveTo, StateSave stateToMove, IStateContainer stateContainer)
        {
            var newCategory = stateContainer.Categories
                .FirstOrDefault(item => item.Name == categoryNameToMoveTo);

            var oldCategory = stateContainer.Categories
                .FirstOrDefault(item => item.States.Contains(stateToMove));
            ////////////////////Early Out //////////////////////
            if (stateToMove == null || categoryNameToMoveTo == null || oldCategory == null)
            {
                return;
            }
            var behaviorsNeedingState = GetBehaviorsNeedingState(stateToMove);
            if (behaviorsNeedingState.Count > 0)
            {
                string message =
                    $"The state {stateToMove.Name} cannot be moved to a different category because it is needed by the following behavior(s):";

                foreach (var behaviorNeedingState in behaviorsNeedingState)
                {
                    message += "\n" + behaviorNeedingState.Name;
                }

                MessageBox.Show(message);
            }

            //////////////////End Early Out /////////////////////




                oldCategory.States.Remove(stateToMove);
            newCategory.States.Add(stateToMove);

            GumCommands.Self.GuiCommands.RefreshStateTreeView();
            _selectedState.SelectedStateSave = stateToMove;

            // make sure to propagate all variables in this new state and
            // also move all existing variables to the new state (use the first)
            if (stateContainer is ElementSave element)
            {
                foreach (var variable in stateToMove.Variables)
                {
                    VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(variable.Name,
                        element, _selectedState.SelectedStateCategorySave);
                }


                var firstState = newCategory.States.FirstOrDefault();
                if (firstState != stateToMove)
                {
                    foreach (var variable in firstState.Variables)
                    {
                        VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(variable.Name,
                            element, _selectedState.SelectedStateCategorySave);
                    }
                }
            }

            PluginManager.Self.StateMovedToCategory(stateToMove, newCategory, oldCategory);

            if (stateContainer is BehaviorSave behavior)
            {
                GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);

            }
            else if (stateContainer is ElementSave asElement)
            {
                GumCommands.Self.FileCommands.TryAutoSaveElement(asElement);
            }
        }


        #endregion

        #region Category

        public void RemoveStateCategory(StateSaveCategory category, IStateContainer stateCategoryListContainer)
        {
            DeleteLogic.Self.RemoveStateCategory(category, stateCategoryListContainer);
        }


        internal void AskToRenameStateCategory(StateSaveCategory category, ElementSave elementSave)
        {
            RenameLogic.AskToRenameStateCategory(category, elementSave);
        }


        #endregion

        #region Behavior

        private List<BehaviorSave> GetBehaviorsNeedingState(StateSave stateSave)
        {
            List<BehaviorSave> toReturn = new List<BehaviorSave>();
            // Try to get the parent container from the state...
            var element = stateSave.ParentContainer;
            if (element == null)
            {
                // ... if we can't find it for some reason, assume it's the current element (is this bad?)
                element = _selectedState.SelectedElement;
            }

            var componentSave = element as ComponentSave;

            if (element != null)
            {
                // uncategorized states can't come from behaviors:
                bool isUncategorized = element.States.Contains(stateSave);
                StateSaveCategory elementCategory = null;

                if (!isUncategorized)
                {
                    elementCategory = element.Categories.FirstOrDefault(item => item.States.Contains(stateSave));
                }

                if (elementCategory != null)
                {
                    var allBehaviorsNeedingCategory = DeleteLogic.Self.GetBehaviorsNeedingCategory(elementCategory, componentSave);

                    foreach (var behavior in allBehaviorsNeedingCategory)
                    {
                        var behaviorCategory = behavior.Categories.First(item => item.Name == elementCategory.Name);

                        bool isStateReferencedInCategory = behaviorCategory.States.Any(item => item.Name == stateSave.Name);

                        if (isStateReferencedInCategory)
                        {
                            toReturn.Add(behavior);
                        }
                    }
                }
            }

            return toReturn;
        }

        public void RemoveBehaviorVariable(BehaviorSave container, VariableSave variable)
        {
            container.RequiredVariables.Variables.Remove(variable);
            GumCommands.Self.FileCommands.TryAutoSaveBehavior(container);
            GumCommands.Self.GuiCommands.RefreshVariables();
        }

        public void AddBehavior()
        {
            if (GumState.Self.ProjectState.NeedsToSaveProject)
            {
                MessageBox.Show("You must first save the project before adding a new component");
                return;
            }

            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new behavior name:";
            tiw.Title = "Add behavior";

            if (tiw.ShowDialog() == DialogResult.OK)
            {
                string name = tiw.Result;

                string whyNotValid;

                NameVerifier.Self.IsBehaviorNameValid(name, null, out whyNotValid);

                if (!string.IsNullOrEmpty(whyNotValid))
                {
                    MessageBox.Show(whyNotValid);
                }
                else
                {
                    var behavior = new BehaviorSave();
                    behavior.Name = name;

                    ProjectManager.Self.GumProjectSave.BehaviorReferences.Add(new BehaviorReference { Name = name });
                    ProjectManager.Self.GumProjectSave.BehaviorReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
                    ProjectManager.Self.GumProjectSave.Behaviors.Add(behavior);
                    ProjectManager.Self.GumProjectSave.Behaviors.Sort((first, second) => first.Name.CompareTo(second.Name));

                    PluginManager.Self.BehaviorCreated(behavior);

                    _selectedState.SelectedBehavior = behavior;

                    GumCommands.Self.FileCommands.TryAutoSaveProject();
                    GumCommands.Self.FileCommands.TryAutoSaveBehavior(behavior);
                }
            }
        }

        #endregion

        #region Element

        public void DuplicateSelectedElement()
        {
            var element = _selectedState.SelectedElement;

            if (element == null)
            {
                MessageBox.Show("You must first save the project before adding a new component");
            }
            else if (element is StandardElementSave)
            {
                MessageBox.Show("Standard Elements cannot be duplicated");
            }
            else if (element is ScreenSave)
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new Screen name:";
                tiw.Title = "Duplicate Screen";

                // todo - handle folders... do we support folders?

                tiw.Result = element.Name + "Copy";

                if (tiw.ShowDialog() == DialogResult.OK)
                {
                    string name = tiw.Result;

                    string whyNotValid;

                    string strippedName = tiw.Result;
                    string prefix = null;
                    if (tiw.Result.Contains("/"))
                    {
                        var indexOfSlash = tiw.Result.LastIndexOf("/");
                        strippedName = tiw.Result.Substring(indexOfSlash + 1);
                        prefix = tiw.Result.Substring(0, indexOfSlash + 1);
                    }

                    NameVerifier.Self.IsElementNameValid(strippedName, null, null, out whyNotValid);

                    if (string.IsNullOrEmpty(whyNotValid))
                    {
                        var newScreen = (element as ScreenSave).Clone();
                        newScreen.Name = prefix + strippedName;
                        newScreen.Initialize(null);
                        StandardElementsManagerGumTool.Self.FixCustomTypeConverters(newScreen);

                        ProjectCommands.Self.AddScreen(newScreen);

                        PluginManager.Self.ElementDuplicate(element, newScreen);
                    }
                    else
                    {
                        MessageBox.Show($"Invalid name for new screen: {whyNotValid}");
                    }
                }
            }
            else if (element is ComponentSave)
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new Component name:";
                tiw.Title = "Duplicate Component";

                FilePath filePath = element.Name;
                var nameWithoutPath = filePath.FileNameNoPath;

                string folder = null;
                if (element.Name.Contains("/"))
                {
                    folder = element.Name.Substring(0, element.Name.LastIndexOf('/'));
                }

                tiw.Result = nameWithoutPath + "Copy";

                if (tiw.ShowDialog() == DialogResult.OK)
                {
                    string name = tiw.Result;

                    string whyNotValid;
                    NameVerifier.Self.IsElementNameValid(tiw.Result, folder, null, out whyNotValid);

                    if (string.IsNullOrEmpty(whyNotValid))
                    {
                        var newComponent = (element as ComponentSave).Clone();
                        if (!string.IsNullOrEmpty(folder))
                        {
                            folder += "/";
                        }
                        newComponent.Name = folder + name;
                        newComponent.Initialize(null);
                        StandardElementsManagerGumTool.Self.FixCustomTypeConverters(newComponent);

                        ProjectCommands.Self.AddComponent(newComponent);

                        PluginManager.Self.ElementDuplicate(element, newComponent);
                    }
                    else
                    {
                        MessageBox.Show($"Invalid name for new component: {whyNotValid}");
                    }
                }
            }

        }

        public void ShowCreateComponentFromInstancesDialog()
        {
            var element = _selectedState.SelectedElement;
            var instances = _selectedState.SelectedInstances.ToList();
            if (instances == null || instances.Count == 0 || element == null)
            {
                MessageBox.Show("You must first save the project before adding a new component");
            }
            else if (instances is List<InstanceSave>)
            {
                CreateComponentWindow createComponentWindow = new CreateComponentWindow();

                FilePath filePath = element.Name;
                var nameWithoutPath = filePath.FileNameNoPath;

                createComponentWindow.Result = $"{nameWithoutPath}Component";
                //tiwcw.Option = $"Replace {nameWithoutPath} and all children with an instance of the new component";

                Nullable<bool> result = createComponentWindow.ShowDialog();

                if (result == true)
                {
                    string name = createComponentWindow.Result;
                    //bool replace = tiwcw.Checked

                    string whyNotValid;
                    NameVerifier.Self.IsElementNameValid(createComponentWindow.Result, "", null, out whyNotValid);

                    if (string.IsNullOrEmpty(whyNotValid))
                    {
                        ComponentSave componentSave = new ComponentSave();
                        componentSave.BaseType = "Container";
                        string folder = null;
                        if (!string.IsNullOrEmpty(folder))
                        {
                            folder += "/";
                        }
                        componentSave.Name = folder + name;

                        StateSave defaultState;

                        // Clone instances
                        foreach (var instance in instances)
                        {
                            // Clone will fail if we are cloning an InstanceSave
                            // in a behavior because its type is BehaviorInstanceSave.
                            // Therefore, we will just manually create a copy:
                            //var instanceSave = instance.Clone();
                            //var instanceSave = instance.Clone();
                            var instanceSave = new InstanceSave();
                            instanceSave.Name = instance.Name;
                            instanceSave.BaseType = instance.BaseType;
                            instanceSave.DefinedByBase = instance.DefinedByBase;
                            instanceSave.Locked = instance.Locked;
                            instanceSave.ParentContainer = componentSave;

                            componentSave.Instances.Add(instanceSave);
                        }

                        // Clone states
                        foreach (var state in element.States)
                        {
                            if (element.DefaultState == state)
                            {
                                defaultState = state.Clone();

                                componentSave.Initialize(defaultState);
                                continue;
                            }
                            componentSave.States.Add(state.Clone());
                        }

                        StandardElementsManagerGumTool.Self.FixCustomTypeConverters(componentSave);
                        ProjectCommands.Self.AddComponent(componentSave);

                    }
                    else
                    {
                        MessageBox.Show($"Invalid name for new component: {whyNotValid}");
                        ShowCreateComponentFromInstancesDialog();
                    }
                }
            }
        }

        public void DisplayReferencesTo(ElementSave element)
        {

            var references = ObjectFinder.Self.GetElementReferencesToThis(element);

            if (references.Count > 0)
            {
                //var stringBuilder = new StringBuilder();
                //stringBuilder.AppendLine($"The following objects reference {element}");
                //foreach(var reference in references)
                //{
                //    stringBuilder.AppendLine(reference.ToString());
                //}

                //GumCommands.Self.GuiCommands.ShowMessage(stringBuilder.ToString());

                ListBoxMessageBox lbmb = new ListBoxMessageBox();
                lbmb.RequiresSelection = true;
                lbmb.Message = $"The following objects reference {element}";
                lbmb.Title = "References";
                lbmb.ItemSelected += (not, used) =>
                {
                    var reference = lbmb.SelectedItem as TypedElementReference;

                    var selectedItem = reference.ReferencingObject;

                    if (selectedItem is InstanceSave instance)
                    {
                        _selectedState.SelectedInstance = instance;
                    }
                    else if (selectedItem is ElementSave selectedElement)
                    {
                        _selectedState.SelectedElement = selectedElement;
                    }
                    else if (selectedItem is VariableSave variable)
                    {
                        ElementSave foundElement = ObjectFinder.Self.GumProjectSave.Screens
                            .FirstOrDefault(item => item.DefaultState.Variables.Contains(variable));
                        if (foundElement == null)
                        {
                            foundElement = ObjectFinder.Self.GumProjectSave.Components
                                .FirstOrDefault(item => item.DefaultState.Variables.Contains(variable));
                        }
                        if (foundElement != null)
                        {
                            // what's the instance?
                            var instanceWithVariable = foundElement.GetInstance(variable.SourceObject);

                            if (instanceWithVariable != null)
                            {
                                _selectedState.SelectedInstance = instanceWithVariable;
                            }
                        }
                    }
                    else if (selectedItem is VariableListSave variableListSave)
                    {
                        var foundElement = reference.OwnerOfReferencingObject;

                        if (foundElement != null)
                        {
                            if (string.IsNullOrEmpty(variableListSave.SourceObject))
                            {
                                _selectedState.SelectedElement = foundElement;
                            }
                            else
                            {
                                var instanceWithVariable = foundElement.GetInstance(variableListSave.SourceObject);

                                if (instanceWithVariable != null)
                                {
                                    _selectedState.SelectedInstance = instanceWithVariable;
                                }
                            }
                        }
                    }
                };
                foreach (var reference in references)
                {
                    lbmb.Items.Add(reference);
                }
                lbmb.HideCancelNoDialog();
                lbmb.Show();

            }
            else
            {
                GumCommands.Self.GuiCommands.ShowMessage($"{element} is not referenced by any other Screen/Component");
            }
        }

        #endregion

        public void DeleteSelection()
        {
            DeleteLogic.Self.HandleDeleteCommand();
        }
    }
}
