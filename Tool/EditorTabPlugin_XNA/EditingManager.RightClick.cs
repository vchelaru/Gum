using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using Gum.Managers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Gum.Wireframe;

public partial class EditingManager
{
    private RightClickViewModel _viewModel;
    ContextMenuStrip mContextMenuStrip;

    public ContextMenuStrip ContextMenuStrip => mContextMenuStrip;

    private void RightClickInitialize(ContextMenuStrip contextMenuStrip)
    {
        _viewModel = new RightClickViewModel(_selectedState, _reorderLogic);
        mContextMenuStrip = contextMenuStrip;

        contextMenuStrip.VisibleChanged += HandleVisibleChange;
    }

    private void HandleVisibleChange(object? sender, EventArgs e)
    {
        _viewModel.HandleVisibilityChanged();
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

        mContextMenuStrip.Items.Clear();

        if (_selectedState.SelectedInstance != null)
        {
            var menuItems = _viewModel.GetMenuItems();

            foreach(var item in menuItems)
            {
                ToolStripMenuItem tsmi = ToToolStripMenuItem(item);
                mContextMenuStrip.Items.Add(tsmi);
            }
        }
    }

    private static ToolStripMenuItem ToToolStripMenuItem(ContextMenuItemViewModel item)
    {
        var toolStripItem = new ToolStripMenuItem(item.Text);

        if (item.Action != null)
        {
            toolStripItem.Click += (_, _) => item.Action();
        }

        foreach (var child in item.Children)
        {
            toolStripItem.DropDownItems.Add(ToToolStripMenuItem(child));
        }

        return toolStripItem;
    }
}
