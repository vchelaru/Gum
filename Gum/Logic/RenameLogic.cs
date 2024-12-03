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

namespace Gum.Logic
{
    #region Enums

    public enum NameChangeAction
    {
        Move,
        Rename
    }

    #endregion

    public class RenameLogic
    {
        static bool isRenamingXmlFile;

        public static void RenameState(StateSave stateSave, string newName)
        {
            using(UndoManager.Self.RequestLock())
            {
                string oldName = stateSave.Name;

                stateSave.Name = newName;
                GumCommands.Self.GuiCommands.RefreshStateTreeView();
                // I don't think we need to save the project when renaming a state:
                //GumCommands.Self.FileCommands.TryAutoSaveProject();

                // Renaming the state should refresh the property grid
                // because it displays the state name at the top
                GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);

                PluginManager.Self.StateRename(stateSave, oldName);

                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }
        }

        public static void RenameCategory(StateSaveCategory category, string oldName, string newName)
        {
            using (UndoManager.Self.RequestLock())
            {
                category.Name = newName;

                GumCommands.Self.GuiCommands.RefreshStateTreeView();
                // I don't think we need to save the project when renaming a state:
                //GumCommands.Self.FileCommands.TryAutoSaveProject();

                PluginManager.Self.CategoryRename(category, oldName);

                GumCommands.Self.FileCommands.TryAutoSaveCurrentObject();
            }
        }

        public static void HandleRename(ElementSave elementSave, InstanceSave instance, string oldName, NameChangeAction action, bool askAboutRename = true)
        {
            try
            {
                isRenamingXmlFile = instance == null;

                bool shouldContinue = true;

                shouldContinue = ValidateWithPopup(elementSave, instance, shouldContinue);

                shouldContinue = AskIfToRename(oldName, askAboutRename, action, shouldContinue);

                if (shouldContinue)
                {

                    RenameAllReferencesTo(elementSave, instance, oldName);

                    // Even though this gets called from the PropertyGrid methods which eventually
                    // save this object, we want to force a save here to make sure it worked.  If it
                    // does, then we're safe to delete the old files.
                    GumCommands.Self.FileCommands.TryAutoSaveElement(elementSave);

                    if (isRenamingXmlFile)
                    {
                        RenameXml(elementSave, oldName);
                    }

                    GumCommands.Self.GuiCommands.RefreshElementTreeView(elementSave);
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
                MessageBox.Show("Error renaming element " + elementSave.ToString() + "\n\n" + e.ToString());
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

            if(instance == null)
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

                    foreach(var variable in screen.DefaultState.Variables.Where(item => item.GetRootName() == "Contained Type"))
                    {
                        if(variable.Value as string == oldName)
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
                    if(component.BaseType == oldName)
                    {
                        component.BaseType = elementSave.Name;
                        shouldSave = true;
                    }

                    foreach (var instanceInScreen in component.Instances)
                    {
                        if (instanceInScreen.BaseType == oldName)
                        {
                            instanceInScreen.BaseType = elementSave.Name;
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
                foreach(var state in SelectedState.Self.SelectedElement.AllStates)
                {
                    var variable = state.Variables.FirstOrDefault(item => item.Name == nameof(ComponentSave.DefaultChildContainer));

                    if(variable?.Value as string != null)
                    {
                        var value = variable.Value as string;
                        if(value == oldName)
                        {
                            variable.Value = newName;
                            renamedDefaultChildContainer = true;
                        }
                    }
                }

                if(renamedDefaultChildContainer)
                {
                    var elementsToConsider = ObjectFinder.Self.GetElementsReferencing(elementSave);

                    foreach(var elementToCheckParent in elementsToConsider)
                    {
                        var shouldSaveElement = false;

                        foreach(var state in elementToCheckParent.AllStates)
                        {
                            foreach(var variable in state.Variables)
                            {
                                if(variable.GetRootName() == "Parent" && (variable.Value as string)?.Contains(".") == true)
                                {
                                    var value = variable.Value as string;
                                    var valueBeforeDot = value.Substring(0, value.IndexOf("."));
                                    var valueAfterDot = value.Substring(value.IndexOf(".") + 1);
                                    if(valueAfterDot == oldName)
                                    {
                                        // let's be safe, see if the instance is of the type elementSave
                                        var parentInstance = elementToCheckParent.GetInstance(valueBeforeDot);
                                        var parentInstanceElement = ObjectFinder.Self.GetElementSave(parentInstance);
                                        if(parentInstanceElement == elementSave)
                                        {
                                            variable.Value = parentInstance.Name + "." + newName;
                                            shouldSaveElement = true;
                                        }
                                    }
                                }
                            }
                        }
                        if(shouldSaveElement)
                        {
                            GumCommands.Self.FileCommands.TryAutoSaveElement(elementToCheckParent);
                        }

                    }
                }
            }
        }

        private static bool AskIfToRename(string oldName, bool askAboutRename, NameChangeAction action, bool shouldContinue)
        {
            if (shouldContinue && isRenamingXmlFile && askAboutRename)
            {
                string moveOrRename;
                if(action == NameChangeAction.Move)
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

        private static bool ValidateWithPopup(ElementSave elementSave, InstanceSave instance, bool shouldContinue)
        {
            if (instance != null)
            {
                string whyNot;
                if (NameVerifier.Self.IsInstanceNameValid(instance.Name, instance, elementSave, out whyNot) == false)
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
                foreach (var instance in possibleElement.Instances.Where(item=>item.IsOfType(containerElement.Name)))
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
    }
}
