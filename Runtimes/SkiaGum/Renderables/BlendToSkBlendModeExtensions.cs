using SkiaSharp;

namespace SkiaGum.Renderables;

internal static class BlendToSkBlendModeExtensions
{
    // SubtractAlpha → DstOut cuts destination alpha by source alpha. MinAlpha → DstIn keeps the
    // destination scaled by source alpha (alpha = srcA * dstA, color scaled to match), the same
    // factors MonoGame and raylib use for MinAlpha on a baked render target
    // (BlendState.MinAlphaOnPremultipliedTarget). It equals min(srcA, dstA) wherever either is 0 or
    // 1, which covers the masking use (a mask sprite over an opaque layer).
    // ReplaceAlpha and Subtract have no SKBlendMode; use ApplyBlend, which gives them a custom blender.
    // The SrcOver returned here for them is only the fallback for a caller that reads the mode alone.
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

    // The result alpha is the source alpha and the destination color is scaled by it. This is the
    // same factor set MonoGame and raylib use for ReplaceAlpha on a baked render target
    // (BlendState.ReplaceAlphaOnPremultipliedTarget: color Zero/SourceAlpha, alpha One/Zero).
    // No SKBlendMode does this: DstIn also multiplies the destination alpha in.
    private const string ReplaceAlphaSksl = @"
half4 main(half4 src, half4 dst) {
    return half4(dst.rgb * src.a, src.a);
}";

    // One immutable blender shared by every paint; it is never disposed.
    private static SKBlender? _replaceAlphaBlender;
    private static bool _replaceAlphaBlenderFailed;

    /// <summary>
    /// Sets the blend on <paramref name="paint"/>: the matching <see cref="SKBlendMode"/>, plus a
    /// custom blender for <c>ReplaceAlpha</c> and <c>Subtract</c>.
    /// </summary>
    public static void ApplyBlend(this SKPaint paint, Gum.RenderingLibrary.Blend blend)
    {
        paint.BlendMode = blend.ToSKBlendMode();

        if (blend == Gum.RenderingLibrary.Blend.ReplaceAlpha)
        {
            paint.Blender = GetReplaceAlphaBlender();
        }
        else if (blend == Gum.RenderingLibrary.Blend.Subtract)
        {
            paint.Blender = GetSubtractBlender();
        }
    }

    // #8263: no SKBlendMode is dest - src clamped (Difference is |dest - src|). Skia's inputs are
    // premultiplied, so src.rgb is the straight color times its alpha, matching MonoGame's
    // SourceAlpha/One/ReverseSubtract. Keeping dst.a leaves the result validly premultiplied.
    // A runtime blender costs more per pixel than a fixed SKBlendMode.
    private const string SubtractSksl = @"
half4 main(half4 src, half4 dst) {
    return half4(max(dst.rgb - src.rgb, 0), dst.a);
}";

    private static SKBlender? _subtractBlender;
    private static bool _subtractBlenderFailed;

    private static SKBlender? GetSubtractBlender()
    {
        if (_subtractBlender == null && !_subtractBlenderFailed)
        {
            using SKRuntimeEffect? effect = SKRuntimeEffect.CreateBlender(SubtractSksl, out string _);
            _subtractBlender = effect?.ToBlender();
            _subtractBlenderFailed = _subtractBlender == null;
        }

        return _subtractBlender;
    }

    private static SKBlender? GetReplaceAlphaBlender()
    {
        if (_replaceAlphaBlender == null && !_replaceAlphaBlenderFailed)
        {
            using SKRuntimeEffect? effect = SKRuntimeEffect.CreateBlender(ReplaceAlphaSksl, out string _);
            _replaceAlphaBlender = effect?.ToBlender();
            _replaceAlphaBlenderFailed = _replaceAlphaBlender == null;
        }

        return _replaceAlphaBlender;
    }
}
