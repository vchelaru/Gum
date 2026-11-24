using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;

namespace Gum.ProjectServices.Editing;

/// <summary>One <c>VariableReferences</c> line to drop, by its index in the list.</summary>
public record MovedReferenceLineDrop(ElementSave Container, StateSave State, VariableListSave List, int Index);

/// <summary>A variable or variable list to remove from a state.</summary>
public record MovedReferenceItemDrop<T>(ElementSave Container, StateSave State, T Item);

/// <summary>
/// What <see cref="IMovedInstanceReferenceDropLogic.Find"/> found to drop, collected before anything changes so
/// line indexes stay valid.
/// </summary>
public class MovedInstanceReferenceDrops
{
    private readonly HashSet<object> _seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
    private readonly HashSet<(VariableListSave, int)> _seenLines = new HashSet<(VariableListSave, int)>();

    /// <summary>One entry per dropped reference, plus the default-state lines the move leaves behind.</summary>
    public List<MovedInstanceReference> Reports { get; } = new List<MovedInstanceReference>();

    /// <summary>The <c>VariableReferences</c> lines to remove.</summary>
    public List<MovedReferenceLineDrop> Lines { get; } = new List<MovedReferenceLineDrop>();

    /// <summary>The variables to remove.</summary>
    public List<MovedReferenceItemDrop<VariableSave>> Variables { get; } = new List<MovedReferenceItemDrop<VariableSave>>();

    /// <summary>The variable lists to remove.</summary>
    public List<MovedReferenceItemDrop<VariableListSave>> Lists { get; } = new List<MovedReferenceItemDrop<VariableListSave>>();

    /// <summary>The moved instances whose sub-animations the source element's animations play.</summary>
    public HashSet<string> InstancesWithSubAnimations { get; } = new HashSet<string>(StringComparer.Ordinal);

    internal void Report(string name, string description)
    {
        MovedInstanceReference report = new MovedInstanceReference(name, description);
        if (!Reports.Contains(report))
        {
            Reports.Add(report);
        }
    }

    internal void AddLine(string name, ElementSave container, StateSave state, VariableListSave list, int index)
    {
        if (_seen.Contains(list) || !_seenLines.Add((list, index)))
        {
            return;
        }
        string line = list.ValueAsIList[index] as string ?? "";
        Lines.Add(new MovedReferenceLineDrop(container, state, list, index));
        Reports.Add(new MovedInstanceReference(name, $"{container.Name} ({state.Name}): {list.Name} line \"{line}\"",
            MovedInstanceReferenceDropLogic.GetKeptValue(state, list, line)));
    }

    internal void AddVariable(string name, ElementSave container, StateSave state, VariableSave variable, string description)
    {
        if (_seen.Add(variable))
        {
            Variables.Add(new MovedReferenceItemDrop<VariableSave>(container, state, variable));
            Report(name, description);
        }
    }

    internal void AddList(string name, ElementSave container, StateSave state, VariableListSave list, string description)
    {
        if (_seen.Add(list))
        {
            Lists.Add(new MovedReferenceItemDrop<VariableListSave>(container, state, list));
            Report(name, description);
        }
    }
}

/// <summary>
/// Finds and drops what breaks when Create Component moves instances out of their element and into a new
/// component: references from outside the moved subtree that name a moved instance. A dropped
/// <c>VariableReferences</c> line leaves behind the value it last resolved to, the same as the references inside
/// the subtree that the move carries as plain values. Undo, saving and animation keyframes stay with the caller.
/// </summary>
public interface IMovedInstanceReferenceDropLogic
{
    /// <summary>
    /// Finds each reference that stops applying once <paramref name="movedInstances"/> leave
    /// <paramref name="sourceElement"/>, with one report per reference, grouped by the moved instance it names. The
    /// reports also list, without dropping them, the <c>VariableReferences</c> lines the moved instances and
    /// <paramref name="promotedInstance"/> own in the default state that read from outside the subtree: the move
    /// leaves those behind and carries their values. Changes nothing.
    /// </summary>
    /// <param name="animations">The source element's animations, or null to leave animations out.</param>
    MovedInstanceReferenceDrops Find(ElementSave sourceElement, InstanceSave promotedInstance,
        IReadOnlyCollection<InstanceSave> movedInstances, ElementAnimationsSave? animations);

    /// <summary>
    /// Removes the lines, variables and lists in <paramref name="drops"/>. A list left with no lines is removed.
    /// </summary>
    void Drop(MovedInstanceReferenceDrops drops);
}

/// <inheritdoc/>
public class MovedInstanceReferenceDropLogic : IMovedInstanceReferenceDropLogic
{
    private static readonly HashSet<string> InstanceNamingVariableRootNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "Parent", "RenderTargetTextureSource"
    };

    private readonly IReferenceFinder _referenceFinder;

    public MovedInstanceReferenceDropLogic(IReferenceFinder referenceFinder)
    {
        _referenceFinder = referenceFinder;
    }

    /// <inheritdoc/>
    public MovedInstanceReferenceDrops Find(ElementSave sourceElement, InstanceSave promotedInstance,
        IReadOnlyCollection<InstanceSave> movedInstances, ElementAnimationsSave? animations)
    {
        MovedInstanceReferenceDrops drops = new MovedInstanceReferenceDrops();
        HashSet<string> insideNames = new HashSet<string>(movedInstances.Select(item => item.Name), StringComparer.Ordinal)
        {
            promotedInstance.Name
        };
        StateSave defaultState = sourceElement.GetDefaultStateOrThrow();
        // The replacement carries these under the names the source exposes, in every state.
        HashSet<string> carriedNames = new HashSet<string>(ComponentExtractionLogic.GetExposedVariableNamesOnReplacement(
            defaultState, promotedInstance.Name, movedInstances.Select(item => item.Name)).Keys, StringComparer.Ordinal);

        foreach (InstanceSave moved in movedInstances)
        {
            FindReferencesTo(moved.Name, sourceElement, promotedInstance.Name, insideNames, carriedNames, defaultState,
                animations, drops);
        }

        drops.Reports.AddRange(GetLinesReadingOutside(sourceElement, defaultState, insideNames));
        return drops;
    }

    /// <inheritdoc/>
    public void Drop(MovedInstanceReferenceDrops drops)
    {
        foreach (IGrouping<VariableListSave, MovedReferenceLineDrop> linesInList in drops.Lines.GroupBy(item => item.List))
        {
            MovedReferenceLineDrop first = linesInList.First();
            foreach (int index in linesInList.Select(item => item.Index).Distinct().OrderByDescending(item => item))
            {
                first.List.ValueAsIList.RemoveAt(index);
            }
            if (first.List.ValueAsIList.Count == 0)
            {
                first.State.VariableLists.Remove(first.List);
            }
        }

        foreach (MovedReferenceItemDrop<VariableSave> drop in drops.Variables)
        {
            drop.State.Variables.Remove(drop.Item);
        }

        foreach (MovedReferenceItemDrop<VariableListSave> drop in drops.Lists)
        {
            drop.State.VariableLists.Remove(drop.Item);
        }
    }

    private void FindReferencesTo(string name, ElementSave sourceElement, string promotedName, HashSet<string> insideNames,
        HashSet<string> carriedNames, StateSave defaultState, ElementAnimationsSave? animations, MovedInstanceReferenceDrops drops)
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
                    if (!isDefault && !carriedNames.Contains(variable.Name))
                    {
                        drops.AddVariable(name, sourceElement, state, variable,
                            $"{sourceElement.Name} ({state.Name}): {variable.Name} = {variable.Value}");
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

    internal static string? GetKeptValue(StateSave state, VariableListSave list, string line)
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
}
