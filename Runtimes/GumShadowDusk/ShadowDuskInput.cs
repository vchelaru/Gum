using System.Collections.Generic;
using System.Linq;
using ShadowDusk.Core;

namespace Gum;

/// <summary>
/// What every ShadowDusk package does with a failed conversion. Shared by the MonoGame, KNI, FNA, raylib and
/// SkiaSharp packages (the raylib and Skia projects link this file).
/// </summary>
internal static class ShadowDuskInput
{
    /// <summary>
    /// Whether ShadowDusk attempted nothing because the input is not a shader format it reads, so the caller may
    /// fall through to the next resolver. Any other failure is a real error in a recognized shader.
    /// </summary>
    public static bool IsUnsupported(IEnumerable<ShaderError> errors) =>
        errors.Any(error => error.Kind == ShaderErrorKind.UnsupportedInput);

    public static string FormatErrors(IEnumerable<ShaderError> errors)
    {
        ShaderError[] list = errors.ToArray();
        string text = string.Join("\n", list.Select(
            error => $"{error.File}({error.Line},{error.Column}): {error.Code}: {error.Message}"));
        return list.Any(IsSkslError) ? text + "\n" + SkslHint : text;
    }

    // SD0660 to SD0673 are the codes of ShadowDusk's SkSL input frontend.
    private static bool IsSkslError(ShaderError error) =>
        string.CompareOrdinal(error.Code, "SD0660") >= 0 && string.CompareOrdinal(error.Code, "SD0673") <= 0;

    /// <summary>What an author of a rejected .sksl file needs to know, appended to the codes and lines ShadowDusk reports.</summary>
    internal const string SkslHint =
        "Gum reads SkSL shaders of the form 'uniform shader SpriteTexture; half4 main(float2 coord)'. " +
        "sk_FragCoord, layout(color), color filters and blenders are not supported on this runtime.";
}
