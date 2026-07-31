namespace Gum.DataTypes.Serialization.Json;

/// <summary>JSON-serializable shape of an <see cref="ElementReference"/>.</summary>
internal sealed class ElementReferenceJson
{
    public string Name { get; set; } = "";
    public ElementType ElementType { get; set; }
    public LinkType LinkType { get; set; }
    public string? Link { get; set; }
}

internal static class ElementReferenceJsonMapper
{
    public static ElementReferenceJson ToJson(ElementReference source)
    {
        return new ElementReferenceJson
        {
            Name = source.Name,
            ElementType = source.ElementType,
            LinkType = source.LinkType,
            Link = source.Link,
        };
    }

    public static ElementReference FromJson(ElementReferenceJson dto)
    {
        return new ElementReference
        {
            Name = dto.Name,
            ElementType = dto.ElementType,
            LinkType = dto.LinkType,
            Link = dto.Link,
        };
    }
}
