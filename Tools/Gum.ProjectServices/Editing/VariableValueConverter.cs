using System;
using System.Linq;
using System.Text.Json;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Resolves a variable name and converts a JSON value to the variable's type the way <c>gumcli edit setVariable</c>
/// does. <see cref="ProjectEditService"/> and the screenshot <c>--set</c> overrides share it. Both methods throw
/// <see cref="EditException"/> with a message for the user.
/// </summary>
internal class VariableValueConverter
{
    private readonly IVariableDefinedByTypeLogic _variableDefinedByType;
    private readonly ITypeResolver _typeResolver;

    public VariableValueConverter(IVariableDefinedByTypeLogic variableDefinedByType, ITypeResolver typeResolver)
    {
        _variableDefinedByType = variableDefinedByType;
        _typeResolver = typeResolver;
    }

    /// <summary>
    /// Resolves <paramref name="requestedName"/> on <paramref name="element"/>: one of its own variables, or
    /// <c>Instance.Variable</c> for one of its instances. A path through nested instances
    /// (<c>Hud.Bar.Visible</c>) is refused: the tool stores no variable on an instance inside another instance.
    /// </summary>
    public (string VariableName, InstanceSave? Instance) ResolveVariable(ElementSave element, string requestedName, string verb)
    {
        string? instanceName = VariableSave.GetSourceObject(requestedName);
        if (instanceName != null && VariableSave.GetSourceObject(VariableSave.GetRootName(requestedName)) is { } innerName)
        {
            ElementSave typeElement = GetInstanceType(element, instanceName);
            string innerVariable = VariableSave.GetRootName(requestedName);
            throw new EditException($"{requestedName} cannot be {verb}: {innerName} is an instance inside {instanceName}. "
                + $"To change it for every {typeElement.Name}, set {innerVariable} on {typeElement.Name}.");
        }
        return ResolveOnElement(element, requestedName, requestedName, verb);
    }

    /// <summary>
    /// Resolves <paramref name="requestedName"/> like <see cref="ResolveVariable"/>, but also through nested
    /// instances: each segment before the last two names an instance in the element the previous segment's
    /// instance is a type of (<c>Hud.Bar.Visible</c> is Bar's Visible inside Hud's component). Returns that
    /// innermost element and the variable name relative to it.
    /// </summary>
    public (ElementSave Owner, string VariableName, InstanceSave? Instance) ResolveNestedVariable(
        ElementSave element, string requestedName, string verb)
    {
        ElementSave owner = element;
        string remaining = requestedName;
        while (VariableSave.GetSourceObject(remaining) is { } instanceName
            && VariableSave.GetSourceObject(VariableSave.GetRootName(remaining)) != null)
        {
            owner = GetInstanceType(owner, instanceName);
            remaining = VariableSave.GetRootName(remaining);
        }
        (string variableName, InstanceSave? instance) = ResolveOnElement(owner, remaining, requestedName, verb);
        return (owner, variableName, instance);
    }

    // requestedName has at most one dot; fullName is the path as the user typed it, for messages.
    private (string VariableName, InstanceSave? Instance) ResolveOnElement(
        ElementSave element, string requestedName, string fullName, string verb)
    {
        string? instanceName = VariableSave.GetSourceObject(requestedName);
        if (!string.IsNullOrEmpty(instanceName))
        {
            InstanceSave instance = GetInstance(element, instanceName);
            ElementSave typeElement = GetInstanceType(element, instanceName);
            string rootName = VariableSave.GetRootName(requestedName);
            if (!_variableDefinedByType.GetIfVariableIsDefinedByType(rootName, typeElement))
            {
                throw new EditException($"{typeElement.Name} has no variable named {rootName}, so {fullName} cannot be {verb}.");
            }
            return (requestedName, instance);
        }

        if (!_variableDefinedByType.GetIfVariableIsDefinedByType(requestedName, element))
        {
            throw new EditException($"{element.Name} has no variable named {requestedName}.");
        }
        // An element's own exposed variable is stored under its instance's name.
        if (element.DefaultState?.Variables.FirstOrDefault(item => item.ExposedAsName == requestedName
                && !string.IsNullOrEmpty(item.SourceObject)) is { } exposed)
        {
            return (exposed.Name, element.GetInstance(exposed.SourceObject!));
        }
        return (requestedName, null);
    }

    private static InstanceSave GetInstance(ElementSave element, string instanceName) =>
        element.GetInstance(instanceName) ?? throw new EditException($"{element.Name} has no instance named {instanceName}.");

    private static ElementSave GetInstanceType(ElementSave element, string instanceName)
    {
        InstanceSave instance = GetInstance(element, instanceName);
        return ObjectFinder.Self.GetElementSave(instance.BaseType)
            ?? throw new EditException($"{instanceName}'s type {instance.BaseType} is not in the project.");
    }

    /// <summary>
    /// The type of <paramref name="variableName"/> as <paramref name="writer"/> resolves it, else the type its
    /// behavior declares when it is a Forms property (a Slider's Maximum), which no element stores. Only the CLI
    /// asks; the tool's own writes keep the writer's answer, so the shape it saves does not change.
    /// </summary>
    public string? ResolveType(IStateVariableWriter writer, ElementSave element, StateSave state, string variableName)
    {
        string? type = writer.ResolveVariableType(element, state, variableName);
        if (!string.IsNullOrEmpty(type))
        {
            return type;
        }

        string? instanceName = VariableSave.GetSourceObject(variableName);
        ElementSave? typeElement = string.IsNullOrEmpty(instanceName)
            ? element
            : ObjectFinder.Self.GetElementSave(element.GetInstance(instanceName)?.BaseType);
        string? declaredType = typeElement == null
            ? null
            : FormsBehaviorChain.FindFormsPropertyDeclaration(typeElement, VariableSave.GetRootName(variableName))?.Type;
        return string.IsNullOrEmpty(declaredType) ? null : declaredType;
    }

    /// <summary>
    /// Converts <paramref name="value"/> for <paramref name="variableName"/> on <paramref name="element"/>
    /// (<c>Instance.Variable</c> for an instance's variable). A state selector (<c>State</c>, or <c>{Category}State</c>
    /// for a category the type defines) takes a state name whatever its type names, because the type is the
    /// category's name and a category can share its name with an enum (<c>InputDevice</c>, <c>Orientation</c>).
    /// Anything else converts as <see cref="ConvertValue"/> does.
    /// </summary>
    public object? ConvertVariableValue(JsonElement value, ElementSave element, string variableName, string type, string displayName)
    {
        if (!IsStateSelector(element, variableName))
        {
            return ConvertValue(value, type, displayName);
        }
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            _ => throw new EditException($"{value.GetRawText()} is not a state name, so {displayName} cannot be set to it.")
        };
    }

    private bool IsStateSelector(ElementSave element, string variableName)
    {
        string rootName = VariableSave.GetRootName(variableName);
        if (rootName == "State")
        {
            return true;
        }
        string? instanceName = VariableSave.GetSourceObject(variableName);
        ElementSave? typeElement = string.IsNullOrEmpty(instanceName)
            ? element
            : ObjectFinder.Self.GetElementSave(element.GetInstance(instanceName)?.BaseType);
        return typeElement != null && _variableDefinedByType.GetVariablesDefinedByType(typeElement)
            .Any(defined => defined.Name == rootName && defined.Category != null);
    }

    public object? ConvertValue(JsonElement value, string type, string variableName)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        // A null value is handled above, so a nullable type ("float?", "Orientation?") converts as its underlying type.
        string valueType = type.EndsWith('?') ? type.Substring(0, type.Length - 1) : type;
        object? converted = valueType switch
        {
            "float" => value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float single) ? single : null,
            "double" => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number) ? number : null,
            "int" => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int integer) ? integer : null,
            "bool" => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null,
            "string" => value.ValueKind == JsonValueKind.String ? value.GetString() : null,
            _ => ConvertOtherType(value, valueType)
        };

        return converted ?? throw new EditException($"{value.GetRawText()} is not a valid {type} value for {variableName}.");
    }

    private object? ConvertOtherType(JsonElement value, string type)
    {
        Type? resolved = _typeResolver.GetTypeFromString(type);
        if (resolved != null && resolved.IsEnum)
        {
            if (value.ValueKind == JsonValueKind.String && Enum.GetNames(resolved).Contains(value.GetString()))
            {
                return Enum.Parse(resolved, value.GetString()!);
            }
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) && Enum.IsDefined(resolved, number))
            {
                return Enum.ToObject(resolved, number);
            }
            return null;
        }

        // States, file names and other text-valued variables.
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}

/// <summary>
/// An edit that cannot apply, with a message for the user.
/// </summary>
internal sealed class EditException : Exception
{
    public EditException(string message) : base(message)
    {
    }
}
