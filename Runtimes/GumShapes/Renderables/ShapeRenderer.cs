using Apos.Shapes;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
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
    public void BeginBatch(Microsoft.Xna.Framework.Matrix? view, RasterizerState? rasterizerState, RenderableShapeBase shape, RenderStateChangeStatistics? statistics)
    {
        _currentView = view;
        _currentRasterizerState = rasterizerState;
        _currentBlend = shape.Blend;
        _currentXnaBlendState = shape.GetEffectiveXnaBlendState();
        _isBatchBegun = true;
        _isViewPushed = false;
        _statistics = statistics;
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: view, blendState: _currentXnaBlendState, rasterizerState: rasterizerState);
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
        _currentXnaBlendState = shape.GetEffectiveXnaBlendState();
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: _currentView, blendState: _currentXnaBlendState, rasterizerState: _currentRasterizerState);
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
        _sb.Begin(view: _currentView, blendState: _currentXnaBlendState, rasterizerState: _currentRasterizerState);
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
        _sb.Begin(view: _currentView, blendState: _currentXnaBlendState, rasterizerState: _currentRasterizerState);
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
        var gumService = GumService.Default;
        if(gumService.IsInitialized == false)
        {
            throw new InvalidOperationException(
                "ShapeRenderer cannot be initialized through the parameterless overload because GumService is not initialized. " +
                "Either initialize GumService first, or call ShapeRenderer.Self.Initialize(graphicsDevice, contentManager) directly " +
                "(useful when rendering through GumBatch without GumService).");
        }

        Initialize(gumService.Game.GraphicsDevice, gumService.Game.Content);
    }

    // contentManager is unused as of Apos.Shapes 0.7.2+ — the shader is embedded in the
    // assembly, so ShapeBatch no longer loads it via the content pipeline. Kept as a parameter
    // for source/binary compatibility with existing callers.
    public void Initialize(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        if(IsInitialized)
        {
            throw new InvalidOperationException("ShapeRenderer is already initialized");
        }
        IsInitialized = true;
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
