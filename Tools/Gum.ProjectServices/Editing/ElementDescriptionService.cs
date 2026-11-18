using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// What <c>gumcli describe</c> reports: the variables an element, or one of its instances, can set, and the
/// element's states and categories.
/// </summary>
public class ElementDescription
{
    /// <summary>The described element's name.</summary>
    public string Element { get; init; } = "";

    /// <summary>The described instance's name, or null when the element's own variables are listed.</summary>
    public string? Instance { get; init; }

    /// <summary>The element whose type defines the listed variables: the element itself, or the instance's type.</summary>
    public string Type { get; init; } = "";

    /// <summary>The variables, named as <c>setVariable</c> takes them and sorted by name.</summary>
    public IReadOnlyList<DescribedVariable> Variables { get; init; } = Array.Empty<DescribedVariable>();

    /// <summary>The element's uncategorized states.</summary>
    public IReadOnlyList<string> States { get; init; } = Array.Empty<string>();

    /// <summary>The element's categories and their states.</summary>
    public IReadOnlyList<DescribedCategory> Categories { get; init; } = Array.Empty<DescribedCategory>();
}

/// <summary>
/// A variable <c>setVariable</c> accepts.
/// </summary>
/// <param name="Name">The name, prefixed with the instance name for an instance's variable.</param>
/// <param name="Type">The variable's type.</param>
/// <param name="Values">The allowed values of an enum or state variable, or null when any value of the type is allowed.</param>
public record DescribedVariable(string Name, string Type, IReadOnlyList<string>? Values);

/// <summary>
/// A category and its states.
/// </summary>
public record DescribedCategory(string Name, IReadOnlyList<string> States);

/// <summary>
/// Lists what an element or instance can set, from <see cref="IVariableDefinedByTypeLogic"/>, so the list matches
/// what <c>gumcli edit</c>'s <c>setVariable</c> accepts.
/// </summary>
public interface IElementDescriptionService
{
    /// <summary>
    /// Describes <paramref name="element"/>, or <paramref name="instance"/> in it. The instance's type must be in the
    /// project <see cref="ObjectFinder"/> resolves.
    /// </summary>
    ElementDescription Describe(ElementSave element, InstanceSave? instance);
}

/// <inheritdoc/>
public class ElementDescriptionService : IElementDescriptionService
{
    private readonly IVariableDefinedByTypeLogic _variableDefinedByType;
    private readonly ITypeResolver _typeResolver;

    public ElementDescriptionService(IVariableDefinedByTypeLogic variableDefinedByType, ITypeResolver typeResolver)
    {
        _variableDefinedByType = variableDefinedByType;
        _typeResolver = typeResolver;
    }

    /// <inheritdoc/>
    public ElementDescription Describe(ElementSave element, InstanceSave? instance)
    {
        ElementSave typeElement = instance == null
            ? element
            : ObjectFinder.Self.GetElementSave(instance.BaseType)
                ?? throw new InvalidOperationException($"{instance.Name}'s type {instance.BaseType} is not in the project.");
        string prefix = instance == null ? "" : instance.Name + ".";

        List<DescribedVariable> variables = _variableDefinedByType.GetVariablesDefinedByType(typeElement)
            .Select(variable => new DescribedVariable(prefix + variable.Name, variable.Type, GetValues(variable, typeElement)))
            .OrderBy(variable => variable.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ElementDescription
        {
            Element = element.Name,
            Instance = instance?.Name,
            Type = typeElement.Name,
            Variables = variables,
            States = element.States.Select(state => state.Name).ToList(),
            Categories = element.Categories
                .Select(category => new DescribedCategory(category.Name, category.States.Select(state => state.Name).ToList()))
                .ToList()
        };
    }

    private IReadOnlyList<string>? GetValues(DefinedVariable variable, ElementSave typeElement)
    {
        if (variable.Category != null)
        {
            return variable.Category.States.Select(state => state.Name).ToList();
        }
        if (variable.Name == "State")
        {
            return new[] { typeElement }.Concat(ObjectFinder.Self.GetBaseElements(typeElement))
                .SelectMany(inChain => inChain.States)
                .Select(state => state.Name)
                .Distinct()
                .ToList();
        }

        Type? type = string.IsNullOrEmpty(variable.Type) ? null : _typeResolver.GetTypeFromString(variable.Type);
        type = type == null ? null : Nullable.GetUnderlyingType(type) ?? type;
        return type is { IsEnum: true } ? Enum.GetNames(type) : null;
    }
}
