using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using ToolsUtilities;

namespace Gum.ProjectServices;

/// <summary>
/// Builds the XmlSerializers an XML project load uses, ahead of the load. Each one generates and
/// compiles its reader on first use, which otherwise happens while the tool waits for the project.
/// They are created through the same caches the load reads from, so the work is not repeated.
/// Safe on any thread.
/// </summary>
public class XmlProjectSerializerWarmup
{
    /// <summary>The root types of the project, element and behavior files.</summary>
    public IReadOnlyList<Type> RootTypes { get; } = new[]
    {
        typeof(GumProjectSave), typeof(ScreenSave), typeof(ComponentSave), typeof(StandardElementSave), typeof(BehaviorSave),
    };

    /// <summary>
    /// Creates the serializers for both file formats: the original one (project version 1) and the
    /// compact one (attribute version), and reads an empty document with each so its reader is
    /// compiled too. Returns the serializers it warmed.
    /// </summary>
    public IReadOnlyList<XmlSerializer> WarmUp()
    {
        List<XmlSerializer> serializers = new List<XmlSerializer>();
        foreach (Type type in RootTypes)
        {
            serializers.Add(Warm(FileManager.GetXmlSerializer(type), type));
            XmlSerializer compact = type == typeof(GumProjectSave)
                ? GumFileSerializer.GetGumProjectCompactSerializer()
                : GumFileSerializer.GetCompactSerializer(type);
            serializers.Add(Warm(compact, type));
        }
        return serializers;
    }

    private XmlSerializer Warm(XmlSerializer serializer, Type type)
    {
        using StringReader reader = new StringReader($"<{type.Name} />");
        serializer.Deserialize(reader);
        return serializer;
    }
}
