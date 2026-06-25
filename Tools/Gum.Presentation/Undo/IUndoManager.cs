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
    ElementHistory CurrentElementHistory { get; }
    BehaviorHistory? CurrentBehaviorHistory { get; }

    event EventHandler<UndoOperationEventArgs> UndosChanged;

    void RecordUndo();

    void BroadcastUndosChanged();

    UndoLock RequestLock();

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

    void PerformUndo();

    void PerformRedo();

    bool CanUndo();
    bool CanRedo();
}
