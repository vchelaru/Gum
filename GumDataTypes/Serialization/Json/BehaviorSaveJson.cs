using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Behaviors;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>JSON-serializable shape of a <see cref="BehaviorSave"/>.</summary>
internal sealed class BehaviorSaveJson
{
    public string Name { get; set; } = "";
    public StateSaveJson RequiredVariables { get; set; } = new StateSaveJson();
    public List<VariableSaveJson> FormsProperties { get; set; } = new List<VariableSaveJson>();
    public List<string> ToolOnlyVariableReferences { get; set; } = new List<string>();
    public List<StateSaveCategoryJson> Categories { get; set; } = new List<StateSaveCategoryJson>();
    public List<BehaviorInstanceSaveJson> RequiredInstances { get; set; } = new List<BehaviorInstanceSaveJson>();
    public List<string> RequiredAnimations { get; set; } = new List<string>();
    public string? DefaultImplementation { get; set; }
}

internal static class BehaviorSaveJsonMapper
{
    public static BehaviorSaveJson ToJson(BehaviorSave source)
    {
        return new BehaviorSaveJson
        {
            Name = source.Name,
            RequiredVariables = StateSaveJsonMapper.ToJson(source.RequiredVariables),
            FormsProperties = source.FormsProperties.Select(VariableSaveJsonMapper.ToJson).ToList(),
            ToolOnlyVariableReferences = new List<string>(source.ToolOnlyVariableReferences),
            Categories = source.Categories.Select(StateSaveJsonMapper.ToJson).ToList(),
            RequiredInstances = source.RequiredInstances.Select(InstanceSaveJsonMapper.ToJson).ToList(),
            RequiredAnimations = new List<string>(source.RequiredAnimations),
            DefaultImplementation = source.DefaultImplementation,
        };
    }

    public static BehaviorSave FromJson(BehaviorSaveJson dto)
    {
        BehaviorSave result = new BehaviorSave
        {
            Name = dto.Name,
            RequiredVariables = StateSaveJsonMapper.FromJson(dto.RequiredVariables),
            DefaultImplementation = dto.DefaultImplementation,
        };
        result.FormsProperties = dto.FormsProperties.Select(VariableSaveJsonMapper.FromJson).ToList();
        result.ToolOnlyVariableReferences = new List<string>(dto.ToolOnlyVariableReferences);
        result.Categories = dto.Categories.Select(StateSaveJsonMapper.FromJson).ToList();
        result.RequiredInstances = dto.RequiredInstances.Select(InstanceSaveJsonMapper.FromJson).ToList();
        result.RequiredAnimations = new List<string>(dto.RequiredAnimations);
        return result;
    }
}
