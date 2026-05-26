using SkiaSharp;

namespace SkiaGum.Renderables;

internal static class BlendToSkBlendModeExtensions
{
    // Replace, ReplaceAlpha, and MinAlpha don't have a clean SkiaSharp equivalent; falling
    // through to SrcOver matches the precedent set by the Raylib mapping (see
    // Runtimes/RaylibGum/Renderables/BlendModeExtensions.cs) — silently picking the wrong
    // mode would change visuals without an obvious cause. SubtractAlpha → DstOut is the one
    // alpha-channel variant Skia exposes directly (cuts destination alpha by source alpha).
    public static SKBlendMode ToSKBlendMode(this Gum.RenderingLibrary.Blend blend)
    {
        return blend switch
        {
            Gum.RenderingLibrary.Blend.Additive => SKBlendMode.Plus,
            Gum.RenderingLibrary.Blend.Replace => SKBlendMode.Src,
            Gum.RenderingLibrary.Blend.SubtractAlpha => SKBlendMode.DstOut,
            _ => SKBlendMode.SrcOver,
        };
    }
}
