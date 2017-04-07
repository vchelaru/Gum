using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.Plugins;
using ToolsUtilities;

namespace Gum.Managers
{
    public enum NameChangeAction
    {
        Move,
        Rename
    }

    public class RenameManager
    {
        static RenameManager mRenameManager;

        public static RenameManager Self
        {
            get
            {
                if (mRenameManager == null)
                {
                    mRenameManager = new RenameManager();
                }
                return mRenameManager;
            }
        }

        public bool IsRenamingXmlFile { get; private set; }

        public void HandleRename(ElementSave elementSave, InstanceSave instance, string oldName, NameChangeAction action, bool askAboutRename = true)
        {
            try
            {
                IsRenamingXmlFile = instance == null;

                bool shouldContinue = true;

                shouldContinue = ValidateWithPopup(elementSave, instance, shouldContinue);

                shouldContinue = AskIfToRename(oldName, askAboutRename, action, shouldContinue);

                if (shouldContinue)
                {
                    RenameAllReferencesTo(elementSave, instance, oldName);

                    // Even though this gets called from the PropertyGrid methods which eventually
                    // save this object, we want to force a save here to make sure it worked.  If it
                    // does, then we're safe to delete the old files.
                    if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
                    {
                        ProjectManager.Self.SaveElement(elementSave);
                    }

                    if (IsRenamingXmlFile)
                    {
                        // If we got here that means all went okay, so we should delete the old files
                        string oldXml = elementSave.GetFullPathXmlFile(oldName);
                        string newXml = elementSave.GetFullPathXmlFile();

                        // Delete the XML.
                        // If the file doesn't
                        // exist, no biggie - we
                        // were going to delete it
                        // anyway.
                        if (System.IO.File.Exists(oldXml))
                        {
                            System.IO.File.Delete(oldXml);
                        }

                        PluginManager.Self.ElementRename(elementSave, oldName);

                        GumCommands.Self.FileCommands.TryAutoSaveProject();

                        var oldDirectory = FileManager.GetDirectory(oldXml);
                        var newDirectory = FileManager.GetDirectory(newXml);

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
                }

                if (!shouldContinue && IsRenamingXmlFile)
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
                IsRenamingXmlFile = false;
            }
        }

        private static void RenameAllReferencesTo(ElementSave elementSave, InstanceSave instance, string oldName)
        {
            // Tell the GumProjectSave to react to the rename.
            // This changes the names of the ElementSave references.
            ProjectManager.Self.GumProjectSave.ReactToRenamed(elementSave, instance, oldName);

            foreach (var screen in ProjectState.Self.GumProjectSave.Screens)
            {
                bool shouldSave = false;
                foreach (var instanceInScreen in screen.Instances)
                {
                    if (instanceInScreen.BaseType == oldName)
                    {
                        instanceInScreen.BaseType = elementSave.Name;
                        shouldSave = true;
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
                foreach (var instanceInScreen in component.Instances)
                {
                    if (instanceInScreen.BaseType == oldName)
                    {
                        instanceInScreen.BaseType = elementSave.Name;
                        shouldSave = true;
                    }
                }

                if (shouldSave)
                {
                    GumCommands.Self.FileCommands.TryAutoSaveElement(component);
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
            }
        }

        private bool AskIfToRename(string oldName, bool askAboutRename, NameChangeAction action, bool shouldContinue)
        {
            if (shouldContinue && IsRenamingXmlFile && askAboutRename)
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

        public void HandleRename(ElementSave containerElement, EventSave eventSave, string oldName)
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
