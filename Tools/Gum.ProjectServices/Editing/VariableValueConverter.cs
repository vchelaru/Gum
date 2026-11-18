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

    public (string VariableName, InstanceSave? Instance) ResolveVariable(ElementSave element, string requestedName, string verb)
    {
        string? instanceName = VariableSave.GetSourceObject(requestedName);
        if (!string.IsNullOrEmpty(instanceName))
        {
            InstanceSave instance = element.GetInstance(instanceName)
                ?? throw new EditException($"{element.Name} has no instance named {instanceName}.");
            ElementSave typeElement = ObjectFinder.Self.GetElementSave(instance.BaseType)
                ?? throw new EditException($"{instanceName}'s type {instance.BaseType} is not in the project.");
            if (!_variableDefinedByType.GetIfVariableIsDefinedByType(VariableSave.GetRootName(requestedName), typeElement))
            {
                throw new EditException($"{typeElement.Name} has no variable named {VariableSave.GetRootName(requestedName)}, so {requestedName} cannot be {verb}.");
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
