using Gum.Commands;
using Gum.Extensions;
using Gum.Logic;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using System.Windows;

namespace Gum.Managers;

public class StateTreeViewRightClickService : IStateTreeViewRightClickService
{
    private readonly StateTreeRightClickViewModel _viewModel;

    System.Windows.Controls.ContextMenu _contextMenu;

    public StateTreeViewRightClickService(ISelectedState selectedState,
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
    }

    public void SetContextMenu(System.Windows.Controls.ContextMenu contextMenu, FrameworkElement contextMenuOwner)
    {
        contextMenuOwner.ContextMenuOpening += (_, args) =>
        {
            if (_contextMenu.Items.Count == 0)
            {
                args.Handled = true;
            }
        };

        _contextMenu = contextMenu;
        _contextMenu.ContextMenuOpening += (s, e) =>
        {
            if (_contextMenu.Items.Count == 0)
            {
                e.Handled = true; // Prevent the menu from opening
            }
        };
    }

    /// <inheritdoc/>
    public void PopulateContextMenu()
    {
        _contextMenu.Items.Clear();

        // "Move Up"/"Move Down" need to re-populate this live context menu after a successful
        // move so their own enabled state is fresh for the next right-click - the one WPF-only
        // side effect the headless view model has no seam for.
        var menuItems = _viewModel.GetMenuItems(
            moveUpClick: () => MoveStateInDirection(-1),
            moveDownClick: () => MoveStateInDirection(1));

        foreach (var item in menuItems)
        {
            _contextMenu.Items.Add(item.ToMenuItem());
        }
    }

    public void MoveStateInDirection(int direction)
    {
        if (_viewModel.MoveStateInDirection(direction))
        {
            PopulateContextMenu();
        }
    }

    public void DeleteCategoryClick() => _viewModel.DeleteCategoryClick();

    public void DeleteStateClick() => _viewModel.DeleteStateClick();

    public void RenameStateClick() => _viewModel.RenameStateClick();

    public void RenameCategoryClick() => _viewModel.RenameCategoryClick();
}
