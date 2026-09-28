using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Logic;

/// <summary>
/// Drops what breaks when Create Component moves instances out of their element and into a new
/// component: references from outside the moved subtree that name a moved instance. A dropped
/// <c>VariableReferences</c> line leaves behind the value it last resolved to, the same as the
/// references inside the subtree that the move carries as plain values. Called by
/// <see cref="CopyPasteLogic.CreateComponentFromInstance"/> before it removes the subtree.
/// </summary>
public interface IMovedInstanceReferenceDropper
{
    /// <summary>
    /// Drops each reference that stops applying once <paramref name="movedInstances"/> leave
    /// <paramref name="sourceElement"/> and returns one entry per reference, grouped by the moved
    /// instance it names. Also returns, without dropping them, the <c>VariableReferences</c> lines the
    /// moved instances and <paramref name="promotedInstance"/> own in the default state that read from
    /// outside the subtree: the move leaves those behind and carries their values. The caller must hold an undo lock: changes to other elements are recorded with
    /// <see cref="IUndoManager.RecordCrossElementVariableChanges"/>, and changes to
    /// <paramref name="sourceElement"/> are covered by its own undo snapshot.
    /// </summary>
    MovedInstanceReference[] DropReferencesBrokenByMove(ElementSave sourceElement, InstanceSave promotedInstance,
        IReadOnlyCollection<InstanceSave> movedInstances);
}

/// <summary>
/// A reference to or from <see cref="MovedInstanceName"/> that was dropped because it moved.
/// <see cref="KeptValue"/> is the stored value the dropped line leaves in place, such as
/// <c>Caption.X = 5</c>, or null when nothing takes its place.
/// </summary>
public record MovedInstanceReference(string MovedInstanceName, string Description, string? KeptValue = null);

/// <inheritdoc/>
public class MovedInstanceReferenceDropper : IMovedInstanceReferenceDropper
{
    private static readonly HashSet<string> InstanceNamingVariableRootNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "Parent", "RenderTargetTextureSource"
    };

    private readonly IReferenceFinder _referenceFinder;
    private readonly IAnimationUndoProvider _animations;
    private readonly IUndoManager _undoManager;
    private readonly IFileCommands _fileCommands;

    public MovedInstanceReferenceDropper(IReferenceFinder referenceFinder, IAnimationUndoProvider animations,
        IUndoManager undoManager, IFileCommands fileCommands)
    {
        _referenceFinder = referenceFinder;
        _animations = animations;
        _undoManager = undoManager;
        _fileCommands = fileCommands;
    }

    /// <inheritdoc/>
    public MovedInstanceReference[] DropReferencesBrokenByMove(ElementSave sourceElement, InstanceSave promotedInstance,
        IReadOnlyCollection<InstanceSave> movedInstances)
    {
        Drops drops = new Drops();
        HashSet<string> insideNames = new HashSet<string>(movedInstances.Select(item => item.Name), StringComparer.Ordinal)
        {
            promotedInstance.Name
        };
        StateSave defaultState = sourceElement.GetDefaultStateOrThrow();

        foreach (InstanceSave moved in movedInstances)
        {
            FindReferencesTo(moved.Name, sourceElement, promotedInstance.Name, insideNames, defaultState, drops);
        }

        List<MovedInstanceReference> results = drops.Reports.ToList();
        results.AddRange(GetLinesReadingOutside(sourceElement, defaultState, insideNames));

        Apply(sourceElement, drops);

        return results.ToArray();
    }

    private void FindReferencesTo(string name, ElementSave sourceElement, string promotedName, HashSet<string> insideNames,
        StateSave defaultState, Drops drops)
    {
        InstanceSave moved = sourceElement.GetInstance(name)!;
        InstanceReferences references = _referenceFinder.GetReferencesToInstance(sourceElement, moved, name);
        ICollection<ElementSave> derivedElements = ObjectFinder.Self.GetElementsInheritingFrom(sourceElement);

        foreach (VariableReferenceChange change in references.VariableReferenceChanges)
        {
            StateSave state = change.Container.AllStates.First(item => item.VariableLists.Contains(change.VariableReferenceList));
            string? owner = change.VariableReferenceList.SourceObject;
            bool isOwnedInsideSubtree = owner != null && insideNames.Contains(owner) &&
                (change.Container == sourceElement || derivedElements.Contains(change.Container));
            // Only the default state moves into the component, and a moved instance's lists anywhere
            // else are dropped whole below. The promoted instance stays, so its other lists stay too.
            bool isCarriedOrDroppedWhole = isOwnedInsideSubtree &&
                (owner != promotedName || (change.Container == sourceElement && state == defaultState));
            if (!isCarriedOrDroppedWhole)
            {
                drops.AddLine(name, change.Container, state, change.VariableReferenceList, change.LineIndex);
            }
        }

        foreach ((ElementSave container, VariableSave variable) in references.ParentVariablesInOtherElements)
        {
            StateSave state = container.AllStates.First(item => item.Variables.Contains(variable));
            drops.AddVariable(name, container, state, variable, $"{container.Name}: {variable.Name} = {variable.Value}");
        }

        foreach (StateSave state in sourceElement.AllStates)
        {
            bool isDefault = state == defaultState;
            foreach (VariableSave variable in state.Variables)
            {
                if (variable.SourceObject == name)
                {
                    if (!isDefault)
                    {
                        drops.AddVariable(name, sourceElement, state, variable,
                            $"{sourceElement.Name} ({state.Name}): {variable.Name} = {variable.Value}");
                    }
                    else if (!string.IsNullOrEmpty(variable.ExposedAsName))
                    {
                        // The variable itself moves into the component, unexposed; what other elements set
                        // or read through the exposed name no longer applies.
                        drops.Report(name, $"{sourceElement.Name}: exposed variable {variable.ExposedAsName} ({variable.Name})");
                        FindUsesOfExposedVariable(name, sourceElement, variable, drops);
                    }
                }
                else if (!insideNames.Contains(variable.SourceObject ?? "") &&
                    InstanceNamingVariableRootNames.Contains(variable.GetRootName()) &&
                    variable.Value is string value && (value == name || value.StartsWith(name + ".", StringComparison.Ordinal)))
                {
                    drops.AddVariable(name, sourceElement, state, variable,
                        $"{sourceElement.Name} ({state.Name}): {variable.Name} = {value}");
                }
                else if (variable.Name == "DefaultChildContainer" && variable.Value as string == name)
                {
                    drops.AddVariable(name, sourceElement, state, variable, $"{sourceElement.Name}: DefaultChildContainer = {name}");
                }
            }
            if (!isDefault)
            {
                foreach (VariableListSave variableList in state.VariableLists.Where(item => item.SourceObject == name))
                {
                    drops.AddList(name, sourceElement, state, variableList, $"{sourceElement.Name} ({state.Name}): {variableList.Name}");
                }
            }
        }

        // A derived element's overrides of the moved instance lose the instance they set.
        foreach (ElementSave derived in derivedElements)
        {
            foreach (StateSave state in derived.AllStates)
            {
                foreach (VariableSave variable in state.Variables.Where(item => item.SourceObject == name))
                {
                    drops.AddVariable(name, derived, state, variable, $"{derived.Name} ({state.Name}): {variable.Name} = {variable.Value}");
                }
                foreach (VariableListSave variableList in state.VariableLists.Where(item => item.SourceObject == name))
                {
                    drops.AddList(name, derived, state, variableList, $"{derived.Name} ({state.Name}): {variableList.Name}");
                }
            }
        }

        ElementAnimationsSave? animations = _animations.GetCurrentAnimations(sourceElement);
        if (animations != null)
        {
            foreach (AnimationSave animation in animations.Animations)
            {
                foreach (AnimationReferenceSave subAnimation in animation.Animations.Where(item => item.SourceObject == name))
                {
                    drops.Report(name, $"{sourceElement.Name}: animation {animation.Name} plays {subAnimation.Name}");
                    drops.InstancesWithSubAnimations.Add(name);
                }
            }
        }
    }

    private void FindUsesOfExposedVariable(string name, ElementSave sourceElement, VariableSave exposed, Drops drops)
    {
        VariableChangeResponse uses = _referenceFinder.GetReferencesToVariable(sourceElement, exposed.Name, exposed.ExposedAsName!);
        foreach (VariableChange use in uses.VariableChanges)
        {
            // A non-removable change is another element exposing a variable of its own under that name.
            if (use.Container is ElementSave container && use.IsRemovableValue)
            {
                drops.AddVariable(name, container, use.State, use.Variable,
                    $"{container.Name} ({use.State.Name}): {use.Variable.Name} = {use.Variable.Value}");
            }
        }
        foreach (VariableReferenceChange change in uses.VariableReferenceChanges)
        {
            StateSave state = change.Container.AllStates.First(item => item.VariableLists.Contains(change.VariableReferenceList));
            drops.AddLine(name, change.Container, state, change.VariableReferenceList, change.LineIndex);
        }
    }

    /// <summary>
    /// The default-state lines owned by an instance inside the subtree whose right side reads from
    /// outside it. The move drops the whole list and carries each line's stored value.
    /// </summary>
    private static IEnumerable<MovedInstanceReference> GetLinesReadingOutside(ElementSave sourceElement, StateSave defaultState,
        HashSet<string> insideNames)
    {
        foreach (VariableListSave list in defaultState.VariableLists)
        {
            if (list.GetRootName() != "VariableReferences" || list.SourceObject is not { } owner || !insideNames.Contains(owner))
            {
                continue;
            }
            foreach (object item in list.ValueAsIList)
            {
                if (item is not string line || line.StartsWith("//") || !line.Contains('='))
                {
                    continue;
                }
                string rightSide = line.Substring(line.IndexOf('=') + 1).Trim();
                int colonIndex = rightSide.IndexOf(':');
                string read = colonIndex >= 0 ? rightSide.Substring(colonIndex + 1) : rightSide;
                if (!insideNames.Any(inside => read.StartsWith(inside + ".", StringComparison.Ordinal)))
                {
                    yield return new MovedInstanceReference(owner,
                        $"{sourceElement.Name} ({defaultState.Name}): {list.Name} line \"{line}\"", GetKeptValue(defaultState, list, line));
                }
            }
        }
    }

    private static string? GetKeptValue(StateSave state, VariableListSave list, string line)
    {
        int equalIndex = line.IndexOf('=');
        if (equalIndex < 0)
        {
            return null;
        }
        string leftSide = line.Substring(0, equalIndex).Trim();
        string qualified = string.IsNullOrEmpty(list.SourceObject) ? leftSide : $"{list.SourceObject}.{leftSide}";
        VariableSave? stored = state.Variables.FirstOrDefault(item => item.Name == qualified);
        return stored == null ? null : $"{qualified} = {stored.Value}";
    }

    private void Apply(ElementSave sourceElement, Drops drops)
    {
        List<CrossElementVariableChange> changes = new List<CrossElementVariableChange>();
        HashSet<ElementSave> otherElements = new HashSet<ElementSave>();

        foreach (IGrouping<VariableListSave, LineDrop> linesInList in drops.Lines.GroupBy(item => item.List))
        {
            LineDrop first = linesInList.First();
            bool isOther = first.Container != sourceElement;
            CrossElementVariableChange? change = isOther ? CrossElementVariableChange.CaptureBefore(first.Container, first.State, first.List) : null;
            foreach (int index in linesInList.Select(item => item.Index).Distinct().OrderByDescending(item => item))
            {
                first.List.ValueAsIList.RemoveAt(index);
            }
            bool isEmpty = first.List.ValueAsIList.Count == 0;
            if (isEmpty)
            {
                first.State.VariableLists.Remove(first.List);
            }
            else
            {
                change?.CaptureAfter(first.List);
            }
            if (change != null)
            {
                changes.Add(change);
                otherElements.Add(first.Container);
            }
        }

        foreach ((ElementSave container, StateSave state, VariableSave variable) in drops.Variables)
        {
            if (container != sourceElement)
            {
                changes.Add(CrossElementVariableChange.CaptureBefore(container, state, variable));
                otherElements.Add(container);
            }
            state.Variables.Remove(variable);
        }

        foreach ((ElementSave container, StateSave state, VariableListSave list) in drops.Lists)
        {
            if (container != sourceElement)
            {
                changes.Add(CrossElementVariableChange.CaptureBefore(container, state, list));
                otherElements.Add(container);
            }
            state.VariableLists.Remove(list);
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
    }

    private record LineDrop(ElementSave Container, StateSave State, VariableListSave List, int Index);

    /// <summary>What to drop, collected before anything changes so line indexes stay valid.</summary>
    private class Drops
    {
        private readonly HashSet<object> _seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        private readonly HashSet<(VariableListSave, int)> _seenLines = new HashSet<(VariableListSave, int)>();

        public List<MovedInstanceReference> Reports { get; } = new List<MovedInstanceReference>();
        public List<LineDrop> Lines { get; } = new List<LineDrop>();
        public List<(ElementSave, StateSave, VariableSave)> Variables { get; } = new List<(ElementSave, StateSave, VariableSave)>();
        public List<(ElementSave, StateSave, VariableListSave)> Lists { get; } = new List<(ElementSave, StateSave, VariableListSave)>();
        public HashSet<string> InstancesWithSubAnimations { get; } = new HashSet<string>(StringComparer.Ordinal);

        public void Report(string name, string description)
        {
            MovedInstanceReference report = new MovedInstanceReference(name, description);
            if (!Reports.Contains(report))
            {
                Reports.Add(report);
            }
        }

        public void AddLine(string name, ElementSave container, StateSave state, VariableListSave list, int index)
        {
            if (_seen.Contains(list) || !_seenLines.Add((list, index)))
            {
                return;
            }
            string line = list.ValueAsIList[index] as string ?? "";
            Lines.Add(new LineDrop(container, state, list, index));
            Reports.Add(new MovedInstanceReference(name, $"{container.Name} ({state.Name}): {list.Name} line \"{line}\"",
                GetKeptValue(state, list, line)));
        }

        public void AddVariable(string name, ElementSave container, StateSave state, VariableSave variable, string description)
        {
            if (_seen.Add(variable))
            {
                Variables.Add((container, state, variable));
                Report(name, description);
            }
        }

        public void AddList(string name, ElementSave container, StateSave state, VariableListSave list, string description)
        {
            if (_seen.Add(list))
            {
                Lists.Add((container, state, list));
                Report(name, description);
            }
        }
    }
}
