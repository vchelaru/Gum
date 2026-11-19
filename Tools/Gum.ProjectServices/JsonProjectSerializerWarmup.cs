using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using Gum.DataTypes.Serialization.Json;

namespace Gum.ProjectServices;

/// <summary>
/// Builds the JSON project format's serializer metadata ahead of the first load. The first read of a
/// <c>.gumj</c> project otherwise spends a couple hundred milliseconds configuring the metadata of
/// every DTO type, while the tool waits for the load. Safe on any thread.
/// </summary>
public class JsonProjectSerializerWarmup
{
    /// <summary>The DTO types the project, element and behavior files are read as.</summary>
    public IReadOnlyList<Type> RootTypes { get; } = new[]
    {
        typeof(GumProjectSaveJson), typeof(ElementSaveJson), typeof(BehaviorSaveJson),
    };

    /// <summary>
    /// Configures the metadata of every type reachable from <see cref="RootTypes"/> and returns the
    /// types it configured.
    /// </summary>
    public IReadOnlyCollection<Type> WarmUp()
    {
        HashSet<Type> visited = new HashSet<Type>();
        Stack<Type> pending = new Stack<Type>(RootTypes);
        while (pending.Count > 0)
        {
            Type type = pending.Pop();
            if (!visited.Add(type))
            {
                continue;
            }

            // GetTypeInfo returns configured metadata, which is the work the first read would do.
            JsonTypeInfo info = GumJsonSerializerContext.Default.Options.GetTypeInfo(type);
            foreach (JsonPropertyInfo property in info.Properties)
            {
                pending.Push(property.PropertyType);
            }
            if (info.ElementType is { } elementType)
            {
                pending.Push(elementType);
            }
            if (info.KeyType is { } keyType)
            {
                pending.Push(keyType);
            }
        }
        return visited;
    }
}
