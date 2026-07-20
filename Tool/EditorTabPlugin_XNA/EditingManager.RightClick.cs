using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Extensions;
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

    public bool IsContextMenuOpen => _contextMenu?.IsOpen == true;

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
                _contextMenu.Items.Add(item.ToMenuItem());
            }
        }
    }
}
