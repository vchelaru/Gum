using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Gum.DataTypes.Behaviors;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>JSON-serializable shape of an <see cref="EventSave"/>.</summary>
internal sealed class EventSaveJson
{
    public string Name { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Enabled { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ExposedAsName { get; set; }
}

/// <summary>JSON-serializable shape of an <see cref="ElementBehaviorReference"/>.</summary>
internal sealed class ElementBehaviorReferenceJson
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? ProjectName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? BaseType { get; set; }
    private List<StateSaveJson> _states = new List<StateSaveJson>();
    public List<StateSaveJson> States { get => _states; set => _states = value ?? throw JsonNullCollection.For(nameof(States)); }
    private List<StateSaveCategoryJson> _categories = new List<StateSaveCategoryJson>();
    public List<StateSaveCategoryJson> Categories { get => _categories; set => _categories = value ?? throw JsonNullCollection.For(nameof(Categories)); }
    private List<InstanceSaveJson> _instances = new List<InstanceSaveJson>();
    public List<InstanceSaveJson> Instances { get => _instances; set => _instances = value ?? throw JsonNullCollection.For(nameof(Instances)); }
    private List<EventSaveJson> _events = new List<EventSaveJson>();
    public List<EventSaveJson> Events { get => _events; set => _events = value ?? throw JsonNullCollection.For(nameof(Events)); }
    private List<ElementBehaviorReferenceJson> _behaviors = new List<ElementBehaviorReferenceJson>();
    public List<ElementBehaviorReferenceJson> Behaviors { get => _behaviors; set => _behaviors = value ?? throw JsonNullCollection.For(nameof(Behaviors)); }
    private List<string> _variablesHiddenFromInstances = new List<string>();
    public List<string> VariablesHiddenFromInstances { get => _variablesHiddenFromInstances; set => _variablesHiddenFromInstances = value ?? throw JsonNullCollection.For(nameof(VariablesHiddenFromInstances)); }
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

/// <summary>
/// The error for a list property written as an explicit JSON <c>null</c>. Every list property of the JSON shapes
/// throws this from its setter, so a hand-written file gets the property name instead of a LINQ argument message.
/// </summary>
internal static class JsonNullCollection
{
    public static ArgumentNullException For(string propertyName) =>
        new ArgumentNullException(propertyName, $"\"{propertyName}\" is null. Write an empty list ([]) or leave the property out.");
}
