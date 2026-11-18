using System.Linq;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Removes states and categories: the data change behind the tool's Delete State and Delete Category, with the
/// rules that block them.
/// </summary>
public interface IStateRemovalLogic
{
    /// <summary>
    /// The behaviors of <paramref name="component"/> that define a category named like <paramref name="category"/>.
    /// </summary>
    List<BehaviorSave> GetBehaviorsNeedingCategory(StateSaveCategory category, ComponentSave? component);

    /// <summary>
    /// The behaviors of <paramref name="element"/> whose category of the same name defines a state named like
    /// <paramref name="state"/>. An uncategorized state is never needed by a behavior.
    /// </summary>
    List<BehaviorSave> GetBehaviorsNeedingState(StateSave state, ElementSave? element);

    /// <summary>
    /// Why <paramref name="state"/> cannot be removed from <paramref name="container"/>, or null when it can: a
    /// behavior needs it, or it is the default state.
    /// </summary>
    string? GetWhyCannotRemoveState(StateSave state, IStateContainer container);

    /// <summary>
    /// Why <paramref name="category"/> cannot be removed from <paramref name="container"/>, or null when it can: a
    /// behavior needs it.
    /// </summary>
    string? GetWhyCannotRemoveCategory(StateSaveCategory category, IStateContainer container);

    /// <summary>
    /// Removes <paramref name="state"/> from <paramref name="container"/>, and the variables elsewhere that set it
    /// so they fall back to their inherited value. Returns those variables.
    /// </summary>
    IReadOnlyList<RemovedVariable> RemoveState(StateSave state, IStateContainer container);

    /// <summary>
    /// Removes <paramref name="category"/> from <paramref name="container"/>, and every variable that sets a state
    /// of it: the container's own, those on elements deriving from it, and those on instances of either. Returns
    /// those variables.
    /// </summary>
    IReadOnlyList<RemovedVariable> RemoveCategory(StateSaveCategory category, IStateContainer container);
}

/// <inheritdoc/>
public class StateRemovalLogic : IStateRemovalLogic
{
    private readonly IReferenceFinder _referenceFinder;
    private readonly IReferenceFinderProjectProvider _projectProvider;

    public StateRemovalLogic(IReferenceFinder referenceFinder, IReferenceFinderProjectProvider projectProvider)
    {
        _referenceFinder = referenceFinder;
        _projectProvider = projectProvider;
    }

    /// <inheritdoc/>
    public List<BehaviorSave> GetBehaviorsNeedingCategory(StateSaveCategory category, ComponentSave? component)
    {
        List<BehaviorSave> behaviors = new List<BehaviorSave>();
        if (component == null || _projectProvider.GumProjectSave is not { } project)
        {
            return behaviors;
        }

        IEnumerable<string?> behaviorNames = component.Behaviors.Select(item => item.BehaviorName);
        foreach (BehaviorSave behavior in project.Behaviors.Where(item => behaviorNames.Contains(item.Name)))
        {
            if (behavior.Categories.Any(item => item.Name == category.Name))
            {
                behaviors.Add(behavior);
            }
        }
        return behaviors;
    }

    /// <inheritdoc/>
    public List<BehaviorSave> GetBehaviorsNeedingState(StateSave state, ElementSave? element)
    {
        List<BehaviorSave> behaviors = new List<BehaviorSave>();
        // An uncategorized state can't come from a behavior.
        StateSaveCategory? elementCategory = element?.Categories.FirstOrDefault(item => item.States.Contains(state));
        if (element == null || elementCategory == null)
        {
            return behaviors;
        }

        foreach (BehaviorSave behavior in GetBehaviorsNeedingCategory(elementCategory, element as ComponentSave))
        {
            StateSaveCategory behaviorCategory = behavior.Categories.First(item => item.Name == elementCategory.Name);
            if (behaviorCategory.States.Any(item => item.Name == state.Name))
            {
                behaviors.Add(behavior);
            }
        }
        return behaviors;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotRemoveState(StateSave state, IStateContainer container)
    {
        List<BehaviorSave> behaviors = GetBehaviorsNeedingState(state, container as ElementSave ?? state.ParentContainer);
        if (behaviors.Count > 0)
        {
            return $"The state {state.Name} cannot be removed because it is needed by the following behavior(s):"
                + string.Concat(behaviors.Select(behavior => "\n" + behavior.Name));
        }
        if (container is ElementSave element && element.DefaultState == state)
        {
            return "This state cannot be removed because it is the default state.";
        }
        return null;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotRemoveCategory(StateSaveCategory category, IStateContainer container)
    {
        List<BehaviorSave> behaviors = GetBehaviorsNeedingCategory(category, container as ComponentSave);
        return behaviors.Count > 0
            ? $"The category {category.Name} cannot be removed because it is needed by the following behavior(s):"
                + string.Concat(behaviors.Select(behavior => "\n" + behavior.Name))
            : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<RemovedVariable> RemoveState(StateSave state, IStateContainer container)
    {
        IReadOnlyList<RemovedVariable> removed = RemoveReferencesToState(state, container);

        container.UncategorizedStates.Remove(state);
        foreach (StateSaveCategory category in container.Categories.Where(item => item.States.Contains(state)))
        {
            category.States.Remove(state);
        }
        return removed;
    }

    /// <inheritdoc/>
    public IReadOnlyList<RemovedVariable> RemoveCategory(StateSaveCategory category, IStateContainer container)
    {
        CategoryReferences references = _referenceFinder.GetReferencesToStateCategory(container, category, category.Name);

        List<RemovedVariable> removed = new List<RemovedVariable>();
        foreach (VariableChange change in references.VariableChanges)
        {
            if (change.Container is ElementSave referencingElement)
            {
                change.State.Variables.Remove(change.Variable);
                removed.Add(new RemovedVariable(referencingElement, change.State, change.Variable));
            }
        }

        container.Categories.Remove(category);
        return removed;
    }

    /// <summary>
    /// Removes variables in other elements that set the state, so they fall back to their inherited value instead of
    /// naming a state that no longer exists.
    /// </summary>
    private IReadOnlyList<RemovedVariable> RemoveReferencesToState(StateSave state, IStateContainer container)
    {
        StateSaveCategory? category = container.Categories.FirstOrDefault(item => item.States.Contains(state));
        StateReferences references = _referenceFinder.GetReferencesToState(state, state.Name, container, category);

        List<RemovedVariable> removed = new List<RemovedVariable>();
        foreach ((ElementSave referencingElement, VariableSave variable) in references.VariablesToUpdate)
        {
            foreach (StateSave referencingState in referencingElement.AllStates)
            {
                if (referencingState.Variables.Remove(variable))
                {
                    removed.Add(new RemovedVariable(referencingElement, referencingState, variable));
                }
            }
        }
        return removed;
    }
}
