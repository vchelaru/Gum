using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.Plugins;
using Gum.Managers;
using Gum.Undo;
using CommonFormsAndControls;
using Gum.Controls;
using System.Drawing;
using System.Windows.Documents.DocumentStructures;

namespace Gum.Logic;

#region Enums

public enum NameChangeAction
{
    Move,
    Rename
}

#endregion

public class VariableChange
{
    public IStateCategoryListContainer Container;
    public StateSaveCategory Category;
    public StateSave State;
    public VariableSave Variable;
    public object NewValue;
}

public class RenameLogic
{
    static bool isRenamingXmlFile;

    #region StateSave

    public static void RenameState(StateSave stateSave, StateSaveCategory category, string newName)
    {
        if (!NameVerifier.Self.IsStateNameValid(newName, category, stateSave, out string whyNotValid))
        {
            GumCommands.Self.GuiCommands.ShowMessage(whyNotValid);
        }
        else
        {
            using (UndoManager.Self.RequestLock())
            {
                string oldName = stateSave.Name;

                stateSave.Name = newName;
                GumCommands.Self.GuiCommands.RefreshStateTreeView();
                // I don't think we need to save the project when renaming a state:
                //GumCommands.Self.FileCommands.TryAutoSaveProject();

                // Renaming the state should refresh the property grid
                // because it displays the state name at the top
                GumCommands.Self.GuiCommands.RefreshVariables(force: true);

                PluginManager.Self.StateRename(stateSave, oldName);

                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }
        }
    }

    #endregion

    #region Category

    public static void AskToRenameStateCategory(StateSaveCategory category, ElementSave elementSave)
    {
        // This category can only be renamed if no behaviors require it
        var behaviorsNeedingCategory = DeleteLogic.Self.GetBehaviorsNeedingCategory(category, elementSave as ComponentSave);

        if (behaviorsNeedingCategory.Any())
        {
            string message =
                $"The category {category.Name} cannot be renamed because it is needed by the following behavior(s):";

            foreach (var behavior in behaviorsNeedingCategory)
            {
                message += "\n" + behavior.Name;
            }

            MessageBox.Show(message);
        }
        else
        {
            CustomizableTextInputWindow tiw = new();
            tiw.Message = "Enter new category name";
			tiw.Title = "New Category";
            tiw.Width = 600;

            tiw.Result = category.Name;
            string oldName = category.Name;
            var changes = GetVariableChangesForCategoryRename(elementSave, category, oldName);

            if(changes.Count > 0)
            {
                tiw.Message += "\n\nThe following variables will be affected:";
                foreach (var change in changes)
                {
                    var containerDisplay = change.Container is ElementSave changeElementSave
                        ? changeElementSave.Name
                        : change.Container.ToString();

                    tiw.Message += $"\n  {change.Variable.Name} in {containerDisplay}";
                }
            }


            if (tiw.ShowDialog() is true)
            {
                string newName = tiw.Result;
                RenameLogic.RenameCategory(elementSave, category, oldName, newName, changes);
            }
        }
    }

    public static void RenameCategory(IStateCategoryListContainer owner, StateSaveCategory category, string oldName, string newName, List<VariableChange> variableChanges)
    {
        using (UndoManager.Self.RequestLock())
        {
            category.Name = newName;


            HashSet<ElementSave> elementsWithChangedVariables = new HashSet<ElementSave>();

            foreach(var change in variableChanges)
            {
                var containerElement = change.Container as ElementSave;
                if(containerElement != null)
                {
                    elementsWithChangedVariables.Add(change.Container as ElementSave);
                }
                change.Variable.Type = newName;
                if (change.Variable.GetRootName() == $"{oldName}State")
                {
                    if(string.IsNullOrEmpty(change.Variable.SourceObject))
                    {
                        change.Variable.Name = $"{newName}State";
                    }
                    else
                    {
                        change.Variable.Name = $"{change.Variable.SourceObject}.{newName}State";
                    }
                }
            }

            GumCommands.Self.GuiCommands.RefreshStateTreeView();
            // I don't think we need to save the project when renaming a state:
            //GumCommands.Self.FileCommands.TryAutoSaveProject();

            PluginManager.Self.CategoryRename(category, oldName);

            GumCommands.Self.FileCommands.TryAutoSaveCurrentObject();

            if(owner is ElementSave ownerAsElementSave)
            {
                StandardElementsManagerGumTool.Self.FixCustomTypeConverters(ownerAsElementSave);
            }

            foreach (var item in elementsWithChangedVariables)
            {
                StandardElementsManagerGumTool.Self.FixCustomTypeConverters(item);

                GumCommands.Self.FileCommands.TryAutoSaveElement(item);
            }
        }
    }

    private static List<VariableChange> GetVariableChangesForCategoryRename(IStateCategoryListContainer owner, StateSaveCategory category, string oldName)
    {
        List<VariableChange> toReturn = new List<VariableChange>();

        var project = GumState.Self.ProjectState.GumProjectSave;

        var ownerAsElement = owner as ElementSave;

        List<ElementSave>? inheritingElements = new List<ElementSave> ();
        if (ownerAsElement != null)
        {
            inheritingElements.Add(ownerAsElement);
            inheritingElements.AddRange(ObjectFinder.Self.GetElementsInheritingFrom(ownerAsElement));
        }

        // This currently only handles categories in elements, not behaviors
        foreach (var screen in project.Screens)
        {
            RenameReferencesInElement(category, oldName, inheritingElements, screen);
        }

        foreach (var component in project.Components)
        {
            RenameReferencesInElement(category, oldName, inheritingElements, component);
        }

        // Standards cannot include instances so no need to loop through them

        return toReturn;

        void RenameReferencesInElement(StateSaveCategory changedCategory, string oldName, ICollection<ElementSave> inheritingElements, ElementSave element)
        {
            // If the element inherits from the owner of the category and if the screen has a variable of this type, change it.
            foreach (var state in element.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    var variableType = variable.Type;

                    if (variableType == oldName)
                    {
                        if (string.IsNullOrEmpty(variable.SourceObject))
                        {
                            if (inheritingElements.Contains(element))
                            {
                                AddVariableToList(element, toReturn, state, variable);
                            }
                        }
                        else
                        {
                            // only do it if the instance is in the inheritance chain
                            var instance = element.GetInstance(variable.SourceObject);
                            if(instance != null)
                            {
                                var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                                if (inheritingElements?.Contains(instanceElement) == true)
                                {
                                    AddVariableToList(element, toReturn, state, variable);
                                }
                            }
                        }
                    }
                }
            }

            static void AddVariableToList(ElementSave element, List<VariableChange> toReturn, StateSave state, VariableSave variable)
            {
                var category = element.Categories.FirstOrDefault(item => item.States.Contains(state));

                toReturn.Add(new VariableChange
                {
                    Container = element,

                    Category = category,
                    State = state,
                    Variable = variable
                });
            }
        }
    }

    #endregion

    #region Element

    public static void HandleRename(IInstanceContainer instanceContainer, InstanceSave instance, string oldName, NameChangeAction action, bool askAboutRename = true)
    {
        try
        {
            isRenamingXmlFile = instance == null;

            bool shouldContinue = true;

            shouldContinue = ValidateWithPopup(instanceContainer, instance, shouldContinue);


            var elementSave = instanceContainer as ElementSave;
            shouldContinue = AskIfToRenameElement(oldName, askAboutRename, action, shouldContinue);

            if (shouldContinue)
            {
                if(elementSave != null)
                {
                    RenameAllReferencesTo(elementSave, instance, oldName);
                }

                // Even though this gets called from the PropertyGrid methods which eventually
                // save this object, we want to force a save here to make sure it worked.  If it
                // does, then we're safe to delete the old files.
                GumCommands.Self.FileCommands.TryAutoSaveObject(instanceContainer);

                if (isRenamingXmlFile)
                {
                    RenameXml(elementSave, oldName);
                }

                GumCommands.Self.GuiCommands.RefreshElementTreeView(instanceContainer);
            }

            if (!shouldContinue && isRenamingXmlFile)
            {
                elementSave.Name = oldName;
            }
            else if (!shouldContinue && instance != null)
            {
                instance.Name = oldName;
            }
        }
        catch (Exception e)
        {
            MessageBox.Show("Error renaming instance container " + instanceContainer.ToString() + "\n\n" + e.ToString());
        }
        finally
        {
            isRenamingXmlFile = false;
        }
    }

    private static void RenameXml(ElementSave elementSave, string oldName)
    {
        // If we got here that means all went okay, so we should delete the old files
        var oldXml = elementSave.GetFullPathXmlFile(oldName);
        var newXml = elementSave.GetFullPathXmlFile();

        // Delete the XML.
        // If the file doesn't
        // exist, no biggie - we
        // were going to delete it
        // anyway.
        if (oldXml.Exists())
        {
            System.IO.File.Delete(oldXml.FullPath);
        }

        PluginManager.Self.ElementRename(elementSave, oldName);

        GumCommands.Self.FileCommands.TryAutoSaveProject();

        var oldDirectory = oldXml.GetDirectoryContainingThis();
        var newDirectory = newXml.GetDirectoryContainingThis();

        bool didMoveToNewDirectory = oldDirectory != newDirectory;

        if (didMoveToNewDirectory)
        {
            // refresh the entire tree view because the node is moving:
            GumCommands.Self.GuiCommands.RefreshElementTreeView();
        }
        else
        {
            GumCommands.Self.GuiCommands.RefreshElementTreeView(elementSave);
        }
    }

    private static void RenameAllReferencesTo(ElementSave elementSave, InstanceSave instance, string oldName)
    {
        var project = ProjectManager.Self.GumProjectSave;
        // Tell the GumProjectSave to react to the rename.
        // This changes the names of the ElementSave references.
        project.ReactToRenamed(elementSave, instance, oldName);

        project.SortElementAndBehaviors();

        GumCommands.Self.FileCommands.TryAutoSaveProject();

        if (instance == null)
        {
            foreach (var screen in ProjectState.Self.GumProjectSave.Screens)
            {
                bool shouldSave = false;

                if (screen.BaseType == oldName)
                {
                    screen.BaseType = elementSave.Name;
                    shouldSave = true;
                }

                foreach (var instanceInScreen in screen.Instances)
                {
                    if (instanceInScreen.BaseType == oldName)
                    {
                        instanceInScreen.BaseType = elementSave.Name;
                        shouldSave = true;
                    }

                }

                foreach (var variable in screen.DefaultState.Variables.Where(item => item.GetRootName() == "Contained Type"))
                {
                    if (variable.Value as string == oldName)
                    {
                        variable.Value = elementSave.Name;
                    }
                }

                if (shouldSave)
                {
                    GumCommands.Self.FileCommands.TryAutoSaveElement(screen);
                }
            }

            foreach (var component in ProjectState.Self.GumProjectSave.Components)
            {
                bool shouldSave = false;
                if (component.BaseType == oldName)
                {
                    component.BaseType = elementSave.Name;
                    shouldSave = true;
                }

                foreach (var instancesInElement in component.Instances)
                {
                    if (instancesInElement.BaseType == oldName)
                    {
                        instancesInElement.BaseType = elementSave.Name;
                        shouldSave = true;
                    }
                }

                foreach (var variable in component.DefaultState.Variables.Where(item => item.GetRootName() == "Contained Type"))
                {
                    if (variable.Value as string == oldName)
                    {
                        variable.Value = elementSave.Name;
                    }
                }

                if (shouldSave)
                {
                    GumCommands.Self.FileCommands.TryAutoSaveElement(component);
                }
            }

        }
        if (instance != null)
        {
            string newName = SelectedState.Self.SelectedInstance.Name;

            foreach (StateSave stateSave in SelectedState.Self.SelectedElement.AllStates)
            {
                stateSave.ReactToInstanceNameChange(instance, oldName, newName);
            }

            foreach (var eventSave in SelectedState.Self.SelectedElement.Events)
            {
                if (eventSave.GetSourceObject() == oldName)
                {
                    eventSave.Name = instance.Name + "." + eventSave.GetRootName();
                }
            }

            var renamedDefaultChildContainer = false;
            foreach (var state in SelectedState.Self.SelectedElement.AllStates)
            {
                var variable = state.Variables.FirstOrDefault(item => item.Name == nameof(ComponentSave.DefaultChildContainer));

                if (variable?.Value as string != null)
                {
                    var value = variable.Value as string;
                    if (value == oldName)
                    {
                        variable.Value = newName;
                        renamedDefaultChildContainer = true;
                    }
                }
            }

            if (renamedDefaultChildContainer)
            {
                var elementsToConsider = ObjectFinder.Self.GetElementsReferencing(elementSave);

                foreach (var elementToCheckParent in elementsToConsider)
                {
                    var shouldSaveElement = false;

                    foreach (var state in elementToCheckParent.AllStates)
                    {
                        foreach (var variable in state.Variables)
                        {
                            if (variable.GetRootName() == "Parent" && (variable.Value as string)?.Contains(".") == true)
                            {
                                var value = variable.Value as string;
                                var valueBeforeDot = value.Substring(0, value.IndexOf("."));
                                var valueAfterDot = value.Substring(value.IndexOf(".") + 1);
                                if (valueAfterDot == oldName)
                                {
                                    // let's be safe, see if the instance is of the type elementSave
                                    var parentInstance = elementToCheckParent.GetInstance(valueBeforeDot);
                                    var parentInstanceElement = ObjectFinder.Self.GetElementSave(parentInstance);
                                    if (parentInstanceElement == elementSave)
                                    {
                                        variable.Value = parentInstance.Name + "." + newName;
                                        shouldSaveElement = true;
                                    }
                                }
                            }
                        }
                    }
                    if (shouldSaveElement)
                    {
                        GumCommands.Self.FileCommands.TryAutoSaveElement(elementToCheckParent);
                    }

                }
            }
        }
    }

    private static bool AskIfToRenameElement(string oldName, bool askAboutRename, NameChangeAction action, bool shouldContinue)
    {
        if (shouldContinue && isRenamingXmlFile && askAboutRename)
        {
            string moveOrRename;
            if (action == NameChangeAction.Move)
            {
                moveOrRename = "move";
            }
            else
            {
                moveOrRename = "rename";
            }

            string message = $"Are you sure you want to {moveOrRename} {oldName}?\n\n" +
                "This will change the file name for " + oldName + " which may break " +
                "external references to this object.";
            var result = MessageBox.Show(message, "Rename Object and File?", MessageBoxButtons.YesNo);

            shouldContinue = result == DialogResult.Yes;
        }

        return shouldContinue;
    }

    private static bool ValidateWithPopup(IInstanceContainer instanceContainer, InstanceSave instance, bool shouldContinue)
    {
        if (instance != null)
        {
            string whyNot;
            if (NameVerifier.Self.IsInstanceNameValid(instance.Name, instance, instanceContainer, out whyNot) == false)
            {
                MessageBox.Show(whyNot);
                shouldContinue = false;
            }
        }

        return shouldContinue;
    }

    public static void HandleRename(ElementSave containerElement, EventSave eventSave, string oldName)
    {
        List<ElementSave> elements = new List<ElementSave>();
        elements.AddRange(ProjectManager.Self.GumProjectSave.Screens);
        elements.AddRange(ProjectManager.Self.GumProjectSave.Components);

        foreach (var possibleElement in elements)
        {
            foreach (var instance in possibleElement.Instances.Where(item => item.IsOfType(containerElement.Name)))
            {
                foreach (var eventToRename in possibleElement.Events.Where(item => item.GetSourceObject() == instance.Name))
                {
                    if (eventToRename.GetRootName() == oldName)
                    {
                        eventToRename.Name = instance.Name + "." + eventSave.ExposedAsName;
                    }
                }

            }

        }



    }


    #endregion

    #region Variable

    public static List<VariableChange> GetVariableChangesForRenamedVariable(IStateCategoryListContainer owner, VariableSave variableSave, string oldName)
    {
        List<VariableChange> toReturn = new List<VariableChange>();

        var project = GumState.Self.ProjectState.GumProjectSave;

        var ownerAsElement = owner as ElementSave;

        // consider:
        // Inheritance
        // Instances using this

        List<ElementSave>? inheritingElements = new List<ElementSave> ();
        if (ownerAsElement != null)
        {
            inheritingElements.AddRange(ObjectFinder.Self.GetElementsInheritingFrom(ownerAsElement));
        }

        foreach (var item in inheritingElements)
        {
            foreach (var state in item.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    if (variable.ExposedAsName == oldName)
                    {
                        toReturn.Add(new VariableChange
                        {
                            Container = item,
                            Category = item.Categories.FirstOrDefault(item => item.States.Contains(state)),
                            State = state,
                            Variable = variable
                        });
                    }
                }

            }
        }

        foreach (var element in project.AllElements)
        {
            foreach (var state in element.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    if (variable.GetRootName() == oldName && !string.IsNullOrEmpty(variable.SourceObject))
                    {
                        var instance = element.GetInstance(variable.SourceObject);
                        if (instance != null)
                        {
                            var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                            if (inheritingElements.Contains(instanceElement) || instanceElement == ownerAsElement)
                            {
                                toReturn.Add(new VariableChange
                                {
                                    Container = element,
                                    Category = element.Categories.FirstOrDefault(item => item.States.Contains(state)),
                                    State = state,
                                    Variable = variable
                                });
                            }
                        }
                    }
                }
            }
        }

        return toReturn;
    }


    #endregion
}
