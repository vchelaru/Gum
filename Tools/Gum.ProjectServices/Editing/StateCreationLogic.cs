using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Adds states and categories: the data change behind the tool's Add State and Add Category, without the
/// variable propagation, tree refresh, plugin events or saving the tool does around it.
/// </summary>
public interface IStateCreationLogic
{
    /// <summary>
    /// Adds <paramref name="state"/> to <paramref name="category"/>, or to the uncategorized states when it is
    /// null, at <paramref name="desiredIndex"/> (the end when null).
    /// </summary>
    void AddState(IStateContainer container, StateSaveCategory? category, StateSave state, int? desiredIndex);

    /// <summary>
    /// Adds a new, empty category named <paramref name="name"/> to <paramref name="container"/>.
    /// </summary>
    StateSaveCategory AddCategory(IStateContainer container, string name);

    /// <summary>
    /// Adds the category's selector variable (<c>{categoryName}State</c>) to the element's default state, which
    /// instances read to show the category. Returns the new variable, or null when it already exists.
    /// </summary>
    VariableSave? AddCategoryStateVariable(ElementSave element, string categoryName);
}

/// <inheritdoc/>
public class StateCreationLogic : IStateCreationLogic
{
    /// <inheritdoc/>
    public void AddState(IStateContainer container, StateSaveCategory? category, StateSave state, int? desiredIndex)
    {
        // A behavior's states have no element, so their ParentContainer stays null.
        state.ParentContainer = (container as ElementSave)!;

        var states = category == null ? container.UncategorizedStates : category.States;
        if (desiredIndex != null)
        {
            states.Insert(desiredIndex.Value, state);
        }
        else
        {
            states.Add(state);
        }
    }

    /// <inheritdoc/>
    public StateSaveCategory AddCategory(IStateContainer container, string name)
    {
        StateSaveCategory category = new StateSaveCategory { Name = name };
        container.Categories.Add(category);
        return category;
    }

    /// <inheritdoc/>
    public VariableSave? AddCategoryStateVariable(ElementSave element, string categoryName)
    {
        StateSave defaultState = element.GetDefaultStateOrThrow();
        if (defaultState.Variables.Any(variable => variable.Name == categoryName + "State"))
        {
            return null;
        }

        VariableSave variable = new VariableSave
        {
            Name = categoryName + "State",
            // The type is the category name without a "State" suffix, as elsewhere in Gum.
            Type = categoryName,
            Value = null
        };
        defaultState.Variables.Add(variable);
        defaultState.Variables.Sort((first, second) => GumProjectSave.CompareNames(first.Name, second.Name));
        return variable;
    }
}
