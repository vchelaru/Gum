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
using System.Windows.Controls;

namespace Gum.Wireframe;

public partial class EditingManager
{
    private RightClickViewModel _viewModel;
    ContextMenu _contextMenu;

    public ContextMenu ContextMenu => _contextMenu;

    private void RightClickInitialize(ContextMenu contextMenu)
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
        _contextMenu = contextMenu;
    }


    public void OnRightClick()
    {
        RefreshContextMenu();
    }

    public void RefreshContextMenu()
    {
        /////////////Early Out////////////////////
        if (_contextMenu == null)
        {
            return;
        }
        ///////////End Early Out//////////////////

        _contextMenu.Items.Clear();

        if (_selectedState.SelectedInstance != null)
        {
            var menuItems = _viewModel.GetMenuItems();

            foreach(var item in menuItems)
            {
                _contextMenu.Items.Add(ToMenuItem(item));
            }
        }
    }

    private static Control ToMenuItem(ContextMenuItemViewModel item)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        var menuItem = new MenuItem { Header = item.Text };

        if (item.Action != null)
        {
            menuItem.Click += (_, _) => item.Action();
        }

        foreach (var child in item.Children)
        {
            menuItem.Items.Add(ToMenuItem(child));
        }

        return menuItem;
    }
}
