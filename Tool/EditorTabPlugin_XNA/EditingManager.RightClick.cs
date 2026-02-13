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
        _viewModel = new RightClickViewModel(
            _selectedState,
            _reorderLogic,
            ObjectFinder.Self,
            _elementCommands,
            _nameVerifier,
            _setVariableLogic,
            _circularReferenceManager,
            _favoriteComponentManager);
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
                var toolStripItem = ToToolStripItem(item);
                mContextMenuStrip.Items.Add(toolStripItem);
            }
        }
    }

    private static ToolStripItem ToToolStripItem(ContextMenuItemViewModel item)
    {
        if (item.IsSeparator)
        {
            return new ToolStripSeparator();
        }

        var toolStripItem = new ToolStripMenuItem(item.Text);

        if (item.Action != null)
        {
            toolStripItem.Click += (_, _) => item.Action();
        }

        foreach (var child in item.Children)
        {
            toolStripItem.DropDownItems.Add(ToToolStripItem(child));
        }

        return toolStripItem;
    }
}
