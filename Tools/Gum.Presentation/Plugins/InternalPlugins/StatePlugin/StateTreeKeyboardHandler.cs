using System.Linq;
using Gum.Input;
using Gum.Logic;
using Gum.ToolStates;

namespace Gum.Managers;

/// <summary>
/// The states tree's hotkeys: reorder, rename, delete, copy and paste of the selected state or
/// category. Both heads' state tree views pass their key presses here.
/// </summary>
public class StateTreeKeyboardHandler
{
    private readonly IStateTreeViewRightClickService _rightClickService;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly ISelectedState _selectedState;
    private readonly ICopyPasteLogic _copyPasteLogic;

    /// <summary>Creates the handler over the commands the keys run.</summary>
    public StateTreeKeyboardHandler(
        IStateTreeViewRightClickService rightClickService,
        IHotkeyManager hotkeyManager,
        ISelectedState selectedState,
        ICopyPasteLogic copyPasteLogic)
    {
        _rightClickService = rightClickService;
        _hotkeyManager = hotkeyManager;
        _selectedState = selectedState;
        _copyPasteLogic = copyPasteLogic;
    }

    /// <summary>Runs the command bound to <paramref name="e"/>, if any.</summary>
    /// <returns>Whether the key was consumed and should be marked handled.</returns>
    public bool HandleKeyDown(GumKeyEventArgs e)
    {
        if (_hotkeyManager.ReorderUp.IsPressed(e))
        {
            _rightClickService.MoveStateInDirection(-1);
            return true;
        }

        if (_hotkeyManager.ReorderDown.IsPressed(e))
        {
            if (!IsSelectedStateUncategorized())
            {
                _rightClickService.MoveStateInDirection(1);
            }
            return true;
        }

        if (_hotkeyManager.Rename.IsPressed(e))
        {
            if (_selectedState.SelectedStateSave != null)
            {
                if (!IsSelectedStateUncategorized())
                {
                    _rightClickService.RenameStateClick();
                }
                return true;
            }
            if (_selectedState.SelectedStateCategorySave != null)
            {
                _rightClickService.RenameCategoryClick();
                return true;
            }
            return false;
        }

        if (_hotkeyManager.Delete.IsPressed(e))
        {
            if (_selectedState.SelectedStateSave != null)
            {
                if (!IsSelectedStateDefault())
                {
                    _rightClickService.DeleteStateClick();
                }
                return true;
            }
            if (_selectedState.SelectedStateCategorySave != null)
            {
                _rightClickService.DeleteCategoryClick();
                return true;
            }
            return false;
        }

        if (_hotkeyManager.Copy.IsPressed(e))
        {
            if (_selectedState.SelectedStateSave != null)
            {
                if (!IsSelectedStateDefault())
                {
                    _copyPasteLogic.OnCopy(CopyType.State);
                }
                return true;
            }
            if (_selectedState.SelectedStateCategorySave != null)
            {
                _copyPasteLogic.OnCopy(CopyType.Category);
                return true;
            }
            return false;
        }

        if (_hotkeyManager.Paste.IsPressed(e))
        {
            _copyPasteLogic.OnPaste(_copyPasteLogic.CopiedData.CopiedCategory != null ? CopyType.Category : CopyType.State);
            return true;
        }

        return false;
    }

    private bool IsSelectedStateUncategorized() =>
        _selectedState.SelectedStateSave is { } state &&
        _selectedState.SelectedStateContainer?.UncategorizedStates.Contains(state) == true;

    private bool IsSelectedStateDefault() =>
        _selectedState.SelectedElement?.DefaultState == _selectedState.SelectedStateSave;
}
