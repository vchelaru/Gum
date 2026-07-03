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

    // True while baking a render-target container's subtree into its offscreen texture. Inside a
    // bake, DrawGumRecursively rebases scissor rects into RT-local space (the clamped top-left is
    // the RT origin) so a ClipsChildren descendant clips correctly within the RT (#3440).
    bool _isBakingRenderTarget;

    // The active bake's clamped top-left in world coords, captured so DrawGumRecursively can convert
    // a descendant's absolute bounds into RT-local scissor pixels. Only meaningful while
    // _isBakingRenderTarget is true; bakes are never re-entrant (post-order means an inner RT is
    // fully baked before its outer one begins), so single fields suffice.
    float _bakeLeft;
    float _bakeTop;

    // Reused across bakes so a bake doesn't allocate a fresh scissor stack each frame. Swapped in
    // for the duration of a bake and restored afterward; balanced push/pop leaves it empty.
    readonly Stack<System.Drawing.Rectangle> _bakeScissorStack = new();

    // Set during the PreRender walk (which already traverses the whole visible tree) when any
    // render-target container is present, so the bake pre-pass is skipped entirely for the common
    // case of screens with no render targets — no extra full traversal.
    bool _frameHasRenderTarget;

    // Cache owners of render-target containers referenced by a visible Sprite's
    // RenderTargetTextureSource. Collected once per Draw (before PreRender) so an INVISIBLE but
    // referenced container still bakes — mirrors MonoGame's #1643 fix (Renderer.cs). Unlike MonoGame
    // this needs no per-host-frame caching: raylib bakes all layers in a single pass per Draw, so one
    // collection per Draw suffices (#3452).
    readonly HashSet<IRenderableIpso> _referencedRenderTargetOwners = new();

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
    /// The raylib <see cref="Camera2D"/> currently established via <c>BeginMode2D</c> for the pass in
    /// progress — the main-walk camera during the main walk, or a render-target container's bake
    /// camera while that container's subtree is baking. Exposed so a mid-walk offscreen consumer
    /// (e.g. <see cref="ShadowBlurRenderer"/>) can re-establish it after its own
    /// <c>BeginTextureMode</c>/<c>EndTextureMode</c> passes, which raylib's <c>EndTextureMode</c>
    /// resets to identity (issue #3460). Primarily for internal renderer/renderable use.
    /// </summary>
    public Camera2D ActiveCamera2D { get; private set; }

    /// <summary>
    /// The render texture a render-target container's bake is currently drawing into via
    /// <c>BeginTextureMode</c>, or <c>null</c> during the main walk (which draws to the screen).
    /// Exposed so a mid-walk offscreen consumer (e.g. <see cref="ShadowBlurRenderer"/>) can
    /// re-establish it after its own <c>BeginTextureMode</c>/<c>EndTextureMode</c> passes, which
    /// raylib's <c>EndTextureMode</c> unconditionally unbinds and does not restore an enclosing
    /// render texture (issue #3464 — the render-target analogue of #3460's camera clobber).
    /// Primarily for internal renderer/renderable use.
    /// </summary>
    public RenderTexture2D? ActiveRenderTexture { get; private set; }

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

        // Reset per-frame; PreRender flips this true if it encounters any render-target container,
        // which is the only thing that makes the bake pre-pass run (#3434 perf fast-out).
        _frameHasRenderTarget = false;

        // Collect the render-target containers referenced by visible Sprites before PreRender so an
        // invisible-but-referenced container still bakes (#3452). Runs every Draw; raylib bakes all
        // layers in one pass so no per-host-frame caching is needed (unlike MonoGame).
        _referencedRenderTargetOwners.Clear();
        for (int i = 0; i < layers.Count; i++)
        {
            CollectReferencedRenderTargets(layers[i].Renderables);
        }

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
        }

        // Only bake if PreRender saw at least one render-target container this frame — screens with
        // none skip the whole extra tree traversal.
        if (_frameHasRenderTarget)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                BakeRenderTargetsInSubtree(layers[i].Renderables, layers[i]);
            }
        }

        // Record the outer camera so a mid-walk offscreen consumer can re-establish it after its
        // own EndTextureMode resets the modelview (issue #3460).
        ActiveCamera2D = camera2D;
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
            // An invisible render-target container still needs its subtree PreRendered (and must flip
            // _frameHasRenderTarget) when a visible Sprite references it, so the bake pre-pass runs and
            // resolves its children's layout-time properties (#3452).
            bool isReferencedRenderTarget = renderable.IsRenderTarget && IsReferencedRenderTargetOwner(renderable);
            if (!renderable.Visible && !isReferencedRenderTarget)
            {
                continue;
            }
            if (renderable.IsRenderTarget)
            {
                _frameHasRenderTarget = true;
            }
            renderable.PreRender();
            if (renderable.Children != null)
            {
                PreRender(renderable.Children);
            }
        }
    }

    // Populates _referencedRenderTargetOwners with the cache owner of every render-target container
    // referenced by a visible Sprite's RenderTargetTextureSource. Both a top-level Sprite (the walk
    // hands the contained Sprite directly) and a nested one (a GraphicalUiElement wrapping the Sprite)
    // are handled. Mirrors MonoGame's CollectReferencedRenderTargets, but resolves the raylib
    // Gum.Renderables.Sprite concretely — raylib has no IRenderTargetTextureReferencer interface (its
    // Texture member is XNA-typed). See #3452 / #1643.
    private void CollectReferencedRenderTargets(System.Collections.Generic.IList<IRenderableIpso> renderables)
    {
        for (int i = 0; i < renderables.Count; i++)
        {
            IRenderableIpso renderable = renderables[i];
            if (!renderable.Visible)
            {
                continue;
            }

            Sprite? sprite = renderable as Sprite
                ?? (renderable as GraphicalUiElement)?.RenderableComponent as Sprite;
            if (sprite?.RenderTargetTextureSource != null)
            {
                _referencedRenderTargetOwners.Add(
                    ResolveRenderTargetCacheOwner(sprite.RenderTargetTextureSource));
            }

            if (renderable.Children != null)
            {
                CollectReferencedRenderTargets(renderable.Children);
            }
        }
    }

    // Whether the given render-target container is referenced by a visible Sprite this Draw. Resolves
    // to the container's cache owner so both the GraphicalUiElement wrapper and the contained
    // renderable form match the keys stored during collection (#3452).
    private bool IsReferencedRenderTargetOwner(IRenderableIpso renderable) =>
        _referencedRenderTargetOwners.Contains(ResolveRenderTargetCacheOwner(renderable));

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
                GetScissorRectangleFor(layer, element),
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
            // A hidden RT container draws nothing (its bake was skipped too). Without this the
            // composite path would blit last frame's stale cached texture for one frame after the
            // container is hidden — a 1-frame ghost (#3434).
            if (!element.Visible)
            {
                return;
            }

            // If a valid baked texture exists, composite it and stop. Otherwise (degenerate/zero
            // clamped size — e.g. a 0-sized pre-layout container, or one whose children draw at
            // offsets) fall through and render the children directly so the subtree doesn't vanish.
            if (TryCompositeRenderTarget(element))
            {
                return;
            }
        }

        element.Render(null);

        if (element.ClipsChildren)
        {
            System.Drawing.Rectangle rect = GetScissorRectangleFor(layer, element);
            System.Drawing.Rectangle effective = _scissorStack.Count > 0
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

        if (element.ClipsChildren)
        {
            _scissorStack.Pop();
            if (_scissorStack.Count > 0)
            {
                System.Drawing.Rectangle parent = _scissorStack.Peek();
                BatchDrawCallCounter.BeginScissorMode(parent.X, parent.Y, parent.Width, parent.Height);
            }
            else
            {
                BatchDrawCallCounter.EndScissorMode();
            }
        }
    }

    // Scissor rectangle for a clipping element, in whatever coordinate space the current pass uses:
    // screen space normally, RT-local pixel space during a bake. During a bake drawing is redirected
    // into the container's offscreen framebuffer via an offset BeginMode2D, so a descendant's clip
    // rect must be expressed relative to the bake origin (the clamped top-left). raylib's
    // BeginScissorMode takes top-left coordinates and applies its own Y flip; passing the RT-local
    // top-left rect directly clips the correct rows on both hardware GL and software GL (Mesa
    // llvmpipe) — no screen-height compensation is needed (that was the #3436 mistake, #3440).
    private System.Drawing.Rectangle GetScissorRectangleFor(Layer layer, IRenderableIpso element)
    {
        if (!_isBakingRenderTarget)
        {
            return _camera.GetScissorRectangleFor(layer, element);
        }

        float zoom = _camera.Zoom;
        int left = global::RenderingLibrary.Math.MathFunctions.RoundToInt(
            (element.GetAbsoluteLeft() - _bakeLeft) * zoom);
        int top = global::RenderingLibrary.Math.MathFunctions.RoundToInt(
            (element.GetAbsoluteTop() - _bakeTop) * zoom);
        int right = global::RenderingLibrary.Math.MathFunctions.RoundToInt(
            (element.GetAbsoluteRight() - _bakeLeft) * zoom);
        int bottom = global::RenderingLibrary.Math.MathFunctions.RoundToInt(
            (element.GetAbsoluteBottom() - _bakeTop) * zoom);
        return new System.Drawing.Rectangle(left, top, right - left, bottom - top);
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
            // An invisible container is normally skipped (it draws nothing to screen), but a
            // referenced one still bakes so a visible Sprite can sample its cached texture (#3452).
            bool isReferencedRenderTarget = renderable.IsRenderTarget && IsReferencedRenderTargetOwner(renderable);
            if (!renderable.Visible && !isReferencedRenderTarget)
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
    // container's clamped top-left maps those into RT-local pixel space. Children are rendered under
    // a premultiply blend (straight src -> premultiplied dst) so the texture composites back without
    // the double-blend dark fringe — see TryCompositeRenderTarget for the matching premultiplied blit.
    // A degenerate (zero-size) clamp bakes nothing; the composite path then renders the children
    // directly so the subtree doesn't vanish.
    private void BakeRenderTarget(IRenderableIpso container, Layer layer)
    {
        ComputeRenderTargetBounds(container, out float left, out float top, out _, out _,
            out int width, out int height);

        // Skip the bake when the container clamps to a non-positive size — a 0-sized pre-layout
        // container, or (the #3475 case) one positioned entirely off-camera. This size guard is what
        // actually stops the off-camera bake: GetFor returns default(RenderTexture2D) for a
        // non-positive size, but because RenderTexture2D is a value type that default does NOT read as
        // null through the RenderTexture2D? local below — RenderTargetServiceBase's TRenderTarget? is
        // an unconstrained nullable, not a Nullable<>, so a plain (even zeroed) struct lifts to a
        // non-null nullable and the `== null` check can never fire here. Without this guard,
        // BeginTextureMode would bind the zeroed texture's FBO id 0 (the default framebuffer) and the
        // ClearBackground below would wipe the whole window to transparent black. Mirrors the matching
        // guard in TryCompositeRenderTarget.
        if (width <= 0 || height <= 0)
        {
            return;
        }

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

        // Swap in the reusable bake scissor stack (avoids a per-bake allocation) so the main-walk
        // scissor stack is isolated from the bake, and capture the bake origin so DrawGumRecursively
        // can rebase a ClipsChildren descendant's clip rect into RT-local space (#3440).
        Stack<System.Drawing.Rectangle> savedScissorStack = _scissorStack;
        _bakeScissorStack.Clear();
        _scissorStack = _bakeScissorStack;
        bool wasBaking = _isBakingRenderTarget;
        float savedBakeLeft = _bakeLeft;
        float savedBakeTop = _bakeTop;
        _isBakingRenderTarget = true;
        _bakeLeft = left;
        _bakeTop = top;

        // Record the bake camera as the active one so a blurred dropshadow drawn inside this bake
        // re-establishes the bake transform (not the main-walk camera) after its offscreen passes
        // (issue #3460). Restored to the main-walk camera before the main BeginMode2D below.
        Camera2D savedActiveCamera = ActiveCamera2D;
        ActiveCamera2D = bakeCamera;

        // Record this bake's render texture as the active one so a blurred dropshadow drawn inside
        // this bake re-establishes it (not the screen) after its own offscreen BeginTextureMode/
        // EndTextureMode passes, which unconditionally unbind whatever render texture was active
        // (issue #3464). Restored to the enclosing bake's texture (or null) below.
        RenderTexture2D? savedActiveRenderTexture = ActiveRenderTexture;
        ActiveRenderTexture = renderTexture;

        counter.BeginTextureMode(renderTexture.Value);
        Raylib.ClearBackground(new Color((byte)0, (byte)0, (byte)0, (byte)0));
        counter.BeginMode2D(bakeCamera);
        // Establish the premultiply pass as the ambient blend for the whole subtree. If a child
        // toggles blend (a Sprite with an explicit Blend, or a nested render-target composite), the
        // counter re-establishes this pass on EndBlendMode so later siblings still bake premultiplied.
        counter.BeginRenderTargetBlend();

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

        counter.EndRenderTargetBlend();
        counter.EndMode2D();
        counter.EndTextureMode();

        _isBakingRenderTarget = wasBaking;
        _bakeLeft = savedBakeLeft;
        _bakeTop = savedBakeTop;
        _scissorStack = savedScissorStack;
        ActiveCamera2D = savedActiveCamera;
        ActiveRenderTexture = savedActiveRenderTexture;
    }

    // Composites a render-target container's baked texture at the container's clamped rectangle,
    // honoring the container's blend and group alpha (#3434, item 2). Returns false when there is no
    // valid baked texture (degenerate size) so the caller can render the children directly instead of
    // dropping them. The baked texture is premultiplied, so Normal blend composites with
    // AlphaPremultiply and Additive uses the premultiplied-additive pass; group alpha is applied by
    // tinting every channel (a premultiplied texture must scale rgb and alpha together).
    private bool TryCompositeRenderTarget(IRenderableIpso container)
    {
        IRenderableIpso cacheOwner = ResolveRenderTargetCacheOwner(container);
        RenderTexture2D? renderTexture = _renderTargetService.TryGetExisting(cacheOwner);
        if (renderTexture == null)
        {
            return false;
        }

        ComputeRenderTargetBounds(container, out float left, out float top, out float right,
            out float bottom, out int width, out int height);
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        byte alpha = (byte)container.Alpha;
        Color tint = new Color(alpha, alpha, alpha, alpha);

        BatchDrawCallCounter counter = BatchDrawCallCounter;
        if (IsAdditiveComposite(cacheOwner))
        {
            counter.BeginBlendModeAdditivePremultiplied();
        }
        else
        {
            counter.BeginBlendMode(BlendMode.AlphaPremultiply);
        }

        // Optional post-process shader (ContainerRuntime.RenderTargetEffect / SourceShaderFile,
        // #3465): bind it around the single composite blit so it post-processes the whole baked
        // container, mirroring MonoGame's DrawRenderTargetToScreen. The effect slot lives on the
        // shared IRenderTargetRenderable — here the InvisibleRenderable resolved as the cache owner.
        Raylib_cs.Shader? renderTargetShader =
            (cacheOwner as IRenderTargetRenderable)?.RenderTargetEffect as Raylib_cs.Shader?;
        if (renderTargetShader != null)
        {
            counter.BeginShaderMode(renderTargetShader.Value);
        }

        // Negative source height flips the v range: an RT is stored bottom-up in GL coordinates, so
        // reading it with height -h yields the upright content (same idiom as ShadowBlurRenderer).
        Raylib.DrawTexturePro(
            renderTexture.Value.Texture,
            new Rectangle(0, 0, width, -height),
            new Rectangle(left, top, right - left, bottom - top),
            System.Numerics.Vector2.Zero,
            0,
            tint);

        if (renderTargetShader != null)
        {
            counter.EndShaderMode();
        }
        counter.EndBlendMode();
        return true;
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

    // Whether the container's blend is Additive. The baked texture is premultiplied, so an additive
    // composite must add the premultiplied color directly (see BeginBlendModeAdditivePremultiplied)
    // rather than raylib's BlendMode.Additive, which would multiply by source alpha a second time and
    // render the glow too dim. Non-additive blends composite premultiplied-over for v1.
    private static bool IsAdditiveComposite(IRenderableIpso cacheOwner)
    {
        return cacheOwner is RenderableBase renderable
            && Gum.RenderingLibrary.BlendExtensions.ToBlend(renderable.BlendState)
                == Gum.RenderingLibrary.Blend.Additive;
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
