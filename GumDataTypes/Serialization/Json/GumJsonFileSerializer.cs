using System.IO;
using System.Text;
using System.Text.Json;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// Entry point for reading and writing <see cref="GumProjectSave"/>, any <see cref="ElementSave"/>
/// subtype, and <see cref="Behaviors.BehaviorSave"/> as JSON (<c>.gumj</c>/<c>.gusj</c>/<c>.gucj</c>/
/// <c>.gutj</c>/<c>.behj</c>). Backed entirely by <see cref="GumJsonSerializerContext"/> (source
/// generation), so — unlike <see cref="ToolsUtilities.FileManager.GetXmlSerializer"/> — none of this
/// relies on runtime reflection and is safe under Native AOT.
/// </summary>
public static class GumJsonFileSerializer
{
    /// <summary>Serializes a <see cref="GumProjectSave"/> to its JSON (<c>.gumj</c>) text representation.</summary>
    public static string SerializeProject(GumProjectSave project)
    {
        GumProjectSaveJson dto = GumProjectSaveJsonMapper.ToJson(project);
        return JsonSerializer.Serialize(dto, GumJsonSerializerContext.Default.GumProjectSaveJson);
    }

    /// <summary>Deserializes a <see cref="GumProjectSave"/> from JSON (<c>.gumj</c>) text.</summary>
    public static GumProjectSave DeserializeProject(string json)
    {
        GumProjectSaveJson dto = JsonSerializer.Deserialize(json, GumJsonSerializerContext.Default.GumProjectSaveJson)!;
        return GumProjectSaveJsonMapper.FromJson(dto);
    }

    /// <summary>
    /// Serializes an <see cref="ElementSave"/> (a <see cref="ScreenSave"/>, <see cref="ComponentSave"/>,
    /// or <see cref="StandardElementSave"/>) to its JSON (<c>.gusj</c>/<c>.gucj</c>/<c>.gutj</c>) text representation.
    /// </summary>
    public static string SerializeElement(ElementSave element)
    {
        ElementSaveJson dto = ElementSaveJsonMapper.ToJson(element);
        return JsonSerializer.Serialize(dto, GumJsonSerializerContext.Default.ElementSaveJson);
    }

    /// <summary>
    /// Deserializes an <see cref="ElementSave"/> subtype from JSON
    /// (<c>.gusj</c>/<c>.gucj</c>/<c>.gutj</c>) text.
    /// </summary>
    public static T DeserializeElement<T>(string json) where T : ElementSave, new()
    {
        ElementSaveJson dto = JsonSerializer.Deserialize(json, GumJsonSerializerContext.Default.ElementSaveJson)!;
        return ElementSaveJsonMapper.FromJson<T>(dto);
    }

    /// <summary>Serializes a <see cref="Behaviors.BehaviorSave"/> to its JSON (<c>.behj</c>) text representation.</summary>
    public static string SerializeBehavior(Behaviors.BehaviorSave behavior)
    {
        BehaviorSaveJson dto = BehaviorSaveJsonMapper.ToJson(behavior);
        return JsonSerializer.Serialize(dto, GumJsonSerializerContext.Default.BehaviorSaveJson);
    }

    /// <summary>Deserializes a <see cref="Behaviors.BehaviorSave"/> from JSON (<c>.behj</c>) text.</summary>
    public static Behaviors.BehaviorSave DeserializeBehavior(string json)
    {
        BehaviorSaveJson dto = JsonSerializer.Deserialize(json, GumJsonSerializerContext.Default.BehaviorSaveJson)!;
        return BehaviorSaveJsonMapper.FromJson(dto);
    }

    /// <summary>
    /// Writes <paramref name="json"/> to <paramref name="fileName"/>, creating the containing directory
    /// if needed and using a BOM-less UTF8 encoding — matching <see cref="ToolsUtilities.FileManager.XmlSerialize(object, string, System.Xml.Serialization.XmlSerializer)"/>'s
    /// on-disk conventions so <c>.gumj</c>/<c>.gusj</c>/<c>.gucj</c>/<c>.gutj</c>/<c>.behj</c> files
    /// look the same on disk as their XML counterparts.
    /// </summary>
    public static void WriteToFile(string fileName, string json)
    {
        string directory = Path.GetDirectoryName(fileName) ?? string.Empty;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fileName, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
