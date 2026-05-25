using Raylib_cs;

namespace Gum.Renderables;

internal static class BlendModeExtensions
{
    public static BlendMode ToRaylibBlendMode(this global::Gum.RenderingLibrary.Blend blend)
    {
        // Unmapped values (Replace, ReplaceAlpha, MinAlpha, SubtractAlpha) fall through
        // to Alpha rather than guessing a wrong raylib mode: e.g. SubtractAlpha targets the
        // alpha channel for masking, which BlendMode.SubtractColors does not — a silent
        // visual mismatch is worse than no effect.
        return blend switch
        {
            global::Gum.RenderingLibrary.Blend.Additive => BlendMode.Additive,
            _ => BlendMode.Alpha,
        };
    }
}
