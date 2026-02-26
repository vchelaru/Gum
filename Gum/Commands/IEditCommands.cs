using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Commands;

/// <summary>
/// User-triggered edit operations. All delete paths that show a dialog and/or
/// acquire an undo lock should go through this interface rather than calling
/// IDeleteLogic directly.
///
/// ## Two Delete Patterns
///
/// ### Pattern 1 — AskTo* methods (states and categories)
/// States and categories are deleted via explicit typed methods with a simple
/// Yes/No confirmation dialog. They have unique blocking conditions (behavior
/// dependency checks, default-state protection, plugin hooks) that are handled
/// before the dialog is shown.
///
///   AskToDeleteState(...)         — validates then Yes/No confirms
///   AskToDeleteStateCategory(...) — validates then Yes/No confirms
///
/// ### Pattern 2 — DeleteSelection (elements, behaviors, instances)
/// Elements, behaviors, and instances are all deleted via DeleteSelection(),
/// which dispatches based on what is currently selected and shows the richer
/// DeleteOptionsWindow. That window is plugin-extensible (e.g. "Delete XML
/// file?" and "Delete children?" options contributed by DeleteObjectPlugin).
///
///   DeleteSelection() — reads selected objects, shows DeleteOptionsWindow
///
/// The two patterns exist because states/categories are in-memory only (no
/// files, no children) so they don't benefit from DeleteOptionsWindow, while
/// elements/behaviors/instances have files and hierarchy that plugins need to
/// act on.
///
/// See also: IDeleteLogic for the pure data-mutation operations that are
/// called after the user confirms.
/// </summary>
public interface IEditCommands
{
    #region State

    /// <summary>
    /// Validates behavior dependencies and plugin hooks, then shows a Yes/No
    /// confirmation before deleting the state. See the IEditCommands summary
    /// for why states use this pattern instead of <see cref="DeleteSelection"/>.
    /// </summary>
    void AskToDeleteState(StateSave stateSave, IStateContainer stateContainer);

    void AskToRenameState(StateSave stateSave, IStateContainer stateContainer);

    void SetSetValuesToDefault(StateSave stateSave, IStateContainer stateContainer);

    #endregion

    #region Category

    void MoveToCategory(string categoryNameToMoveTo, StateSave stateToMove, IStateContainer stateContainer);

    /// <summary>
    /// Validates behavior dependencies and plugin hooks, then shows a Yes/No
    /// confirmation before deleting the category. See the IEditCommands summary
    /// for why categories use this pattern instead of <see cref="DeleteSelection"/>.
    /// </summary>
    void AskToDeleteStateCategory(StateSaveCategory category, IStateContainer stateCategoryListContainer);

    void AskToRenameStateCategory(StateSaveCategory category, IStateContainer owner);

    #endregion


    #region Behavior

    void RemoveBehaviorVariable(BehaviorSave container, VariableSave variable);

    void AddBehavior();

    #endregion

    #region Element

    void DuplicateSelectedElement();

    void ShowCreateComponentFromInstancesDialog();

    #endregion

    /// <summary>
    /// Deletes whatever is currently selected (elements, behaviors, instances,
    /// or folders). Shows the plugin-extensible DeleteOptionsWindow for
    /// elements/behaviors/instances. For states and categories, use
    /// <see cref="AskToDeleteState"/> or <see cref="AskToDeleteStateCategory"/>
    /// instead — they have their own validation and dialog flow.
    /// </summary>
    void DeleteSelection();
}
