using System;
using System.Collections.Generic;
using Gum.ViewModels;

namespace Gum.Managers;

/// <summary>
/// The states tree's right-click menu and the commands behind it. The menu is kept as neutral
/// <see cref="ContextMenuItemViewModel"/> items that each head renders into its own control; see
/// <see cref="StateTreeRightClickService"/>.
/// </summary>
public interface IStateTreeViewRightClickService
{
    /// <summary>The menu for the current selection, as of the last <see cref="PopulateContextMenu"/>.</summary>
    IReadOnlyList<ContextMenuItemViewModel> MenuItems { get; }

    /// <summary>Raised after <see cref="MenuItems"/> is rebuilt.</summary>
    event Action? MenuItemsChanged;

    /// <summary>
    /// Rebuilds the right-click context menu items to match the currently selected state/category.
    /// </summary>
    void PopulateContextMenu();

    /// <summary>Moves the selected state up (-1) or down (1) within its category.</summary>
    void MoveStateInDirection(int direction);

    /// <summary>Asks to delete the selected category.</summary>
    void DeleteCategoryClick();

    /// <summary>Asks to delete the selected state.</summary>
    void DeleteStateClick();

    /// <summary>Asks for a new name for the selected state.</summary>
    void RenameStateClick();

    /// <summary>Asks for a new name for the selected category.</summary>
    void RenameCategoryClick();
}
