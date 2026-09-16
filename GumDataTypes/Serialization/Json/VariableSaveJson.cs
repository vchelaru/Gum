using Gum.DataTypes.Variables;
using System.Text.Json.Serialization;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// JSON-serializable shape of a <see cref="VariableSave"/>. See <see cref="BoxedValueJson"/> for why
/// <see cref="VariableSave.Value"/> is represented as a set of typed choice properties instead of a
/// polymorphic <c>object</c>.
/// </summary>
/// <remarks>
/// Every property below except <see cref="Name"/> and <see cref="Type"/> is null/default for most
/// variable instances (issue #4757) - <see cref="JsonIgnoreCondition.WhenWritingDefault"/> omits them
/// rather than writing 12+ null/false fields per instance across a project's thousands of variables.
/// </remarks>
internal sealed class VariableSaveJson
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? StandardizedName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Category { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ExposedAsName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SetsValue { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsFile { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsFont { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsHiddenInPropertyGrid { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsCustomVariable { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Description { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ValueAsString { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float? ValueAsFloat { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? ValueAsInt { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long? ValueAsLong { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? ValueAsDouble { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
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
