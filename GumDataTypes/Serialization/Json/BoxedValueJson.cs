using System;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// Converts a boxed <see cref="object"/> (as stored by <see cref="Variables.VariableSave.Value"/> and
/// <see cref="CustomPropertySave.Value"/>) to and from a set of typed, nullable JSON properties.
/// </summary>
/// <remarks>
/// <see cref="System.Text.Json"/> source generation cannot serialize an <c>object</c>-typed member
/// polymorphically without runtime reflection, which Native AOT forbids. Rather than boxing/unboxing
/// through a hand-written <see cref="System.Text.Json.Serialization.JsonConverter"/>, JSON DTOs
/// expose one nullable property per supported CLR type (mirroring the existing
/// <c>ValueAsString</c>/<c>ValueAsFloat</c>/... XML choice pattern already used by
/// <see cref="CustomPropertySave"/>) and this class maps a boxed value on and off that shape.
/// Every real project file value observed in this repo boxes one of: <see cref="bool"/>,
/// <see cref="int"/>, <see cref="long"/>, <see cref="float"/>, <see cref="double"/>, or
/// <see cref="string"/>.
/// </remarks>
internal static class BoxedValueJson
{
    public static void Assign(
        object? value,
        out string? asString,
        out float? asFloat,
        out int? asInt,
        out long? asLong,
        out double? asDouble,
        out bool? asBool)
    {
        asString = null;
        asFloat = null;
        asInt = null;
        asLong = null;
        asDouble = null;
        asBool = null;

        switch (value)
        {
            case null:
                break;
            case bool boolValue:
                asBool = boolValue;
                break;
            case int intValue:
                asInt = intValue;
                break;
            case long longValue:
                asLong = longValue;
                break;
            case float floatValue:
                asFloat = floatValue;
                break;
            case double doubleValue:
                asDouble = doubleValue;
                break;
            case string stringValue:
                asString = stringValue;
                break;
            default:
                throw new NotSupportedException(
                    $"Cannot JSON-serialize a boxed value of type '{value.GetType()}'. " +
                    "Supported types are bool, int, long, float, double, and string.");
        }
    }

    public static object? Read(
        string? asString,
        float? asFloat,
        int? asInt,
        long? asLong,
        double? asDouble,
        bool? asBool)
    {
        if (asBool.HasValue)
        {
            return asBool.Value;
        }
        if (asInt.HasValue)
        {
            return asInt.Value;
        }
        if (asLong.HasValue)
        {
            return asLong.Value;
        }
        if (asFloat.HasValue)
        {
            return asFloat.Value;
        }
        if (asDouble.HasValue)
        {
            return asDouble.Value;
        }
        return asString;
    }
}
