using System;
using System.Collections.Generic;
using Gum.Commands;
using Gum.Logic;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.ViewModels;

namespace Gum.Managers;

/// <summary>
/// Keeps the states tree's right-click menu current for the selection, as neutral menu items, and
/// runs the commands behind it. Each head renders <see cref="MenuItems"/> into its own context menu
/// and re-renders on <see cref="MenuItemsChanged"/>; which items to show is decided by
/// <see cref="StateTreeRightClickViewModel"/>.
/// </summary>
public class StateTreeRightClickService : IStateTreeViewRightClickService
{
    private readonly StateTreeRightClickViewModel _viewModel;

    /// <summary>Creates the service over the commands its menu items run.</summary>
    public StateTreeRightClickService(ISelectedState selectedState,
        IElementCommands elementCommands,
        IEditCommands editCommands,
        IDialogService dialogService,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ICopyPasteLogic copyPasteLogic)
    {
        _viewModel = new StateTreeRightClickViewModel(
            selectedState,
            elementCommands,
            editCommands,
            dialogService,
            guiCommands,
            fileCommands,
            copyPasteLogic);
        MenuItems = Array.Empty<ContextMenuItemViewModel>();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ContextMenuItemViewModel> MenuItems { get; private set; }

    /// <inheritdoc/>
    public event Action? MenuItemsChanged;

    /// <inheritdoc/>
    public void PopulateContextMenu()
    {
        // "Move Up"/"Move Down" rebuild the menu after a successful move, so their own enabled
        // state is fresh for the next right-click.
        MenuItems = _viewModel.GetMenuItems(
            moveUpClick: () => MoveStateInDirection(-1),
            moveDownClick: () => MoveStateInDirection(1));
        MenuItemsChanged?.Invoke();
    }

    /// <inheritdoc/>
    public void MoveStateInDirection(int direction)
    {
        if (_viewModel.MoveStateInDirection(direction))
        {
            PopulateContextMenu();
        }
    }

    /// <inheritdoc/>
    public void DeleteCategoryClick() => _viewModel.DeleteCategoryClick();

    /// <inheritdoc/>
    public void DeleteStateClick() => _viewModel.DeleteStateClick();

    /// <inheritdoc/>
    public void RenameStateClick() => _viewModel.RenameStateClick();

    /// <inheritdoc/>
    public void RenameCategoryClick() => _viewModel.RenameCategoryClick();
}
