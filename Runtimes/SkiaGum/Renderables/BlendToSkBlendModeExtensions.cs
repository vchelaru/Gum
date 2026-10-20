using SkiaSharp;

namespace SkiaGum.Renderables;

internal static class BlendToSkBlendModeExtensions
{
    // ReplaceAlpha doesn't have a clean SkiaSharp equivalent; falling through to SrcOver matches
    // the precedent set by the Raylib mapping (see Runtimes/RaylibGum/Renderables/BlendModeExtensions.cs)
    // — silently picking the wrong mode would change visuals without an obvious cause.
    // SubtractAlpha → DstOut cuts destination alpha by source alpha. MinAlpha → DstIn keeps the
    // destination scaled by source alpha (alpha = srcA * dstA, color scaled to match), the same
    // factors MonoGame and raylib use for MinAlpha on a baked render target
    // (BlendState.MinAlphaOnPremultipliedTarget). It equals min(srcA, dstA) wherever either is 0 or
    // 1, which covers the masking use (a mask sprite over an opaque layer).
    public static SKBlendMode ToSKBlendMode(this Gum.RenderingLibrary.Blend blend)
    {
        return blend switch
        {
            Gum.RenderingLibrary.Blend.Additive => SKBlendMode.Plus,
            Gum.RenderingLibrary.Blend.Replace => SKBlendMode.Src,
            Gum.RenderingLibrary.Blend.SubtractAlpha => SKBlendMode.DstOut,
            Gum.RenderingLibrary.Blend.MinAlpha => SKBlendMode.DstIn,
            _ => SKBlendMode.SrcOver,
        };
    }
}
