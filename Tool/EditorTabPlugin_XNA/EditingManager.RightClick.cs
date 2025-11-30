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

namespace Gum.Wireframe;

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
        mBringToFront.Text = "Bring to Front";
        mBringToFront.Click += OnBringToFrontClick;

        mSendToBack = new ToolStripMenuItem();
        mSendToBack.Text = "Send to Back";
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
        _reorderLogic.MoveSelectedInstanceForward();
    }



    void OnMoveBackward(object sender, EventArgs e)
    {
        _reorderLogic.MoveSelectedInstanceBackward();
    }

    void OnSendToBack(object sender, EventArgs e)
    {
        _reorderLogic.MoveSelectedInstanceToBack();
    }


    void OnBringToFrontClick(object sender, EventArgs e)
    {
        _reorderLogic.MoveSelectedInstanceToFront();
    }

    public void OnRightClick()
    {
        RefreshContextMenuStrip();
    }

    public void RefreshContextMenuStrip()
    {
        /////////////Early Out////////////////////
        if (mContextMenuStrip == null)
        {
            return;
        }
        ///////////End Early Out//////////////////

        if (_selectedState.SelectedInstance != null)
        {
            mContextMenuStrip.Items.Add(mBringToFront);
            mContextMenuStrip.Items.Add(mMoveForward);
            mContextMenuStrip.Items.Add(mMoveInFrontOf);
            mContextMenuStrip.Items.Add(mMoveBackward);
            mContextMenuStrip.Items.Add(mSendToBack);

            PopulateMoveInFrontOfMenuItem();

        }
        else
        {
            mContextMenuStrip.Items.Clear();
        }
    }

    private void PopulateMoveInFrontOfMenuItem()
    {
        mMoveInFrontOf.DropDownItems.Clear();

        var selectedInstance = _selectedState.SelectedInstance;
        var selectedElement = _selectedState.SelectedElement;

        string selectedParent = null;

        if (selectedInstance != null && selectedElement != null)
        {
            selectedParent = GetEffectiveParentNameFor(selectedInstance, selectedElement);

            foreach (var instance in selectedElement.Instances)
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

        _reorderLogic.MoveSelectedInstanceInFrontOf(whatToMoveInFrontOf);
    }

}
