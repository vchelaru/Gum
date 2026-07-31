using Gum.DataTypes.Variables;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// JSON-serializable shape of a <see cref="VariableSave"/>. See <see cref="BoxedValueJson"/> for why
/// <see cref="VariableSave.Value"/> is represented as a set of typed choice properties instead of a
/// polymorphic <c>object</c>.
/// </summary>
internal sealed class VariableSaveJson
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? StandardizedName { get; set; }
    public string? Category { get; set; }
    public string? ExposedAsName { get; set; }
    public bool SetsValue { get; set; }
    public bool IsFile { get; set; }
    public bool IsFont { get; set; }
    public bool IsHiddenInPropertyGrid { get; set; }
    public bool IsCustomVariable { get; set; }
    public string? Description { get; set; }

    public string? ValueAsString { get; set; }
    public float? ValueAsFloat { get; set; }
    public int? ValueAsInt { get; set; }
    public long? ValueAsLong { get; set; }
    public double? ValueAsDouble { get; set; }
    public bool? ValueAsBool { get; set; }
}

internal static class VariableSaveJsonMapper
{
    public static VariableSaveJson ToJson(VariableSave source)
    {
        VariableSaveJson dto = new VariableSaveJson
        {
            Name = source.Name,
            Type = source.Type,
            StandardizedName = string.IsNullOrEmpty(source.StandardizedName) ? null : source.StandardizedName,
            Category = string.IsNullOrEmpty(source.Category) ? null : source.Category,
            ExposedAsName = source.ExposedAsName,
            SetsValue = source.SetsValue,
            IsFile = source.IsFile,
            IsFont = source.IsFont,
            IsHiddenInPropertyGrid = source.IsHiddenInPropertyGrid,
            IsCustomVariable = source.IsCustomVariable,
            Description = source.Description,
        };

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

    public static VariableSave FromJson(VariableSaveJson dto)
    {
        VariableSave result = new VariableSave
        {
            Name = dto.Name,
            Type = dto.Type,
            StandardizedName = dto.StandardizedName ?? "",
            Category = dto.Category ?? "",
            ExposedAsName = dto.ExposedAsName,
            SetsValue = dto.SetsValue,
            IsFile = dto.IsFile,
            IsFont = dto.IsFont,
            IsHiddenInPropertyGrid = dto.IsHiddenInPropertyGrid,
            IsCustomVariable = dto.IsCustomVariable,
            Description = dto.Description,
            Value = BoxedValueJson.Read(
                dto.ValueAsString,
                dto.ValueAsFloat,
                dto.ValueAsInt,
                dto.ValueAsLong,
                dto.ValueAsDouble,
                dto.ValueAsBool),
        };

        return result;
    }
}
