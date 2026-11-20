using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Undo;
using Gum.Wireframe;
using GumRuntime;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// What <see cref="IVariableReferenceCascade.ApplyToElementsReading"/> changed.
/// </summary>
/// <param name="Changes">Every stored value the cascade changed.</param>
/// <param name="ChangedElements">The elements whose states were re-applied, which the caller saves.</param>
/// <param name="ElementsScanned">How many elements were looked at, so a caller can count its work.</param>
public record ReferenceCascadeResult(StoredValueChange[] Changes, IReadOnlyCollection<ElementSave> ChangedElements, int ElementsScanned);

/// <summary>One member an edit set: <paramref name="QualifiedName"/> is <c>Instance.Member</c> for an instance's variable.</summary>
public record EditedMember(InstanceSave? Instance, string UnqualifiedMember, string QualifiedName);

/// <summary>What <see cref="IVariableReferenceCascade.ReactToEdit"/> did.</summary>
/// <param name="Cascade">The elements that read the edited element, which the caller saves.</param>
/// <param name="BehaviorLinesScanned">How many behavior tool-only lines were looked at.</param>
/// <param name="DidSetDeepReference">Whether a reference inside an instance's type had a deeper value written.</param>
public record EditReaction(ReferenceCascadeResult Cascade, int BehaviorLinesScanned, bool DidSetDeepReference);

/// <summary>
/// Re-applies the variable references that read values an edit changed: the element's other states, the
/// elements that read it, and the instance variables a reference inside an instance's type sets. Shared by the
/// tool's Variables tab and <c>gumcli edit</c>; the tool adds refreshing and saving around it.
/// </summary>
public interface IVariableReferenceCascade
{
    /// <summary>
    /// The whole reaction to a committed edit of <paramref name="state"/>, whoever made it (the tool's Variables
    /// tab or <c>gumcli edit</c>): the state's own references, the behaviors' tool-only lines, the element's other
    /// states, the elements that read this one, and references inside the edited instances' types. The caller
    /// takes <paramref name="valuesBefore"/> with <see cref="VariableReferenceCascade.SnapshotValues"/> before it
    /// edits, and saves <see cref="ReferenceCascadeResult.ChangedElements"/>.
    /// </summary>
    EditReaction ReactToEdit(ElementSave element, StateSave state, IReadOnlyDictionary<string, object?> valuesBefore,
        IReadOnlyList<EditedMember> edits, Func<ElementSave, GraphicalUiElement?> liveRootFor, bool isFullCommit = true);

    /// <summary>
    /// What an edit changed in <paramref name="state"/>: <paramref name="editedNames"/> plus every variable whose
    /// value differs from <paramref name="valuesBefore"/>, the names <see cref="ApplyOtherStatesReading"/> takes.
    /// </summary>
    HashSet<string> GetChangedNames(StateSave state, IReadOnlyDictionary<string, object?> valuesBefore, IEnumerable<string> editedNames);

    /// <summary>
    /// Re-reads the rows of <paramref name="element"/>'s other states that read <paramref name="changedNames"/>.
    /// A row that reads nothing that changed is left alone, and a row in a loop is never evaluated.
    /// </summary>
    void ApplyOtherStatesReading(ElementSave element, StateSave editedState, ICollection<string> changedNames,
        GraphicalUiElement? liveRoot, bool isFullCommit);

    /// <summary>
    /// Re-applies every state that reads <paramref name="element"/> or an element downstream of it, in dependency
    /// order, including <paramref name="element"/>'s own states that read it through its qualified name.
    /// <paramref name="liveRootFor"/> returns an element's displayed tree, or null when nothing is displayed.
    /// </summary>
    ReferenceCascadeResult ApplyToElementsReading(ElementSave element, Func<ElementSave, GraphicalUiElement?> liveRootFor,
        bool isFullCommit);

    /// <summary>
    /// When setting <paramref name="unqualifiedVariableName"/> on <paramref name="instance"/> also sets a variable
    /// inside the instance's type through a reference there, writes that deeper value into
    /// <paramref name="stateSave"/>. Returns whether anything was written.
    /// </summary>
    bool ApplyToInstanceInternals(ElementSave container, InstanceSave? instance, StateSave stateSave,
        string unqualifiedVariableName, object? newValue);
}

/// <inheritdoc/>
public class VariableReferenceCascade : IVariableReferenceCascade
{
    /// <summary>
    /// The rows to re-read when <paramref name="changedNames"/> changed: <paramref name="seedRows"/> (rows that
    /// were added) and every row that reads a changed variable or the result of an affected row.
    /// </summary>
    public static HashSet<ReferenceRow> FindRowsToApply(VariableReferenceGraph graph, IEnumerable<string> changedNames,
        List<ReferenceRow> seedRows)
    {
        HashSet<string> names = new HashSet<string>(changedNames);
        names.UnionWith(seedRows.Select(row => row.WrittenName));
        List<ReferenceRow> affected = new List<ReferenceRow>();
        graph.FindAffectedRows(names, affected, includeLiveLayoutRows: false);
        return new HashSet<ReferenceRow>(seedRows.Concat(affected));
    }

    /// <summary>The values of <paramref name="state"/> before an edit, for <see cref="IVariableReferenceCascade.ReactToEdit"/>.</summary>
    public static Dictionary<string, object?> SnapshotValues(StateSave state)
    {
        Dictionary<string, object?> values = new Dictionary<string, object?>();
        foreach (VariableSave variable in state.Variables)
        {
            values[variable.Name] = variable.Value;
        }
        return values;
    }

    /// <inheritdoc/>
    public EditReaction ReactToEdit(ElementSave element, StateSave state, IReadOnlyDictionary<string, object?> valuesBefore,
        IReadOnlyList<EditedMember> edits, Func<ElementSave, GraphicalUiElement?> liveRootFor, bool isFullCommit = true)
    {
        GraphicalUiElement? liveRoot = liveRootFor(element);
        ElementSaveExtensions.ApplyVariableReferences(element, state, liveRoot, isFullCommit);
        int behaviorLinesScanned = BehaviorToolOnlyReferencesApplier.Apply(element, state);

        HashSet<string> changedNames = GetChangedNames(state, valuesBefore, edits.Select(item => item.QualifiedName));
        ApplyOtherStatesReading(element, state, changedNames, liveRoot, isFullCommit);
        ReferenceCascadeResult cascade = ApplyToElementsReading(element, liveRootFor, isFullCommit);

        // A tunneled variable may need its value carried one level deeper, into the instance's type.
        bool didSetDeepReference = false;
        foreach (EditedMember edit in edits)
        {
            didSetDeepReference |= ApplyToInstanceInternals(element, edit.Instance, state, edit.UnqualifiedMember,
                state.GetValueRecursive(edit.QualifiedName));
        }

        return new EditReaction(cascade, behaviorLinesScanned, didSetDeepReference);
    }

    /// <inheritdoc/>
    public HashSet<string> GetChangedNames(StateSave state, IReadOnlyDictionary<string, object?> valuesBefore, IEnumerable<string> editedNames)
    {
        HashSet<string> changedNames = new HashSet<string>(editedNames);
        foreach (VariableSave variable in state.Variables)
        {
            if (!valuesBefore.TryGetValue(variable.Name, out object? oldValue) || !ElementSaveExtensions.ValueEquality(oldValue, variable.Value))
            {
                changedNames.Add(variable.Name);
            }
        }
        return changedNames;
    }

    /// <inheritdoc/>
    public void ApplyOtherStatesReading(ElementSave element, StateSave editedState, ICollection<string> changedNames,
        GraphicalUiElement? liveRoot, bool isFullCommit)
    {
        foreach (StateSave state in element.AllStates)
        {
            if (state == editedState)
            {
                continue;
            }

            VariableReferenceGraph graph = VariableReferenceGraph.GetForState(element, state);
            if (!graph.HasRows)
            {
                continue;
            }

            HashSet<ReferenceRow> toApply = FindRowsToApply(graph, changedNames, new List<ReferenceRow>());
            if (toApply.Count > 0)
            {
                ElementSaveExtensions.ApplyVariableReferences(element, state, liveRoot, isFullCommit, onlyRows: toApply);
            }
        }
    }

    /// <inheritdoc/>
    public ReferenceCascadeResult ApplyToElementsReading(ElementSave element, Func<ElementSave, GraphicalUiElement?> liveRootFor,
        bool isFullCommit)
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        if (project == null)
        {
            return new ReferenceCascadeResult(Array.Empty<StoredValueChange>(), Array.Empty<ElementSave>(), 0);
        }

        // Every state that reads this element or an element downstream of it, whichever of its values
        // changed; simpler than tracking which references read the changed variable. Elements are
        // applied in dependency order, so a reader of two changed elements sees both new values.
        List<ElementSave> allElements = new List<ElementSave>();
        allElements.AddRange(project.StandardElements);
        allElements.AddRange(project.Components);
        allElements.AddRange(project.Screens);

        HashSet<string> changedElementNames = new HashSet<string> { ObjectFinder.Self.GetQualifiedElementName(element) };
        HashSet<ElementSave> changedElements = new HashSet<ElementSave>();
        List<StoredValueChange> changes = new List<StoredValueChange>();

        // The edited element can read itself through its qualified name.
        ApplyStatesReading(element, changedElementNames, changedElements, changes, liveRootFor, isFullCommit);
        List<ElementSave> dependents = ReferenceDependencies.GetDependentsInOrder(allElements, element);
        foreach (ElementSave dependent in dependents)
        {
            ApplyStatesReading(dependent, changedElementNames, changedElements, changes, liveRootFor, isFullCommit);
            changedElementNames.Add(ObjectFinder.Self.GetQualifiedElementName(dependent));
        }

        return new ReferenceCascadeResult(changes.ToArray(), changedElements, allElements.Count);
    }

    private static void ApplyStatesReading(ElementSave owner, HashSet<string> elementNames, HashSet<ElementSave> changedElements,
        List<StoredValueChange> changes, Func<ElementSave, GraphicalUiElement?> liveRootFor, bool isFullCommit)
    {
        foreach (StateSave state in owner.AllStates)
        {
            if (ReferenceDependencies.StateReadsAny(state, elementNames))
            {
                Dictionary<string, object?> before = SnapshotValues(state);

                ElementSaveExtensions.ApplyVariableReferences(owner, state, liveRootFor(owner), isFullCommit);
                changedElements.Add(owner);

                string? categoryName = owner.Categories.FirstOrDefault(item => item.States.Contains(state))?.Name;
                foreach (VariableSave variable in state.Variables)
                {
                    if (!before.TryGetValue(variable.Name, out object? oldValue) || !Equals(oldValue, variable.Value))
                    {
                        changes.Add(new StoredValueChange(owner, categoryName, state.Name, variable.Name, variable.Type, variable.Value));
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool ApplyToInstanceInternals(ElementSave container, InstanceSave? instance, StateSave stateSave,
        string unqualifiedVariableName, object? newValue)
    {
        var didAssignDeepReference = false;
        ElementSave? instanceElement = null;
        if (instance != null)
        {
            instanceElement = ObjectFinder.Self.GetElementSave(instance.BaseType);
        }
        if (instance != null && instanceElement != null)
        {
            // Equivalent to the tool-only ElementSaveExtensionMethodsGumTool.GetVariableFromThisOrBase(element,
            // variable, forceDefault: true) expansion, without depending on that Locator-touching, Gum-only file.
            var variableOnInstance = instanceElement.GetDefaultStateOrThrow().GetVariableRecursive(unqualifiedVariableName);

            List<TypedElementReference> references = ObjectFinder.Self.GetElementReferencesToThis(instanceElement);
            var filteredReferences = references
                .Where(item => item.ReferenceType == ReferenceType.VariableReference)
                .ToArray();

            if (variableOnInstance != null)
            {
                foreach (var reference in filteredReferences)
                {
                    if (reference.ReferencingObject is VariableListSave variableListSave &&
                        variableListSave.ValueAsIList is List<string> stringList)
                    {
                        foreach (var assignment in stringList)
                        {
                            if (assignment.Contains("="))
                            {
                                var leftSide = assignment.Substring(0, assignment.IndexOf("=")).Trim();
                                var rightSide = assignment.Substring(assignment.IndexOf("=") + 1).Trim();

                                var simplifiedRightSide = rightSide;

                                var instanceElementQualified = ObjectFinder.Self.GetQualifiedElementName(instanceElement);

                                if (rightSide.StartsWith(instanceElementQualified + "."))
                                {
                                    simplifiedRightSide = rightSide.Substring(instanceElementQualified.Length
                                        // +1 to take off the period before the variable name
                                        + 1);
                                }

                                var didAssignReferencedVariable = simplifiedRightSide == variableOnInstance.Name ||
                                    simplifiedRightSide == variableOnInstance.ExposedAsName;

                                if (didAssignReferencedVariable)
                                {
                                    var ownerOfVariableReferenceName = variableListSave.SourceObject;

                                    var ownerOfVariableReferenceInstanceSave = instanceElement.GetInstance(ownerOfVariableReferenceName);

                                    if (ownerOfVariableReferenceInstanceSave != null)
                                    {
                                        // Setting this variable results in ownerOfVariableReferenceInstanceSave.leftSide also being assigned
                                        // but ownerOfVariableReferenceInstanceSave is inside of instanceElement, so we need to find all instances
                                        // of instanceElement in container, and assign those.
                                        var qualifiedNameInInstanceElement = ownerOfVariableReferenceInstanceSave.Name + "." + leftSide;

                                        var deepQualifiedName = instance.Name + "." + qualifiedNameInInstanceElement;

                                        stateSave.SetValue(deepQualifiedName, newValue);
                                        didAssignDeepReference = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return didAssignDeepReference;
    }
}
