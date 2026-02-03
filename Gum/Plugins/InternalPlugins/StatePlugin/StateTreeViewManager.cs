using System;
using System.Collections.Generic;
using System.Linq;
using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System.Windows.Forms;
using Gum.ToolStates;
using Gum.Plugins;
using Gum.Services;

namespace Gum.Managers;

public partial class StateTreeViewManager
{
    #region Fields

    static StateTreeViewManager mSelf;

    StateTreeViewRightClickService _stateTreeViewRightClickService;
    HotkeyManager _hotkeyManager;
    private readonly ISelectedState _selectedState;

    #endregion

    #region Properties

    public static StateTreeViewManager Self
    {
        get
        {
            if (mSelf == null)
            {
                mSelf = new StateTreeViewManager();
            }
            return mSelf;
        }
    }

    #endregion

    public StateTreeViewManager()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
    }

    public void Initialize(
        StateTreeViewRightClickService stateTreeViewRightClickService,
        HotkeyManager hotkeyManager)
    {
        _stateTreeViewRightClickService = stateTreeViewRightClickService;
        _hotkeyManager = hotkeyManager;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if(_hotkeyManager.Rename.IsPressed(e))
        {
            if(_selectedState.SelectedStateSave != null )
            {
                if(_selectedState.SelectedElement?.DefaultState != _selectedState.SelectedStateSave)
                {
                    _stateTreeViewRightClickService.RenameStateClick();

                }
            }
            else if(_selectedState.SelectedStateCategorySave != null)
            {
                _stateTreeViewRightClickService.RenameCategoryClick();
            }
        }
        else if(_hotkeyManager.Delete.IsPressed(e))
        {
            if (_selectedState.SelectedStateSave != null)
            {
                if (_selectedState.SelectedElement?.DefaultState != _selectedState.SelectedStateSave)
                {
                    _stateTreeViewRightClickService.DeleteStateClick();

                }
            }
            else if (_selectedState.SelectedStateCategorySave != null)
            {
                _stateTreeViewRightClickService.DeleteCategoryClick();
            }
        }
    }

    //private void HandleTreeViewKeyPressed(object? sender, KeyPressEventArgs e)
    //{
    //    switch(e.)
    //}

}


static class TreeNodeExtensions
{
    public static IEnumerable<TreeNode> AllNodes(this TreeNodeCollection treeNodes)
    {
        foreach (TreeNode node in treeNodes)
        {
            foreach (var item in node.Nodes.AllNodes())
            {
                yield return item;
            }

            yield return node;
        }
    }


}
