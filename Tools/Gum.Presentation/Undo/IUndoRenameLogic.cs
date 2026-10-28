using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using ToolsUtilities;

namespace Gum.Undo;

/// <summary>
/// Narrow rename port the undo subsystem uses to re-apply name changes when an
/// undo/redo restores an element or variable. It exposes only the rename calls
/// <see cref="UndoManager"/> actually makes, all of which take headless
/// (<c>GumDataTypes</c>/<c>ToolsUtilities</c>) parameters, so the undo logic no longer
/// needs to depend on the full, MEF/WinForms-entangled <c>IRenameLogic</c> (ADR-0005 Phase 3).
/// The live implementation is <c>RenameLogic</c>, bridged via DI to the same instance as
/// <c>IRenameLogic</c>. This mirrors <see cref="IUndoPluginNotifier"/>.
/// </summary>
public interface IUndoRenameLogic
{
    GeneralResponse HandleRename(IInstanceContainer instanceContainer, InstanceSave? instance, string oldName, NameChangeAction action, bool askAboutRename = true);

    VariableChangeResponse GetChangesForRenamedVariable(IStateContainer owner, string oldFullName, string oldStrippedOrExposedName);

    /// <summary>
    /// Gives <paramref name="behavior"/> <paramref name="newName"/> and updates what names it: the
    /// references on elements, the project's reference and the behavior's file. Used when undo or
    /// redo of a behavior rename restores the other name.
    /// </summary>
    void RenameBehavior(BehaviorSave behavior, string newName);

    void ApplyVariableRenameChanges(VariableChangeResponse changes, string oldStrippedOrExposedName, string newStrippedOrExposedName, HashSet<ElementSave> elementsNeedingSave);
}
