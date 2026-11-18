using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Adds a custom variable to a Screen or Component: the data change behind the tool's Add Variable.
/// </summary>
public interface ICustomVariableLogic
{
    /// <summary>
    /// The types a custom variable can have.
    /// </summary>
    IReadOnlyList<string> AvailableTypes { get; }

    /// <summary>
    /// The value a new custom variable of <paramref name="type"/> starts with, or null for an unknown type.
    /// </summary>
    object? GetDefaultValue(string? type);

    /// <summary>
    /// Adds a custom variable named <paramref name="name"/> to the element's default state, keeping its states'
    /// variables sorted. The name must already be validated.
    /// </summary>
    VariableSave AddCustomVariable(ElementSave element, string name, string type, object? value);
}

/// <inheritdoc/>
public class CustomVariableLogic : ICustomVariableLogic
{
    private readonly IStateVariableSorter _sorter;

    public CustomVariableLogic(IStateVariableSorter sorter)
    {
        _sorter = sorter;
        AvailableTypes = new[] { "float", "int", "string", "bool" };
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> AvailableTypes { get; }

    /// <inheritdoc/>
    public object? GetDefaultValue(string? type) => type switch
    {
        "float" => 1.0f,
        "int" => 10,
        "string" => "Hello",
        "bool" => true,
        _ => null
    };

    /// <inheritdoc/>
    public VariableSave AddCustomVariable(ElementSave element, string name, string type, object? value)
    {
        VariableSave variable = new VariableSave
        {
            Name = name,
            Type = type,
            Value = value,
            IsCustomVariable = true
        };
        element.GetDefaultStateOrThrow().Variables.Add(variable);
        _sorter.SortStateVariables(element);
        return variable;
    }
}
