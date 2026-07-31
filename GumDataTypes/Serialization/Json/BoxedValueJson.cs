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
/// <remarks>
/// A boxed <see cref="Enum"/> (e.g. <c>DimensionUnitType.Absolute</c>) is also accepted, converted
/// to its underlying <see cref="int"/> value. This shape only occurs on a variable that was built
/// programmatically and never round-tripped through XML yet (e.g. <c>StandardElementsManager</c>'s
/// default Standards) - <see cref="System.Xml.Serialization.XmlSerializer"/> already stores a boxed
/// enum on <see cref="Variables.VariableSave.Value"/> as <c>xsi:type="xsd:int"</c>, reconstructing a
/// plain <see cref="int"/> (never the enum type) on load, so this mirrors that existing convention
/// rather than introducing a new one.
/// </remarks>
/// <remarks>
/// Known, accepted limitation: <see cref="CustomPropertySave.Value"/>'s XML shape additionally
/// declares a <c>[XmlElement("ValueAsObject", typeof(object))]</c> catch-all (plus a commented-out,
/// never-implemented <c>ValueAsEnum</c> case) for values outside the six supported types above. This
/// class does not replicate that catch-all - it throws <see cref="NotSupportedException"/> instead.
/// Confirmed via repo-wide search: no <c>CustomPropertySave.Value</c> in any checked-in
/// <c>.gumx</c>/<c>.gucx</c>/<c>.gusx</c>/<c>.gutx</c>/<c>.behx</c> file (including the FormsBehaviors
/// templates) is ever populated at all, let alone with a type that would need the catch-all - every
/// real project's <c>CustomProperties</c> list is empty. Revisit if a real project ever needs it.
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
            case Enum enumValue:
                asInt = Convert.ToInt32(enumValue);
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
