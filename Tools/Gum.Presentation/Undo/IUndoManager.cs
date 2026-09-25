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
    void RecordCrossElementVariableChanges(IEnumerable<CrossElementVariableChange> changes);

    void PerformUndo();

    void PerformRedo();

    bool CanUndo();
    bool CanRedo();
}
