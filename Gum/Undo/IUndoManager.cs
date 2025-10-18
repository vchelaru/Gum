using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Undo;
public interface IUndoManager
{
    ElementHistory CurrentElementHistory { get; }

    event EventHandler<UndoOperationEventArgs> UndosChanged;

    void RecordUndo();

    void BroadcastUndosChanged();

    UndoLock RequestLock();

    void ClearAll();

    void RecordState();

    void ApplyUndoSnapshotToElement(UndoSnapshot undoSnapshot, ElementSave toApplyTo, bool propagateNameChanges);

    void PerformUndo();

    void PerformRedo();

    bool CanUndo();
    bool CanRedo();
}
