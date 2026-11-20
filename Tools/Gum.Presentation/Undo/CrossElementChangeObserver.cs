using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;

namespace Gum.Undo;

/// <summary>
/// Captures each variable or list a rename rewrites on another element as a <see cref="CrossElementVariableChange"/>,
/// in the order they were rewritten, for the caller to hand to the undo manager.
/// </summary>
public class CrossElementChangeObserver : IRenameChangeObserver
{
    /// <summary>The changes captured so far.</summary>
    public List<CrossElementVariableChange> Changes { get; } = new List<CrossElementVariableChange>();

    /// <inheritdoc/>
    public object? BeforeChange(ElementSave container, StateSave state, VariableSave variable)
    {
        CrossElementVariableChange change = CrossElementVariableChange.CaptureBefore(container, state, variable);
        Changes.Add(change);
        return change;
    }

    /// <inheritdoc/>
    public void AfterChange(object? token, VariableSave variable) => ((CrossElementVariableChange)token!).CaptureAfter(variable);

    /// <inheritdoc/>
    public object? BeforeChange(ElementSave container, StateSave state, VariableListSave list)
    {
        CrossElementVariableChange change = CrossElementVariableChange.CaptureBefore(container, state, list);
        Changes.Add(change);
        return change;
    }

    /// <inheritdoc/>
    public void AfterChange(object? token, VariableListSave list) => ((CrossElementVariableChange)token!).CaptureAfter(list);
}
