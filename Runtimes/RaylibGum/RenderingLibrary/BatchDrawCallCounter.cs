using Gum.Renderables;
using Raylib_cs;
using System;
using System.Runtime.InteropServices;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Measures the authoritative number of GPU draw calls the raylib renderer issues for a Gum
/// render pass, by owning a private <see cref="RenderBatch"/> and reading raylib's own draw
/// bookkeeping instead of predicting it.
///
/// <para><b>Why own a batch:</b> raylib's default render batch is a file-scoped <c>static</c> in
/// rlgl with no public accessor, so its draw counter can't be read. <c>rlSetRenderBatchActive</c>
/// lets us substitute a batch we own; raylib then accumulates draws into it with all of its real
/// grouping rules (texture-change and draw-mode-change splits), and we read the result.</para>
///
/// <para><b>Why bank at each flush:</b> raylib resets the batch's <c>DrawCounter</c> to 1 on every
/// flush (<c>rlDrawRenderBatch</c>), and a flush is forced by every scissor/blend/render-target/
/// shader/mode change. So a single end-of-pass read would only see the last segment. Instead the
/// renderer calls <see cref="Bank"/> immediately before each such state change; <see cref="Bank"/>
/// counts the draw-call slots raylib actually filled this segment (slots with a non-zero vertex
/// count — exactly what <c>rlDrawRenderBatch</c> would draw) and adds them to the active
/// <see cref="RenderStateChangeStatistics"/>.</para>
///
/// <para><b>Lifecycle:</b> the owned batch is loaded lazily on first use (it needs a live GL
/// context) and pinned for the process lifetime. Mirroring <c>ShadowBlurRenderer</c>, there is no
/// explicit unload — raylib's <c>CloseWindow</c> reclaims the GL resources at shutdown.</para>
/// </summary>
public sealed unsafe class BatchDrawCallCounter
{
    // raylib desktop (GL 3.3) defaults for the internal batch — RL_DEFAULT_BATCH_BUFFERS and
    // RL_DEFAULT_BATCH_BUFFER_ELEMENTS. Matching them keeps our owned batch's auto-flush behavior
    // identical to the default batch raylib would otherwise use.
    private const int DefaultBatchBuffers = 1;
    private const int DefaultBatchBufferElements = 8192;

    // Single-element array pinned for the process so the RenderBatch lives at a stable address:
    // rlSetRenderBatchActive stores the pointer, so the struct must not move while it is active.
    private RenderBatch[] _batchStorage;
    private GCHandle _pinHandle;
    private RenderBatch* _batch;
    private bool _initialized;

    private RenderStateChangeStatistics _statistics;
    private bool _active;

    // GL blend factor / equation constants for the render-target premultiply pass. Kept here so
    // the raw-GL dependency stays contained to the one place that owns blend state.
    private const int GlOne = 1;
    private const int GlSrcAlpha = 0x0302;
    private const int GlOneMinusSrcAlpha = 0x0303;
    private const int GlFuncAdd = 0x8006;

    // While true, the ambient blend mode is the render-target premultiply pass (not straight
    // BLEND_ALPHA). raylib's EndBlendMode always resets to BLEND_ALPHA and never restores the
    // previous mode, so any child that toggles blend mid-bake (a Sprite with an explicit Blend, or
    // a nested render-target composite) would otherwise leave every following sibling baking with
    // straight alpha — reintroducing the double-blend dark fringe. With this flag set, EndBlendMode
    // re-establishes the premultiply pass instead (issue #3434).
    private bool _renderTargetBlendActive;

    /// <summary>
    /// Activates the owned batch for a render pass and routes subsequent <see cref="Bank"/> calls
    /// into <paramref name="statistics"/>. If the GL context isn't ready (so the batch can't be
    /// loaded), counting is silently disabled for the pass and rendering proceeds normally.
    /// The caller is responsible for resetting <paramref name="statistics"/> before the pass.
    /// </summary>
    public void BeginPass(RenderStateChangeStatistics statistics)
    {
        EnsureInitialized();
        if (!_initialized)
        {
            _active = false;
            return;
        }

        _statistics = statistics;
        _active = true;
        // Switching to our batch flushes whatever was queued in the previous (default) batch, so
        // any draws issued before the Gum pass are accounted to the default batch, not ours.
        Rlgl.SetRenderBatchActive(_batch);
    }

    /// <summary>
    /// Counted wrapper for <see cref="Raylib.BeginMode2D"/>: banks the pending segment, then
    /// performs the state change (which flushes the batch).
    /// </summary>
    public void BeginMode2D(Camera2D camera)
    {
        Bank();
        Raylib.BeginMode2D(camera);
    }

    /// <summary>Counted wrapper for <see cref="Raylib.EndMode2D"/>.</summary>
    public void EndMode2D()
    {
        Bank();
        Raylib.EndMode2D();
    }

    /// <summary>Counted wrapper for <see cref="Raylib.BeginScissorMode"/>.</summary>
    public void BeginScissorMode(int x, int y, int width, int height)
    {
        Bank();
        Raylib.BeginScissorMode(x, y, width, height);
    }

    /// <summary>Counted wrapper for <see cref="Raylib.EndScissorMode"/>.</summary>
    public void EndScissorMode()
    {
        Bank();
        Raylib.EndScissorMode();
    }

    /// <summary>Counted wrapper for <see cref="Raylib.BeginBlendMode"/>.</summary>
    public void BeginBlendMode(BlendMode mode)
    {
        Bank();
        Raylib.BeginBlendMode(mode);
    }

    /// <summary>
    /// Counted wrapper that reproduces a Gum <see cref="global::Gum.RenderingLibrary.Blend"/> value
    /// on raylib. <c>Normal</c>/<c>Additive</c> use a canned raylib <see cref="BlendMode"/>; every
    /// other value (<c>Replace</c>, <c>ReplaceAlpha</c>, <c>SubtractAlpha</c>, <c>MinAlpha</c> —
    /// issue #3470) sets explicit per-channel GL blend factors/equations via
    /// <c>Rlgl.SetBlendFactorsSeparate</c> and <see cref="BlendMode.CustomSeparate"/>, since raylib
    /// has no canned mode for them.
    ///
    /// <para>Whether this is called while <see cref="_renderTargetBlendActive"/> determines which
    /// color-factor derivation those four values use — see
    /// <see cref="Gum.Renderables.BlendModeExtensions"/>'s remarks for why: outside a bake there is
    /// no further compositing to keep premultiplied-consistent, so <c>Replace</c> etc. use the
    /// straightforward "ignore alpha" factors matching MonoGame; inside a bake they must leave a
    /// validly premultiplied pixel or leftover color bleeds through <c>CompositeRenderTarget</c>'s
    /// <c>AlphaPremultiply</c> composite (issue #3470 follow-up).</para>
    ///
    /// Pair with <see cref="EndBlendMode"/>.
    /// </summary>
    public void BeginBlendMode(global::Gum.RenderingLibrary.Blend blend)
    {
        Bank();

        if (blend.TryGetSimpleRaylibBlendMode(out BlendMode mode))
        {
            Raylib.BeginBlendMode(mode);
            return;
        }

        (int srcRgb, int dstRgb, int srcAlpha, int dstAlpha, int eqRgb, int eqAlpha) =
            blend.ToGlBlendFactorsSeparate(isPremultiplyingContext: _renderTargetBlendActive);
        Rlgl.SetBlendFactorsSeparate(srcRgb, dstRgb, srcAlpha, dstAlpha, eqRgb, eqAlpha);
        Raylib.BeginBlendMode(BlendMode.CustomSeparate);
    }

    /// <summary>
    /// Enters the render-target premultiply blend pass and marks it as the ambient blend for the
    /// duration of a bake: straight-alpha children accumulate premultiplied color (color blends
    /// standard-over, alpha accumulates as coverage), so the baked texture composites back without
    /// the double-blend dark fringe (issue #3434). Pair with <see cref="EndRenderTargetBlend"/>.
    /// While active, <see cref="EndBlendMode"/> re-establishes this pass instead of resetting to
    /// straight alpha, so a child toggling blend mid-bake cannot clobber it for later siblings.
    /// </summary>
    public void BeginRenderTargetBlend()
    {
        Bank();
        _renderTargetBlendActive = true;
        ApplyRenderTargetBlend();
    }

    /// <summary>
    /// Exits the render-target premultiply blend pass and restores straight alpha as the ambient
    /// blend. Pair with <see cref="BeginRenderTargetBlend"/>.
    /// </summary>
    public void EndRenderTargetBlend()
    {
        Bank();
        _renderTargetBlendActive = false;
        Raylib.EndBlendMode();
    }

    /// <summary>
    /// Enters an additive blend that is correct for an already-premultiplied source: it adds the
    /// premultiplied color directly (factors ONE/ONE) rather than multiplying by source alpha a
    /// second time the way <see cref="BlendMode.Additive"/> would. Used when compositing an
    /// additive-blend render-target container back to its destination (issue #3434). Pair with
    /// <see cref="EndBlendMode"/>.
    /// </summary>
    public void BeginBlendModeAdditivePremultiplied()
    {
        Bank();
        Rlgl.SetBlendFactorsSeparate(GlOne, GlOne, GlOne, GlOne, GlFuncAdd, GlFuncAdd);
        Raylib.BeginBlendMode(BlendMode.CustomSeparate);
    }

    /// <summary>
    /// Counted wrapper for <see cref="Raylib.EndBlendMode"/>. While a render-target bake is active
    /// (see <see cref="BeginRenderTargetBlend"/>) this re-establishes the premultiply pass rather
    /// than resetting to straight alpha, so mid-bake blend toggles don't leak into later siblings.
    /// </summary>
    public void EndBlendMode()
    {
        Bank();
        if (_renderTargetBlendActive)
        {
            ApplyRenderTargetBlend();
        }
        else
        {
            Raylib.EndBlendMode();
        }
    }

    private void ApplyRenderTargetBlend()
    {
        Rlgl.SetBlendFactorsSeparate(
            GlSrcAlpha, GlOneMinusSrcAlpha, GlOne, GlOneMinusSrcAlpha, GlFuncAdd, GlFuncAdd);
        Raylib.BeginBlendMode(BlendMode.CustomSeparate);
    }

    /// <summary>Counted wrapper for <see cref="Raylib.BeginTextureMode"/>.</summary>
    public void BeginTextureMode(RenderTexture2D target)
    {
        Bank();
        Raylib.BeginTextureMode(target);
    }

    /// <summary>Counted wrapper for <see cref="Raylib.EndTextureMode"/>.</summary>
    public void EndTextureMode()
    {
        Bank();
        Raylib.EndTextureMode();
    }

    /// <summary>Counted wrapper for <see cref="Raylib.BeginShaderMode"/>.</summary>
    public void BeginShaderMode(Shader shader)
    {
        Bank();
        Raylib.BeginShaderMode(shader);
    }

    /// <summary>Counted wrapper for <see cref="Raylib.EndShaderMode"/>.</summary>
    public void EndShaderMode()
    {
        Bank();
        Raylib.EndShaderMode();
    }

    /// <summary>
    /// Banks the draw calls accumulated since the last flush, then ends the pass and restores
    /// raylib's default batch. Call after the last draw of the pass.
    /// </summary>
    public void EndPass()
    {
        if (!_active)
        {
            return;
        }

        Bank();
        Rlgl.SetRenderBatchActive(null);
        _active = false;
        _statistics = null;
    }

    /// <summary>
    /// Counts the draw-call slots raylib filled since the previous flush and adds them to the
    /// active statistics. Must be called immediately before any raylib state change that flushes
    /// the batch (scissor/blend/render-target/shader/mode). No-op when counting is inactive.
    /// </summary>
    public void Bank()
    {
        if (!_active)
        {
            return;
        }

        int drawCounter = _batch->DrawCounter;
        int realDrawCalls = 0;
        for (int i = 0; i < drawCounter; i++)
        {
            // rlDrawRenderBatch only emits slots with vertices, and zeroes every slot's vertex
            // count on flush — so non-empty slots in [0, DrawCounter) are exactly the GPU draws
            // for this segment, with no stale or empty-segment overcount.
            if (_batch->Draws[i].VertexCount > 0)
            {
                realDrawCalls++;
            }
        }

        _statistics?.AddDrawCalls(realDrawCalls);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        // LoadRenderBatch allocates GL vertex buffers, so it requires an initialized window. If the
        // window isn't ready yet, leave counting disabled for this pass and retry next frame.
        if (!Raylib.IsWindowReady())
        {
            return;
        }

        _batchStorage = new RenderBatch[1];
        _pinHandle = GCHandle.Alloc(_batchStorage, GCHandleType.Pinned);
        _batch = (RenderBatch*)_pinHandle.AddrOfPinnedObject();
        *_batch = Rlgl.LoadRenderBatch(DefaultBatchBuffers, DefaultBatchBufferElements);
        _initialized = true;
    }
}
