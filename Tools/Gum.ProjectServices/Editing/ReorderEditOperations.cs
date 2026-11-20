using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The <c>moveInstance</c> and <c>moveState</c> operations of <c>gumcli edit</c>: the tool's reorder of an instance
/// (and the derived components following it), reorder of a state, and move of a state to another category,
/// through <see cref="ListMover"/>, the list change the tool uses for them.
/// </summary>
internal sealed class ReorderEditOperations
{
    private readonly IInheritanceSync _inheritanceSync;
    private readonly IStateRemovalLogic _stateRemoval;
    private readonly ICategoryVariablePropagator _propagator;
    private readonly IElementNameValidator _nameValidator;

    public ReorderEditOperations(
        IInheritanceSync inheritanceSync,
        IStateRemovalLogic stateRemoval,
        ICategoryVariablePropagator propagator,
        IElementNameValidator nameValidator)
    {
        _inheritanceSync = inheritanceSync;
        _stateRemoval = stateRemoval;
        _propagator = propagator;
        _nameValidator = nameValidator;
    }

    public void MoveInstance(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        InstanceSave instance = element.GetInstance(name)
            ?? throw new EditException($"{element.Name} has no instance named {name}.");
        if (instance.DefinedByBase)
        {
            throw new EditException(
                $"{name} comes from {element.Name}'s base type, so its order follows the base. Move it in {element.BaseType}.");
        }

        // Front is drawn last, so it is the end of the list; the same meaning the tool's Bring to Front has.
        (ReorderPlacement placement, string? anchorName) = ReadPlacement(
            operation, "moveInstance", allowNone: false,
            ("toFront", ReorderPlacement.ToEnd), ("toBack", ReorderPlacement.ToStart))!.Value;

        InstanceSave? anchor = null;
        if (anchorName != null)
        {
            anchor = element.GetInstance(anchorName)
                ?? throw new EditException($"{element.Name} has no instance named {anchorName}.");
            if (anchor == instance)
            {
                throw new EditException($"{name} cannot be placed before or after itself; name a different instance.");
            }
            List<InstanceSave> siblings = instance.GetSiblingsIncludingThis();
            if (!siblings.Contains(anchor))
            {
                throw new EditException(
                    $"{name} and {anchorName} have different parents, so they cannot be ordered against each other. "
                    + $"The instances that share {name}'s parent: {string.Join(", ", siblings.Where(item => item != instance).Select(item => item.Name))}.");
            }
        }

        ListMover.Move(element.Instances, instance, placement, anchor);
        _inheritanceSync.ReorderInstance(instance);
    }

    public void MoveState(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = GetElement(project, operation);
        string name = GetString(operation, "name");
        StateSaveCategory? from = GetOptionalString(operation, "category") is { } fromName ? GetCategory(element, fromName) : null;
        IList<StateSave> fromList = from?.States ?? ((IStateContainer)element).UncategorizedStates;
        StateSave state = fromList.FirstOrDefault(item => item.Name == name)
            ?? throw new EditException(from == null
                ? $"{element.Name} has no uncategorized state named {name}."
                : $"{element.Name}'s category {from.Name} has no state named {name}.");

        StateSaveCategory? to = from;
        if (GetOptionalString(operation, "toCategory") is { } toName)
        {
            to = GetCategory(element, toName);
        }
        bool changesCategory = to != from;

        if (element.DefaultState == state)
        {
            throw new EditException($"{name} is the default state, which stays first.");
        }
        if (changesCategory)
        {
            ValidateCategoryChange(element, state, from, to!);
        }

        (ReorderPlacement placement, string? anchorName)? read = ReadPlacement(
            operation, "moveState", allowNone: changesCategory,
            ("toStart", ReorderPlacement.ToStart), ("toEnd", ReorderPlacement.ToEnd));
        (ReorderPlacement placement, string? anchorName) = read ?? (ReorderPlacement.ToEnd, null);

        IList<StateSave> toList = to?.States ?? ((IStateContainer)element).UncategorizedStates;
        StateSave? anchor = null;
        if (anchorName != null)
        {
            anchor = toList.FirstOrDefault(item => item.Name == anchorName)
                ?? throw new EditException(to == null
                    ? $"{element.Name} has no uncategorized state named {anchorName}."
                    : $"{element.Name}'s category {to.Name} has no state named {anchorName}.");
            if (anchor == state)
            {
                throw new EditException($"{name} cannot be placed before or after itself; name a different state.");
            }
        }
        // The first uncategorized state is Default, which the tool does not let a state move above.
        if (to == null && (placement == ReorderPlacement.ToStart || (placement == ReorderPlacement.Before && anchor == toList[0])))
        {
            throw new EditException($"Nothing can go before {toList[0].Name}, the default state. Use after: \"{toList[0].Name}\" to make {name} the first other state.");
        }

        if (changesCategory)
        {
            ListMover.MoveBetween(fromList, toList, state, placement, anchor);
            _propagator.PropagateAllVariablesInCategory(element, to!);
        }
        else
        {
            ListMover.Move(toList, state, placement, anchor);
        }
    }

    private void ValidateCategoryChange(ElementSave element, StateSave state, StateSaveCategory? from, StateSaveCategory to)
    {
        // The tool moves a state only from one category to another.
        if (from == null)
        {
            throw new EditException(
                $"{state.Name} is not in a category; the tool moves states only between categories. Add the state to {to.Name} with addState and remove this one.");
        }
        List<BehaviorSave> behaviors = _stateRemoval.GetBehaviorsNeedingState(state, element);
        if (behaviors.Count > 0)
        {
            throw new EditException(
                $"The state {state.Name} cannot be moved to a different category because it is needed by the following behavior(s): "
                + string.Join(", ", behaviors.Select(behavior => behavior.Name)));
        }
        if (!_nameValidator.IsStateNameValid(state.Name, to, null, out string? whyNotValid))
        {
            throw new EditException($"{state.Name} cannot move to {to.Name}: {whyNotValid}");
        }
    }

    // Reads the position fields: "before" or "after" naming a sibling, or one keyword. Exactly one is allowed,
    // so no position is ever guessed; null when none is given and allowNone is set.
    private static (ReorderPlacement Placement, string? AnchorName)? ReadPlacement(
        JsonElement operation, string op, bool allowNone, params (string Field, ReorderPlacement Placement)[] keywords)
    {
        List<(string Field, ReorderPlacement Placement, string? Anchor)> given = new List<(string, ReorderPlacement, string?)>();
        if (GetOptionalString(operation, "before") is { } before)
        {
            given.Add(("before", ReorderPlacement.Before, before));
        }
        if (GetOptionalString(operation, "after") is { } after)
        {
            given.Add(("after", ReorderPlacement.After, after));
        }
        foreach ((string field, ReorderPlacement placement) in keywords)
        {
            if (operation.TryGetProperty(field, out JsonElement value))
            {
                if (!value.GetBoolean())
                {
                    throw new EditException($"{op}'s \"{field}\" must be true; leave it out to not use it.");
                }
                given.Add((field, placement, null));
            }
        }

        string choices = string.Join(", ", new[] { "before", "after" }.Concat(keywords.Select(keyword => keyword.Field)));
        if (given.Count == 0)
        {
            return allowNone ? null : throw new EditException($"{op} needs one of {choices} to say where it goes.");
        }
        if (given.Count > 1)
        {
            throw new EditException($"{op} takes only one of {choices}, but got {string.Join(" and ", given.Select(item => item.Field))}.");
        }
        return (given[0].Placement, given[0].Anchor);
    }

    private static ElementSave GetElement(GumProjectSave project, JsonElement operation)
    {
        string name = GetString(operation, "element");
        return ProjectElementFinder.Find(project, name)
            ?? throw new EditException($"The project has no Screen, Component or standard element named {name}.");
    }

    private static StateSaveCategory GetCategory(ElementSave element, string categoryName) =>
        element.Categories.FirstOrDefault(item => item.Name == categoryName)
            ?? throw new EditException($"{element.Name} has no category named {categoryName}.");

    private static string GetString(JsonElement operation, string field) => operation.GetProperty(field).GetString()!;

    private static string? GetOptionalString(JsonElement operation, string field) =>
        operation.TryGetProperty(field, out JsonElement value) ? value.GetString() : null;
}
