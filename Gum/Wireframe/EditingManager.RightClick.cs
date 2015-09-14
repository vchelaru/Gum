﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.ToolCommands;
using ToolsUtilities;
using Gum.Debug;
using Gum.Gui.Forms;

namespace Gum.Wireframe
{
    public enum CopyType
    {
        Instance,
        State
    }

    public partial class EditingManager
    {
        #region Fields

        ContextMenuStrip mContextMenuStrip;

        ToolStripMenuItem mBringToFront;
        ToolStripMenuItem mSendToBack;

        ToolStripMenuItem mMoveForward;
        ToolStripMenuItem mMoveBackward;

        ToolStripMenuItem mMoveInFrontOf;

        List<InstanceSave> mCopiedInstances = new List<InstanceSave>();
        StateSave mCopiedState = new StateSave();
        
        CopyType mCopyType;

        #endregion

        public ContextMenuStrip ContextMenuStrip
        {
            get { return mContextMenuStrip; }
        }

        private void RightClickInitialize(ContextMenuStrip contextMenuStrip)
        {
            mContextMenuStrip = contextMenuStrip;

            mBringToFront = new ToolStripMenuItem();
            mBringToFront.Text = "Bring to front";
            mBringToFront.Click += OnBringToFrontClick;

            mSendToBack = new ToolStripMenuItem();
            mSendToBack.Text = "Send to back";
            mSendToBack.Click += OnSendToBack;

            mMoveForward = new ToolStripMenuItem();
            mMoveForward.Text = "Move Forward";
            mMoveForward.Click += OnMoveForward;

            mMoveBackward = new ToolStripMenuItem();
            mMoveBackward.Text = "Move Backward";
            mMoveBackward.Click += OnMoveBackward;

            mMoveInFrontOf = new ToolStripMenuItem();
            mMoveInFrontOf.Text = "Move In Front Of";

            contextMenuStrip.VisibleChanged += HandleVisibleChange;
        }

        private void HandleVisibleChange(object sender, EventArgs e)
        {

            PopulateMoveInFrontOfMenuItem();
        }

        void OnMoveForward(object sender, EventArgs e)
        {
                        
            InstanceSave instance = SelectedState.Self.SelectedInstance;
            ElementSave element = SelectedState.Self.SelectedElement;

            if (instance != null)
            {
                int index = element.Instances.IndexOf(instance);

                if (index != element.Instances.Count - 1)
                {
                    element.Instances.RemoveAt(index);
                    element.Instances.Insert(index + 1, instance);

                    RefreshInResponseToReorder();
                }
            }
        }

        void OnMoveBackward(object sender, EventArgs e)
        {
            InstanceSave instance = SelectedState.Self.SelectedInstance;
            ElementSave element = SelectedState.Self.SelectedElement;

            if (instance != null)
            {
                int index = element.Instances.IndexOf(instance);

                if (index != 0)
                {
                    element.Instances.RemoveAt(index);
                    element.Instances.Insert(index - 1, instance);

                    RefreshInResponseToReorder();
                }
            }
        }

        void OnSendToBack(object sender, EventArgs e)
        {
            InstanceSave instance = SelectedState.Self.SelectedInstance;
            ElementSave element = SelectedState.Self.SelectedElement;

            if (instance != null)
            {
                // to bring to back, we're going to remove, then insert at index 0
                element.Instances.Remove(instance);
                element.Instances.Insert(0, instance);

                RefreshInResponseToReorder();
            }
        }

        public void OnCopy(CopyType copyType)
        {
            StoreCopiedObject(copyType);
        }

        public void OnCut(CopyType copyType)
        {
            StoreCopiedObject(copyType);

            ElementSave sourceElement = SelectedState.Self.SelectedElement;
            InstanceSave sourceInstance = SelectedState.Self.SelectedInstance;

            if (sourceElement.Instances.Contains(sourceInstance))
            {
                // Not sure why we weren't just using
                // ElementCommands here - maybe an oversight?
                // This should improve things like 
                //sourceElement.Instances.Remove(sourceInstance);

                ElementCommands.Self.RemoveInstance(sourceInstance, sourceElement);
                if( ProjectManager.Self.GeneralSettingsFile.AutoSave)
                {
                    ProjectManager.Self.SaveElement(sourceElement);
                }
                WireframeObjectManager.Self.RefreshAll(true);
                PropertyGridManager.Self.RefreshUI();
                GumCommands.Self.GuiCommands.RefreshElementTreeView();
            }
        }
        
        public void OnPaste(CopyType copyType)
        {
            // To make sure we didn't copy one type and paste another
            if (mCopyType == copyType)
            {
                if (mCopyType == CopyType.Instance)
                {
                    // We need to both duplicate the InstanceSave, but we also need to duplicate all of the variables
                    // that use the copied InstanceSave.
                    if (mCopiedInstances.Count == 0)
                    {
                        // do nothing
                    }
                    else
                    {
                        PasteCopiedInstanceSave();
                    }
                }
                else if (mCopyType == CopyType.State)
                {
                    if (mCopiedState != null)
                    {
                        PastedCopiedState();

                    }
                }
            }

        }

        

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

            ProjectVerifier.Self.AssertSelectedIpsosArePartOfRenderer();
        }



        private void StoreCopiedObject(CopyType copyType)
        {
            mCopyType = copyType;

            mCopiedInstances.Clear();
            mCopiedState.Variables.Clear();
            mCopiedState.VariableLists.Clear();

            if (copyType == CopyType.Instance)
            {
                StoreCopiedInstances();
            }
            else if (copyType == CopyType.State)
            {
                StoreCopiedState();
            }
        }

        private void StoreCopiedState()
        {
            if (SelectedState.Self.SelectedStateSave != null)
            {
                mCopiedState = SelectedState.Self.SelectedStateSave.Clone();
            }
        }

        private void StoreCopiedInstances()
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                var element = SelectedState.Self.SelectedElement;

                // When copying we want to grab all instances in the order that they are in their container.
                // That way when they're pasted they are pasted in the right order
                foreach (var instance in SelectedState.Self.SelectedInstances.OrderBy(item=>element.Instances.IndexOf(item)))
                {
                    mCopiedInstances.Add(instance.Clone());
                }
                mCopiedState = SelectedState.Self.SelectedStateSave.Clone();

                // Clear out any variables that don't pertain to the selected instance:
                for (int i = mCopiedState.Variables.Count - 1; i > -1; i--)
                {
                    if (mCopiedInstances.Any(item=>item.Name == mCopiedState.Variables[i].SourceObject) == false)
                    {
                        mCopiedState.Variables.RemoveAt(i);
                    }
                }

                // And also any VariableLists:
                for (int i = mCopiedState.VariableLists.Count - 1; i > -1; i--)
                {
                    if (mCopiedInstances.Any(item=>item.Name == mCopiedState.VariableLists[i].SourceObject) == false)
                    {
                        mCopiedState.VariableLists.RemoveAt(i);
                    }
                }
            }
        }



        private void PasteCopiedInstanceSave()
        {
            PasteInstanceSaves(mCopiedInstances, mCopiedState, SelectedState.Self.SelectedElement);
        }

        private void PastedCopiedState()
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


        public void PasteInstanceSaves(List<InstanceSave> copiedInstances, StateSave copiedState, ElementSave targetElement)
        {
            List<InstanceSave> newInstances = new List<InstanceSave>();
            foreach (var instanceAsObject in copiedInstances)
            {
                InstanceSave sourceInstance = instanceAsObject as InstanceSave;
                ElementSave sourceElement = sourceInstance.ParentContainer;

                InstanceSave targetInstance = sourceInstance.Clone();
                newInstances.Add(targetInstance);

                if (targetElement != null)
                {
                    PastedCopiedInstance(sourceInstance, sourceElement, targetElement, targetInstance, copiedState);
                }
            }

            if (newInstances.Count > 1)
            {
                SelectedState.Self.SelectedInstances = newInstances;
            }
        }

        private void PastedCopiedInstance(InstanceSave sourceInstance, ElementSave sourceElement, ElementSave targetElement, InstanceSave targetInstance, StateSave copiedState)
        {
            targetInstance.Name = StringFunctions.MakeStringUnique(targetInstance.Name, targetElement.Instances.Select(item=>item.Name));

            targetElement.Instances.Add(targetInstance);

            // We now have to copy over the states
            if (targetElement != sourceElement)
            {
                if (sourceElement.States.Count != 1)
                {
                    MessageBox.Show("Only the default state variables will be copied since the source and target elements differ.");
                }

                StateSave stateSave = copiedState;

                // Why do we reverse loop here?
                for (int i = stateSave.Variables.Count - 1; i > -1; i--)
                {
                    VariableSave sourceVariable = stateSave.Variables[i];

                    VariableSave copiedVariable = sourceVariable.Clone();
                    // Only copy over the variables that apply to the object that was dropped
                    if (sourceVariable.SourceObject == sourceInstance.Name)
                    {
                        copiedVariable.Name = targetInstance.Name + "." + copiedVariable.GetRootName();

                        // We don't want to copy exposed variables.
                        // If we did, the user would have 2 variables exposed with the same.
                        copiedVariable.ExposedAsName = null;

                        targetElement.DefaultState.Variables.Add(copiedVariable);
                    }
                }

                for(int i = 0; i < stateSave.VariableLists.Count; i++)
                {
                    var sourceVariableList = stateSave.VariableLists[i];

                    var copiedVariableList = sourceVariableList.Clone();
                    // Only copy over the variables that apply to the object that was dropped
                    if (sourceVariableList.SourceObject == sourceInstance.Name)
                    {
                        copiedVariableList.Name = targetInstance.Name + "." + copiedVariableList.GetRootName();
                        copiedVariableList.SourceObject = targetInstance.Name;
                        targetElement.DefaultState.VariableLists.Add(copiedVariableList);
                    }
                }
            }
            else
            {
                StateSave stateSave = copiedState;

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
                        copiedVariable.Name = targetInstance.Name + "." + copiedVariable.GetRootName();

                        // We don't want to copy exposed variables.
                        // If we did, the user would have 2 variables exposed with the same.
                        copiedVariable.ExposedAsName = null;

                        SelectedState.Self.SelectedStateSave.Variables.Add(copiedVariable);
                    }
                }
                // Copy over the VariableLists too
                for (int i = stateSave.VariableLists.Count - 1; i > -1; i--)
                {

                    VariableListSave sourceList = stateSave.VariableLists[i];
                    if (sourceList.SourceObject == sourceInstance.Name)
                    {
                        VariableListSave copiedList = sourceList.Clone();
                        copiedList.Name = targetInstance.Name + "." + copiedList.GetRootName();
                        copiedList.SourceObject = targetInstance.Name;
                        
                        SelectedState.Self.SelectedStateSave.VariableLists.Add(copiedList);
                    }
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

            targetInstance.ParentContainer = targetElement;
            // We need to call InstanceAdd before we select the new object - the Undo manager expects it
            // This includes before other managers refresh
            PluginManager.Self.InstanceAdd(targetElement, targetInstance);
            WireframeObjectManager.Self.RefreshAll(true);



            GumCommands.Self.GuiCommands.RefreshElementTreeView(targetElement);


            SelectedState.Self.SelectedInstance = targetInstance;

            GumCommands.Self.FileCommands.TryAutoSaveElement(targetElement);
        }

        

        void OnBringToFrontClick(object sender, EventArgs e)
        {
            InstanceSave instance = SelectedState.Self.SelectedInstance;
            ElementSave element = SelectedState.Self.SelectedElement;

            if (SelectedState.Self.SelectedInstance != null)
            {
                // to bring to back, we're going to remove, then add (at the end)
                element.Instances.Remove(instance);
                element.Instances.Add(instance);

                RefreshInResponseToReorder();
            }
        }

        private void RefreshInResponseToReorder()
        {
            InstanceSave instance = SelectedState.Self.SelectedInstance;
            ElementSave element = SelectedState.Self.SelectedElement;

            GumCommands.Self.GuiCommands.RefreshElementTreeView();


            WireframeObjectManager.Self.RefreshAll(true);

            SelectionManager.Self.Refresh();
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }


        public void OnRightClick()
        {
            // Note:  This
            // code assumes
            // that the object
            // that was right-clicked
            // on is the selected object.
            // We can bet this is the case
            // because selection happens on
            // a mouse push (even on a right-push).
            // The click will happen *after* so the object
            // should already be selected.
            if (SelectedState.Self.SelectedInstance != null && mContextMenuStrip != null)
            {
                mContextMenuStrip.Items.Add(mBringToFront);
                mContextMenuStrip.Items.Add(mMoveForward);
                mContextMenuStrip.Items.Add(mMoveInFrontOf);
                mContextMenuStrip.Items.Add(mMoveBackward);
                mContextMenuStrip.Items.Add(mSendToBack);

                PopulateMoveInFrontOfMenuItem();
            }
        }

        private void PopulateMoveInFrontOfMenuItem()
        {
            mMoveInFrontOf.DropDownItems.Clear();

            foreach (var instance in SelectedState.Self.SelectedElement.Instances)
            {
                var selectedInstance = SelectedState.Self.SelectedInstance;
                // Ignore the current instance
                if (instance != selectedInstance)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(instance.Name);

                    item.Tag = instance;
                    mMoveInFrontOf.DropDownItems.Add(item);

                    item.Click += HandleMoveInFrontOfClick;
                }
            }
        }

        private void HandleMoveInFrontOfClick(object sender, EventArgs e)
        {
            InstanceSave instance = ((ToolStripMenuItem)sender).Tag as InstanceSave;

            var element = SelectedState.Self.SelectedElement;
            var whatToInsert = SelectedState.Self.SelectedInstance;
            element.Instances.Remove(SelectedState.Self.SelectedInstance);
            int whereToInsert = element.Instances.IndexOf(instance) + 1;

            element.Instances.Insert(whereToInsert, whatToInsert);

            RefreshInResponseToReorder();

            if (ProjectManager.Self.GeneralSettingsFile.AutoSave)
            {
                ProjectManager.Self.SaveElement(element);
            }
        }


        public void RemoveSelectedElement()
        {
            RemoveElement(SelectedState.Self.SelectedElement);
        }

        private void RemoveElement(ElementSave elementToRemove)
        {
            ScreenSave asScreenSave = elementToRemove as ScreenSave;
            ComponentSave asComponentSave = elementToRemove as ComponentSave;

            if (asScreenSave != null)
            {
                ProjectCommands.Self.RemoveScreen(asScreenSave);
            }
            else if (asComponentSave != null)
            {
                ProjectCommands.Self.RemoveComponent(asComponentSave);
            }

            GumCommands.Self.GuiCommands.RefreshElementTreeView();
            StateTreeViewManager.Self.RefreshUI(null);
            PropertyGridManager.Self.RefreshUI();
            Wireframe.WireframeObjectManager.Self.RefreshAll(true);

            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }
    }
}
