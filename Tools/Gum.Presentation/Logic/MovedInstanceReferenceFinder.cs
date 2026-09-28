using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.StateAnimation.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Logic;

/// <summary>
/// Finds what breaks when Create Component moves instances out of their element and into a new
/// component: references from outside the moved subtree that name a moved instance. Called by
/// <see cref="CopyPasteLogic.CreateComponentFromInstance"/> before it removes the subtree.
/// </summary>
public interface IMovedInstanceReferenceFinder
{
    /// <summary>
    /// Returns one line per reference that stops applying once <paramref name="movedInstances"/>
    /// leave <paramref name="sourceElement"/>, grouped by the moved instance it names. Values the
    /// move itself carries (the moved instances' default-state variables, and references owned by
    /// the moved instances or by <paramref name="promotedInstance"/>) are not reported.
    /// </summary>
    MovedInstanceReference[] GetReferencesBrokenByMove(ElementSave sourceElement, InstanceSave promotedInstance,
        IReadOnlyCollection<InstanceSave> movedInstances);
}

/// <summary>A reference to <see cref="MovedInstanceName"/> that no longer applies after it moved.</summary>
public record MovedInstanceReference(string MovedInstanceName, string Description);

/// <inheritdoc/>
public class MovedInstanceReferenceFinder : IMovedInstanceReferenceFinder
{
    private static readonly HashSet<string> InstanceNamingVariableRootNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "Parent", "RenderTargetTextureSource"
    };

    private readonly IReferenceFinder _referenceFinder;
    private readonly IElementAnimationsProvider _animationsProvider;
    private readonly IReferenceFinderProjectProvider _projectProvider;

    public MovedInstanceReferenceFinder(IReferenceFinder referenceFinder, IElementAnimationsProvider animationsProvider,
        IReferenceFinderProjectProvider projectProvider)
    {
        _referenceFinder = referenceFinder;
        _animationsProvider = animationsProvider;
        _projectProvider = projectProvider;
    }

    /// <inheritdoc/>
    public MovedInstanceReference[] GetReferencesBrokenByMove(ElementSave sourceElement, InstanceSave promotedInstance,
        IReadOnlyCollection<InstanceSave> movedInstances)
    {
        HashSet<string> insideNames = new HashSet<string>(movedInstances.Select(item => item.Name), StringComparer.Ordinal)
        {
            promotedInstance.Name
        };
        StateSave defaultState = sourceElement.GetDefaultStateOrThrow();
        ElementAnimationsSave? animations = _projectProvider.GumProjectSave is { } project
            ? _animationsProvider.GetAnimationsFor(sourceElement, project)
            : null;

        List<MovedInstanceReference> results = new List<MovedInstanceReference>();
        foreach (InstanceSave moved in movedInstances)
        {
            List<string> descriptions = new List<string>();
            string name = moved.Name;
            InstanceReferences references = _referenceFinder.GetReferencesToInstance(sourceElement, moved, name);

            foreach (VariableReferenceChange change in references.VariableReferenceChanges)
            {
                bool isOwnedInsideSubtree = change.Container == sourceElement &&
                    change.VariableReferenceList.SourceObject is { } owner && insideNames.Contains(owner);
                if (isOwnedInsideSubtree)
                {
                    continue;
                }
                string stateName = change.Container.AllStates
                    .FirstOrDefault(state => state.VariableLists.Contains(change.VariableReferenceList))?.Name ?? "";
                string line = change.VariableReferenceList.ValueAsIList[change.LineIndex] as string ?? "";
                descriptions.Add($"{change.Container.Name} ({stateName}): {change.VariableReferenceList.Name} line \"{line}\"");
            }

            foreach ((ElementSave container, VariableSave variable) in references.ParentVariablesInOtherElements)
            {
                descriptions.Add($"{container.Name}: {variable.Name} = {variable.Value}");
            }

            if (references.DefaultChildContainerWillChange)
            {
                descriptions.Add($"{sourceElement.Name}: DefaultChildContainer = {name}");
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
                            // Only the default state moves into the component.
                            descriptions.Add($"{sourceElement.Name} ({state.Name}): {variable.Name} = {variable.Value}");
                        }
                        else if (!string.IsNullOrEmpty(variable.ExposedAsName))
                        {
                            descriptions.Add($"{sourceElement.Name}: exposed variable {variable.ExposedAsName} ({variable.Name})");
                        }
                    }
                    else if (!insideNames.Contains(variable.SourceObject ?? "") &&
                        InstanceNamingVariableRootNames.Contains(variable.GetRootName()) &&
                        variable.Value is string value && (value == name || value.StartsWith(name + ".", StringComparison.Ordinal)))
                    {
                        descriptions.Add($"{sourceElement.Name} ({state.Name}): {variable.Name} = {value}");
                    }
                }
                if (!isDefault)
                {
                    foreach (VariableListSave variableList in state.VariableLists.Where(item => item.SourceObject == name))
                    {
                        descriptions.Add($"{sourceElement.Name} ({state.Name}): {variableList.Name}");
                    }
                }
            }

            // A derived element's overrides of the moved instance lose the instance they set.
            foreach (ElementSave derived in ObjectFinder.Self.GetElementsInheritingFrom(sourceElement))
            {
                foreach (StateSave state in derived.AllStates)
                {
                    foreach (VariableSave variable in state.Variables.Where(item => item.SourceObject == name))
                    {
                        descriptions.Add($"{derived.Name} ({state.Name}): {variable.Name} = {variable.Value}");
                    }
                    foreach (VariableListSave variableList in state.VariableLists.Where(item => item.SourceObject == name))
                    {
                        descriptions.Add($"{derived.Name} ({state.Name}): {variableList.Name}");
                    }
                }
            }

            if (animations != null)
            {
                foreach (AnimationSave animation in animations.Animations)
                {
                    foreach (AnimationReferenceSave subAnimation in animation.Animations.Where(item => item.SourceObject == name))
                    {
                        descriptions.Add($"{sourceElement.Name}: animation {animation.Name} plays {subAnimation.Name}");
                    }
                }
            }

            results.AddRange(descriptions.Distinct().Select(description => new MovedInstanceReference(name, description)));
        }
        return results.ToArray();
    }
}
