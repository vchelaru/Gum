namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// JSON-serializable shape of a <see cref="CustomPropertySave"/>. See <see cref="BoxedValueJson"/> for
/// why <see cref="CustomPropertySave.Value"/> is represented as a set of typed choice properties.
/// </summary>
internal sealed class CustomPropertySaveJson
{
    public string? Name { get; set; }

    public string? ValueAsString { get; set; }
    public float? ValueAsFloat { get; set; }
    public int? ValueAsInt { get; set; }
    public long? ValueAsLong { get; set; }
    public double? ValueAsDouble { get; set; }
    public bool? ValueAsBool { get; set; }
}

internal static class CustomPropertySaveJsonMapper
{
    public static CustomPropertySaveJson ToJson(CustomPropertySave source)
    {
        CustomPropertySaveJson dto = new CustomPropertySaveJson { Name = source.Name };

        BoxedValueJson.Assign(
            source.Value,
            out string? asString,
            out float? asFloat,
            out int? asInt,
            out long? asLong,
            out double? asDouble,
            out bool? asBool);
        dto.ValueAsString = asString;
        dto.ValueAsFloat = asFloat;
        dto.ValueAsInt = asInt;
        dto.ValueAsLong = asLong;
        dto.ValueAsDouble = asDouble;
        dto.ValueAsBool = asBool;

        return dto;
    }

    public static CustomPropertySave FromJson(CustomPropertySaveJson dto)
    {
        return new CustomPropertySave
        {
            Name = dto.Name,
            Value = BoxedValueJson.Read(
                dto.ValueAsString,
                dto.ValueAsFloat,
                dto.ValueAsInt,
                dto.ValueAsLong,
                dto.ValueAsDouble,
                dto.ValueAsBool),
        };
    }
}
