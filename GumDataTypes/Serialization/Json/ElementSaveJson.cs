using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Behaviors;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>JSON-serializable shape of an <see cref="EventSave"/>.</summary>
internal sealed class EventSaveJson
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string? ExposedAsName { get; set; }
}

/// <summary>JSON-serializable shape of an <see cref="ElementBehaviorReference"/>.</summary>
internal sealed class ElementBehaviorReferenceJson
{
    public string? ProjectName { get; set; }
    public string? BehaviorName { get; set; }
}

/// <summary>
/// JSON-serializable shape shared by <see cref="ScreenSave"/>, <see cref="ComponentSave"/>, and
/// <see cref="StandardElementSave"/> — all three share <see cref="ElementSave"/>'s full serialized
/// surface and differ only in fixed, non-serialized metadata (subfolder, file extension).
/// </summary>
internal sealed class ElementSaveJson
{
    public string Name { get; set; } = "";
    public string? BaseType { get; set; }
    public List<StateSaveJson> States { get; set; } = new List<StateSaveJson>();
    public List<StateSaveCategoryJson> Categories { get; set; } = new List<StateSaveCategoryJson>();
    public List<InstanceSaveJson> Instances { get; set; } = new List<InstanceSaveJson>();
    public List<EventSaveJson> Events { get; set; } = new List<EventSaveJson>();
    public List<ElementBehaviorReferenceJson> Behaviors { get; set; } = new List<ElementBehaviorReferenceJson>();
    public List<string> VariablesHiddenFromInstances { get; set; } = new List<string>();
}

internal static class ElementSaveJsonMapper
{
    public static ElementSaveJson ToJson(ElementSave source)
    {
        return new ElementSaveJson
        {
            Name = source.Name,
            BaseType = source.BaseType,
            States = source.States.Select(StateSaveJsonMapper.ToJson).ToList(),
            Categories = source.Categories.Select(StateSaveJsonMapper.ToJson).ToList(),
            Instances = source.Instances.Select(InstanceSaveJsonMapper.ToJson).ToList(),
            Events = source.Events.Select(EventSaveJsonMapper.ToJson).ToList(),
            Behaviors = source.Behaviors.Select(ElementBehaviorReferenceJsonMapper.ToJson).ToList(),
            VariablesHiddenFromInstances = new List<string>(source.VariablesHiddenFromInstances ?? new List<string>()),
        };
    }

    public static T FromJson<T>(ElementSaveJson dto) where T : ElementSave, new()
    {
        T result = new T
        {
            Name = dto.Name,
            BaseType = dto.BaseType,
        };
        result.States = dto.States.Select(StateSaveJsonMapper.FromJson).ToList();
        result.Categories = dto.Categories.Select(StateSaveJsonMapper.FromJson).ToList();
        result.Instances = dto.Instances.Select(InstanceSaveJsonMapper.FromJson).ToList();
        result.Events = dto.Events.Select(EventSaveJsonMapper.FromJson).ToList();
        result.Behaviors = dto.Behaviors.Select(ElementBehaviorReferenceJsonMapper.FromJson).ToList();
        result.VariablesHiddenFromInstances = new List<string>(dto.VariablesHiddenFromInstances);
        return result;
    }
}

internal static class EventSaveJsonMapper
{
    public static EventSaveJson ToJson(EventSave source)
    {
        return new EventSaveJson
        {
            Name = source.Name,
            Enabled = source.Enabled,
            ExposedAsName = source.ExposedAsName,
        };
    }

    public static EventSave FromJson(EventSaveJson dto)
    {
        return new EventSave
        {
            Name = dto.Name,
            Enabled = dto.Enabled,
            ExposedAsName = dto.ExposedAsName,
        };
    }
}

internal static class ElementBehaviorReferenceJsonMapper
{
    public static ElementBehaviorReferenceJson ToJson(ElementBehaviorReference source)
    {
        return new ElementBehaviorReferenceJson
        {
            ProjectName = source.ProjectName,
            BehaviorName = source.BehaviorName,
        };
    }

    public static ElementBehaviorReference FromJson(ElementBehaviorReferenceJson dto)
    {
        return new ElementBehaviorReference
        {
            ProjectName = dto.ProjectName,
            BehaviorName = dto.BehaviorName,
        };
    }
}
