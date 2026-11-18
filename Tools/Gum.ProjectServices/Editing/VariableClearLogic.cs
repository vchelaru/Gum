using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Where a category state variable's cleared value comes from.
/// </summary>
public enum CategoryResetSource
{
    /// <summary>The default state's value.</summary>
    DefaultState,
    /// <summary>A state variable the default state does not set; cleared to null.</summary>
    ClearedState,
    /// <summary>What the default state inherits (base type, standard element).</summary>
    Inherited,
    /// <summary>Nothing gives the variable a value.</summary>
    NotFound
}

/// <summary>
/// The value a category state variable takes when cleared, and where it comes from.
/// </summary>
public readonly record struct CategoryResetValue(CategoryResetSource Source, object? Value);

/// <summary>
/// What <see cref="IVariableClearLogic.ClearVariable"/> did.
/// </summary>
/// <param name="Message">A note for the user when the value could not simply be cleared, such as a category state
/// variable taking the default state's value; otherwise null.</param>
public sealed record VariableClearResult(string? Message);

/// <summary>
/// Clears a value set in a state so the inherited value applies: the data change behind the Variables tab's Make
/// Default.
/// </summary>
public interface IVariableClearLogic
{
    /// <summary>
    /// Clears <paramref name="variable"/>'s value in <paramref name="state"/>. The variable is removed unless
    /// something still needs its entry: an exposed variable keeps it with <see cref="VariableSave.SetsValue"/>
    /// false, a custom variable's definition takes its type's zero value, a variable in a category state takes the
    /// default state's value, and a state variable of a category the element defines is cleared in place.
    /// </summary>
    VariableClearResult ClearVariable(ElementSave element, StateSave state, VariableSave variable, bool isPartOfCategory);

    /// <summary>
    /// The value <see cref="ClearVariable"/> gives <paramref name="variable"/> in a category state. Every state in a
    /// category sets the variable, so it can't be removed; it takes the default state's value, falling back to what
    /// the default state inherits, which is also the value <see cref="ICategoryVariablePropagator"/> gives the
    /// category's other states. A state variable the default state doesn't set is cleared instead.
    /// </summary>
    CategoryResetValue GetCategoryResetValue(ElementSave element, VariableSave variable);

    /// <summary>
    /// Whether <paramref name="variable"/> is the definition of a custom variable, which has no base value to fall
    /// back to.
    /// </summary>
    bool IsCustomVariableDefinition(VariableSave variable, StateSave? state);
}

/// <inheritdoc/>
public class VariableClearLogic : IVariableClearLogic
{
    /// <inheritdoc/>
    public VariableClearResult ClearVariable(ElementSave element, StateSave state, VariableSave variable,
        bool isPartOfCategory)
    {
        // An exposed variable keeps its entry, or it would be un-exposed. A category state keeps every variable of
        // the category.
        bool shouldRemove = string.IsNullOrEmpty(variable.ExposedAsName) && !isPartOfCategory && !variable.IsCustomVariable;

        // A state variable of a category this element defines is the variable's base definition.
        if (shouldRemove && variable.IsState(element, out ElementSave? categoryContainer, out StateSaveCategory? categoryForVariable))
        {
            shouldRemove = !(categoryForVariable != null && categoryContainer == element);
        }

        string? message = null;
        if (shouldRemove)
        {
            state.Variables.Remove(variable);
        }
        else if (isPartOfCategory)
        {
            string? categoryName = element.Categories.FirstOrDefault(item => item.States.Contains(state))?.Name;
            CategoryResetValue reset = GetCategoryResetValue(element, variable);
            switch (reset.Source)
            {
                case CategoryResetSource.DefaultState:
                    message = $"The variable {variable.Name} is part of the category {categoryName} so it cannot be removed. Instead, the value has been set to the value in the default state";
                    variable.Value = reset.Value;
                    break;
                case CategoryResetSource.ClearedState:
                    variable.Value = null;
                    variable.SetsValue = true;
                    break;
                case CategoryResetSource.Inherited:
                    message = $"The variable {variable.Name} is part of the category {categoryName} so it cannot be removed. Instead, the value has been set to the value the default state inherits";
                    variable.Value = reset.Value;
                    break;
                default:
                    message = $"Could not set {variable.Name} to default because neither the default state nor its base types set this value";
                    break;
            }
        }
        else if (IsCustomVariableDefinition(variable, state))
        {
            // The default state holds the definition of a custom variable, so there is no base value to fall back
            // to. Leave it a number rather than blank.
            variable.Value = TypeZeroValue.For(variable.Type);
        }
        else
        {
            variable.Value = null;
            variable.SetsValue = false;
        }

        return new VariableClearResult(message);
    }

    /// <inheritdoc/>
    public CategoryResetValue GetCategoryResetValue(ElementSave element, VariableSave variable)
    {
        StateSave defaultState = element.GetDefaultStateOrThrow();
        VariableSave? variableInDefault = defaultState.GetVariableSave(variable.Name);
        if (variableInDefault != null)
        {
            // A custom variable saved with no default value still has to leave the state holding a number.
            object? value = variableInDefault.Value
                ?? (variableInDefault.IsCustomVariable ? TypeZeroValue.For(variableInDefault.Type) : null);
            return new CategoryResetValue(CategoryResetSource.DefaultState, value);
        }

        if (variable.IsState(element))
        {
            return new CategoryResetValue(CategoryResetSource.ClearedState, null);
        }

        object? inheritedValue = defaultState.GetValueRecursive(variable.Name);
        return inheritedValue != null
            ? new CategoryResetValue(CategoryResetSource.Inherited, inheritedValue)
            : new CategoryResetValue(CategoryResetSource.NotFound, null);
    }

    /// <inheritdoc/>
    public bool IsCustomVariableDefinition(VariableSave variable, StateSave? state) =>
        variable.IsCustomVariable && state != null && state == state.ParentContainer?.DefaultState;
}
