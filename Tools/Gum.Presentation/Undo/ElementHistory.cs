using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.Undo;

public class HistoryAction
{
    public UndoSnapshot UndoState { get; set; }
    public UndoSnapshot RedoState { get; set; }

    public override string ToString()
    {
        return $"Undo:{UndoState}";
    }
}

public class ElementHistory
{
    public ElementSave FinalState { get; set; }

    /// <summary>
    /// A list of actions for the current element, where the most recent action is at the end of the list.
    /// </summary>
    public List<HistoryAction> Actions { get; set; } = new List<HistoryAction>();

    /// <summary>
    /// The index of the next undo to perform. If this is -1, then there are no undos to perform.
    /// Note that this means that the next redo to perform is at UndoIndex + 1.
    /// </summary>
    public int UndoIndex { get; set; } = -1;
}
