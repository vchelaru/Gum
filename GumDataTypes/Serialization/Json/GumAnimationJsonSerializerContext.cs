using System.Text.Json.Serialization;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the <c>.ganj</c> JSON animation format.
/// Kept separate from <see cref="GumJsonSerializerContext"/> because animation save classes are not
/// part of the FRB1-shared source surface (see <see cref="ElementAnimationsSaveJson"/>).
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ElementAnimationsSaveJson))]
internal partial class GumAnimationJsonSerializerContext : JsonSerializerContext
{
}
