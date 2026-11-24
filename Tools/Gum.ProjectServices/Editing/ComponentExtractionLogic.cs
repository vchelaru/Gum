using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// A component made from an instance by <see cref="IComponentExtractionLogic.CreateComponent"/>, not yet added
/// to the project.
/// </summary>
public class ComponentExtraction
{
    public ComponentExtraction(ComponentSave component, InstanceSave instance, ElementSave sourceElement,
        IReadOnlyList<InstanceSave> movedInstances)
    {
        Component = component;
        Instance = instance;
        SourceElement = sourceElement;
        MovedInstances = movedInstances;
    }

    /// <summary>The new component.</summary>
    public ComponentSave Component { get; }

    /// <summary>The instance that became the component's root.</summary>
    public InstanceSave Instance { get; }

    /// <summary>The element that holds <see cref="Instance"/>.</summary>
    public ElementSave SourceElement { get; }

    /// <summary>The instances under <see cref="Instance"/> in <see cref="SourceElement"/>, which the component copies.</summary>
    public IReadOnlyList<InstanceSave> MovedInstances { get; }
}

/// <summary>
/// The data change behind the tool's Create Component: an instance becomes a new component and can be replaced
/// by one instance of it. Saving, undo, selection, plugin events and dropping the references the move breaks
/// (<see cref="IMovedInstanceReferenceDropLogic"/>) stay with the caller.
/// </summary>
public interface IComponentExtractionLogic
{
    /// <summary>
    /// Makes a component from <paramref name="instance"/> without adding it to the project: the instance's type
    /// becomes the base type, the instances attached under it become the component's instances, and the
    /// instance's own default-state variables, other than its position, become the component's. Variable
    /// references are not copied; the values they last wrote are.
    /// </summary>
    /// <param name="instance">The instance to promote. Its <see cref="InstanceSave.ParentContainer"/> is the source element.</param>
    /// <param name="componentName">The new component's name, including any folder.</param>
    ComponentExtraction CreateComponent(InstanceSave instance, string componentName);

    /// <summary>
    /// Why <paramref name="instance"/> cannot become a component, or null. The new component exposes what the
    /// source element exposes from the instances under <paramref name="instance"/>, so each exposed name must be
    /// free in a component of the instance's type, by the check Expose Variable uses.
    /// </summary>
    /// <param name="getWhyVariableNameInvalid">The Expose Variable name check: why a new variable name is invalid
    /// in an element, or null when it is valid.</param>
    string? GetWhyCannotCreateComponent(InstanceSave instance, Func<string, ElementSave, string?> getWhyVariableNameInvalid);

    /// <summary>
    /// Removes the promoted instance and the instances under it from the source element and puts one instance of
    /// the new component, with the same name, in the promoted instance's place in the instance order. The
    /// replacement keeps the promoted instance's position, what other states set on it, and the values outside
    /// the subtree that name it.
    /// </summary>
    /// <param name="extraction">What <see cref="CreateComponent"/> returned.</param>
    /// <param name="removeInstance">Removes one instance from the source element, as the caller removes instances.</param>
    /// <returns>The replacement instance.</returns>
    InstanceSave ReplaceWithComponentInstance(ComponentExtraction extraction, Action<InstanceSave> removeInstance);
}

/// <inheritdoc/>
public class ComponentExtractionLogic : IComponentExtractionLogic
{
    /// <summary>
    /// The variables that place an instance in its parent. They stay with the replacement instance instead of
    /// moving to the component root, which has no parent to be placed in. Mirrors the Position category of the
    /// standard elements plus the Parent attachment.
    /// </summary>
    private static readonly HashSet<string> PositionalRootNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "X", "Y", "XUnits", "YUnits", "XOrigin", "YOrigin", "Parent"
    };

    private readonly IElementCreationLogic _elementCreation;

    public ComponentExtractionLogic(IElementCreationLogic elementCreation)
    {
        _elementCreation = elementCreation;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotCreateComponent(InstanceSave instance, Func<string, ElementSave, string?> getWhyVariableNameInvalid)
    {
        StateSave? sourceDefault = instance.ParentContainer?.DefaultState;
        if (sourceDefault == null)
        {
            return null;
        }
        HashSet<string> movedNames = new HashSet<string>(instance.ParentContainer!.GetInstancesAndChildrenOf(new[] { instance })
            .Where(item => item.Name != instance.Name)
            .Select(item => item.Name), StringComparer.Ordinal);
        List<VariableSave> exposed = sourceDefault.Variables
            .Where(item => !string.IsNullOrEmpty(item.ExposedAsName) && item.SourceObject is { } owner && movedNames.Contains(owner))
            .ToList();
        if (exposed.Count == 0)
        {
            return null;
        }

        // An empty component of the instance's type has every name the new component inherits.
        ComponentSave scratch = new ComponentSave();
        _elementCreation.PrepareNewComponent(scratch, instance.Name + "Component", instance.BaseType);
        foreach (VariableSave variable in exposed)
        {
            if (getWhyVariableNameInvalid(variable.ExposedAsName!, scratch) is { } why)
            {
                return $"{variable.Name} is exposed as {variable.ExposedAsName}, which the new component cannot expose: {why}";
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public ComponentExtraction CreateComponent(InstanceSave instance, string componentName)
    {
        ElementSave sourceElement = instance.ParentContainer
            ?? throw new InvalidOperationException(
                $"The instance {instance.Name} must have a ParentContainer to be promoted into a component.");
        StateSave sourceDefault = sourceElement.GetDefaultStateOrThrow();

        ComponentSave component = new ComponentSave();
        _elementCreation.PrepareNewComponent(component, componentName, instance.BaseType);
        StateSave componentDefault = component.GetDefaultStateOrThrow();

        // The promoted instance becomes the component root, not one of its instances.
        List<InstanceSave> descendants = sourceElement.GetInstancesAndChildrenOf(new[] { instance })
            .Where(item => item.Name != instance.Name)
            .ToList();
        HashSet<string> descendantNames = new HashSet<string>(descendants.Select(item => item.Name), StringComparer.Ordinal);

        foreach (InstanceSave descendant in descendants)
        {
            component.Instances.Add(new InstanceSave
            {
                Name = descendant.Name,
                BaseType = descendant.BaseType,
                Locked = descendant.Locked,
                ParentContainer = component,
            });
        }

        // A direct child of the promoted instance now attaches to the component root.
        foreach (VariableSave variable in sourceDefault.Variables)
        {
            if (string.IsNullOrEmpty(variable.SourceObject) || !descendantNames.Contains(variable.SourceObject))
            {
                continue;
            }

            // The exposure moves with the variable, so the new component exposes it under the same name.
            VariableSave clone = variable.Clone();

            if (clone.GetRootName() == "Parent" && clone.Value is string parentValue)
            {
                if (parentValue == instance.Name)
                {
                    continue;
                }
                if (parentValue.StartsWith(instance.Name + "."))
                {
                    // Was attached to a default child container of the promoted instance.
                    clone.Value = parentValue.Substring(instance.Name.Length + 1);
                }
            }

            componentDefault.Variables.Add(clone);
        }

        foreach (VariableListSave variableList in sourceDefault.VariableLists)
        {
            // A reference may read the promoted instance; the value it last wrote is copied above as a variable.
            if (variableList.GetRootName() == "VariableReferences")
            {
                continue;
            }
            if (!string.IsNullOrEmpty(variableList.SourceObject) && descendantNames.Contains(variableList.SourceObject))
            {
                componentDefault.VariableLists.Add(variableList.Clone());
            }
        }

        // The promoted instance's own variables move to the root, unqualified, except its position.
        foreach (VariableSave variable in sourceDefault.Variables)
        {
            if (variable.SourceObject != instance.Name)
            {
                continue;
            }
            string rootName = variable.GetRootName();
            if (PositionalRootNames.Contains(rootName))
            {
                continue;
            }
            // The component's own variable has no instance to be exposed from; the replacement keeps that exposure.
            VariableSave clone = variable.Clone();
            clone.Name = rootName;
            clone.ExposedAsName = null;
            componentDefault.Variables.RemoveAll(item => item.Name == rootName);
            componentDefault.Variables.Add(clone);
        }

        foreach (VariableListSave variableList in sourceDefault.VariableLists)
        {
            if (variableList.SourceObject != instance.Name)
            {
                continue;
            }
            string rootName = variableList.GetRootName();
            if (rootName == "VariableReferences")
            {
                continue;
            }
            VariableListSave clone = variableList.Clone();
            clone.Name = rootName;
            componentDefault.VariableLists.RemoveAll(item => item.Name == rootName);
            componentDefault.VariableLists.Add(clone);
        }

        // An event of a moved instance moves with it, exposure included, the same as its variables.
        foreach (EventSave sourceEvent in sourceElement.Events)
        {
            if (sourceEvent.GetSourceObject() is { } owner && descendantNames.Contains(owner))
            {
                component.Events.Add(CloneEvent(sourceEvent, sourceEvent.Name));
            }
        }

        return new ComponentExtraction(component, instance, sourceElement, descendants);
    }

    /// <inheritdoc/>
    public InstanceSave ReplaceWithComponentInstance(ComponentExtraction extraction, Action<InstanceSave> removeInstance)
    {
        InstanceSave instance = extraction.Instance;
        ElementSave sourceElement = extraction.SourceElement;
        IReadOnlyList<InstanceSave> descendants = extraction.MovedInstances;
        StateSave sourceDefault = sourceElement.GetDefaultStateOrThrow();

        // The instance order is the draw order, so the replacement takes the promoted instance's place.
        int replacementIndex = sourceElement.Instances
            .TakeWhile(item => item != instance)
            .Count(item => !descendants.Contains(item));

        // Removing the instance strips everything that names it, but the replacement keeps its name.
        List<(StateSave State, VariableSave Variable)> keptVariables =
            GetVariablesKeptByReplacement(instance, sourceElement, sourceDefault, descendants);
        keptVariables.AddRange(GetExposedVariablesCarriedByReplacement(instance, sourceElement, sourceDefault, descendants));
        List<(StateSave State, VariableListSave List)> keptLists = sourceElement.AllStates
            .Where(state => state != sourceDefault)
            .SelectMany(state => state.VariableLists
                .Where(item => item.SourceObject == instance.Name)
                .Select(item => (state, item.Clone())))
            .ToList();
        List<EventSave> keptEvents = GetEventsKeptByReplacement(instance, sourceElement, descendants);

        foreach (InstanceSave descendant in descendants)
        {
            removeInstance(descendant);
        }
        removeInstance(instance);

        InstanceSave replacement = new InstanceSave
        {
            Name = instance.Name,
            BaseType = extraction.Component.Name,
            ParentContainer = sourceElement,
        };
        sourceElement.Instances.Insert(Math.Min(replacementIndex, sourceElement.Instances.Count), replacement);

        foreach ((StateSave state, VariableSave variable) in keptVariables)
        {
            state.Variables.RemoveAll(item => item.Name == variable.Name);
            state.Variables.Add(variable);
        }
        foreach ((StateSave state, VariableListSave list) in keptLists)
        {
            state.VariableLists.RemoveAll(item => item.Name == list.Name);
            state.VariableLists.Add(list);
        }
        foreach (EventSave keptEvent in keptEvents)
        {
            sourceElement.Events.RemoveAll(item => item.Name == keptEvent.Name);
            sourceElement.Events.Add(keptEvent);
        }

        return replacement;
    }

    /// <summary>
    /// Maps each variable of the subtree that <paramref name="sourceDefault"/> exposes to its name on the replacement
    /// instance: a moved Title.Text exposed as TitleText becomes Header.TitleText, because the new component exposes
    /// it under the same name. The promoted instance's own exposed variables keep their names.
    /// </summary>
    public static Dictionary<string, string> GetExposedVariableNamesOnReplacement(StateSave sourceDefault,
        string promotedName, IEnumerable<string> movedNames)
    {
        HashSet<string> moved = new HashSet<string>(movedNames, StringComparer.Ordinal);
        Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (VariableSave variable in sourceDefault.Variables)
        {
            if (GetNameOnReplacement(variable.Name, variable.SourceObject, variable.ExposedAsName, promotedName, moved)
                is { } newName)
            {
                names[variable.Name] = newName;
            }
        }
        return names;
    }

    /// <summary>
    /// The name an exposed variable or event of the subtree has on the replacement instance, or null when the subtree
    /// does not expose it. Variables and events share it so both follow one rule.
    /// </summary>
    private static string? GetNameOnReplacement(string name, string? owner, string? exposedAs, string promotedName,
        HashSet<string> moved)
    {
        if (string.IsNullOrEmpty(exposedAs) || owner == null)
        {
            return null;
        }
        if (owner == promotedName)
        {
            return name;
        }
        return moved.Contains(owner) ? $"{promotedName}.{exposedAs}" : null;
    }

    /// <summary>
    /// The events the source element keeps through the replacement: the promoted instance's own events, which name an
    /// instance that still exists, and each exposed event of a moved instance under the name the new component
    /// exposes it as. Removing the instances removes the originals.
    /// </summary>
    private static List<EventSave> GetEventsKeptByReplacement(InstanceSave instance, ElementSave sourceElement,
        IReadOnlyList<InstanceSave> descendants)
    {
        HashSet<string> moved = new HashSet<string>(descendants.Select(item => item.Name), StringComparer.Ordinal);
        List<EventSave> kept = new List<EventSave>();
        foreach (EventSave sourceEvent in sourceElement.Events)
        {
            string? owner = sourceEvent.GetSourceObject();
            if (owner == instance.Name)
            {
                kept.Add(CloneEvent(sourceEvent, sourceEvent.Name));
            }
            else if (GetNameOnReplacement(sourceEvent.Name, owner, sourceEvent.ExposedAsName, instance.Name, moved)
                is { } newName)
            {
                kept.Add(CloneEvent(sourceEvent, newName));
            }
        }
        return kept;
    }

    private static EventSave CloneEvent(EventSave sourceEvent, string name) => new EventSave
    {
        Name = name,
        Enabled = sourceEvent.Enabled,
        ExposedAsName = sourceEvent.ExposedAsName,
    };

    /// <summary>
    /// The variables that keep the source element's exposures of the subtree working through the replacement, in
    /// every state, so what other elements set or read through an exposed name still applies. Clears the exposure
    /// on the variables about to be removed so the removal does not clear those outside values.
    /// </summary>
    private static List<(StateSave State, VariableSave Variable)> GetExposedVariablesCarriedByReplacement(
        InstanceSave instance, ElementSave sourceElement, StateSave sourceDefault, IReadOnlyList<InstanceSave> descendants)
    {
        Dictionary<string, string> newNames = GetExposedVariableNamesOnReplacement(sourceDefault, instance.Name,
            descendants.Select(item => item.Name));
        List<(StateSave State, VariableSave Variable)> carried = new List<(StateSave State, VariableSave Variable)>();
        foreach (StateSave state in sourceElement.AllStates)
        {
            foreach (VariableSave variable in state.Variables)
            {
                if (!newNames.TryGetValue(variable.Name, out string? newName))
                {
                    continue;
                }
                VariableSave clone = variable.Clone();
                clone.Name = newName;
                if (state == sourceDefault)
                {
                    variable.ExposedAsName = null;
                }
                carried.Add((state, clone));
            }
        }
        return carried;
    }

    /// <summary>
    /// Copies of the variables in <paramref name="sourceElement"/> that still apply to the replacement instance:
    /// its position (the rest of its default state became the component root's), what the other states set on
    /// it, and values outside the subtree that name it, such as a Parent set in another state.
    /// </summary>
    private static List<(StateSave State, VariableSave Variable)> GetVariablesKeptByReplacement(InstanceSave instance,
        ElementSave sourceElement, StateSave sourceDefault, IReadOnlyList<InstanceSave> descendants)
    {
        HashSet<string> insideNames = new HashSet<string>(descendants.Select(item => item.Name), StringComparer.Ordinal)
        {
            instance.Name
        };
        List<(StateSave State, VariableSave Variable)> kept = new List<(StateSave State, VariableSave Variable)>();
        foreach (StateSave state in sourceElement.AllStates)
        {
            foreach (VariableSave variable in state.Variables)
            {
                string rootName = variable.GetRootName();
                bool isOnInstance = variable.SourceObject == instance.Name &&
                    (state != sourceDefault || PositionalRootNames.Contains(rootName));
                bool namesInstance = !insideNames.Contains(variable.SourceObject ?? "") &&
                    (rootName == "Parent" || rootName == "RenderTargetTextureSource") &&
                    variable.Value is string value &&
                    (value == instance.Name || value.StartsWith(instance.Name + ".", StringComparison.Ordinal));
                if (isOnInstance || namesInstance)
                {
                    kept.Add((state, variable.Clone()));
                }
            }
        }
        return kept;
    }
}
