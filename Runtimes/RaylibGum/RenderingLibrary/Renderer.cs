using Gum;
using Gum.Renderables;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics;
public class Renderer : IRenderer
{
    /// <summary>
    /// Whether renderable objects should call Render
    /// on contained children. This is true by default, 
    /// results in a hierarchical rendering order.
    /// </summary>
    public static bool RenderUsingHierarchy = true;

    /// <summary>
    /// When true (the default), the render walk skips any renderable that falls entirely outside
    /// the active clip rectangle, along with its subtree — avoiding draw work for scrolled-off
    /// content in clipping containers (#2998). Experimental until validated on real projects; set
    /// false to render all clipped content. Forwards to the backend-agnostic
    /// <see cref="CameraScissorExtensions.CullOffscreenWhenClipped"/>.
    /// </summary>
    public static bool CullOffscreenWhenClipped
    {
        get => CameraScissorExtensions.CullOffscreenWhenClipped;
        set => CameraScissorExtensions.CullOffscreenWhenClipped = value;
    }

    List<Layer> _layers;
    ReadOnlyCollection<Layer> _layersReadOnly;

    // Raylib's BeginScissorMode replaces the active scissor rather than intersecting,
    // so we must track ancestors and intersect manually for nested ClipsChildren.
    Stack<System.Drawing.Rectangle> _scissorStack = new();

    // Per-render-target-container offscreen textures, keyed by the container's contained
    // renderable (issue #3434). Baked in a pre-pass before the outer BeginMode2D and composited
    // back in place during the main walk. Reuses the shared RenderTargetServiceBase lifecycle
    // (exact-size recreation on resize + frame-boundary sweep) exactly like ShadowBlur.
    readonly Gum.Renderables.RenderTextureService _renderTargetService = new();

    // True while baking a render-target container's subtree into its offscreen texture. Used to
    // suppress the screen-space scissor machinery inside DrawGumRecursively, whose rects are wrong
    // once drawing is redirected into an RT framebuffer (raylib flips scissor Y by the *screen*
    // height, not the RT height). Clipping descendants *inside* an RT container is therefore a
    // documented v1 limitation — see #3434. This flag is the seam where RT-local scissor rebasing
    // will hook in.
    bool _isBakingRenderTarget;

    // GL blend-factor / equation constants used by the render-target premultiply pass. Declared
    // here to avoid a raw-GL dependency leaking into callers; only the bake uses them.
    const int GlOne = 1;
    const int GlSrcAlpha = 0x0302;
    const int GlOneMinusSrcAlpha = 0x0303;
    const int GlFuncAdd = 0x8006;

#if XNALIKE
    SpriteRenderer spriteRenderer = new SpriteRenderer();
#endif

    public static Renderer Self
    {
        get
        {
            // Why is this using a singleton instead of system managers default? This seems bad...

            //if (mSelf == null)
            //{
            //    mSelf = new Renderer();
            //}
            //return mSelf;
            if (SystemManagers.Default == null)
            {
                throw new InvalidOperationException(
                    "The SystemManagers.Default is null. You must either specify the default SystemManagers, or use a custom SystemsManager if your app has multiple SystemManagers.");
            }
            return SystemManagers.Default.Renderer;

        }
    }
    Camera _camera = new Camera();
    public Camera Camera => _camera;

    /// <summary>
    /// Per-renderable shadow-blur service. Owns the Gaussian shader and the per-renderable
    /// render textures (issue #2865). Renderables call <c>Renderer.Self.ShadowBlur.Draw(this, ...)</c>
    /// from their <c>Render</c> method; the renderer sweeps unused entries at the top of every
    /// frame via <see cref="ClearUnusedRenderTargetsLastFrame"/>.
    /// </summary>
    public ShadowBlurRenderer ShadowBlur { get; }

    /// <summary>
    /// Per-frame render-state-change counters for this renderer, including the authoritative
    /// <see cref="RenderStateChangeStatistics.DrawCallCount"/> measured via the owned RenderBatch.
    /// Reset at the start of each <see cref="Draw(SystemManagers)"/> and readable afterward to
    /// gauge how well a frame batches.
    /// </summary>
    public RenderStateChangeStatistics RenderStateChangeStatistics { get; private set; }

    /// <summary>
    /// Owns a private raylib <c>RenderBatch</c> and counts the GPU draw calls issued during a Gum
    /// render pass. Renderables that issue raylib state changes (blend / scissor / render-target /
    /// shader) must route them through this counter's wrapper methods so the count stays accurate.
    /// </summary>
    public BatchDrawCallCounter BatchDrawCallCounter { get; private set; }

    public static BlendState NormalBlendState
    {
        get;
        set;
    } = BlendState.NonPremultiplied;


    public Layer MainLayer => _layers[0];
    public ReadOnlyCollection<Layer> Layers => _layersReadOnly;

    public Renderer()
    {
        _layers = new List<Layer>();
        _layersReadOnly = new ReadOnlyCollection<Layer>(_layers);
        _layers.Add(new Layer());
        ShadowBlur = new ShadowBlurRenderer();
        RenderStateChangeStatistics = new RenderStateChangeStatistics();
        BatchDrawCallCounter = new BatchDrawCallCounter();
    }


    internal void Draw(SystemManagers systemManagers)
    {
        //ClearPerformanceRecordingVariables();

        if (systemManagers == null)
        {
            systemManagers = SystemManagers.Default;
        }

        Draw(systemManagers, _layers);
    }

    private void Draw(SystemManagers managers, List<Layer> layers)
    {
        // Frame-boundary RT sweep — release per-renderable shadow RTs whose owner wasn't drawn
        // last frame (renderable removed, hidden, or resized). Mirrors the MonoGame RenderTargetService
        // pattern in RenderingLibrary/Graphics/Renderer.cs.
        ShadowBlur.ClearUnusedRenderTargetsLastFrame();

        // Same frame-boundary sweep for render-target-container textures (#3434). A container that
        // stopped being an RT, was removed, hidden, or resized has its offscreen texture reclaimed.
        _renderTargetService.ClearUnusedRenderTargetsLastFrame();

        // Clear last frame's counts before the pass; the owned-batch counter repopulates
        // DrawCallCount as it banks each flush below.
        RenderStateChangeStatistics.Reset();

        _camera.ClientWidth = Raylib.GetScreenWidth();
        _camera.ClientHeight = Raylib.GetScreenHeight();

        var camera2D = new Camera2D
        {
            Zoom = _camera.Zoom,
            Target = new System.Numerics.Vector2(_camera.X, _camera.Y),
            Offset = _camera.CameraCenterOnScreen == CameraCenterOnScreen.Center
                ? new System.Numerics.Vector2(_camera.ClientWidth / 2f, _camera.ClientHeight / 2f)
                : System.Numerics.Vector2.Zero,
            Rotation = 0,
        };

        // Substitute our owned RenderBatch for raylib's default so its draw counter can be read,
        // then route the mode/scissor state changes below through the counter so each batch flush
        // is banked into RenderStateChangeStatistics.DrawCallCount.
        BatchDrawCallCounter.BeginPass(RenderStateChangeStatistics);

        // Pre-pass, BEFORE the outer BeginMode2D: resolve layout-time properties, then bake every
        // render-target container's subtree into its offscreen texture. Baking must happen outside
        // the outer camera's active Mode2D — the RT bake runs its own BeginTextureMode +
        // BeginMode2D, and the outer camera matrix would otherwise leak into it. This mirrors how
        // the MonoGame renderer runs its render-target PreRender before BeginSpriteBatch (#3434).
        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            // Walk the visual tree calling PreRender on every visible IRenderable before
            // drawing the layer. Mirrors SkiaGum's Renderer.PreRender pattern. This is the
            // canonical hook for runtimes that need camera/zoom-aware resolution of
            // properties (e.g. StrokeWidthUnits = ScreenPixel on CircleRuntime /
            // RectangleRuntime / PolygonRuntime — see #2757). Without it those runtimes had
            // to push StrokeWidth immediately in the setter as a workaround.
            PreRender(layer.Renderables);
            BakeRenderTargetsInSubtree(layer.Renderables, layer);
        }

        BatchDrawCallCounter.BeginMode2D(camera2D);

        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            if (layer.IsLinearFilteringEnabled != null)
            {
                //mRenderStateVariables.Filtering = layer.IsLinearFilteringEnabled.Value;
            }
            else
            {
                //mRenderStateVariables.Filtering = TextureFilter == TextureFilter.Linear;
            }
            RenderLayer(managers, layer, prerender: false);
        }

        BatchDrawCallCounter.EndMode2D();
        BatchDrawCallCounter.EndPass();
    }
    public void RenderLayer(ISystemManagers managers, Layer layer, bool prerender = true)
    {

        layer.SortRenderables();

        Render(layer.Renderables, managers, layer);
    }

    // Recursive PreRender walk — mirrors SkiaGum.Renderer.PreRender. Hidden subtrees are
    // skipped (no need to resolve unit-aware properties for things that won't draw). Both the
    // layer's ReadOnlyCollection and a renderable's ObservableCollection<IRenderableIpso>
    // implement IList<IRenderableIpso>, so one overload covers both.
    private void PreRender(System.Collections.Generic.IList<IRenderableIpso> renderables)
    {
        for (int i = 0; i < renderables.Count; i++)
        {
            IRenderableIpso renderable = renderables[i];
            if (!renderable.Visible)
            {
                continue;
            }
            renderable.PreRender();
            if (renderable.Children != null)
            {
                PreRender(renderable.Children);
            }
        }
    }

    private void Render(ReadOnlyCollection<IRenderableIpso> renderables, ISystemManagers managers, Layer layer)
    {
        _scissorStack.Clear();
        for(int i = 0; i < renderables.Count; i++)
        {
            IRenderableIpso renderable = renderables[i];

            DrawGumRecursively(renderable, layer);

            //if (renderable is GraphicalUiElement graphicalUiElement)
            //{
            //    DrawGumRecursively(graphicalUiElement);

            //    //if (RenderUsingHierarchy)
            //    //{
            //    //    DrawGumRecursively(graphicalUiElement);
            //    //}
            //    //else
            //    //{
            //    //    graphicalUiElement.Render(null);
            //    //}
            //}
            //else
            //{
            //    renderable.Render(null);
            //}
        }
    }

    private void DrawGumRecursively(IRenderableIpso element, Layer layer)
    {
        // #2998 off-screen cull: when a clip is active (the scissor stack is non-empty), skip this
        // element and its subtree if it falls entirely outside the active clip, expanded by a small
        // margin. Mirrors the XNA orderer cull via the same shared predicate.
        if (CameraScissorExtensions.CullOffscreenWhenClipped
            && _scissorStack.Count > 0
            && CameraScissorExtensions.IsFullyOutside(
                _camera.GetScissorRectangleFor(layer, element),
                _scissorStack.Peek(),
                CameraScissorExtensions.OffscreenCullMarginInPixels))
        {
            return;
        }

        // Render-target container (#3434): its subtree was already baked into an offscreen texture
        // during the pre-pass, so composite that texture in place of walking the live children. This
        // fires both in the main walk (composite to screen) and inside an outer container's bake
        // (composite a nested inner RT into the outer texture), which is what makes nesting work.
        if (element.IsRenderTarget)
        {
            CompositeRenderTarget(element);
            return;
        }

        element.Render(null);

        // Inside an RT bake the screen-space scissor rects are invalid (raylib flips scissor Y by
        // the screen height, not the RT height), so skip the clip machinery entirely. Descendant
        // clipping inside an RT container is a documented v1 limitation (#3434).
        bool suppressScissor = _isBakingRenderTarget;

        if (element.ClipsChildren && !suppressScissor)
        {
            var rect = _camera.GetScissorRectangleFor(layer, element);
            var effective = _scissorStack.Count > 0
                ? System.Drawing.Rectangle.Intersect(_scissorStack.Peek(), rect)
                : rect;
            _scissorStack.Push(effective);
            BatchDrawCallCounter.BeginScissorMode(effective.X, effective.Y, effective.Width, effective.Height);
        }

        if (element.Children != null)
        {
            foreach (var child in element.Children)
            {
                if (child is GraphicalUiElement childGue && childGue.Visible)
                {
                    DrawGumRecursively(childGue, layer);
                }
            }
        }

        if (element.ClipsChildren && !suppressScissor)
        {
            _scissorStack.Pop();
            if (_scissorStack.Count > 0)
            {
                var parent = _scissorStack.Peek();
                BatchDrawCallCounter.BeginScissorMode(parent.X, parent.Y, parent.Width, parent.Height);
            }
            else
            {
                BatchDrawCallCounter.EndScissorMode();
            }
        }
    }

    // Post-order (innermost-first) walk that bakes every render-target container's subtree into an
    // offscreen texture before the main compositing walk. Post-order so a nested inner RT is baked
    // before its outer container, which then composites the inner's finished texture while baking.
    private void BakeRenderTargetsInSubtree(
        System.Collections.Generic.IList<IRenderableIpso> renderables, Layer layer)
    {
        for (int i = 0; i < renderables.Count; i++)
        {
            IRenderableIpso renderable = renderables[i];
            if (!renderable.Visible)
            {
                continue;
            }

            if (renderable.Children != null)
            {
                BakeRenderTargetsInSubtree(renderable.Children, layer);
            }

            if (renderable.IsRenderTarget)
            {
                BakeRenderTarget(renderable, layer);
            }
        }
    }

    // Bakes a single render-target container's child subtree into its cached offscreen texture.
    // Children draw at their absolute world coordinates; an offset BeginMode2D targeting the
    // container's clamped top-left maps those into RT-local pixel space. Children are rendered with
    // a premultiply blend (straight src -> premultiplied dst) so the texture composites back without
    // the double-blend dark fringe — see CompositeRenderTarget for the matching AlphaPremultiply blit.
    private void BakeRenderTarget(IRenderableIpso container, Layer layer)
    {
        ComputeRenderTargetBounds(container, out float left, out float top, out _, out _,
            out int width, out int height);

        IRenderableIpso cacheOwner = ResolveRenderTargetCacheOwner(container);
        RenderTexture2D? renderTexture = _renderTargetService.GetFor(cacheOwner, width, height);
        if (renderTexture == null)
        {
            return;
        }

        BatchDrawCallCounter counter = BatchDrawCallCounter;

        Camera2D bakeCamera = new Camera2D
        {
            Target = new System.Numerics.Vector2(left, top),
            Offset = System.Numerics.Vector2.Zero,
            Zoom = _camera.Zoom,
            Rotation = 0,
        };

        // Isolate the scissor stack for the bake; it is restored afterward. Scissor is suppressed
        // inside the bake anyway (see _isBakingRenderTarget), so this only guards against leakage.
        Stack<System.Drawing.Rectangle> savedScissorStack = _scissorStack;
        _scissorStack = new Stack<System.Drawing.Rectangle>();
        bool wasBaking = _isBakingRenderTarget;
        _isBakingRenderTarget = true;

        counter.BeginTextureMode(renderTexture.Value);
        Raylib.ClearBackground(new Color((byte)0, (byte)0, (byte)0, (byte)0));
        counter.BeginMode2D(bakeCamera);
        // Straight-alpha children -> premultiplied RT: color blends standard over, alpha accumulates
        // as coverage (src.a + dst.a*(1-src.a)). The result is premultiplied, which the composite
        // blit reads back with AlphaPremultiply for correct edges and group opacity.
        counter.BeginBlendModeSeparate(
            GlSrcAlpha, GlOneMinusSrcAlpha, GlOne, GlOneMinusSrcAlpha, GlFuncAdd, GlFuncAdd);

        if (container.Children != null)
        {
            foreach (IRenderableIpso child in container.Children)
            {
                if (child is GraphicalUiElement childGue && childGue.Visible)
                {
                    DrawGumRecursively(childGue, layer);
                }
            }
        }

        counter.EndBlendMode();
        counter.EndMode2D();
        counter.EndTextureMode();

        _isBakingRenderTarget = wasBaking;
        _scissorStack = savedScissorStack;
    }

    // Composites a render-target container's baked texture at the container's clamped on-screen
    // rectangle, honoring the container's blend and group alpha (#3434, item 2). The baked texture
    // is premultiplied, so the default (Normal) blend composites with AlphaPremultiply; group alpha
    // is applied by tinting every channel (a premultiplied texture must scale rgb and alpha together).
    private void CompositeRenderTarget(IRenderableIpso container)
    {
        IRenderableIpso cacheOwner = ResolveRenderTargetCacheOwner(container);
        RenderTexture2D? renderTexture = _renderTargetService.TryGetExisting(cacheOwner);
        if (renderTexture == null)
        {
            return;
        }

        ComputeRenderTargetBounds(container, out float left, out float top, out float right,
            out float bottom, out int width, out int height);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        BlendMode blendMode = ResolveCompositeBlendMode(cacheOwner);
        byte alpha = (byte)container.Alpha;
        Color tint = new Color(alpha, alpha, alpha, alpha);

        BatchDrawCallCounter counter = BatchDrawCallCounter;
        counter.BeginBlendMode(blendMode);
        // Negative source height flips the v range: an RT is stored bottom-up in GL coordinates, so
        // reading it with height -h yields the upright content (same idiom as ShadowBlurRenderer).
        Raylib.DrawTexturePro(
            renderTexture.Value.Texture,
            new Rectangle(0, 0, width, -height),
            new Rectangle(left, top, right - left, bottom - top),
            System.Numerics.Vector2.Zero,
            0,
            tint);
        counter.EndBlendMode();
    }

    // Clamps the container's absolute bounds to the on-screen visible intersection and returns the
    // RT pixel size at camera zoom (crisp when zoomed). Mirrors the MonoGame GetRenderTargetFor
    // clamping so an off-screen container bakes only its visible portion.
    private void ComputeRenderTargetBounds(IRenderableIpso container,
        out float left, out float top, out float right, out float bottom, out int width, out int height)
    {
        left = System.Math.Max(_camera.AbsoluteLeft, container.GetAbsoluteLeft());
        right = System.Math.Min(_camera.AbsoluteRight, container.GetAbsoluteRight());
        top = System.Math.Max(_camera.AbsoluteTop, container.GetAbsoluteTop());
        bottom = System.Math.Min(_camera.AbsoluteBottom, container.GetAbsoluteBottom());

        width = global::RenderingLibrary.Math.MathFunctions.RoundToInt((right - left) * _camera.Zoom);
        height = global::RenderingLibrary.Math.MathFunctions.RoundToInt((bottom - top) * _camera.Zoom);
    }

    // For a nested render-target container the walk hands us the GraphicalUiElement wrapper; for a
    // top-level one it hands us the contained InvisibleRenderable directly. The RT cache key is
    // always the contained renderable so both forms resolve to the same texture (#3434 gotcha #8).
    private static IRenderableIpso ResolveRenderTargetCacheOwner(IRenderableIpso source)
    {
        if (source is GraphicalUiElement gue && gue.RenderableComponent is IRenderableIpso contained)
        {
            return contained;
        }

        return source;
    }

    // The baked texture is premultiplied. Normal (NonPremultiplied) container blend therefore
    // composites with AlphaPremultiply; Additive maps to raylib's additive mode. Other blends are
    // approximated as premultiplied-over for v1.
    private static BlendMode ResolveCompositeBlendMode(IRenderableIpso cacheOwner)
    {
        if (cacheOwner is RenderableBase renderable
            && Gum.RenderingLibrary.BlendExtensions.ToBlend(renderable.BlendState)
                == Gum.RenderingLibrary.Blend.Additive)
        {
            return BlendMode.Additive;
        }

        return BlendMode.AlphaPremultiply;
    }

    /// <summary>
    /// Whether a baked offscreen texture is currently cached for the given render-target container.
    /// Resolves the container's cache owner internally. Intended for tests and diagnostics.
    /// </summary>
    public bool HasBakedRenderTargetFor(IRenderableIpso container) =>
        _renderTargetService.HasCachedRenderTarget(ResolveRenderTargetCacheOwner(container));

    /// <summary>
    /// Returns the baked offscreen texture cached for the given render-target container, or
    /// <c>null</c> if none exists. Resolves the container's cache owner internally. Intended for
    /// tests and diagnostics that need to sample what was baked.
    /// </summary>
    public RenderTexture2D? TryGetBakedRenderTargetFor(IRenderableIpso container) =>
        _renderTargetService.TryGetExisting(ResolveRenderTargetCacheOwner(container));

}
