using System.Collections.Generic;
using Gum.DataTypes.Behaviors;

namespace Gum.Undo;

public class BehaviorSnapshot
{
    public BehaviorSave Behavior { get; set; }
}

public class BehaviorHistoryAction
{
    public BehaviorSnapshot UndoState { get; set; }
    public BehaviorSnapshot? RedoState { get; set; }
}

public class BehaviorHistory
{
    public List<BehaviorHistoryAction> Actions { get; set; } = new List<BehaviorHistoryAction>();

    /// <summary>
    /// The index of the next undo to perform. If this is -1, there are no undos to perform.
    /// </summary>
    public int UndoIndex { get; set; } = -1;
}
