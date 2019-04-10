using System;
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
using Gum.DataTypes.Behaviors;
using Gum.Logic;

namespace Gum.Wireframe
{
    public enum CopyType
    {
        InstanceOrElement = 1,
        State = 2,
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
        ElementSave mCopiedElement = null;

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
            ReorderLogic.Self.MoveSelectedInstanceForward();
        }



        void OnMoveBackward(object sender, EventArgs e)
        {
            ReorderLogic.Self.MoveSelectedInstanceBackward();
        }

        void OnSendToBack(object sender, EventArgs e)
        {
            ReorderLogic.Self.MoveSelectedInstanceToBack();
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
                ElementCommands.Self.RemoveInstance(sourceInstance, sourceElement);

                if( ProjectManager.Self.GeneralSettingsFile.AutoSave)
                {
                    ProjectManager.Self.SaveElement(sourceElement);
                }
                WireframeObjectManager.Self.RefreshAll(true);
                PropertyGridManager.Self.RefreshUI();
                GumCommands.Self.GuiCommands.RefreshElementTreeView();
            }

            // todo: need to handle cut Element saves, but I don't want to do it yet due to the danger of losing valid data...


        }
        
        public void OnPaste(CopyType copyType)
        {
            // To make sure we didn't copy one type and paste another
            if (mCopyType == copyType)
            {
                if (mCopyType == CopyType.InstanceOrElement)
                {
                    if(mCopiedElement != null)
                    {
                        PasteCopiedElement();

                    }
                    // We need to both duplicate the InstanceSave, but we also need to duplicate all of the variables
                    // that use the copied InstanceSave.
                    else if (mCopiedInstances.Count != 0)
                    {
                        PasteCopiedInstanceSave();
                    }
                }
                else if (mCopyType == CopyType.State && mCopiedState != null)
                {
                    PastedCopiedState();
                }
            }

        }

        private void StoreCopiedObject(CopyType copyType)
        {
            mCopyType = copyType;
            mCopiedElement = null;
            mCopiedInstances.Clear();
            mCopiedState.Variables.Clear();
            mCopiedState.VariableLists.Clear();

            if (copyType == CopyType.InstanceOrElement)
            {
                if(ProjectState.Self.Selected.SelectedInstances.Count() != 0)
                {
                    StoreCopiedInstances();
                }
                else if(ProjectState.Self.Selected.SelectedElement != null)
                {
                    StoreCopiedElementSave();
                }
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

        private void StoreCopiedElementSave()
        {
            if(SelectedState.Self.SelectedElement != null)
            {
                if (SelectedState.Self.SelectedElement is ScreenSave)
                {
                    mCopiedElement = ((ScreenSave)SelectedState.Self.SelectedElement).Clone();
                }
                else if(SelectedState.Self.SelectedElement is ComponentSave)
                {
                    mCopiedElement = ((ComponentSave)SelectedState.Self.SelectedElement).Clone();
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

            StateSave stateSave = copiedState;
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
                targetState = SelectedState.Self.SelectedStateSave;
            }

            // why reverse loop?
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

                    targetState.Variables.Add(copiedVariable);
                }
            }
            // Copy over the VariableLists too
            for (int i = stateSave.VariableLists.Count - 1; i > -1; i--)
            {

                VariableListSave sourceVariableList = stateSave.VariableLists[i];
                if (sourceVariableList.SourceObject == sourceInstance.Name)
                {
                    VariableListSave copiedList = sourceVariableList.Clone();
                    copiedList.Name = targetInstance.Name + "." + copiedList.GetRootName();

                    targetState.VariableLists.Add(copiedList);
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


        private void PasteCopiedElement()
        {
            ElementSave toAdd;
            
            if(mCopiedElement is ScreenSave)
            {
                toAdd = ((ScreenSave)mCopiedElement).Clone();
                toAdd.Initialize(null);
            }
            else
            {
                toAdd = ((ComponentSave)mCopiedElement).Clone();
                ((ComponentSave)toAdd).InitializeDefaultAndComponentVariables();
            }


            List<string> allElementNames = new List<string>();
            allElementNames.AddRange(ProjectState.Self.GumProjectSave.Screens.Select(item=>item.Name.ToLowerInvariant()));
            allElementNames.AddRange(ProjectState.Self.GumProjectSave.Components.Select(item=>item.Name.ToLowerInvariant()));
            allElementNames.AddRange(ProjectState.Self.GumProjectSave.StandardElements.Select(item=>item.Name.ToLowerInvariant()));

            while(allElementNames.Contains(toAdd.Name.ToLowerInvariant()))
            {
                toAdd.Name = StringFunctions.IncrementNumberAtEnd(toAdd.Name);
            }

            if(toAdd is ScreenSave)
            {
                ProjectCommands.Self.AddScreen(toAdd as ScreenSave);
            }
            else
            {
                ProjectCommands.Self.AddComponent(toAdd as ComponentSave);
            }


            GumCommands.Self.GuiCommands.RefreshElementTreeView();

            SelectedState.Self.SelectedElement = toAdd;

            GumCommands.Self.FileCommands.TryAutoSaveElement(toAdd);
            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }



        void OnBringToFrontClick(object sender, EventArgs e)
        {
            ReorderLogic.Self.MoveSelectedInstanceToFront();
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
            InstanceSave whatToMoveInFrontOf = ((ToolStripMenuItem)sender).Tag as InstanceSave;

            ReorderLogic.Self.MoveSelectedInstanceInFrontOf(whatToMoveInFrontOf);
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


        public void RemoveSelectedBehavior()
        {
            var behavior = SelectedState.Self.SelectedBehavior;
            ProjectCommands.Self.RemoveBehavior(behavior);

            GumCommands.Self.GuiCommands.RefreshElementTreeView();
            StateTreeViewManager.Self.RefreshUI(null);
            PropertyGridManager.Self.RefreshUI();
            // I don't think we have to refresh the wireframe since nothing is being shown
            //Wireframe.WireframeObjectManager.Self.RefreshAll(true);

            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }
    }
}
