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

    /// <summary>The element's base type, or null for a standard element, an element with none, or an instance description.</summary>
    public string? BaseType { get; init; }

    /// <summary>The names of the behaviors the element implements, or null for an instance description.</summary>
    public IReadOnlyList<string>? Behaviors { get; init; }

    /// <summary>The element's instances in order, inherited ones included, or null for an instance description.</summary>
    public IReadOnlyList<DescribedInstance>? Instances { get; init; }

    /// <summary>The variables the element exposes, or null for an instance description.</summary>
    public IReadOnlyList<DescribedExposedVariable>? ExposedVariables { get; init; }

    /// <summary>The element's variable reference lines, or null for an instance description.</summary>
    public IReadOnlyList<DescribedVariableReferences>? VariableReferences { get; init; }
}

/// <summary>
/// An instance in an element.
/// </summary>
/// <param name="Name">The instance name.</param>
/// <param name="Type">The instance's base type.</param>
/// <param name="Parent">The instance's Parent variable, or null when it has none.</param>
/// <param name="DefinedByBase">True when the instance comes from the element's base type.</param>
/// <param name="Locked">True when the instance is locked in the tool.</param>
public record DescribedInstance(string Name, string Type, string? Parent, bool DefinedByBase, bool Locked);

/// <summary>
/// A variable an element exposes.
/// </summary>
/// <param name="Name">The exposed name other elements set.</param>
/// <param name="Variable">The instance variable it stands for, such as <c>Label.Text</c>.</param>
public record DescribedExposedVariable(string Name, string Variable);

/// <summary>
/// The variable reference lines one state holds for the element or for one of its instances.
/// </summary>
/// <param name="Instance">The instance the lines belong to, or null for the element's own.</param>
/// <param name="State">The state holding the lines.</param>
/// <param name="Category">The state's category, or null for an uncategorized state.</param>
/// <param name="Lines">The reference lines.</param>
public record DescribedVariableReferences(string? Instance, string State, string? Category, IReadOnlyList<string> Lines);

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

        bool isElement = instance == null;
        return new ElementDescription
        {
            Element = element.Name,
            Instance = instance?.Name,
            Type = typeElement.Name,
            Variables = variables,
            States = element.States.Select(state => state.Name).ToList(),
            Categories = element.Categories
                .Select(category => new DescribedCategory(category.Name, category.States.Select(state => state.Name).ToList()))
                .ToList(),
            BaseType = isElement && !string.IsNullOrEmpty(element.BaseType) ? element.BaseType : null,
            Behaviors = isElement ? element.Behaviors.Select(behavior => behavior.BehaviorName ?? "").ToList() : null,
            Instances = isElement ? element.Instances.Select(item => DescribeInstance(element, item)).ToList() : null,
            ExposedVariables = isElement
                ? element.GetDefaultStateOrThrow().Variables
                    .Where(variable => !string.IsNullOrEmpty(variable.ExposedAsName))
                    .Select(variable => new DescribedExposedVariable(variable.ExposedAsName!, variable.Name))
                    .ToList()
                : null,
            VariableReferences = isElement ? DescribeReferences(element) : null
        };
    }

    private static DescribedInstance DescribeInstance(ElementSave element, InstanceSave instance)
    {
        string? parent = element.GetDefaultStateOrThrow().GetValueRecursive(instance.Name + ".Parent") as string;
        return new DescribedInstance(instance.Name, instance.BaseType, string.IsNullOrEmpty(parent) ? null : parent,
            instance.DefinedByBase, instance.Locked);
    }

    private static List<DescribedVariableReferences> DescribeReferences(ElementSave element)
    {
        List<DescribedVariableReferences> references = new List<DescribedVariableReferences>();
        IEnumerable<(StateSave State, string? Category)> states = element.States.Select(state => (state, (string?)null))
            .Concat(element.Categories.SelectMany(category => category.States.Select(state => (state, (string?)category.Name))));
        foreach ((StateSave state, string? category) in states)
        {
            foreach (VariableListSave list in state.VariableLists.Where(list => list.Name.EndsWith("VariableReferences", StringComparison.Ordinal)))
            {
                string? instanceName = list.Name == "VariableReferences" ? null : list.Name.Substring(0, list.Name.Length - ".VariableReferences".Length);
                List<string> lines = list.ValueAsIList.OfType<string>().ToList();
                if (lines.Count > 0)
                {
                    references.Add(new DescribedVariableReferences(instanceName, state.Name, category, lines));
                }
            }
        }
        return references;
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
