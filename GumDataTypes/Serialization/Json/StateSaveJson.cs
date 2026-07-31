using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Variables;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>JSON-serializable shape of a <see cref="StateSave"/>.</summary>
internal sealed class StateSaveJson
{
    public string Name { get; set; } = "";
    public List<VariableSaveJson> Variables { get; set; } = new List<VariableSaveJson>();
    public List<VariableListSaveJson> VariableLists { get; set; } = new List<VariableListSaveJson>();
}

/// <summary>JSON-serializable shape of a <see cref="StateSaveCategory"/>.</summary>
internal sealed class StateSaveCategoryJson
{
    public string Name { get; set; } = "";
    public List<StateSaveJson> States { get; set; } = new List<StateSaveJson>();
}

internal static class StateSaveJsonMapper
{
    public static StateSaveJson ToJson(StateSave source)
    {
        return new StateSaveJson
        {
            Name = source.Name,
            Variables = source.Variables.Select(VariableSaveJsonMapper.ToJson).ToList(),
            VariableLists = source.VariableLists.Select(VariableListSaveJsonMapper.ToJson).ToList(),
        };
    }

    public static StateSave FromJson(StateSaveJson dto)
    {
        StateSave result = new StateSave { Name = dto.Name };
        result.Variables = dto.Variables.Select(VariableSaveJsonMapper.FromJson).ToList();
        result.VariableLists = dto.VariableLists.Select(VariableListSaveJsonMapper.FromJson).ToList();
        return result;
    }

    public static StateSaveCategoryJson ToJson(StateSaveCategory source)
    {
        return new StateSaveCategoryJson
        {
            Name = source.Name,
            States = source.States.Select(ToJson).ToList(),
        };
    }

    public static StateSaveCategory FromJson(StateSaveCategoryJson dto)
    {
        StateSaveCategory result = new StateSaveCategory { Name = dto.Name };
        result.States = dto.States.Select(FromJson).ToList();
        return result;
    }
}
