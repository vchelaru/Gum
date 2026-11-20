using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Told about each variable or variable list a rename rewrites on another element, before and after it changes, so
/// the tool can record the change for undo. The CLI passes none.
/// </summary>
public interface IRenameChangeObserver
{
    /// <summary>Called before <paramref name="variable"/>, which lives in <paramref name="state"/>, is rewritten.</summary>
    object? BeforeChange(ElementSave container, StateSave state, VariableSave variable);

    /// <summary>Called once <paramref name="variable"/> is rewritten.</summary>
    void AfterChange(object? token, VariableSave variable);

    /// <summary>Called before <paramref name="list"/>, which lives in <paramref name="state"/>, is rewritten.</summary>
    object? BeforeChange(ElementSave container, StateSave state, VariableListSave list);

    /// <summary>Called once <paramref name="list"/> is rewritten.</summary>
    void AfterChange(object? token, VariableListSave list);
}

/// <summary>
/// The data change behind a rename's effect on the rest of the project: it rewrites what the
/// <see cref="IReferenceFinder"/> found, and the names inside the renamed thing's own element. The tool wraps it with
/// undo, plugins and saving; <c>gumcli edit</c> calls it directly. Each method adds the elements it changed to
/// <c>changedElements</c> so the caller can save them.
/// </summary>
public interface IRenameReferenceApplier
{
    /// <summary>Points every variable that selects the renamed state at its new name.</summary>
    void ApplyStateReferences(StateReferences changes, StateSave state, HashSet<ElementSave> changedElements, IRenameChangeObserver? observer = null);

    /// <summary>
    /// Renames <paramref name="category"/> and rewrites the variables of its <c>{Name}State</c> type. Returns the
    /// owner's own state-selector variables, which the tool needs to rebuild the converters of.
    /// </summary>
    IReadOnlyList<VariableSave> ApplyCategoryReferences(CategoryReferences changes, IStateContainer owner, StateSaveCategory category,
        string oldName, string newName, HashSet<ElementSave> changedElements, IRenameChangeObserver? observer = null);

    /// <summary>
    /// Rewrites the references to a renamed instance on other elements: <c>Parent</c> values through a
    /// <c>DefaultChildContainer</c> and <c>VariableReferences</c> lines.
    /// </summary>
    void ApplyInstanceReferences(InstanceReferences changes, string newName, string oldName, HashSet<ElementSave> changedElements, IRenameChangeObserver? observer = null);

    /// <summary>
    /// Renames what <paramref name="container"/> itself keys by the instance's name: variables and lists set on the
    /// instance, events, and a <c>DefaultChildContainer</c> naming it. <paramref name="instance"/> already has its new name.
    /// </summary>
    void RenameInstanceInContainer(ElementSave container, InstanceSave instance, string oldName);

    /// <summary>
    /// The part of <see cref="RenameInstanceInContainer"/> that is not per state: the events sourced from the instance
    /// and a <c>DefaultChildContainer</c> naming it.
    /// </summary>
    void RenameInstanceEventsAndChildContainer(ElementSave container, InstanceSave instance, string oldName);

    /// <summary>
    /// Renames what one state keys by the instance's name: variables and lists set on the instance, and the
    /// <c>Parent</c> and render target values naming it.
    /// </summary>
    void RenameInstanceInState(StateSave state, string oldName, string newName);

    /// <summary>Points the base types, contained types and <c>VariableReferences</c> lines naming the element at its new name.</summary>
    void ApplyElementReferences(ElementReferences changes, ElementSave element, string oldName, HashSet<ElementSave> changedElements);

    /// <summary>Renames the variable, or its exposed name, wherever the finder found it.</summary>
    void ApplyVariableRenameChanges(VariableChangeResponse changes, string oldName, string newName, HashSet<ElementSave> changedElements);

    /// <summary>Rewrites the name in each <c>VariableReferences</c> line, leaving the rest of the line as typed.</summary>
    void ApplyVariableReferenceRenames(IEnumerable<VariableReferenceChange> referenceChanges, string oldName, string newName, HashSet<ElementSave> changedElements);

    /// <summary>
    /// Renames a custom variable or an exposed name in one element's states. Returns whether anything changed. With
    /// <paramref name="type"/> different from the variable's, the variable takes the type and loses its value.
    /// </summary>
    bool RenameVariableInElement(ElementSave element, string oldName, string newName, string type, bool renameExposedName);

    /// <summary>
    /// Renames a variable of <paramref name="element"/> (or its exposed name) everywhere it lives: the element,
    /// the elements deriving from it, the states of every element with an instance of it (as <c>Instance.Name</c>),
    /// and <c>VariableReferences</c> lines. <paramref name="references"/> is what the finder found for the old name.
    /// </summary>
    void RenameVariable(ElementSave element, string oldName, string newName, string type, bool renameExposedName,
        VariableChangeResponse references, HashSet<ElementSave> changedElements);
}

/// <inheritdoc/>
public class RenameReferenceApplier : IRenameReferenceApplier
{
    /// <inheritdoc/>
    public void ApplyStateReferences(StateReferences changes, StateSave state, HashSet<ElementSave> changedElements, IRenameChangeObserver? observer = null)
    {
        foreach ((ElementSave container, VariableSave variable) in changes.VariablesToUpdate)
        {
            StateSave? owningState = container.AllStates.FirstOrDefault(item => item.Variables.Contains(variable));
            object? token = owningState == null ? null : observer?.BeforeChange(container, owningState, variable);

            variable.Value = state.Name;
            changedElements.Add(container);

            if (owningState != null)
            {
                observer?.AfterChange(token, variable);
            }
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<VariableSave> ApplyCategoryReferences(CategoryReferences changes, IStateContainer owner, StateSaveCategory category,
        string oldName, string newName, HashSet<ElementSave> changedElements, IRenameChangeObserver? observer = null)
    {
        // Gather self-referencing state variables in the owner element before mutating anything
        ElementSave? ownerAsElement = owner as ElementSave;
        List<VariableSave> selfVariables = ownerAsElement?.AllStates
            .SelectMany(state => state.Variables)
            .Where(variable => variable.Type == oldName + "State")
            .ToList() ?? new List<VariableSave>();

        category.Name = newName;

        List<(object? Token, VariableSave Variable)> captured = new List<(object?, VariableSave)>();
        foreach (VariableChange change in changes.VariableChanges)
        {
            if (change.Container is ElementSave containerElement)
            {
                changedElements.Add(containerElement);
                captured.Add((observer?.BeforeChange(containerElement, change.State, change.Variable), change.Variable));
            }
            change.Variable.Type = newName;
            if (change.Variable.GetRootName() == $"{oldName}State")
            {
                change.Variable.Name = string.IsNullOrEmpty(change.Variable.SourceObject)
                    ? $"{newName}State"
                    : $"{change.Variable.SourceObject}.{newName}State";
            }
        }

        foreach (VariableSave variable in selfVariables)
        {
            variable.Name = category.Name + "State";
            variable.Type = category.Name + "State";
        }

        if (ownerAsElement != null)
        {
            foreach (StateSave state in ownerAsElement.AllStates)
            {
                state.Variables.Sort((first, second) => GumProjectSave.CompareNames(first.Name, second.Name));
            }
        }

        foreach ((object? token, VariableSave variable) in captured)
        {
            observer?.AfterChange(token, variable);
        }
        return selfVariables;
    }

    /// <inheritdoc/>
    public void ApplyInstanceReferences(InstanceReferences changes, string newName, string oldName, HashSet<ElementSave> changedElements, IRenameChangeObserver? observer = null)
    {
        foreach ((ElementSave container, VariableSave variable) in changes.ParentVariablesInOtherElements)
        {
            if (variable.Value is string value)
            {
                int dotIndex = value.IndexOf(".");
                if (dotIndex >= 0 && value.Substring(dotIndex + 1) == oldName)
                {
                    StateSave? owningState = container.AllStates.FirstOrDefault(item => item.Variables.Contains(variable));
                    object? token = owningState == null ? null : observer?.BeforeChange(container, owningState, variable);

                    variable.Value = value.Substring(0, dotIndex + 1) + newName;
                    changedElements.Add(container);

                    if (owningState != null)
                    {
                        observer?.AfterChange(token, variable);
                    }
                }
            }
        }

        // One undo change per list, captured before its first line is rewritten.
        Dictionary<VariableListSave, object?> listTokens = new Dictionary<VariableListSave, object?>();

        foreach (VariableReferenceChange referenceChange in changes.VariableReferenceChanges)
        {
            if (referenceChange.Container != null)
            {
                changedElements.Add(referenceChange.Container);
            }

            VariableListSave variableList = referenceChange.VariableReferenceList;
            if (referenceChange.Container != null && !listTokens.ContainsKey(variableList))
            {
                StateSave? owningState = referenceChange.Container.AllStates.FirstOrDefault(item => item.VariableLists.Contains(variableList));
                if (owningState != null)
                {
                    listTokens[variableList] = observer?.BeforeChange(referenceChange.Container, owningState, variableList);
                }
            }

            if (variableList.ValueAsIList[referenceChange.LineIndex] is not string oldLine) continue;

            variableList.ValueAsIList[referenceChange.LineIndex] = RewriteReferenceLine(oldLine, referenceChange.ChangedSide,
                leftSide => leftSide.StartsWith(oldName + ".") ? newName + leftSide.Substring(oldName.Length) : leftSide,
                rightSide =>
                {
                    // Preserve optional state prefix (e.g. "Highlighted:OldName.X")
                    int colonIndex = rightSide.IndexOf(":");
                    string statePrefix = colonIndex >= 0 ? rightSide.Substring(0, colonIndex + 1) : string.Empty;
                    string rightCore = colonIndex >= 0 ? rightSide.Substring(colonIndex + 1) : rightSide;

                    if (rightCore.StartsWith(oldName + "."))
                    {
                        rightCore = newName + rightCore.Substring(oldName.Length);
                    }
                    else
                    {
                        // Handle qualified cross-component reference: "Components/ComponentA/Sprite.Width"
                        string qualifiedOldPattern = "." + oldName + ".";
                        int patternIndex = rightCore.LastIndexOf(qualifiedOldPattern);
                        if (patternIndex >= 0)
                        {
                            rightCore = rightCore.Substring(0, patternIndex + 1) + newName +
                                        rightCore.Substring(patternIndex + 1 + oldName.Length);
                        }
                    }

                    return statePrefix + rightCore;
                });
        }

        foreach ((VariableListSave variableList, object? token) in listTokens)
        {
            observer?.AfterChange(token, variableList);
        }
    }

    /// <inheritdoc/>
    public void RenameInstanceInContainer(ElementSave container, InstanceSave instance, string oldName)
    {
        string newName = instance.Name;
        foreach (StateSave state in container.AllStates)
        {
            RenameInstanceInState(state, oldName, newName);
        }

        RenameInstanceEventsAndChildContainer(container, instance, oldName);
    }

    /// <inheritdoc/>
    public void RenameInstanceEventsAndChildContainer(ElementSave container, InstanceSave instance, string oldName)
    {
        string newName = instance.Name;
        foreach (EventSave eventSave in container.Events)
        {
            if (eventSave.GetSourceObject() == oldName)
            {
                eventSave.Name = newName + "." + eventSave.GetRootName();
            }
        }

        foreach (StateSave state in container.AllStates)
        {
            VariableSave? variable = state.Variables.FirstOrDefault(item => item.Name == "DefaultChildContainer");

            if (variable?.Value as string == oldName)
            {
                variable!.Value = newName;
            }
        }
    }

    /// <inheritdoc/>
    public void RenameInstanceInState(StateSave state, string oldName, string newName)
    {
        foreach (VariableSave variable in state.Variables)
        {
            if (variable.SourceObject == oldName)
            {
                variable.Name = newName + "." + variable.Name.Substring((oldName + ".").Length);
            }

            // Variables whose value is an instance name (Parent, and a Sprite's render-target
            // source) must have that value rewritten when the referenced instance is renamed, or
            // the reference goes stale and silently resolves to nothing.
            string rootName = variable.GetRootName();
            if ((rootName == "Parent" || rootName == "RenderTargetTextureSource") && variable.SetsValue
                && variable.Value is string valueAsString && !string.IsNullOrEmpty(valueAsString))
            {
                if (valueAsString == oldName)
                {
                    variable.Value = newName;
                }
                else if (valueAsString.StartsWith(oldName + "."))
                {
                    variable.Value = newName + "." + valueAsString.Substring(oldName.Length + 1);
                }
            }
        }

        foreach (VariableListSave variableList in state.VariableLists)
        {
            if (variableList.SourceObject == oldName)
            {
                variableList.Name = newName + "." + variableList.Name.Substring((oldName + ".").Length);
            }
        }
    }

    /// <inheritdoc/>
    public void ApplyElementReferences(ElementReferences changes, ElementSave element, string oldName, HashSet<ElementSave> changedElements)
    {
        foreach (ElementSave derived in changes.ElementsWithBaseTypeReference)
        {
            derived.BaseType = element.Name;
            changedElements.Add(derived);
        }

        foreach ((ElementSave container, InstanceSave instance) in changes.InstancesWithBaseTypeReference)
        {
            instance.BaseType = element.Name;
            changedElements.Add(container);
        }

        foreach ((ElementSave container, VariableSave variable) in changes.ContainedTypeVariableReferences)
        {
            variable.Value = element.Name;
            changedElements.Add(container);
        }

        string qualifiedOldName = ElementReference.GetQualifiedName(element, oldName);
        string qualifiedNewName = ElementReference.GetQualifiedName(element, element.Name);

        foreach (VariableReferenceChange referenceChange in changes.VariableReferenceChanges)
        {
            VariableListSave variableList = referenceChange.VariableReferenceList;
            if (variableList.ValueAsIList[referenceChange.LineIndex] is not string oldLine || !oldLine.Contains('=')) continue;

            variableList.ValueAsIList[referenceChange.LineIndex] = RewriteReferenceLine(oldLine, SideOfEquals.Right,
                left => left,
                right => right.StartsWith(qualifiedOldName + ".")
                    ? qualifiedNewName + right.Substring(qualifiedOldName.Length)
                    : right);
            changedElements.Add(referenceChange.Container);
        }
    }

    /// <inheritdoc/>
    public void ApplyVariableRenameChanges(VariableChangeResponse changes, string oldName, string newName, HashSet<ElementSave> changedElements)
    {
        foreach (VariableChange change in changes.VariableChanges)
        {
            if (change.Container is ElementSave element)
            {
                changedElements.Add(element);
            }

            if (change.Variable.ExposedAsName == oldName)
            {
                change.Variable.ExposedAsName = newName;
            }
            else if (change.Variable.GetRootName() == oldName)
            {
                string prefix = change.Variable.SourceObject != null
                    ? change.Variable.SourceObject + "."
                    : string.Empty;
                change.Variable.Name = prefix + newName;
            }
        }

        ApplyVariableReferenceRenames(changes.VariableReferenceChanges, oldName, newName, changedElements);
    }

    /// <inheritdoc/>
    public void ApplyVariableReferenceRenames(IEnumerable<VariableReferenceChange> referenceChanges, string oldName, string newName, HashSet<ElementSave> changedElements)
    {
        foreach (VariableReferenceChange referenceChange in referenceChanges)
        {
            changedElements.Add(referenceChange.Container);

            IList<object> lines = referenceChange.VariableReferenceList.ValueAsIList;
            if (lines[referenceChange.LineIndex] is not string oldLine) continue;

            lines[referenceChange.LineIndex] = RewriteReferenceLine(oldLine, referenceChange.ChangedSide,
                left => left == oldName ? newName : left,
                right =>
                    right == oldName ? newName
                    : right.EndsWith("." + oldName)
                        ? right.Substring(0, right.Length - oldName.Length) + newName
                    : right);
        }
    }

    /// <inheritdoc/>
    public bool RenameVariableInElement(ElementSave element, string oldName, string newName, string type, bool renameExposedName)
    {
        bool changed = false;
        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableSave variable in state.Variables)
            {
                if ((variable.Name == oldName && !renameExposedName) ||
                    (variable.ExposedAsName == oldName && renameExposedName))
                {
                    variable.Name = newName;
                    if (variable.Type != type)
                    {
                        variable.Type = type;
                        variable.Value = null;
                    }
                    changed = true;
                }
            }
        }
        return changed;
    }

    /// <inheritdoc/>
    public void RenameVariable(ElementSave element, string oldName, string newName, string type, bool renameExposedName,
        VariableChangeResponse references, HashSet<ElementSave> changedElements)
    {
        if (RenameVariableInElement(element, oldName, newName, type, renameExposedName))
        {
            changedElements.Add(element);
        }
        RenameVariableInInstances(element, oldName, newName, type, changedElements);

        foreach (ElementSave derived in ObjectFinder.Self.GetElementsInheritingFrom(element))
        {
            if (RenameVariableInElement(derived, oldName, newName, type, renameExposedName))
            {
                changedElements.Add(derived);
            }
            RenameVariableInInstances(derived, oldName, newName, type, changedElements);
        }

        ApplyVariableReferenceRenames(references.VariableReferenceChanges, oldName, newName, changedElements);
    }

    private void RenameVariableInInstances(ElementSave element, string oldName, string newName, string type, HashSet<ElementSave> changedElements)
    {
        foreach (ElementReference reference in ObjectFinder.Self.GetElementReferencesToThis(element)
            .Where(item => item.ReferenceType == ReferenceType.InstanceOfType))
        {
            // InstanceOfType references always point at an instance, and ObjectFinder sets the owner on each.
            InstanceSave instance = (InstanceSave)reference.ReferencingObject;
            if (reference.OwnerOfReferencingObject is not { } owner)
            {
                continue;
            }

            // Instances treat the name as a normal variable, so this is always a full rename.
            if (RenameVariableInElement(owner, instance.Name + "." + oldName, instance.Name + "." + newName, type, renameExposedName: false))
            {
                changedElements.Add(owner);
            }
        }
    }

    /// <summary>
    /// Rewrites the sides of a VariableReferences line named by <paramref name="changedSide"/>. Each
    /// rewrite sees the side trimmed, and the whitespace around it is put back, so the line keeps the
    /// spacing it was typed with and an undo that reverses the rename restores it exactly. The
    /// assignment is the first '='; the right side may itself contain '=' (==, &lt;=, ...).
    /// </summary>
    public static string RewriteReferenceLine(string line, SideOfEquals changedSide,
        Func<string, string> rewriteLeft, Func<string, string> rewriteRight)
    {
        int equalsIndex = line.IndexOf('=');
        if (equalsIndex < 0) return line;

        string left = line.Substring(0, equalsIndex);
        string right = line.Substring(equalsIndex + 1);

        if (changedSide is SideOfEquals.Left or SideOfEquals.Both)
        {
            left = RewriteTrimmed(left, rewriteLeft);
        }

        if (changedSide is SideOfEquals.Right or SideOfEquals.Both)
        {
            right = RewriteTrimmed(right, rewriteRight);
        }

        return left + "=" + right;
    }

    private static string RewriteTrimmed(string side, Func<string, string> rewrite)
    {
        string trimmed = side.Trim();
        int start = side.Length - side.TrimStart().Length;
        return side.Substring(0, start) + rewrite(trimmed) + side.Substring(start + trimmed.Length);
    }
}
