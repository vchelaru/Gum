using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Plugins;
using Gum.Undo;
using System.Collections.Generic;

namespace Gum.Services;

/// <summary>
/// Removes values that other elements set for a variable that is going away (a deleted custom
/// variable, an un-exposed variable), on their instances or, for an inheriting element, on itself,
/// and records each removal with
/// <see cref="IUndoManager.RecordCrossElementVariableChanges"/> so the caller's undo entry restores
/// them. See ADR 0016.
/// </summary>
public interface IInstanceOverrideRemover
{
    /// <summary>
    /// Removes each override from its state, saves its element and notifies plugins. The caller must
    /// hold an undo lock, since the recorded removals attach to the action recorded when it releases.
    /// An override whose container is not an element, or an instance value whose instance no longer
    /// exists, is skipped.
    /// </summary>
    void RemoveAndRecordForUndo(IEnumerable<VariableChange> overrides);
}

/// <inheritdoc/>
public class InstanceOverrideRemover : IInstanceOverrideRemover
{
    private readonly IUndoManager _undoManager;
    private readonly IFileCommands _fileCommands;
    private readonly IPluginManager _pluginManager;

    public InstanceOverrideRemover(IUndoManager undoManager, IFileCommands fileCommands, IPluginManager pluginManager)
    {
        _undoManager = undoManager;
        _fileCommands = fileCommands;
        _pluginManager = pluginManager;
    }

    /// <inheritdoc/>
    public void RemoveAndRecordForUndo(IEnumerable<VariableChange> overrides)
    {
        List<CrossElementVariableChange> removals = new List<CrossElementVariableChange>();
        foreach (VariableChange change in overrides)
        {
            if (change.Container is not ElementSave element)
            {
                continue;
            }

            InstanceSave? instance = string.IsNullOrEmpty(change.Variable.SourceObject)
                ? null
                : element.GetInstance(change.Variable.SourceObject);

            // An inheriting element's own value may name no instance, or one it inherits without a copy.
            if (instance == null && !change.IsInheritingElementValue)
            {
                continue;
            }

            string notifiedName = instance == null
                ? change.Variable.ExposedAsName ?? change.Variable.GetRootName()
                : change.Variable.GetRootName();

            removals.Add(CrossElementVariableChange.CaptureBefore(element, change.State, change.Variable));

            change.State.Variables.Remove(change.Variable);
            _fileCommands.TryAutoSaveElement(element);
            _pluginManager.VariableSet(element, instance, notifiedName, null);
        }

        if (removals.Count > 0)
        {
            _undoManager.RecordCrossElementVariableChanges(removals);
        }
    }
}
