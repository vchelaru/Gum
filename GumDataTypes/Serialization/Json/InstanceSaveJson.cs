using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Behaviors;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>JSON-serializable shape of an <see cref="InstanceSave"/>.</summary>
internal class InstanceSaveJson
{
    public string Name { get; set; } = "";
    public string BaseType { get; set; } = "";
    public bool DefinedByBase { get; set; }
    public bool Locked { get; set; }
    public bool IsSlot { get; set; }
}

/// <summary>JSON-serializable shape of a <see cref="BehaviorInstanceSave"/>.</summary>
internal sealed class BehaviorInstanceSaveJson : InstanceSaveJson
{
    public List<BehaviorReferenceJson> Behaviors { get; set; } = new List<BehaviorReferenceJson>();
}

internal static class InstanceSaveJsonMapper
{
    public static InstanceSaveJson ToJson(InstanceSave source)
    {
        return new InstanceSaveJson
        {
            Name = source.Name,
            BaseType = source.BaseType,
            DefinedByBase = source.DefinedByBase,
            Locked = source.Locked,
            IsSlot = source.IsSlot,
        };
    }

    public static InstanceSave FromJson(InstanceSaveJson dto)
    {
        return new InstanceSave
        {
            Name = dto.Name,
            BaseType = dto.BaseType,
            DefinedByBase = dto.DefinedByBase,
            Locked = dto.Locked,
            IsSlot = dto.IsSlot,
        };
    }

    public static BehaviorInstanceSaveJson ToJson(BehaviorInstanceSave source)
    {
        return new BehaviorInstanceSaveJson
        {
            Name = source.Name,
            BaseType = source.BaseType,
            DefinedByBase = source.DefinedByBase,
            Locked = source.Locked,
            IsSlot = source.IsSlot,
            Behaviors = source.Behaviors.Select(BehaviorReferenceJsonMapper.ToJson).ToList(),
        };
    }

    public static BehaviorInstanceSave FromJson(BehaviorInstanceSaveJson dto)
    {
        BehaviorInstanceSave result = new BehaviorInstanceSave
        {
            Name = dto.Name,
            BaseType = dto.BaseType,
            DefinedByBase = dto.DefinedByBase,
            Locked = dto.Locked,
            IsSlot = dto.IsSlot,
        };
        result.Behaviors = dto.Behaviors.Select(BehaviorReferenceJsonMapper.FromJson).ToList();
        return result;
    }
}
