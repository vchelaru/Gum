using System;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolCommands;
using Gum.Logic;
using Gum.DataTypes.Behaviors;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Wireframe
{
    public partial class EditingManager
    {
        #region Fields

        ContextMenuStrip mContextMenuStrip;

        ToolStripMenuItem mBringToFront;
        ToolStripMenuItem mSendToBack;

        ToolStripMenuItem mMoveForward;
        ToolStripMenuItem mMoveBackward;

        ToolStripMenuItem mMoveInFrontOf;

        #endregion

        public ContextMenuStrip ContextMenuStrip => mContextMenuStrip;

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


        void OnBringToFrontClick(object sender, EventArgs e)
        {
            ReorderLogic.Self.MoveSelectedInstanceToFront();
        }

        public void OnRightClick()
        {
            /////////////Early Out////////////////////
            if(mContextMenuStrip == null)
            {
                return;
            }
            ///////////End Early Out//////////////////

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
            if (SelectedState.Self.SelectedInstance != null)
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

            var selectedInstance = SelectedState.Self.SelectedInstance;
            var selectedElement = SelectedState.Self.SelectedElement;

            string selectedParent = null;

            if (selectedInstance != null)
            {
                selectedParent = GetEffectiveParentNameFor(selectedInstance, selectedElement);
            }


            foreach (var instance in SelectedState.Self.SelectedElement.Instances)
            {
                // Ignore the current instance
                if (instance != selectedInstance)
                {
                    var instanceParent = GetEffectiveParentNameFor(instance, selectedElement);
                    var hasSameParent = instanceParent == selectedParent;

                    // for move in front of, we only want to allow the selected instance to be moved in front of its siblings.
                    // This menu item cannot be used to move to a different parent.
                    if(hasSameParent)
                    {
                        ToolStripMenuItem item = new ToolStripMenuItem(instance.Name);
                        item.Tag = instance;
                        mMoveInFrontOf.DropDownItems.Add(item);

                        item.Click += HandleMoveInFrontOfClick;
                    }
                }
            }
        }

        private string GetEffectiveParentNameFor(InstanceSave instance, ElementSave owner)
        {
            var variableName = instance.Name + ".Parent";

            var state = owner.DefaultState;

            var parentVariableValue = state.Variables.Find(item => item.Name == variableName)?.Value;

            var parentName = (string)parentVariableValue;

            // even though an instance may have a parent variable, we don't consider
            // it an actual parent if there is no instance with that name (it could be 
            // a left-over variable).
            if(!string.IsNullOrEmpty(parentName))
            {
                var matchingInstance = owner.GetInstance(parentName);
                if(matchingInstance == null)
                {
                    parentName = null;
                }
            }


            return parentName;
        }

        private void HandleMoveInFrontOfClick(object sender, EventArgs e)
        {
            InstanceSave whatToMoveInFrontOf = ((ToolStripMenuItem)sender).Tag as InstanceSave;

            ReorderLogic.Self.MoveSelectedInstanceInFrontOf(whatToMoveInFrontOf);
        }


        public void RemoveSelectedElement()
        {
            RemoveElement(SelectedState.Self.SelectedElement);

            SelectedState.Self.SelectedElement = null;
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
            GumCommands.Self.GuiCommands.RefreshStateTreeView();
            
            GumCommands.Self.GuiCommands.RefreshVariables();
            Wireframe.WireframeObjectManager.Self.RefreshAll(true);

            GumCommands.Self.FileCommands.TryAutoSaveProject();
        }


        public void RemoveSelectedBehavior()
        {
            var behavior = SelectedState.Self.SelectedBehavior;
            string behaviorName = behavior.Name;

            GumProjectSave gps = ProjectManager.Self.GumProjectSave;
            List<BehaviorReference> references = gps.BehaviorReferences;

            references.RemoveAll(item => item.Name == behavior.Name);

            gps.Behaviors.Remove(behavior);

            List<ElementSave> elementsReferencingBehavior = new List<ElementSave>();

            foreach(var element in ObjectFinder.Self.GumProjectSave.AllElements)
            {
                var matchingBehavior = element.Behaviors.FirstOrDefault(item =>
                    item.BehaviorName == behaviorName);

                if(matchingBehavior != null)
                {
                    element.Behaviors.Remove(matchingBehavior);
                    elementsReferencingBehavior.Add(element);
                }
            }

            SelectedState.Self.SelectedBehavior = null;

            GumCommands.Self.GuiCommands.RefreshElementTreeView();
            GumCommands.Self.GuiCommands.RefreshStateTreeView();
            GumCommands.Self.GuiCommands.RefreshVariables();
            // I don't think we have to refresh the wireframe since nothing is being shown
            //Wireframe.WireframeObjectManager.Self.RefreshAll(true);

            GumCommands.Self.FileCommands.TryAutoSaveProject();

            foreach(var element in elementsReferencingBehavior)
            {
                GumCommands.Self.FileCommands.TryAutoSaveElement(element);
            }
        }
    }
}
