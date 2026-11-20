using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Adds and removes the behaviors a Component implements: the data change behind the tool's Behaviors tab and
/// dropping a behavior on a Component, without the tree refresh, plugin events or saving the tool does around it.
/// </summary>
public interface IComponentBehaviorLogic
{
    /// <summary>
    /// Why <paramref name="behaviorName"/> cannot be added to <paramref name="component"/> (the project has no such
    /// behavior, or the component already has it), or null when it can.
    /// </summary>
    string? GetWhyCannotAdd(GumProjectSave project, ComponentSave component, string behaviorName);

    /// <summary>
    /// Adds a reference to <paramref name="behavior"/> to <paramref name="component"/> and copies the behavior's
    /// categories and states into it. Returns the categories it created, which the caller may need to announce.
    /// </summary>
    IReadOnlyList<StateSaveCategory> AddBehavior(ComponentSave component, BehaviorSave behavior);

    /// <summary>
    /// Copies <paramref name="behavior"/>'s categories and states into <paramref name="element"/>, creating the
    /// ones it lacks. Returns the categories it created.
    /// </summary>
    IReadOnlyList<StateSaveCategory> AddCategoriesFromBehavior(BehaviorSave behavior, ElementSave element);

    /// <summary>
    /// Why <paramref name="behaviorName"/> cannot be removed from <paramref name="component"/> (it does not have
    /// it), or null when it can.
    /// </summary>
    string? GetWhyCannotRemove(ComponentSave component, string behaviorName);

    /// <summary>
    /// Removes the reference to <paramref name="behaviorName"/>. Like the tool's Behaviors tab, it leaves the
    /// categories the behavior added, which the component may now use for itself.
    /// </summary>
    void RemoveBehavior(ComponentSave component, string behaviorName);
}

/// <inheritdoc/>
public class ComponentBehaviorLogic : IComponentBehaviorLogic
{
    private readonly IStateCreationLogic _stateCreation;

    public ComponentBehaviorLogic(IStateCreationLogic stateCreation)
    {
        _stateCreation = stateCreation;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotAdd(GumProjectSave project, ComponentSave component, string behaviorName)
    {
        if (project.Behaviors.All(item => item.Name != behaviorName))
        {
            string known = project.Behaviors.Count == 0
                ? "The project has no behaviors."
                : "The project's behaviors are: " + string.Join(", ", project.Behaviors.Select(item => item.Name)) + ".";
            return $"The project has no behavior named {behaviorName}. {known}";
        }
        if (component.Behaviors.Any(item => item.BehaviorName == behaviorName))
        {
            return $"{component.Name} already has the behavior {behaviorName}.";
        }
        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<StateSaveCategory> AddBehavior(ComponentSave component, BehaviorSave behavior)
    {
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behavior.Name });
        return AddCategoriesFromBehavior(behavior, component);
    }

    /// <inheritdoc/>
    public IReadOnlyList<StateSaveCategory> AddCategoriesFromBehavior(BehaviorSave behavior, ElementSave element)
    {
        List<StateSaveCategory> created = new List<StateSaveCategory>();
        foreach (StateSaveCategory behaviorCategory in behavior.Categories)
        {
            StateSaveCategory? category = element.Categories.FirstOrDefault(item => item.Name == behaviorCategory.Name);
            if (category == null)
            {
                // The same two steps as the tool's Add Category, so instances can use the category.
                category = _stateCreation.AddCategory(element, behaviorCategory.Name);
                _stateCreation.AddCategoryStateVariable(element, category.Name);
                created.Add(category);
            }

            foreach (StateSave behaviorState in behaviorCategory.States)
            {
                if (category.States.All(item => item.Name != behaviorState.Name))
                {
                    _stateCreation.AddState(element, category, new StateSave { Name = behaviorState.Name }, null);
                }
            }
        }
        return created;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotRemove(ComponentSave component, string behaviorName) =>
        component.Behaviors.Any(item => item.BehaviorName == behaviorName)
            ? null
            : $"{component.Name} does not have the behavior {behaviorName}.";

    /// <inheritdoc/>
    public void RemoveBehavior(ComponentSave component, string behaviorName) =>
        component.Behaviors.RemoveAll(item => item.BehaviorName == behaviorName);
}
