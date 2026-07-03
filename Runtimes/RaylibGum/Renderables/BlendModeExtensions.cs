using Raylib_cs;

namespace Gum.Renderables;

/// <summary>
/// Translates Gum's backend-agnostic <see cref="global::Gum.RenderingLibrary.Blend"/> values into
/// what raylib needs to reproduce them. <see cref="global::Gum.RenderingLibrary.Blend.Normal"/> and
/// <see cref="global::Gum.RenderingLibrary.Blend.Additive"/> have a direct raylib
/// <see cref="BlendMode"/> equivalent; every other value (<c>Replace</c>, <c>ReplaceAlpha</c>,
/// <c>SubtractAlpha</c>, <c>MinAlpha</c> — issue #3470) needs distinct per-channel blend
/// factors/equations that only <c>Rlgl.SetBlendFactorsSeparate</c> +
/// <see cref="BlendMode.CustomSeparate"/> can express.
///
/// <para>Rather than re-deriving those factors, this reuses
/// <see cref="global::Gum.RenderingLibrary.BlendExtensions.ToBlendState"/> — the same
/// backend-agnostic <see cref="global::Gum.BlendState"/> that MonoGame turns into XNA
/// <c>BlendState</c> objects — and maps its abstract <see cref="global::Gum.Blend"/> /
/// <see cref="global::Gum.BlendFunction"/> fields onto the equivalent GL constants. This keeps the
/// raylib result matching MonoGame's blend semantics exactly instead of approximating them.</para>
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
    /// <c>Rlgl.SetBlendFactorsSeparate</c> needs to reproduce it exactly (for the values
    /// <see cref="TryGetSimpleRaylibBlendMode"/> can't handle: <c>Replace</c>, <c>ReplaceAlpha</c>,
    /// <c>SubtractAlpha</c>, <c>MinAlpha</c>).
    /// </summary>
    public static (int srcRgb, int dstRgb, int srcAlpha, int dstAlpha, int eqRgb, int eqAlpha) ToGlBlendFactorsSeparate(
        this global::Gum.RenderingLibrary.Blend blend)
    {
        // raylib currently has no premultiplied-alpha texture handling, so false matches existing
        // raylib behavior (see Sprite/NineSlice, which never premultiply on load).
        global::Gum.BlendState blendState = global::Gum.RenderingLibrary.BlendExtensions.ToBlendState(
            blend, isUsingPremultipliedAlpha: false);

        return (
            ToGlBlendFactor(blendState.ColorSourceBlend),
            ToGlBlendFactor(blendState.ColorDestinationBlend),
            ToGlBlendFactor(blendState.AlphaSourceBlend),
            ToGlBlendFactor(blendState.AlphaDestinationBlend),
            ToGlBlendEquation(blendState.ColorBlendFunction),
            ToGlBlendEquation(blendState.AlphaBlendFunction));
    }

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
