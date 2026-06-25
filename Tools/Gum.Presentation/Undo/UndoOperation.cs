using System;

namespace Gum.Undo;

public enum UndoOperation
{
    Undo,
    Redo,
    EntireHistoryChange,
    HistoryAppended
}

public class UndoOperationEventArgs : EventArgs
{
    public UndoOperation Operation { get; set; }
}
