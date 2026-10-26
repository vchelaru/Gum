using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Undo;
public interface IUndoManager
{
    ElementHistory? CurrentElementHistory { get; }
    BehaviorHistory? CurrentBehaviorHistory { get; }

    event EventHandler<UndoOperationEventArgs> UndosChanged;

    void RecordUndo();

    void BroadcastUndosChanged();

    UndoLock RequestLock();

    /// <summary>
    /// As <see cref="RequestLock()"/>, for a change made to <paramref name="element"/> while it is
    /// not necessarily the selected element (a tree drop into another element, say). The change
    /// records in that element's own history when the lock is released. Must be requested before
    /// the element is changed. For the selected element this is the same as a plain lock.
    /// </summary>
    UndoLock RequestLock(ElementSave element);

    void ClearAll();

    /// <summary>
    /// Reacts to a project load. Reopening the file that was already open (an import or Add Forms
    /// saves and reopens the project) keeps the history of every element and behavior the reload
    /// left unchanged, moved onto the reloaded objects; anything else clears all history.
    /// </summary>
    void HandleProjectLoaded(GumProjectSave project);

    void RecordState();
    void RecordBehaviorState();
    /// <summary>
    /// Records the current state of a specific behavior for undo purposes, bypassing the
    /// undo lock. Use this when the caller is already inside an undo lock but must capture
    /// the pre-change state (for example, during drag+drop operations targeting a behavior
    /// that may not be the currently selected one).
    /// </summary>
    void RecordBehaviorState(BehaviorSave behavior);
    void RecordBehaviorUndo();

    void ApplyUndoSnapshotToElement(UndoSnapshot undoSnapshot, ElementSave toApplyTo, bool propagateNameChanges);

    /// <summary>
    /// Records variable changes made on OTHER elements as part of the selected element's current edit,
    /// so undoing that edit reverses them and redoing it re-applies them. Call while holding the
    /// <see cref="RequestLock()"/> that brackets the edit; the changes attach to the action recorded
    /// when the lock is released. Ignored when no lock is held. See ADR 0016.
    /// </summary>
    /// <summary>
    /// Brings every undo and redo snapshot of the changed elements up to date with values written outside
    /// an undoable edit (a reference re-evaluated after a reload from disk). The history stays; undoing an
    /// earlier edit no longer restores the replaced value. No history entry is added.
    /// </summary>
    void RebaseStoredValues(IReadOnlyList<StoredValueChange> changes);

    void RecordCrossElementVariableChanges(IEnumerable<CrossElementVariableChange> changes);

    void PerformUndo();

    void PerformRedo();

    bool CanUndo();
    bool CanRedo();
}
