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

    public static string FormatErrors(IEnumerable<ShaderError> errors) =>
        string.Join("\n", errors.Select(
            error => $"{error.File}({error.Line},{error.Column}): {error.Code}: {error.Message}"));
}
