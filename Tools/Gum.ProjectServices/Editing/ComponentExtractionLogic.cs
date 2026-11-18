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

            VariableSave clone = variable.Clone();
            clone.ExposedAsName = null;

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
        List<(StateSave State, VariableListSave List)> keptLists = sourceElement.AllStates
            .Where(state => state != sourceDefault)
            .SelectMany(state => state.VariableLists
                .Where(item => item.SourceObject == instance.Name)
                .Select(item => (state, item.Clone())))
            .ToList();

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

        return replacement;
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
