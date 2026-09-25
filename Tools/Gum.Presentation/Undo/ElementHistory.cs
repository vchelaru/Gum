using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.Undo;

public class HistoryAction
{
    public required UndoSnapshot UndoState { get; set; }

    /// <summary>Null when the action has nothing to redo.</summary>
    public UndoSnapshot? RedoState { get; set; }

    /// <summary>
    /// Variable changes on OTHER elements made by this action (e.g. deleting a state removes the
    /// variables that set it elsewhere). Null when this action has none. Undo reverses each entry;
    /// redo re-applies it. See ADR 0016.
    /// </summary>
    public List<CrossElementVariableChange>? CrossElementVariableChanges { get; set; }

    public override string ToString()
    {
        return $"Undo:{UndoState}";
    }
}

public class ElementHistory
{
    /// <summary>The element as it was before the first undo from the end of the history; null until then.</summary>
    public ElementSave? FinalState { get; set; }

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
