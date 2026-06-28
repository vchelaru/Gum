using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Gum.Undo;

/// <summary>
/// Pure orchestrator for undo/redo. It owns the undo-lock lifecycle and the <see cref="UndosChanged"/>
/// event, and delegates all domain-specific snapshot/diff/apply work to per-domain
/// <see cref="IUndoStrategy"/> implementations (element + behavior). The strategy is chosen by the
/// current selection. See #3403 (PR 1 of 2 toward #3399) for the rationale behind the split.
/// </summary>
public class UndoManager : IUndoManager
{
    #region Fields

    private readonly ElementUndoStrategy _elementStrategy;
    private readonly BehaviorUndoStrategy _behaviorStrategy;
    private readonly IReadOnlyList<IUndoStrategy> _strategies;

    internal ObservableCollection<UndoLock> UndoLocks { get; private set; }

    public UndoSnapshot? RecordedSnapshot => _elementStrategy.RecordedSnapshot;
    public ElementHistory CurrentElementHistory => _elementStrategy.CurrentElementHistory;
    public BehaviorHistory? CurrentBehaviorHistory => _behaviorStrategy.CurrentBehaviorHistory;

    // The element strategy is the fallback (its AppliesToCurrentSelection is always true), so the
    // first applicable strategy is the behavior strategy when a behavior is selected, else the element
    // strategy — mirroring the original `SelectedBehavior != null` dispatch.
    private IUndoStrategy ActiveStrategy => _strategies.First(strategy => strategy.AppliesToCurrentSelection);

    #endregion

    #region Events/Invokations

    public event EventHandler<UndoOperationEventArgs>? UndosChanged;

    public void BroadcastUndosChanged() => InvokeUndosChanged(UndoOperation.EntireHistoryChange);

    void InvokeUndosChanged(UndoOperation operation) => UndosChanged?.Invoke(this, new UndoOperationEventArgs { Operation = operation });

    #endregion

    public UndoManager(ISelectedState selectedState,
        IUndoRenameLogic renameLogic,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IMessenger messenger,
        IUndoPluginNotifier pluginNotifier,
        IAnimationUndoProvider animationUndoProvider)
    {
        UndoLocks = new ObservableCollection<UndoLock>();
        UndoLocks.CollectionChanged += HandleUndoLockChanged;

        bool AreUndoLocksActive() => UndoLocks.Count > 0;

        _elementStrategy = new ElementUndoStrategy(selectedState, renameLogic, guiCommands, fileCommands,
            messenger, pluginNotifier, animationUndoProvider, AreUndoLocksActive, InvokeUndosChanged);
        _behaviorStrategy = new BehaviorUndoStrategy(selectedState, guiCommands, fileCommands,
            messenger, pluginNotifier, AreUndoLocksActive, InvokeUndosChanged);

        // Order matters: behavior first, element last (the fallback). See ActiveStrategy.
        _strategies = new IUndoStrategy[] { _behaviorStrategy, _elementStrategy };
    }

    private void HandleUndoLockChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (UndoLocks.Count == 0)
        {
            // Mirror the original `RecordUndo(); RecordBehaviorUndo();` — each strategy self-guards on
            // whether it has a captured baseline, so calling both is safe regardless of selection.
            _elementStrategy.TryRecord();
            _behaviorStrategy.TryRecord();
        }
    }

    /// <summary>
    /// Records the current state, which serves as the "restore point" when an undo occurs. This state will be compared
    /// with the current state in the RecordUndo call to see if any changes should be made.
    /// </summary>
    public void RecordState() => _elementStrategy.CaptureBaseline();

    /// <summary>
    /// Records an undo if any values have changed. This should be called whenever some editing activity has finished.
    /// For example, whenever a value changes in a text box or whenever a drag has finished.
    /// </summary>
    public void RecordUndo() => _elementStrategy.TryRecord();

    public void RecordBehaviorState() => _behaviorStrategy.CaptureBaseline();

    /// <inheritdoc cref="IUndoManager.RecordBehaviorState(BehaviorSave)"/>
    public void RecordBehaviorState(BehaviorSave behavior) => _behaviorStrategy.CaptureBaseline(behavior);

    public void RecordBehaviorUndo() => _behaviorStrategy.TryRecord();

    public void ApplyUndoSnapshotToElement(UndoSnapshot undoSnapshot, ElementSave toApplyTo, bool propagateNameChanges)
        => _elementStrategy.ApplyUndoSnapshotToElement(undoSnapshot, toApplyTo, propagateNameChanges);

    public UndoLock RequestLock()
    {
        // UndoLock lives in the headless Gum.Presentation assembly and can no longer reach back
        // into UndoManager directly, so we hand it the removal as a callback (ADR-0005 Phase 3).
        UndoLock undoLock = null!;
        undoLock = new UndoLock(() => UndoLocks.Remove(undoLock));

        UndoLocks.Add(undoLock);

        return undoLock;
    }

    public bool CanUndo() => ActiveStrategy.CanUndo();

    public bool CanRedo() => ActiveStrategy.CanRedo();

    public void PerformUndo() => ActiveStrategy.PerformUndo();

    public void PerformRedo() => ActiveStrategy.PerformRedo();

    public void ClearAll()
    {
        _elementStrategy.Clear();
        _behaviorStrategy.Clear();
    }
}
