using Raylib_cs;

namespace Gum.Renderables;

/// <summary>
/// Translates Gum's backend-agnostic <see cref="global::Gum.RenderingLibrary.Blend"/> values into
/// what raylib needs to reproduce them. <see cref="global::Gum.RenderingLibrary.Blend.Normal"/> and
/// <see cref="global::Gum.RenderingLibrary.Blend.Additive"/> have a direct raylib
/// <see cref="BlendMode"/> equivalent; every other value (<c>Replace</c>, <c>ReplaceAlpha</c>,
/// <c>SubtractAlpha</c>, <c>MinAlpha</c> — issue #3470) needs distinct per-channel blend
/// factors/equations that only <c>Rlgl.SetBlendFactorsSeparate</c> + <see cref="BlendMode.CustomSeparate"/>
/// can express.
///
/// <para><b>Why the color factors are NOT simply reused from <c>BlendExtensions.ToBlendState</c>:</b>
/// that shared, backend-agnostic <see cref="global::Gum.BlendState"/> model was designed for
/// MonoGame, which stores a baked render target as plain straight (non-premultiplied) color+alpha
/// and re-multiplies by alpha at composite time (<c>Renderer.NormalBlendState</c> defaults to
/// <c>BlendState.NonPremultiplied</c>) — so a blend that intentionally leaves color untouched while
/// changing alpha (the whole point of the "alpha-only" family) is safe there. raylib's render-target
/// bake pipeline instead <b>stores an already-premultiplied result</b> and composites it back with
/// <c>BlendMode.AlphaPremultiply</c> (see <c>Renderer.BakeRenderTarget</c> /
/// <c>TryCompositeRenderTarget</c>, issue #3434) — so any draw inside a bake must leave a validly
/// premultiplied pixel (color proportional to its own alpha), or leftover full-intensity color bleeds
/// through at composite time even where alpha reads near zero (issue #3470 follow-up: SubtractAlpha
/// showed magenta instead of a transparent hole). The alpha factors are always reused as-is from
/// <c>ToBlendState</c> (alpha has no premultiplication concept). The color factors only switch to
/// raylib's own premultiplied-consistent derivation while a bake is actually active — outside a bake
/// (e.g. a plain Sprite drawn straight to the screen) there is nothing further compositing the
/// result, so <c>ToBlendState</c>'s "ignore alpha" factors are used directly instead, matching
/// MonoGame's own visual (a translucent <c>Replace</c> sprite should still render at full brightness
/// there, not dimmed by its own alpha).</para>
/// </summary>
internal static class BlendModeExtensions
{
    // GL blend-factor / equation constants (OpenGL / OpenGL ES spec, not raylib-specific).
    private const int GlZero = 0;
    private const int GlOne = 1;
    private const int GlSrcColor = 0x0300;
    private const int GlOneMinusSrcColor = 0x0301;
    private const int GlSrcAlpha = 0x0302;
    private const int GlOneMinusSrcAlpha = 0x0303;
    private const int GlDstAlpha = 0x0304;
    private const int GlOneMinusDstAlpha = 0x0305;
    private const int GlDstColor = 0x0306;
    private const int GlOneMinusDstColor = 0x0307;
    private const int GlSrcAlphaSaturate = 0x0308;
    private const int GlConstantColor = 0x8001;
    private const int GlOneMinusConstantColor = 0x8002;

    private const int GlFuncAdd = 0x8006;
    private const int GlMin = 0x8007;
    private const int GlMax = 0x8008;
    private const int GlFuncSubtract = 0x800A;
    private const int GlFuncReverseSubtract = 0x800B;

    /// <summary>
    /// Attempts to map <paramref name="blend"/> onto one of raylib's built-in canned
    /// <see cref="BlendMode"/> values. Only <c>Normal</c> and <c>Additive</c> have one; every other
    /// value returns <c>false</c> and must go through <see cref="ToGlBlendFactorsSeparate"/> instead.
    /// </summary>
    public static bool TryGetSimpleRaylibBlendMode(this global::Gum.RenderingLibrary.Blend blend, out BlendMode mode)
    {
        switch (blend)
        {
            case global::Gum.RenderingLibrary.Blend.Additive:
                mode = BlendMode.Additive;
                return true;
            case global::Gum.RenderingLibrary.Blend.Normal:
                mode = BlendMode.Alpha;
                return true;
            default:
                mode = BlendMode.Alpha;
                return false;
        }
    }

    /// <summary>
    /// Converts <paramref name="blend"/> into the six GL parameters
    /// <c>Rlgl.SetBlendFactorsSeparate</c> needs to reproduce it (for the values
    /// <see cref="TryGetSimpleRaylibBlendMode"/> can't handle: <c>Replace</c>, <c>ReplaceAlpha</c>,
    /// <c>SubtractAlpha</c>, <c>MinAlpha</c>). The alpha factors always come from the shared
    /// <c>BlendExtensions.ToBlendState</c> model (alpha has no premultiplication concept). The color
    /// factors depend on <paramref name="isPremultiplyingContext"/> — see the class remarks: inside a
    /// render-target bake they use raylib's own premultiplied-consistent derivation; outside one they
    /// reuse <c>ToBlendState</c>'s factors directly, matching MonoGame's "ignore alpha" semantics.
    /// </summary>
    public static (int srcRgb, int dstRgb, int srcAlpha, int dstAlpha, int eqRgb, int eqAlpha) ToGlBlendFactorsSeparate(
        this global::Gum.RenderingLibrary.Blend blend, bool isPremultiplyingContext)
    {
        // raylib currently has no premultiplied-alpha texture handling, so false matches existing
        // raylib behavior (see Sprite/NineSlice, which never premultiply on load).
        global::Gum.BlendState blendState = global::Gum.RenderingLibrary.BlendExtensions.ToBlendState(
            blend, isUsingPremultipliedAlpha: false);

        (int colorSrc, int colorDst, int colorFunc) = isPremultiplyingContext
            ? ToPremultipliedConsistentColorFactors(blend)
            : (ToGlBlendFactor(blendState.ColorSourceBlend), ToGlBlendFactor(blendState.ColorDestinationBlend),
                ToGlBlendEquation(blendState.ColorBlendFunction));

        return (
            colorSrc,
            colorDst,
            ToGlBlendFactor(blendState.AlphaSourceBlend),
            ToGlBlendFactor(blendState.AlphaDestinationBlend),
            colorFunc,
            ToGlBlendEquation(blendState.AlphaBlendFunction));
    }

    // Raylib-specific color factors that keep a bake's premultiplied-storage invariant intact — see
    // the class remarks. Each scales color down in lockstep with however that mode changes alpha,
    // rather than reusing ToBlendState's MonoGame-oriented "leave color untouched" factors. Every
    // case (Add) is a plain scale-and-keep, never a subtraction, so the equation is always Add.
    //
    // Known limitation: ReplaceAlpha/MinAlpha's color scale (Dst*SrcAlpha) is only exactly correct
    // when the destination was already fully opaque going in — fixed-function blending can't divide
    // by the destination's own current alpha to generalize further. Replace and SubtractAlpha have no
    // such caveat (Replace discards the destination outright; SubtractAlpha's complementary factor
    // falls out of the same subtraction driving its alpha). This matches every documented use of
    // these blends (masking over an opaque backing layer), so the common case renders exactly right
    // and the edge case (masking onto an already-translucent layer) is a minor intensity error, not a
    // wrong-color leak.
    private static (int colorSrc, int colorDst, int colorFunc) ToPremultipliedConsistentColorFactors(global::Gum.RenderingLibrary.Blend blend) => blend switch
    {
        // Fully replaces the destination, so premultiply the incoming color by its own alpha
        // (Src*SrcAlpha) rather than writing it raw (Src*One) — fully general, no destination
        // assumption needed since Replace discards the destination outright.
        global::Gum.RenderingLibrary.Blend.Replace => (GlSrcAlpha, GlZero, GlFuncAdd),
        // Alpha is overwritten with SrcAlpha; scale destination color by that same SrcAlpha so
        // color and alpha shrink together.
        global::Gum.RenderingLibrary.Blend.ReplaceAlpha => (GlZero, GlSrcAlpha, GlFuncAdd),
        global::Gum.RenderingLibrary.Blend.MinAlpha => (GlZero, GlSrcAlpha, GlFuncAdd),
        // Alpha is reverse-subtracted by SrcAlpha (Dst - Src); scale destination color by the
        // complementary (1 - SrcAlpha) so it shrinks toward zero exactly as alpha does.
        global::Gum.RenderingLibrary.Blend.SubtractAlpha => (GlZero, GlOneMinusSrcAlpha, GlFuncAdd),
        _ => throw new System.ArgumentOutOfRangeException(nameof(blend), blend, message: null),
    };

    private static int ToGlBlendFactor(global::Gum.Blend blend) => blend switch
    {
        global::Gum.Blend.Zero => GlZero,
        global::Gum.Blend.One => GlOne,
        global::Gum.Blend.SourceColor => GlSrcColor,
        global::Gum.Blend.InverseSourceColor => GlOneMinusSrcColor,
        global::Gum.Blend.SourceAlpha => GlSrcAlpha,
        global::Gum.Blend.InverseSourceAlpha => GlOneMinusSrcAlpha,
        global::Gum.Blend.DestinationAlpha => GlDstAlpha,
        global::Gum.Blend.InverseDestinationAlpha => GlOneMinusDstAlpha,
        global::Gum.Blend.DestinationColor => GlDstColor,
        global::Gum.Blend.InverseDestinationColor => GlOneMinusDstColor,
        global::Gum.Blend.SourceAlphaSaturation => GlSrcAlphaSaturate,
        global::Gum.Blend.BlendFactor => GlConstantColor,
        global::Gum.Blend.InverseBlendFactor => GlOneMinusConstantColor,
        _ => throw new System.ArgumentOutOfRangeException(nameof(blend), blend, message: null),
    };

    private static int ToGlBlendEquation(global::Gum.BlendFunction function) => function switch
    {
        global::Gum.BlendFunction.Add => GlFuncAdd,
        global::Gum.BlendFunction.Subtract => GlFuncSubtract,
        global::Gum.BlendFunction.ReverseSubtract => GlFuncReverseSubtract,
        global::Gum.BlendFunction.Min => GlMin,
        global::Gum.BlendFunction.Max => GlMax,
        _ => throw new System.ArgumentOutOfRangeException(nameof(function), function, message: null),
    };
}
