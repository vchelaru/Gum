using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Plugins;
using Gum.ProjectServices.Editing;
using Gum.Undo;
using System.Collections.Generic;
using System.Linq;

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
    /// Removes each override from its state (<see cref="IVariableOverrideRemover"/>), then
    /// <see cref="RecordRemoved"/>. The caller must hold an undo lock, since the recorded removals attach to the
    /// action recorded when it releases. An override whose container is not an element, or an instance value whose
    /// instance no longer exists, is skipped.
    /// </summary>
    void RemoveAndRecordForUndo(IEnumerable<VariableChange> overrides);

    /// <summary>
    /// Records overrides already removed for undo, saves their elements and notifies plugins. The caller must hold
    /// an undo lock.
    /// </summary>
    void RecordRemoved(IReadOnlyList<RemovedVariable> removed);
}

/// <inheritdoc/>
public class InstanceOverrideRemover : IInstanceOverrideRemover
{
    private readonly IUndoManager _undoManager;
    private readonly IFileCommands _fileCommands;
    private readonly IPluginManager _pluginManager;
    private readonly IVariableOverrideRemover _overrideRemover;

    public InstanceOverrideRemover(IUndoManager undoManager, IFileCommands fileCommands, IPluginManager pluginManager,
        IVariableOverrideRemover overrideRemover)
    {
        _undoManager = undoManager;
        _fileCommands = fileCommands;
        _pluginManager = pluginManager;
        _overrideRemover = overrideRemover;
    }

    /// <inheritdoc/>
    public void RemoveAndRecordForUndo(IEnumerable<VariableChange> overrides) =>
        RecordRemoved(_overrideRemover.Remove(overrides));

    /// <inheritdoc/>
    public void RecordRemoved(IReadOnlyList<RemovedVariable> removed)
    {
        foreach (RemovedVariable item in removed)
        {
            InstanceSave? instance = string.IsNullOrEmpty(item.Variable.SourceObject)
                ? null
                : item.Container.GetInstance(item.Variable.SourceObject);
            string notifiedName = instance == null
                ? item.Variable.ExposedAsName ?? item.Variable.GetRootName()
                : item.Variable.GetRootName();

            _fileCommands.TryAutoSaveElement(item.Container);
            _pluginManager.VariableSet(item.Container, instance, notifiedName, null);
        }

        if (removed.Count > 0)
        {
            _undoManager.RecordCrossElementVariableChanges(removed
                .Select(item => CrossElementVariableChange.CaptureBefore(item.Container, item.State, item.Variable))
                .ToList());
        }
    }
}
