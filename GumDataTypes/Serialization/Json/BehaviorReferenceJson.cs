using Gum.DataTypes.Behaviors;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>JSON-serializable shape of a <see cref="BehaviorReference"/>.</summary>
internal sealed class BehaviorReferenceJson
{
    public string Name { get; set; } = "";
    public string? SourcePath { get; set; }
    public string? DefaultImplementationOverride { get; set; }
}

internal static class BehaviorReferenceJsonMapper
{
    public static BehaviorReferenceJson ToJson(BehaviorReference source)
    {
        return new BehaviorReferenceJson
        {
            Name = source.Name,
            SourcePath = source.SourcePath,
            DefaultImplementationOverride = source.DefaultImplementationOverride,
        };
    }

    public static BehaviorReference FromJson(BehaviorReferenceJson dto)
    {
        return new BehaviorReference
        {
            Name = dto.Name,
            SourcePath = dto.SourcePath,
            DefaultImplementationOverride = dto.DefaultImplementationOverride,
        };
    }
}
