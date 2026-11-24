using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Gum.Undo;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Logic;

/// <summary>
/// Drops what breaks when Create Component moves instances out of their element and into a new
/// component, through <see cref="IMovedInstanceReferenceDropLogic"/>, and records it for undo, removes the
/// animation keyframes that play a moved instance's sub-animations, and saves the other elements it changed.
/// Called by <see cref="CopyPasteLogic.CreateComponentFromInstance"/> before it removes the subtree.
/// </summary>
public interface IMovedInstanceReferenceDropper
{
    /// <summary>
    /// Drops each reference that stops applying once <paramref name="movedInstances"/> leave
    /// <paramref name="sourceElement"/> and returns what <see cref="IMovedInstanceReferenceDropLogic.Find"/>
    /// reports. The caller must hold an undo lock: changes to other elements are recorded with
    /// <see cref="IUndoManager.RecordCrossElementVariableChanges"/>, and changes to
    /// <paramref name="sourceElement"/> are covered by its own undo snapshot.
    /// </summary>
    MovedInstanceReference[] DropReferencesBrokenByMove(ElementSave sourceElement, InstanceSave promotedInstance,
        IReadOnlyCollection<InstanceSave> movedInstances);
}

/// <inheritdoc/>
public class MovedInstanceReferenceDropper : IMovedInstanceReferenceDropper
{
    private readonly IMovedInstanceReferenceDropLogic _dropLogic;
    private readonly IAnimationUndoProvider _animations;
    private readonly IUndoManager _undoManager;
    private readonly IFileCommands _fileCommands;

    public MovedInstanceReferenceDropper(IMovedInstanceReferenceDropLogic dropLogic, IAnimationUndoProvider animations,
        IUndoManager undoManager, IFileCommands fileCommands)
    {
        _dropLogic = dropLogic;
        _animations = animations;
        _undoManager = undoManager;
        _fileCommands = fileCommands;
    }

    /// <inheritdoc/>
    public MovedInstanceReference[] DropReferencesBrokenByMove(ElementSave sourceElement, InstanceSave promotedInstance,
        IReadOnlyCollection<InstanceSave> movedInstances)
    {
        MovedInstanceReferenceDrops drops = _dropLogic.Find(sourceElement, promotedInstance, movedInstances,
            _animations.GetCurrentAnimations(sourceElement));

        List<CrossElementVariableChange> changes = new List<CrossElementVariableChange>();
        List<(CrossElementVariableChange Change, VariableListSave List)> listsLosingLines =
            new List<(CrossElementVariableChange, VariableListSave)>();
        HashSet<ElementSave> otherElements = new HashSet<ElementSave>();

        IEnumerable<(ElementSave Container, StateSave State, VariableListSave List)> changedLists = drops.Lines
            .Select(item => (item.Container, item.State, item.List))
            .Concat(drops.Rewrites.Select(item => (item.Container, item.State, item.List)));
        foreach ((ElementSave container, StateSave state, VariableListSave list) in changedLists.GroupBy(item => item.List)
            .Select(group => group.First()))
        {
            if (container != sourceElement)
            {
                CrossElementVariableChange change = CrossElementVariableChange.CaptureBefore(container, state, list);
                changes.Add(change);
                listsLosingLines.Add((change, list));
                otherElements.Add(container);
            }
        }
        List<(CrossElementVariableChange Change, VariableSave Variable)> renamed =
            new List<(CrossElementVariableChange, VariableSave)>();
        foreach (MovedReferenceVariableRename rename in drops.Renames.Where(item => item.Container != sourceElement))
        {
            // A rename is the old variable removed and the new one added.
            changes.Add(CrossElementVariableChange.CaptureBefore(rename.Container, rename.State, rename.Variable));
            foreach (VariableSave replaced in rename.State.Variables.Where(item => item.Name == rename.NewName))
            {
                changes.Add(CrossElementVariableChange.CaptureBefore(rename.Container, rename.State, replaced));
            }
            CrossElementVariableChange added = CrossElementVariableChange.CaptureBefore(rename.Container, rename.State, rename.Variable);
            added.Before = null;
            changes.Add(added);
            renamed.Add((added, rename.Variable));
            otherElements.Add(rename.Container);
        }
        foreach (MovedReferenceItemDrop<VariableSave> drop in drops.Variables.Where(item => item.Container != sourceElement))
        {
            changes.Add(CrossElementVariableChange.CaptureBefore(drop.Container, drop.State, drop.Item));
            otherElements.Add(drop.Container);
        }
        foreach (MovedReferenceItemDrop<VariableListSave> drop in drops.Lists.Where(item => item.Container != sourceElement))
        {
            changes.Add(CrossElementVariableChange.CaptureBefore(drop.Container, drop.State, drop.Item));
            otherElements.Add(drop.Container);
        }

        _dropLogic.Drop(drops);

        foreach ((CrossElementVariableChange change, VariableListSave list) in listsLosingLines)
        {
            // A list left empty is removed, so it has no after.
            if (list.ValueAsIList.Count > 0)
            {
                change.CaptureAfter(list);
            }
        }
        foreach ((CrossElementVariableChange change, VariableSave variable) in renamed)
        {
            change.CaptureAfter(variable);
        }

        foreach (string name in drops.InstancesWithSubAnimations)
        {
            // Joins the source element's undo record, whose snapshot includes its animations.
            _animations.RemoveKeyframesPlaying(sourceElement, name);
        }

        if (changes.Count > 0)
        {
            _undoManager.RecordCrossElementVariableChanges(changes);
        }
        foreach (ElementSave element in otherElements)
        {
            _fileCommands.TryAutoSaveElement(element);
        }

        return drops.Reports.ToArray();
    }
}
