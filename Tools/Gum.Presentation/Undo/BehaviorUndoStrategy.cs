using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes.Behaviors;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Undo;

/// <summary>
/// The behavior undo/redo track: whole-object clone snapshots, an <see cref="FileManager.AreSaveObjectsEqual"/>
/// diff, and a wholesale apply. Redo uses the explicit per-action <see cref="BehaviorHistoryAction.RedoState"/>.
/// Extracted verbatim from UndoManager's <c>#region Behavior Undo/Redo</c> in #3403.
/// </summary>
public class BehaviorUndoStrategy : IUndoStrategy
{
    private readonly ISelectedState _selectedState;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IMessenger _messenger;
    private readonly IUndoPluginNotifier _pluginNotifier;
    private readonly Func<bool> _areUndoLocksActive;
    private readonly Action<UndoOperation> _raiseUndosChanged;

    bool isRecordingUndos = true;

    Dictionary<BehaviorSave, BehaviorHistory> _behaviorUndos = new Dictionary<BehaviorSave, BehaviorHistory>();

    BehaviorSnapshot? _recordedBehaviorSnapshot;

    public BehaviorHistory? CurrentBehaviorHistory
    {
        get
        {
            var behavior = _selectedState.SelectedBehavior;
            if (behavior != null && _behaviorUndos.TryGetValue(behavior, out var history))
            {
                return history;
            }
            return null;
        }
    }

    public bool AppliesToCurrentSelection => _selectedState.SelectedBehavior != null;

    public BehaviorUndoStrategy(ISelectedState selectedState,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        IMessenger messenger,
        IUndoPluginNotifier pluginNotifier,
        Func<bool> areUndoLocksActive,
        Action<UndoOperation> raiseUndosChanged)
    {
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _messenger = messenger;
        _pluginNotifier = pluginNotifier;
        _areUndoLocksActive = areUndoLocksActive;
        _raiseUndosChanged = raiseUndosChanged;
    }

    public void CaptureBaseline()
    {
        if (_areUndoLocksActive())
        {
            return;
        }

        _recordedBehaviorSnapshot = null;

        var behavior = _selectedState.SelectedBehavior;
        if (behavior != null)
        {
            if (!_behaviorUndos.ContainsKey(behavior))
            {
                _behaviorUndos.Add(behavior, new BehaviorHistory());
            }

            _recordedBehaviorSnapshot = new BehaviorSnapshot
            {
                Behavior = CloneBehavior(behavior)
            };
        }
    }

    /// <inheritdoc cref="IUndoManager.RecordBehaviorState(BehaviorSave)"/>
    public void CaptureBaseline(BehaviorSave behavior)
    {
        if (!_behaviorUndos.ContainsKey(behavior))
        {
            _behaviorUndos.Add(behavior, new BehaviorHistory());
        }

        _recordedBehaviorSnapshot = new BehaviorSnapshot
        {
            Behavior = CloneBehavior(behavior)
        };
    }

    public void TryRecord()
    {
        var canUndo = _recordedBehaviorSnapshot != null &&
            _selectedState.SelectedBehavior != null &&
            isRecordingUndos &&
            !_areUndoLocksActive();

        if (!canUndo)
        {
            return;
        }

        var behavior = _selectedState.SelectedBehavior!;
        bool didChange = !FileManager.AreSaveObjectsEqual(_recordedBehaviorSnapshot!.Behavior, behavior);

        if (didChange && _behaviorUndos.ContainsKey(behavior))
        {
            var history = _behaviorUndos[behavior];

            var isAtEnd = history.UndoIndex == history.Actions.Count - 1;
            if (!isAtEnd)
            {
                while (history.Actions.Count > history.UndoIndex + 1)
                {
                    history.Actions.RemoveAt(history.Actions.Count - 1);
                }
            }

            var action = new BehaviorHistoryAction
            {
                UndoState = new BehaviorSnapshot { Behavior = CloneBehavior(_recordedBehaviorSnapshot.Behavior) },
                RedoState = new BehaviorSnapshot { Behavior = CloneBehavior(behavior) }
            };

            history.Actions.Add(action);
            history.UndoIndex = history.Actions.Count - 1;

            CaptureBaseline();

            _raiseUndosChanged(UndoOperation.HistoryAppended);
        }
    }

    public bool CanUndo()
    {
        var behavior = _selectedState.SelectedBehavior;
        if (behavior != null && _behaviorUndos.TryGetValue(behavior, out var history))
        {
            return history.Actions.Count > 0 && history.UndoIndex > -1;
        }
        return false;
    }

    public bool CanRedo()
    {
        var behavior = _selectedState.SelectedBehavior;
        if (behavior != null && _behaviorUndos.TryGetValue(behavior, out var history))
        {
            var indexToApply = history.UndoIndex + 1;
            return indexToApply < history.Actions.Count &&
                   history.Actions[indexToApply].RedoState != null;
        }
        return false;
    }

    public void PerformUndo()
    {
        var behavior = _selectedState.SelectedBehavior;
        if (behavior == null || !_behaviorUndos.TryGetValue(behavior, out var history))
        {
            return;
        }

        if (history.UndoIndex < 0)
        {
            return;
        }

        var action = history.Actions[history.UndoIndex];
        ApplyBehaviorSnapshot(action.UndoState, behavior);
        history.UndoIndex--;

        DoAfterBehaviorUndoLogic(behavior, UndoOperation.Undo);
    }

    public void PerformRedo()
    {
        var behavior = _selectedState.SelectedBehavior;
        if (behavior == null || !_behaviorUndos.TryGetValue(behavior, out var history))
        {
            return;
        }

        var indexToApply = history.UndoIndex + 1;
        if (indexToApply >= history.Actions.Count)
        {
            return;
        }

        var action = history.Actions[indexToApply];
        if (action.RedoState == null)
        {
            return;
        }

        ApplyBehaviorSnapshot(action.RedoState, behavior);
        history.UndoIndex++;

        DoAfterBehaviorUndoLogic(behavior, UndoOperation.Redo);
    }

    private void ApplyBehaviorSnapshot(BehaviorSnapshot snapshot, BehaviorSave target)
    {
        target.Categories.Clear();
        foreach (var category in snapshot.Behavior.Categories)
        {
            target.Categories.Add(category);
        }

        UndoStateHelper.SetStateContentsFrom(target.RequiredVariables, snapshot.Behavior.RequiredVariables);

        target.RequiredInstances.Clear();
        foreach (var instance in snapshot.Behavior.RequiredInstances)
        {
            target.RequiredInstances.Add(instance);
        }
    }

    private void DoAfterBehaviorUndoLogic(BehaviorSave behavior, UndoOperation operation)
    {
        CaptureBaseline();

        _raiseUndosChanged(operation);

        _messenger.Send(new AfterUndoMessage());

        _guiCommands.RefreshStateTreeView();

        _pluginNotifier.BehaviorSelected(behavior);

        _fileCommands.TryAutoSaveBehavior(behavior);
    }

    private static BehaviorSave CloneBehavior(BehaviorSave behavior)
    {
        return FileManager.CloneSaveObject(behavior);
    }

    public void Clear()
    {
        _behaviorUndos.Clear();
        _recordedBehaviorSnapshot = null;
    }

    /// <summary>
    /// Builds the History-tab description for a single behavior undo action by diffing its before/after
    /// snapshots. The behavior domain owns its own wording here (moved out of UndosViewModel in #3403)
    /// so each domain's descriptions live next to its undo logic.
    /// </summary>
    public static string GetBehaviorActionDescription(BehaviorHistoryAction action)
    {
        var before = action.UndoState.Behavior;
        var after = action.RedoState?.Behavior;

        if (after == null)
        {
            return "Behavior change";
        }

        var parts = new List<string>();

        var beforeCategoryNames = before.Categories.Select(c => c.Name).ToHashSet();
        var afterCategoryNames = after.Categories.Select(c => c.Name).ToHashSet();

        var addedCategoryNames = afterCategoryNames.Except(beforeCategoryNames).ToList();
        var removedCategoryNames = beforeCategoryNames.Except(afterCategoryNames).ToList();

        if (addedCategoryNames.Count > 0)
        {
            parts.Add($"Add categories: {string.Join(", ", addedCategoryNames)}");
        }
        if (removedCategoryNames.Count > 0)
        {
            parts.Add($"Remove categories: {string.Join(", ", removedCategoryNames)}");
        }

        var addedStateNames = new List<string>();
        var removedStateNames = new List<string>();
        foreach (var afterCategory in after.Categories)
        {
            var beforeCategory = before.Categories.FirstOrDefault(c => c.Name == afterCategory.Name);
            if (beforeCategory == null)
            {
                continue;
            }

            var beforeStateNames = beforeCategory.States.Select(s => s.Name).ToHashSet();
            var afterStateNames = afterCategory.States.Select(s => s.Name).ToHashSet();

            addedStateNames.AddRange(afterStateNames.Except(beforeStateNames));
            removedStateNames.AddRange(beforeStateNames.Except(afterStateNames));
        }

        if (addedStateNames.Count > 0)
        {
            parts.Add($"Add states: {string.Join(", ", addedStateNames)}");
        }
        if (removedStateNames.Count > 0)
        {
            parts.Add($"Remove states: {string.Join(", ", removedStateNames)}");
        }

        var beforeInstanceNames = before.RequiredInstances.Select(i => i.Name).ToHashSet();
        var afterInstanceNames = after.RequiredInstances.Select(i => i.Name).ToHashSet();

        var addedInstanceNames = afterInstanceNames.Except(beforeInstanceNames).ToList();
        var removedInstanceNames = beforeInstanceNames.Except(afterInstanceNames).ToList();

        if (addedInstanceNames.Count > 0)
        {
            parts.Add($"Add instances: {string.Join(", ", addedInstanceNames)}");
        }
        if (removedInstanceNames.Count > 0)
        {
            parts.Add($"Remove instances: {string.Join(", ", removedInstanceNames)}");
        }

        return parts.Count > 0 ? string.Join("\n    ", parts) : "Behavior change";
    }
}
