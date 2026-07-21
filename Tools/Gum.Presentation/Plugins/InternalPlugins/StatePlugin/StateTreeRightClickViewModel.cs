using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.ViewModels;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Headless decision logic behind <see cref="StateTreeViewRightClickService.PopulateContextMenu"/>
/// (ADR-0005): which items to show for the current selection, their header text, and the actions
/// they perform. <see cref="StateTreeViewRightClickService"/> stays responsible only for converting
/// the returned <see cref="ContextMenuItemViewModel"/> tree into real WPF <c>ContextMenu</c> items,
/// and for the one WPF-only side effect ("Move Up"/"Move Down" re-populating the live context menu
/// so its enabled state is fresh for the next right-click).
/// </summary>
public class StateTreeRightClickViewModel
{
    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IEditCommands _editCommands;
    private readonly IDialogService _dialogService;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ICopyPasteLogic _copyPasteLogic;

    public StateTreeRightClickViewModel(
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IEditCommands editCommands,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ICopyPasteLogic copyPasteLogic)
    {
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _editCommands = editCommands;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _copyPasteLogic = copyPasteLogic;
    }

    /// <summary>
    /// Builds the right-click menu items for the currently selected state/category.
    /// </summary>
    /// <param name="moveUpClick">
    /// Overrides the "^ Move Up" item's action, used by <see cref="StateTreeViewRightClickService"/>
    /// to re-populate the live WPF context menu after a successful move. Defaults to a move with no
    /// further side effect, which is sufficient for headless callers/tests.
    /// </param>
    /// <param name="moveDownClick">Same as <paramref name="moveUpClick"/>, for "v Move Down".</param>
    public List<ContextMenuItemViewModel> GetMenuItems(Action? moveUpClick = null, Action? moveDownClick = null)
    {
        var items = new List<ContextMenuItemViewModel>();

        if (_selectedState.SelectedStateContainer == null)
        {
            return items;
        }

        if (_selectedState.SelectedStateCategorySave != null)
        {
            // As of 5/24/2023, we no longer support uncategorized states
            items.Add(new ContextMenuItemViewModel
            {
                Text = "Add State",
                Action = () => _dialogService.Show<AddStateDialogViewModel>()
            });
        }

        items.Add(new ContextMenuItemViewModel
        {
            Text = "Add Category",
            Action = () => _dialogService.Show<AddCategoryDialogViewModel>()
        });

        if (_copyPasteLogic.CopiedData.CopiedCategory != null)
        {
            items.Add(new ContextMenuItemViewModel
            {
                Text = "Paste Category",
                Action = () => _copyPasteLogic.OnPaste(CopyType.Category)
            });
        }

        if (_selectedState.SelectedStateSave != null)
        {
            bool isDefault = _selectedState.SelectedStateSave == _selectedState.SelectedElement?.DefaultState;

            if (!isDefault)
            {
                items.Add(new ContextMenuItemViewModel { IsSeparator = true });

                items.Add(new ContextMenuItemViewModel
                {
                    Text = "Rename [" + _selectedState.SelectedStateSave.Name + "]",
                    Action = RenameStateClick
                });
                items.Add(new ContextMenuItemViewModel
                {
                    Text = "Delete [" + _selectedState.SelectedStateSave.Name + "]",
                    Action = DeleteStateClick
                });
                items.Add(new ContextMenuItemViewModel
                {
                    Text = "Duplicate [" + _selectedState.SelectedStateSave.Name + "]",
                    Action = DuplicateStateClick
                });
                items.Add(new ContextMenuItemViewModel
                {
                    Text = "Set [" + _selectedState.SelectedStateSave.Name + "] variables to default",
                    Action = AssignToDefault
                });

                AddMoveToCategoryItems(items);

                items.Add(new ContextMenuItemViewModel { IsSeparator = true });

                if (GetIfCanMoveUp(_selectedState.SelectedStateSave, _selectedState.SelectedStateCategorySave))
                {
                    items.Add(new ContextMenuItemViewModel
                    {
                        Text = "^ Move Up",
                        Action = moveUpClick ?? MoveUpClick,
                        Shortcut = "Alt+Up"
                    });
                }
                if (GetIfCanMoveDown(_selectedState.SelectedStateSave, _selectedState.SelectedStateCategorySave))
                {
                    items.Add(new ContextMenuItemViewModel
                    {
                        Text = "v Move Down",
                        Action = moveDownClick ?? MoveDownClick,
                        Shortcut = "Alt+Down"
                    });
                }
            }
        }
        // We used to show the category editing commands if a state was selected
        // (if a state is selected, a category is implicitly selected too). Now we
        // check if a category is highlighted (not state)
        if (_selectedState.SelectedStateCategorySave != null && _selectedState.SelectedStateSave == null)
        {
            items.Add(new ContextMenuItemViewModel { IsSeparator = true });

            items.Add(new ContextMenuItemViewModel
            {
                Text = "Rename Category",
                Action = RenameCategoryClick
            });
            items.Add(new ContextMenuItemViewModel
            {
                Text = "Copy [" + _selectedState.SelectedStateCategorySave.Name + "]",
                Action = () => _copyPasteLogic.OnCopy(CopyType.Category)
            });
            items.Add(new ContextMenuItemViewModel
            {
                Text = "Delete [" + _selectedState.SelectedStateCategorySave.Name + "]",
                Action = DeleteCategoryClick
            });
        }

        return items;
    }

    private void AddMoveToCategoryItems(List<ContextMenuItemViewModel> items)
    {
        var categoryNames = _selectedState.SelectedStateContainer?.Categories
            .Where(item => item != _selectedState.SelectedStateCategorySave)
            .Select(item => item.Name).ToList();

        // As of before 2024 we no longer allow uncategorized non-default states
        if (categoryNames?.Count != 0)
        {
            items.Add(new ContextMenuItemViewModel { IsSeparator = true });

            var moveToCategory = new ContextMenuItemViewModel { Text = "Move to category" };
            items.Add(moveToCategory);

            foreach (var categoryName in categoryNames)
            {
                moveToCategory.Children.Add(new ContextMenuItemViewModel
                {
                    Text = categoryName,
                    Action = () => MoveToCategory(categoryName)
                });
            }
        }
    }

    private void MoveUpClick()
    {
        MoveStateInDirection(-1);
    }

    private void MoveDownClick()
    {
        MoveStateInDirection(1);
    }

    /// <summary>
    /// Moves the selected state up/down within its category (or uncategorized list).
    /// </summary>
    /// <param name="direction">-1 to move up, 1 to move down.</param>
    /// <returns>True if the state actually moved (and the tree/state were saved).</returns>
    public bool MoveStateInDirection(int direction)
    {
        var state = _selectedState.SelectedStateSave;
        var list = _selectedState.SelectedStateContainer.UncategorizedStates;
        if (_selectedState.SelectedStateCategorySave != null)
        {
            list = _selectedState.SelectedStateCategorySave.States;
        }

        bool didMove = false;

        if (list != null && list.Contains(state))
        {
            int oldIndex = list.IndexOf(state);

            if (direction == -1 && GetIfCanMoveUp(state, _selectedState.SelectedStateCategorySave))
            {
                list.RemoveAt(oldIndex);
                list.Insert(oldIndex - 1, state);
                didMove = true;
            }
            else if (direction == 1 && GetIfCanMoveDown(state, _selectedState.SelectedStateCategorySave))
            {
                list.RemoveAt(oldIndex);
                list.Insert(oldIndex + 1, state);
                didMove = true;
            }

            if (didMove)
            {
                _guiCommands.RefreshStateTreeView();
                _fileCommands.TryAutoSaveCurrentObject();
            }
        }

        return didMove;
    }

    private bool GetIfCanMoveUp(StateSave state, StateSaveCategory category)
    {
        var list = _selectedState.SelectedStateCategorySave?.States;
        if (category != null)
        {
            list = category.States;
        }

        if (list == null)
        {
            return false;
        }

        int stateIndex = list.IndexOf(state);

        int indexToBeGreaterThan = 0;
        if (category == null)
        {
            // Uncategorized, so it can't move up above the Default state
            indexToBeGreaterThan = 1;
        }

        return stateIndex > indexToBeGreaterThan;
    }

    private bool GetIfCanMoveDown(StateSave state, StateSaveCategory category)
    {
        var list = _selectedState.SelectedStateCategorySave?.States;
        if (category != null)
        {
            list = category.States;
        }

        if (list == null)
        {
            return false;
        }

        int oldIndex = list.IndexOf(state);
        return oldIndex != list.Count - 1;
    }

    public void DeleteCategoryClick()
    {
        _editCommands.AskToDeleteStateCategory(
            _selectedState.SelectedStateCategorySave,
            _selectedState.SelectedStateContainer);
    }

    public void DeleteStateClick()
    {
        _editCommands.AskToDeleteState(
            _selectedState.SelectedStateSave,
            _selectedState.SelectedStateContainer);
    }

    private void DuplicateStateClick()
    {
        // Is there a "custom" current state save, like an interpolation or animation?
        if (_selectedState.CustomCurrentStateSave != null)
        {
            _dialogService.ShowMessage("Cannot duplicate state while a custom state is displaying. Are you creating or playing animations?");
            return;
        }
        if (_selectedState.SelectedStateCategorySave == null)
        {
            _dialogService.ShowMessage("Cannot duplicate uncategorized states. Select a state in a category first.");
            return;
        }
        ////////End Early Out///////////////

        StateSave newState = _selectedState.SelectedStateSave.Clone();

        newState.ParentContainer = _selectedState.SelectedElement;

        int index = _selectedState.SelectedStateCategorySave.States.IndexOf(_selectedState.SelectedStateSave);

        while (_selectedState.SelectedStateContainer.AllStates.Any(item => item != newState && item.Name == newState.Name))
        {
            newState.Name = StringFunctions.IncrementNumberAtEnd(newState.Name);
        }

        _elementCommands.AddState(_selectedState.SelectedStateContainer, _selectedState.SelectedStateCategorySave, newState, index + 1);

        _guiCommands.RefreshStateTreeView();

        _selectedState.SelectedStateSave = newState;

        _fileCommands.TryAutoSaveCurrentElement();
    }

    public void RenameStateClick()
    {
        _editCommands.AskToRenameState(_selectedState.SelectedStateSave,
            _selectedState.SelectedStateContainer);
    }

    public void RenameCategoryClick()
    {
        _editCommands.AskToRenameStateCategory(
            _selectedState.SelectedStateCategorySave,
            _selectedState.SelectedStateContainer);
    }

    private void MoveToCategory(string categoryNameToMoveTo)
    {
        var stateToMove = _selectedState.SelectedStateSave;
        var stateContainer = _selectedState.SelectedStateContainer;
        _editCommands.MoveToCategory(categoryNameToMoveTo, stateToMove, stateContainer);
    }

    private void AssignToDefault()
    {
        var selectedStateSave = _selectedState.SelectedStateSave;
        var selectedStateContainer = _selectedState.SelectedStateContainer;

        if (selectedStateSave == null || selectedStateContainer == null)
        {
            return;
        }

        _editCommands.SetSetValuesToDefault(selectedStateSave, selectedStateContainer);
    }
}
