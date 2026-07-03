using System;
using System.Numerics;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using static Raylib_cs.Raylib;

namespace Gum.Renderables;

/// <summary>
/// Render-to-texture + separable Gaussian blur replacement for the band-stack shadow
/// approximation in <see cref="LineRectangle"/> / <see cref="LineCircle"/> / <see cref="LineArc"/>.
/// Issue #2865.
///
/// <para><b>Algorithm:</b> paint the shape's silhouette into an offscreen render texture, run
/// a two-pass separable Gaussian (horizontal then vertical), composite the blurred mask
/// modulated by the shadow tint. This matches what Skia's <c>SKImageFilter.CreateDropShadow</c>
/// does internally. The prior 32-concentric-band approach inverted source-over compositing
/// against a target alpha profile but then scaled the per-band alpha after the inversion —
/// source-over is non-linear under scaling, so shadows at <c>DropshadowAlpha &lt; 255</c>
/// rendered ~2.5× too opaque.</para>
///
/// <para><b>Lifecycle:</b> two <see cref="RenderTargetServiceBase{TRenderTarget}"/> subclasses
/// (one each for the silhouette + V-blur target and the H-blur intermediate target) hold
/// per-renderable RTs. Each <see cref="Draw"/> call marks its owner as used this frame; the
/// raylib <c>Renderer</c> calls <see cref="ClearUnusedRenderTargetsLastFrame"/> at the top of
/// every frame, so RTs are reclaimed automatically when a renderable disappears, becomes
/// invisible, or resizes. This is the raylib counterpart to MonoGame's per-renderable
/// RenderTargetService pattern (<see cref="RenderTargetServiceBase{T}"/>).</para>
///
/// <para><b>Status:</b> first render-to-texture path in the raylib runtime. The shared base
/// class is intended to host future raylib RT consumers (clipping, post-effects) — each new
/// consumer adds its own <see cref="RenderTargetServiceBase{TRenderTarget}"/> subclass alongside
/// <see cref="RenderTextureService"/>.</para>
/// </summary>
public sealed class ShadowBlurRenderer
{
    // Fixed unrolled loop bound — required for GLSL ES / older desktop drivers, harmless on
    // modern ones. Cost is O(2*MaxRadius) taps per pass, 4*MaxRadius total. With MaxRadius=32
    // that's 128 taps per pixel.
    public const int MaxRadius = 32;

    private static readonly string FragmentShader = $@"#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
out vec4 finalColor;
uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform vec2 direction;
uniform float invTwoSigmaSq;
uniform int radius;
void main() {{
    vec4 sum = vec4(0.0);
    float total = 0.0;
    for (int i = -{MaxRadius}; i <= {MaxRadius}; i++) {{
        if (i < -radius || i > radius) continue;
        float fi = float(i);
        float w = exp(-fi * fi * invTwoSigmaSq);
        sum += texture(texture0, fragTexCoord + direction * fi) * w;
        total += w;
    }}
    finalColor = (sum / total) * fragColor * colDiffuse;
}}
";

    private Shader _shader;
    private bool _shaderLoaded;
    private int _locDirection;
    private int _locInvTwoSigmaSq;
    private int _locRadius;

    private readonly RenderTextureService _maskA;
    private readonly RenderTextureService _maskB;

    public ShadowBlurRenderer()
    {
        _maskA = new RenderTextureService();
        _maskB = new RenderTextureService();
    }

    /// <summary>
    /// Paints a soft-edge shadow of a shape into the current draw target.
    /// </summary>
    /// <param name="owner">The renderable that owns this shadow. Used as the cache key for the
    /// per-renderable RTs — passing different shape instances keeps each shape's RT independent
    /// (stable size across frames, automatic cleanup when the shape is removed). Pass <c>this</c>
    /// from the renderable's <c>Render</c> method.</param>
    /// <param name="shadowMinX">Screen-space top-left X of the shape bounds (already offset by DropshadowOffsetX).</param>
    /// <param name="shadowMinY">Screen-space top-left Y of the shape bounds (already offset by DropshadowOffsetY).</param>
    /// <param name="shapeW">Shape width in pixels.</param>
    /// <param name="shapeH">Shape height in pixels.</param>
    /// <param name="sigma">Gaussian sigma in pixels. Mirrors Skia's <c>SKImageFilter.CreateDropShadow</c> sigma.</param>
    /// <param name="tint">Shadow color including alpha. Multiplied into the blurred mask at composite time.</param>
    /// <param name="activeCamera">The <see cref="Camera2D"/> currently established via <c>BeginMode2D</c>
    /// for the pass this shadow draws in (see <see cref="Renderer.ActiveCamera2D"/>). raylib's
    /// <c>EndTextureMode</c> resets the modelview to identity, so the offscreen blur passes below would
    /// otherwise clobber this camera for the shadow's own composite and every later draw in the frame;
    /// it is re-established after the passes (issue #3460).</param>
    /// <param name="activeRenderTexture">The render texture a render-target container's bake is
    /// currently drawing into (see <see cref="Renderer.ActiveRenderTexture"/>), or <c>null</c> when
    /// drawing directly to the screen. raylib's <c>EndTextureMode</c> unconditionally unbinds the
    /// active render texture and does not restore an enclosing one, so the offscreen blur passes
    /// below would otherwise leave the shadow's own composite — and every later draw in the bake —
    /// landing on the screen instead of back in the container's texture; it is re-established after
    /// the passes, alongside the camera (issue #3464).</param>
    /// <param name="drawSilhouetteAt">
    /// Callback invoked inside the offscreen render pass. Receives the RT-local top-left
    /// coordinates the caller should paint a SOLID-WHITE silhouette of the shape at. The
    /// silhouette's alpha is what gets blurred; its RGB must be white so the tint multiplies
    /// cleanly at composite time.
    /// </param>
    public void Draw(
        IRenderableIpso owner,
        float shadowMinX,
        float shadowMinY,
        float shapeW,
        float shapeH,
        float sigma,
        Color tint,
        Camera2D activeCamera,
        RenderTexture2D? activeRenderTexture,
        Action<float, float> drawSilhouetteAt)
    {
        if (sigma <= 0f || shapeW <= 0f || shapeH <= 0f || tint.A == 0)
        {
            return;
        }

        // Anything past 3σ contributes <0.5% of a Gaussian; ceil(3σ) captures the visible
        // falloff without wasting RT area.
        int radius = Math.Min(MaxRadius, Math.Max(1, (int)MathF.Ceiling(sigma * 3f)));
        int rtW = (int)MathF.Ceiling(shapeW) + 2 * radius;
        int rtH = (int)MathF.Ceiling(shapeH) + 2 * radius;

        EnsureShader();
        RenderTexture2D rtA = _maskA.GetFor(owner, rtW, rtH);
        RenderTexture2D rtB = _maskB.GetFor(owner, rtW, rtH);

        Color clear = new Color((byte)0, (byte)0, (byte)0, (byte)0);

        // Every begin/end render-target, blend, and shader state change below flushes the active
        // render batch. Route them through the renderer's draw-call counter so the shadow's
        // offscreen draws are banked into the frame's draw-call count (folded into the main count).
        var counter = global::RenderingLibrary.Graphics.Renderer.Self.BatchDrawCallCounter;

        // 1) Silhouette pass: clear RT_A and let the caller paint a white mask at (radius, radius).
        // Default BLEND_ALPHA is fine here — opaque white onto cleared transparent black gives
        // (1,1,1,1) inside the shape, (0,0,0,0) outside.
        counter.BeginTextureMode(rtA);
        ClearBackground(clear);
        drawSilhouetteAt(radius, radius);
        counter.EndTextureMode();

        float invTwoSigmaSq = 1f / (2f * sigma * sigma);
        SetShaderValue(_shader, _locInvTwoSigmaSq, invTwoSigmaSq, ShaderUniformDataType.Float);
        SetShaderValue(_shader, _locRadius, radius, ShaderUniformDataType.Int);

        // Blur + composite use AlphaPremultiply (glBlendFunc(ONE, ONE_MINUS_SRC_ALPHA)). Under
        // BLEND_ALPHA the GPU would multiply src.rgb by src.a at every blend step; since the
        // shader output has src.rgb == src.a == weighted_avg, each pass would square the alpha
        // and two passes + composite would collapse the shadow to near-zero alpha.
        // AlphaPremultiply treats the shader output as already premultiplied (true here, since
        // the silhouette's rgb is white), so writes are 1:1 into the cleared RT and the final
        // on-screen blend gives perceived alpha = mask_alpha * tint.a — what DropshadowAlpha
        // specifies.

        // 2) Horizontal blur RT_A -> RT_B. Direction is in texture-coordinate units (1 texel).
        // RTs from RenderTextureService are exact-fit (no oversizing), so the standard raylib
        // negative-height idiom Rectangle(0, 0, rtW, -rtH) reads the full upright content.
        Vector2 hDir = new Vector2(1f / rtW, 0f);
        counter.BeginTextureMode(rtB);
        ClearBackground(clear);
        counter.BeginBlendMode(BlendMode.AlphaPremultiply);
        SetShaderValue(_shader, _locDirection, hDir, ShaderUniformDataType.Vec2);
        counter.BeginShaderMode(_shader);
        // Negative source height flips the v range so the upright top-down content of an RT
        // (stored bottom-up in GL coords) reads correctly.
        DrawTextureRec(rtA.Texture, new Rectangle(0, 0, rtW, -rtH), Vector2.Zero, Color.White);
        counter.EndShaderMode();
        counter.EndBlendMode();
        counter.EndTextureMode();

        // 3) Vertical blur RT_B -> RT_A.
        Vector2 vDir = new Vector2(0f, 1f / rtH);
        counter.BeginTextureMode(rtA);
        ClearBackground(clear);
        counter.BeginBlendMode(BlendMode.AlphaPremultiply);
        SetShaderValue(_shader, _locDirection, vDir, ShaderUniformDataType.Vec2);
        counter.BeginShaderMode(_shader);
        DrawTextureRec(rtB.Texture, new Rectangle(0, 0, rtW, -rtH), Vector2.Zero, Color.White);
        counter.EndShaderMode();
        counter.EndBlendMode();
        counter.EndTextureMode();

        // Re-establish the enclosing render target clobbered by the offscreen passes. raylib's
        // EndTextureMode unconditionally unbinds the active render texture and does NOT restore a
        // previously-active one, so without this the shadow composite below — and every later draw
        // in the bake — would render to the screen instead of back into the container's texture
        // (issue #3464). Must happen before re-establishing the camera below, mirroring the order
        // BakeRenderTarget itself uses (BeginTextureMode then BeginMode2D).
        if (activeRenderTexture.HasValue)
        {
            counter.BeginTextureMode(activeRenderTexture.Value);
        }

        // Re-establish the active camera clobbered by the offscreen passes. raylib's EndTextureMode
        // resets the modelview to identity and does NOT restore the enclosing BeginMode2D transform,
        // so without this the shadow composite below — and every later draw in the frame — would
        // render at identity zoom/pan (issue #3460).
        counter.BeginMode2D(activeCamera);

        // 4) Composite RT_A onto the screen, tinted. shadowMinX/Y are the shape's top-left;
        // the RT contains `radius` pixels of falloff padding around the shape, so the on-screen
        // destination starts `radius` pixels up-and-left of that. AlphaPremultiply here too —
        // the RT contents are in premultiplied form (rgb == a, since silhouette was white).
        counter.BeginBlendMode(BlendMode.AlphaPremultiply);
        DrawTextureRec(
            rtA.Texture,
            new Rectangle(0, 0, rtW, -rtH),
            new Vector2(shadowMinX - radius, shadowMinY - radius),
            tint);
        counter.EndBlendMode();
    }

    /// <summary>
    /// Frame-boundary sweep — releases the per-renderable RTs of any owner that wasn't
    /// passed to <see cref="Draw"/> this frame. Call once per frame from the raylib renderer.
    /// </summary>
    public void ClearUnusedRenderTargetsLastFrame()
    {
        _maskA.ClearUnusedRenderTargetsLastFrame();
        _maskB.ClearUnusedRenderTargetsLastFrame();
    }

    /// <summary>Releases all cached RTs and the blur shader. Call on renderer shutdown.</summary>
    public void DisposeAll()
    {
        _maskA.DisposeAll();
        _maskB.DisposeAll();
        if (_shaderLoaded)
        {
            UnloadShader(_shader);
            _shaderLoaded = false;
        }
    }

    private void EnsureShader()
    {
        if (_shaderLoaded)
        {
            return;
        }
        _shader = LoadShaderFromMemory(null, FragmentShader);
        _locDirection = GetShaderLocation(_shader, "direction");
        _locInvTwoSigmaSq = GetShaderLocation(_shader, "invTwoSigmaSq");
        _locRadius = GetShaderLocation(_shader, "radius");
        _shaderLoaded = true;
    }
}

/// <summary>
/// raylib subclass of <see cref="RenderTargetServiceBase{TRenderTarget}"/> that supplies the
/// raylib create / destroy / size primitives for <see cref="RenderTexture2D"/>. Currently only
/// used by <see cref="ShadowBlurRenderer"/>; future raylib offscreen-rendering consumers (RT-
/// based clipping, post-effects) should each own their own instance keyed by the renderable
/// that owns the offscreen pass.
/// </summary>
internal sealed class RenderTextureService : RenderTargetServiceBase<RenderTexture2D>
{
    protected override RenderTexture2D Create(int width, int height) =>
        LoadRenderTexture(width, height);

    protected override void Destroy(RenderTexture2D renderTarget) =>
        UnloadRenderTexture(renderTarget);

    protected override int GetWidth(RenderTexture2D renderTarget) =>
        renderTarget.Texture.Width;

    protected override int GetHeight(RenderTexture2D renderTarget) =>
        renderTarget.Texture.Height;
}
