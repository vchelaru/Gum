using Apos.Shapes;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameAndGum.Renderables;

public class ShapeRenderer
{
    // Bump this whenever the shipped apos-shapes.xnb files in
    // buildTransitive/MonoGame/Content/{DesktopGL,WindowsDX}/ are regenerated
    // against a new Apos.Shapes version. The XnbBuilderMonoGame* projects must
    // be rebuilt and their output XNBs copied in alongside this constant change.
    const string CompiledAgainstAposShapesVersion = "0.6.10-alpha";

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
    bool _isBatchBegun;

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
        _isBatchBegun = true;
        _statistics = statistics;
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: view, blendState: shape.GetEffectiveXnaBlendState(), rasterizerState: rasterizerState);
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
        _statistics?.RecordShapeBatchBegin();
        _sb.Begin(view: _currentView, blendState: shape.GetEffectiveXnaBlendState(), rasterizerState: _currentRasterizerState);
    }

    /// <summary>
    /// Ends the open ShapeBatch. Called by the batch owner from <c>RenderableShapeBase.EndBatch</c>
    /// when the BatchOrchestrator transitions away from the Apos.Shapes batch.
    /// </summary>
    public void EndBatch()
    {
        _isBatchBegun = false;
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

    public void Initialize(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        if(IsInitialized)
        {
            throw new InvalidOperationException("ShapeRenderer is already initialized");
        }
        ValidateAposShapesVersion();
        IsInitialized = true;
        _sb = new ShapeBatch(graphicsDevice, contentManager);

        // Belt-and-suspenders for consumers using GumBatch directly (without GumService).
        // GumService.Initialize already triggers this via reflection scan; calling it here
        // covers the path that bypasses GumService. Idempotent via the guard inside.
        Gum.GueDeriving.AposShapeRuntime.RegisterRuntimeTypes();
    }

    [Conditional("DEBUG")]
    static void ValidateAposShapesVersion()
    {
        var assembly = typeof(ShapeBatch).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "<unknown>";

        // InformationalVersion may include a +commit suffix; strip it for comparison.
        var plusIndex = informational.IndexOf('+');
        var resolved = plusIndex >= 0 ? informational.Substring(0, plusIndex) : informational;

        if (!resolved.StartsWith(CompiledAgainstAposShapesVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Gum.Shapes was built against Apos.Shapes {CompiledAgainstAposShapesVersion}, " +
                $"but the resolved Apos.Shapes assembly reports version '{resolved}'. " +
                "The shipped apos-shapes.xnb may be incompatible with this Apos.Shapes runtime. " +
                "To fix, follow Runtimes/GumShapes/XnbBuilder/README.md.");
        }
    }
}
