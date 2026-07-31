using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Gum.DataTypes.Variables;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// JSON-serializable 2D point, standing in for <see cref="Vector2"/> so JSON DTOs never need to
/// serialize a struct whose members are public fields rather than properties.
/// </summary>
internal sealed class PointJson
{
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>
/// JSON-serializable shape of a <see cref="VariableListSave"/>. The abstract base is closed over a
/// small, known set of element types (see the <c>[XmlInclude]</c> list on <see cref="VariableListSave"/>);
/// rather than modeling that polymorphism with reflection-based converters, exactly one of the typed
/// list properties below is populated, selected by which closed generic <see cref="VariableListSave{T}"/>
/// the source instance is.
/// </summary>
internal sealed class VariableListSaveJson
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Category { get; set; }
    public bool IsFile { get; set; }
    public bool IsHiddenInPropertyGrid { get; set; }

    public List<string>? StringValues { get; set; }
    public List<float>? FloatValues { get; set; }
    public List<int>? IntValues { get; set; }
    public List<long>? LongValues { get; set; }
    public List<double>? DoubleValues { get; set; }
    public List<bool>? BoolValues { get; set; }
    public List<PointJson>? Vector2Values { get; set; }
}

internal static class VariableListSaveJsonMapper
{
    public static VariableListSaveJson ToJson(VariableListSave source)
    {
        VariableListSaveJson dto = new VariableListSaveJson
        {
            Name = source.Name,
            Type = source.Type,
            Category = string.IsNullOrEmpty(source.Category) ? null : source.Category,
            IsFile = source.IsFile,
            IsHiddenInPropertyGrid = source.IsHiddenInPropertyGrid,
        };

        switch (source)
        {
            case VariableListSave<string> stringList:
                dto.StringValues = new List<string>(stringList.Value);
                break;
            case VariableListSave<float> floatList:
                dto.FloatValues = new List<float>(floatList.Value);
                break;
            case VariableListSave<int> intList:
                dto.IntValues = new List<int>(intList.Value);
                break;
            case VariableListSave<long> longList:
                dto.LongValues = new List<long>(longList.Value);
                break;
            case VariableListSave<double> doubleList:
                dto.DoubleValues = new List<double>(doubleList.Value);
                break;
            case VariableListSave<bool> boolList:
                dto.BoolValues = new List<bool>(boolList.Value);
                break;
            case VariableListSave<Vector2> vector2List:
                dto.Vector2Values = vector2List.Value.Select(point => new PointJson { X = point.X, Y = point.Y }).ToList();
                break;
            default:
                throw new NotSupportedException(
                    $"Cannot JSON-serialize a VariableListSave with element type '{source.GetType()}'.");
        }

        return dto;
    }

    public static VariableListSave FromJson(VariableListSaveJson dto)
    {
        VariableListSave result;

        if (dto.StringValues != null)
        {
            result = new VariableListSave<string> { Value = new List<string>(dto.StringValues) };
        }
        else if (dto.FloatValues != null)
        {
            result = new VariableListSave<float> { Value = new List<float>(dto.FloatValues) };
        }
        else if (dto.IntValues != null)
        {
            result = new VariableListSave<int> { Value = new List<int>(dto.IntValues) };
        }
        else if (dto.LongValues != null)
        {
            result = new VariableListSave<long> { Value = new List<long>(dto.LongValues) };
        }
        else if (dto.DoubleValues != null)
        {
            result = new VariableListSave<double> { Value = new List<double>(dto.DoubleValues) };
        }
        else if (dto.BoolValues != null)
        {
            result = new VariableListSave<bool> { Value = new List<bool>(dto.BoolValues) };
        }
        else if (dto.Vector2Values != null)
        {
            result = new VariableListSave<Vector2>
            {
                Value = dto.Vector2Values.Select(point => new Vector2(point.X, point.Y)).ToList()
            };
        }
        else
        {
            throw new NotSupportedException($"VariableList '{dto.Name}' has no populated value list.");
        }

        result.Name = dto.Name;
        result.Type = dto.Type;
        result.Category = dto.Category ?? "";
        result.IsFile = dto.IsFile;
        result.IsHiddenInPropertyGrid = dto.IsHiddenInPropertyGrid;

        return result;
    }
}
