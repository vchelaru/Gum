using Apos.Shapes;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonoGameAndGum.Renderables;

public class ShapeRenderer
{
    static ShapeRenderer _self = default!;
    ShapeBatch _sb = default!;

    // Issue #2937 — mid-batch blend state, mirroring SpriteBatchStack. BatchKey identifies the
    // tech ("Apos.Shapes"), NOT the blend, so a whole run of shapes shares one batch. When a
    // shape draws with a blend different from the one the batch is currently using, EnsureBlend
    // ends and re-begins the ShapeBatch with the new blend, reusing the view/rasterizer the
    // batch was opened with (the same End/Begin trick SpriteBatchStack.ReplaceRenderStates uses).
    Microsoft.Xna.Framework.Matrix? _currentView;
    RasterizerState? _currentRasterizerState;
    Gum.RenderingLibrary.Blend _currentBlend;
    BlendState? _currentXnaBlendState;
    bool _isBatchBegun;
    bool _isBakingRenderTarget;

    // Issue #5689 — alpha-only blends (MinAlpha, ReplaceAlpha) on a shape apply to the shape's whole
    // bounding rectangle, not just the pixels the shape draws: Apos.Shapes only shades pixels within
    // the shape's own AA margin, so the rest of the bounds is covered by an extra alpha-0 rectangle
    // drawn with the same blend.
    //
    // Issue #6235 — Apos.Shapes draws a quad larger than the shape and does not discard the fragments
    // with zero coverage, so those write alpha 0 (ReplaceAlpha) or min(dst, 0) (MinAlpha) in a margin
    // around the shape, outside its bounds. A stencil bit therefore clips every draw of the shape to
    // its bounds: bit 0 marks the bounds, bit 1 marks what the body drew, and the cover then draws only
    // where bit 0 is set and bit 1 is not. The stencil is cleared per masked shape, so the bits never
    // outlive it. Stencil exists only while baking a render target (bake targets carry a stencil
    // buffer), which is also the only place an alpha-only blend on a shape means anything.
    DepthStencilState? _currentDepthStencilState;
    Microsoft.Xna.Framework.Vector2 _maskTopLeft;
    Microsoft.Xna.Framework.Vector2 _maskSize;
    float _maskRotationRadians;
    bool _isMaskActive;

    // Issue #4509 — the view the batch was opened with, saved while a single renderable draws
    // through a transformed one. Apos.Shapes has no per-draw transform, so a non-uniformly scaled
    // SVG has to re-open the batch around its own draw and restore the view afterwards.
    Microsoft.Xna.Framework.Matrix? _viewBeforePush;
    bool _isViewPushed;

    // Per-frame ShapeBatch begin counter, owned by the active Renderer and reset each frame.
    // Captured in BeginBatch so EnsureBlend's mid-run re-begins are counted too. Null when the
    // batch was opened without a Renderer in scope (e.g. unit tests), making the records no-ops.
    RenderStateChangeStatistics? _statistics;

    public ShapeBatch ShapeBatch
    {
        get
        {
            return _sb;
        }
    }

    /// <summary>
    /// Opens the ShapeBatch for a run of shapes with <paramref name="shape"/>'s blend, recording
    /// the begin parameters so a later <see cref="EnsureBlend"/> can re-open with a different
    /// blend mid-run. Called by the batch owner from <c>RenderableShapeBase.StartBatch</c>.
    /// </summary>
    public void BeginBatch(Microsoft.Xna.Framework.Matrix? view, RasterizerState? rasterizerState, RenderableShapeBase shape, RenderStateChangeStatistics? statistics) =>
        BeginBatch(view, rasterizerState, shape, statistics, isBakingRenderTarget: false);

    /// <summary>
    /// Same as the four-parameter overload, but records whether a render-target bake is in
    /// progress (#5672). Internal use: called by <c>RenderableShapeBase.StartBatch</c>. A separate
    /// overload (not an optional parameter) keeps the old signature binary compatible (#5679).
    /// </summary>
    public void BeginBatch(Microsoft.Xna.Framework.Matrix? view, RasterizerState? rasterizerState, RenderableShapeBase shape, RenderStateChangeStatistics? statistics, bool isBakingRenderTarget)
    {
        _currentView = view;
        _currentRasterizerState = rasterizerState;
        _currentBlend = shape.Blend;
        _isBakingRenderTarget = isBakingRenderTarget;
        _currentXnaBlendState = shape.GetEffectiveXnaBlendState(isBakingRenderTarget);
        _isBatchBegun = true;
        _isViewPushed = false;
        _currentDepthStencilState = null;
        _isMaskActive = false;
        _statistics = statistics;
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: view, blendState: _currentXnaBlendState, rasterizerState: rasterizerState);
    }

    /// <summary>
    /// Issue #5689 — call from a shape's <c>Render</c> right after <see cref="EnsureBlend"/>, before
    /// any of its draws, with the shape's bounding rectangle (<paramref name="topLeft"/> already
    /// adjusted for Apos.Shapes' rotation around the center). For an alpha-only blend while baking,
    /// clips the shape's draws to those bounds and marks what they draw, so
    /// <see cref="EndAlphaBoundsMask"/> can apply the blend to the rest of the bounds. Pair with
    /// <see cref="EndAlphaBoundsMask"/>. Internal use: only Gum's own shape renderables call this;
    /// game code should not.
    /// </summary>
    /// <param name="topLeft">Top-left of the bounds before rotation around its center.</param>
    /// <param name="size">Size of the bounds.</param>
    /// <param name="rotationRadians">Rotation of the bounds around its center.</param>
    public void BeginAlphaBoundsMask(Microsoft.Xna.Framework.Vector2 topLeft, Microsoft.Xna.Framework.Vector2 size, float rotationRadians)
    {
        if (!_isBatchBegun || !_isBakingRenderTarget)
        {
            return;
        }

        _isMaskActive = true;
        _maskTopLeft = topLeft;
        _maskSize = size;
        _maskRotationRadians = rotationRadians;

        _sb.End();
        _sb.GraphicsDevice.Clear(ClearOptions.Stencil, Microsoft.Xna.Framework.Color.Transparent, 0f, 0);
        _currentDepthStencilState = _markBoundsState;
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: _currentView, blendState: _noColorWriteBlendState, depthStencilState: _currentDepthStencilState, rasterizerState: _currentRasterizerState);
        DrawMaskRectangle(Microsoft.Xna.Framework.Color.White);
        ReopenWithDepthStencil(_clipToBoundsState);
    }

    /// <summary>
    /// Issue #5689 — the second half of <see cref="BeginAlphaBoundsMask"/>, called after the shape's
    /// last draw. Draws an alpha-0 rectangle over the shape's bounds with the shape's blend, only
    /// where the shape did not draw: MinAlpha erases there (min(dst, 0)) and ReplaceAlpha sets alpha 0.
    /// Internal use: only Gum's own shape renderables call this; game code should not.
    /// </summary>
    public void EndAlphaBoundsMask()
    {
        if (!_isMaskActive)
        {
            return;
        }
        _isMaskActive = false;

        ReopenWithDepthStencil(_coverOutsideBodyState);
        // Alpha 1/255, not 0: Apos.Shapes.KNI drops draws whose colors are fully transparent, so a
        // true alpha-0 cover would never reach the GPU there. 1/255 is invisible after compositing.
        DrawMaskRectangle(new Microsoft.Xna.Framework.Color((byte)0, (byte)0, (byte)0, (byte)1));
        ReopenWithDepthStencil(null);
    }

    private void DrawMaskRectangle(Microsoft.Xna.Framework.Color color)
    {
        _sb.DrawRectangle(_maskTopLeft, _maskSize, color, color, 1, 0f, _maskRotationRadians, 0);
    }

    // A pixel belongs to the bounds when its center is inside them (the bounds rectangle is drawn without
    // anti-aliasing), so a pixel the bounds only partly cover is left as it was.
    // Stencil bit 0 marks the shape's bounds; bit 1 marks what the body drew. The read mask picks the bits a
    // test looks at and the write mask the bits a pass changes, so one fixed reference serves every shape.
    private readonly DepthStencilState _markBoundsState = new DepthStencilState
    {
        DepthBufferEnable = false,
        StencilEnable = true,
        StencilFunction = CompareFunction.Always,
        StencilPass = StencilOperation.Replace,
        ReferenceStencil = 1,
        StencilWriteMask = 1,
    };

    private readonly DepthStencilState _clipToBoundsState = new DepthStencilState
    {
        DepthBufferEnable = false,
        StencilEnable = true,
        StencilFunction = CompareFunction.Equal,
        StencilPass = StencilOperation.Replace,
        ReferenceStencil = 3,
        StencilMask = 1,
        StencilWriteMask = 2,
    };

    private readonly DepthStencilState _coverOutsideBodyState = new DepthStencilState
    {
        DepthBufferEnable = false,
        StencilEnable = true,
        StencilFunction = CompareFunction.Equal,
        StencilPass = StencilOperation.Keep,
        ReferenceStencil = 1,
        StencilMask = 3,
    };

    private readonly BlendState _noColorWriteBlendState = new BlendState { ColorWriteChannels = ColorWriteChannels.None };

    private void ReopenWithDepthStencil(DepthStencilState? depthStencilState)
    {
        _sb.End();
        _currentDepthStencilState = depthStencilState;
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: _currentView, blendState: _currentXnaBlendState, depthStencilState: _currentDepthStencilState, rasterizerState: _currentRasterizerState);
    }

    /// <summary>
    /// Ensures the open ShapeBatch is drawing with <paramref name="shape"/>'s blend. If it
    /// differs from the blend the batch is currently using, the batch is flushed (End) and
    /// re-opened (Begin) with the new blend, reusing the cached view/rasterizer — the same
    /// in-place state-change mechanism <c>SpriteBatchStack</c> uses for SpriteBatch.
    /// No-op when the blend already matches or no batch is open (e.g. unit tests with no device).
    /// Each shape's <c>Render</c> calls this before drawing.
    /// </summary>
    public void EnsureBlend(RenderableShapeBase shape)
    {
        if (!_isBatchBegun || shape.Blend == _currentBlend)
        {
            return;
        }
        _sb.End();
        _currentBlend = shape.Blend;
        _currentXnaBlendState = shape.GetEffectiveXnaBlendState(_isBakingRenderTarget);
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: _currentView, blendState: _currentXnaBlendState, depthStencilState: _currentDepthStencilState, rasterizerState: _currentRasterizerState);
    }

    /// <summary>
    /// Re-opens the ShapeBatch with <paramref name="view"/> applied on top of the view it was
    /// opened with, so the next draw is transformed in world space and still sees the camera.
    /// Must be paired with <see cref="PopView"/> around that draw. No-op when no batch is open
    /// (e.g. unit tests with no device) or a view is already pushed.
    /// </summary>
    public void PushView(Microsoft.Xna.Framework.Matrix view)
    {
        if (!_isBatchBegun || _isViewPushed)
        {
            return;
        }
        _viewBeforePush = _currentView;
        _isViewPushed = true;
        _sb.End();
        // Row-vector order: the caller's world-space transform runs first, then the camera view
        // BeginBatch was handed (zoom, scroll, any GumBatch.Begin forced matrix).
        _currentView = _viewBeforePush.HasValue ? view * _viewBeforePush.Value : view;
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: _currentView, blendState: _currentXnaBlendState, depthStencilState: _currentDepthStencilState, rasterizerState: _currentRasterizerState);
    }

    /// <summary>
    /// Restores the view <see cref="PushView"/> replaced, re-opening the ShapeBatch so following
    /// shapes draw untransformed. No-op when no view is pushed.
    /// </summary>
    public void PopView()
    {
        if (!_isBatchBegun || !_isViewPushed)
        {
            return;
        }
        _isViewPushed = false;
        _sb.End();
        _currentView = _viewBeforePush;
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: _currentView, blendState: _currentXnaBlendState, depthStencilState: _currentDepthStencilState, rasterizerState: _currentRasterizerState);
    }

    /// <summary>
    /// Ends the open ShapeBatch. Called by the batch owner from <c>RenderableShapeBase.EndBatch</c>
    /// when the BatchOrchestrator transitions away from the Apos.Shapes batch.
    /// </summary>
    public void EndBatch()
    {
        _isBatchBegun = false;
        _isViewPushed = false;
        _sb.End();
    }

    public bool IsInitialized { get; private set; }

    // Issue #3112 — test-only seam. Forces IsInitialized without a real GraphicsDevice so the
    // headless shapes unit tests can exercise the Apos two-slot model (true) or its absence
    // (false). Production code initializes through Initialize(GraphicsDevice, ContentManager);
    // this never runs in shipping paths. Reachable via InternalsVisibleTo("MonoGameGum.Shapes.Tests").
    internal void SetIsInitializedForTesting(bool value) => IsInitialized = value;

    public static ShapeRenderer Self
    {
        get
        {
            _self ??= new ShapeRenderer();
            return _self;
        }
    }

    public void Initialize()
    {
        // Resolve the device from the active SystemManagers rather than GumService.Default, so this
        // works for a GumService created with "new GumService()" (and never creates a Default).
        GraphicsDevice? graphicsDevice = SystemManagers.Default?.Renderer?.GraphicsDevice;
        if(graphicsDevice == null)
        {
            throw new InvalidOperationException(
                "ShapeRenderer cannot be initialized through the parameterless overload because GumService is not initialized. " +
                "Either initialize GumService first, or call ShapeRenderer.Self.Initialize(graphicsDevice, contentManager) directly " +
                "(useful when rendering through GumBatch without GumService).");
        }

        InitializeInternal(graphicsDevice);
    }

    // contentManager is unused as of Apos.Shapes 0.7.2+ — the shader is embedded in the
    // assembly, so ShapeBatch no longer loads it via the content pipeline. Kept as a parameter
    // for source/binary compatibility with existing callers.
    public void Initialize(GraphicsDevice graphicsDevice, ContentManager contentManager) =>
        InitializeInternal(graphicsDevice);

    private void InitializeInternal(GraphicsDevice graphicsDevice)
    {
        if(IsInitialized)
        {
            throw new InvalidOperationException("ShapeRenderer is already initialized");
        }
        IsInitialized = true;
        // Eager on purpose, so the shader compile happens at startup and not mid-game.
        _sb = new ShapeBatch(graphicsDevice, (Effect?)null);

        // Belt-and-suspenders for consumers using GumBatch directly (without GumService).
        // GumService.Initialize already triggers this via reflection scan; calling it here
        // covers the path that bypasses GumService. Idempotent via the guard inside.
        Gum.GueDeriving.AposShapeRuntime.RegisterRuntimeTypes();
    }

    /// <summary>
    /// Releases the ShapeBatch's GPU resources and resets <see cref="IsInitialized"/> so
    /// <see cref="Initialize(GraphicsDevice, ContentManager)"/> can be called again. Safe to call
    /// when never initialized (e.g. invoked from GumService.Uninitialize for an app that never set
    /// up shapes) — a no-op in that case, since this is called via GumService's reflection-based
    /// UninitializeRuntimeTypes hook (see AposShapeRuntime.UninitializeRuntimeTypes) regardless of
    /// whether this app actually uses shapes.
    /// </summary>
    public void Uninitialize()
    {
        if (!IsInitialized)
        {
            return;
        }

        _sb?.Dispose();
        _sb = default!;

        IsInitialized = false;
        _isBatchBegun = false;
        _isViewPushed = false;
        _currentView = null;
        _viewBeforePush = null;
        _currentRasterizerState = null;
        _currentXnaBlendState = null;
        _statistics = null;
    }
}
