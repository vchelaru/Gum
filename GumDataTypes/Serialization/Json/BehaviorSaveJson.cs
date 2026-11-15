using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Gum.DataTypes.Behaviors;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>JSON-serializable shape of a <see cref="BehaviorSave"/>.</summary>
internal sealed class BehaviorSaveJson
{
    public string Name { get; set; } = "";
    public StateSaveJson RequiredVariables { get; set; } = new StateSaveJson();
    private List<VariableSaveJson> _formsProperties = new List<VariableSaveJson>();
    public List<VariableSaveJson> FormsProperties { get => _formsProperties; set => _formsProperties = value ?? throw JsonNullCollection.For(nameof(FormsProperties)); }
    private List<string> _toolOnlyVariableReferences = new List<string>();
    public List<string> ToolOnlyVariableReferences { get => _toolOnlyVariableReferences; set => _toolOnlyVariableReferences = value ?? throw JsonNullCollection.For(nameof(ToolOnlyVariableReferences)); }
    private List<StateSaveCategoryJson> _categories = new List<StateSaveCategoryJson>();
    public List<StateSaveCategoryJson> Categories { get => _categories; set => _categories = value ?? throw JsonNullCollection.For(nameof(Categories)); }
    private List<BehaviorInstanceSaveJson> _requiredInstances = new List<BehaviorInstanceSaveJson>();
    public List<BehaviorInstanceSaveJson> RequiredInstances { get => _requiredInstances; set => _requiredInstances = value ?? throw JsonNullCollection.For(nameof(RequiredInstances)); }
    private List<string> _requiredAnimations = new List<string>();
    public List<string> RequiredAnimations { get => _requiredAnimations; set => _requiredAnimations = value ?? throw JsonNullCollection.For(nameof(RequiredAnimations)); }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
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
