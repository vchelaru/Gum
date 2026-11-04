using Gum.Diagnostics;
using Gum.Commands;
using Gum.PropertyGridHelpers;
using Gum.Undo;
using System;
using System.Linq;
using WpfDataUi.DataTypes;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Owns the batch-level side effects of a multi-select edit: one undo record and one structural
/// refresh per commit, instead of one per selected instance.
/// </summary>
public interface IMultiSelectCommitLogic
{
    /// <summary>
    /// Wires <paramref name="member"/> so its wrapped rows defer refresh/undo to the batch, and the
    /// batch performs them once after every row has been set.
    /// </summary>
    void Attach(MultiSelectInstanceMember member);
}

/// <inheritdoc cref="IMultiSelectCommitLogic"/>
public class MultiSelectCommitLogic : IMultiSelectCommitLogic
{
    private readonly IUndoManager _undoManager;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly IPluginManager _pluginManager;
    private readonly IFileCommands _fileCommands;
    private readonly IVariableReferenceLogic _variableReferenceLogic;

    /// <summary>Creates the logic over the undo manager and the set logic that decides refreshes.</summary>
    public MultiSelectCommitLogic(IUndoManager undoManager, ISetVariableLogic setVariableLogic,
        IPluginManager pluginManager, IFileCommands fileCommands, IVariableReferenceLogic variableReferenceLogic)
    {
        _undoManager = undoManager;
        _setVariableLogic = setVariableLogic;
        _pluginManager = pluginManager;
        _fileCommands = fileCommands;
        _variableReferenceLogic = variableReferenceLogic;
    }

    /// <inheritdoc/>
    public void Attach(MultiSelectInstanceMember member)
    {
        foreach (StateReferencingInstanceMember row in member.InstanceMembers.OfType<StateReferencingInstanceMember>())
        {
            row.IsCallingRefresh = false;
        }

        IDisposable? undoLock = null;
        IDisposable? pluginBatch = null;
        IDisposable? saveBatch = null;
        IDisposable? referenceBatch = null;

        ToolLog.Scope commitLog = default;
        member.BeforeMultiSet += (args) =>
        {
            commitLog = ToolLog.Begin("MultiSelectCommit", count: member.InstanceMembers.Count(), detail: args.CommitType.ToString());
            // Only lock for Full commits to avoid locking during intermediate changes (like dragging sliders)
            if (args.CommitType == SetPropertyCommitType.Full)
            {
                undoLock = _undoManager.RequestLock();
                // Element-wide work (the error check, code generation, the file save) runs once for the
                // batch, not once per selected instance.
                pluginBatch = _pluginManager.BeginVariableSetBatch();
                saveBatch = _fileCommands.BeginAutoSaveBatch();
                // Re-applying the state's references is state-wide too, so it runs once after the last instance.
                referenceBatch = _variableReferenceLogic.BeginReferenceBatch();
            }
        };

        member.AfterMultiSet += (args) =>
        {
            // The reference reaction runs first, still inside the plugin and save batches, so what it changes
            // is announced and saved once.
            referenceBatch?.Dispose();
            referenceBatch = null;
            pluginBatch?.Dispose();
            pluginBatch = null;
            saveBatch?.Dispose();
            saveBatch = null;
            undoLock?.Dispose();
            undoLock = null;
            commitLog.Dispose();
            commitLog = default;

            if (args.CommitType != SetPropertyCommitType.Full)
            {
                return;
            }

            // Record undo after all values have been set
            using (ToolLog.Begin("RecordUndo"))
            {
                _undoManager.RecordUndo();
            }

            // Every row shares the root variable and the containing element, so the first row
            // decides whether the batch needs a tree/grid rebuild.
            StateReferencingInstanceMember? firstRow = member.InstanceMembers
                .OfType<StateReferencingInstanceMember>()
                .FirstOrDefault();
            if (firstRow?.ElementSave != null)
            {
                _setVariableLogic.RefreshInResponseToVariableChange(firstRow.RootVariableName, firstRow.ElementSave, firstRow.InstanceSave);
            }
        };
    }
}
