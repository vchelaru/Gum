using System.Text.Json.Serialization;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the <c>.gumj</c>/<c>.gusj</c>/<c>.gucj</c>/
/// <c>.gutj</c>/<c>.behj</c> JSON project format. Generated at compile time — no runtime reflection —
/// so loading a project through this context is safe under Native AOT + trimming, unlike
/// <see cref="System.Xml.Serialization.XmlSerializer"/>.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(GumProjectSaveJson))]
[JsonSerializable(typeof(ElementSaveJson))]
[JsonSerializable(typeof(BehaviorSaveJson))]
internal partial class GumJsonSerializerContext : JsonSerializerContext
{
}
