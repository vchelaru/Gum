using System.Text.Json;
using Gum.StateAnimation.SaveClasses;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// Entry point for reading and writing <see cref="ElementAnimationsSave"/> as JSON (<c>.ganj</c>).
/// See <see cref="GumJsonFileSerializer"/> for the <c>.gumj</c>/<c>.gusj</c>/<c>.gucj</c>/<c>.gutj</c>/
/// <c>.behj</c> equivalent.
/// </summary>
public static class GumAnimationJsonFileSerializer
{
    /// <summary>Serializes an <see cref="ElementAnimationsSave"/> to its JSON (<c>.ganj</c>) text representation.</summary>
    public static string SerializeElementAnimations(ElementAnimationsSave animations)
    {
        ElementAnimationsSaveJson dto = ElementAnimationsSaveJsonMapper.ToJson(animations);
        return JsonSerializer.Serialize(dto, GumAnimationJsonSerializerContext.Default.ElementAnimationsSaveJson);
    }

    /// <summary>Deserializes an <see cref="ElementAnimationsSave"/> from JSON (<c>.ganj</c>) text.</summary>
    public static ElementAnimationsSave DeserializeElementAnimations(string json)
    {
        ElementAnimationsSaveJson dto = JsonSerializer.Deserialize(json, GumAnimationJsonSerializerContext.Default.ElementAnimationsSaveJson)!;
        return ElementAnimationsSaveJsonMapper.FromJson(dto);
    }
}
