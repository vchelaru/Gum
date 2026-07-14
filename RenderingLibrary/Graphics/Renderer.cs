#if MONOGAME || XNA || KNI || FNA
#define XNALIKE
#endif
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Content;
using BlendState = Gum.BlendState;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Gum;
using System.Reflection.Emit;
using Microsoft.Xna.Framework;
using System.Linq;
using RenderingLibrary.Math.Geometry;
using Gum.Wireframe;
using System.Diagnostics;

namespace RenderingLibrary.Graphics;

#region RenderStateVariables Class

public class RenderStateVariables
{
    public BlendState BlendState;
    public ColorOperation ColorOperation;
    public bool Filtering;
    public bool Wrap;

    public Rectangle? ClipRectangle;
}

#endregion

public class Renderer : IRenderer
{
    /// <summary>
    /// Whether renderable objects should call Render
    /// on contained children. This is true by default,
    /// results in a hierarchical rendering order.
    /// </summary>
    public static bool RenderUsingHierarchy = true;

    /// <summary>
    /// Orderer used by the main render pass to flatten a layer's renderables into the
    /// <see cref="DrawCommand"/> sequence consumed by the submit phase. Defaults to
    /// <see cref="HierarchicalOrderer.Instance"/>, which preserves the legacy depth-first walk.
    /// Swap in alternative implementations (e.g. batch-grouped) to change main-pass ordering
    /// without touching the renderer.
    /// </summary>
    public static IRenderableOrderer SiblingOrdering { get; set; } = HierarchicalOrderer.Instance;

    /// <summary>
    /// When true (the default), the render walk skips any renderable that falls entirely outside
    /// the active clip rectangle, along with its subtree — avoiding draw and render-state work for
    /// scrolled-off content in ListBoxes, ScrollViewers, etc. (#2998).
    /// <para>
    /// Treat as experimental until validated on real projects: set to false to render all clipped
    /// content if a renderable's visuals intentionally bleed far past its own bounds. Forwards to
    /// the backend-agnostic <see cref="CameraScissorExtensions.CullOffscreenWhenClipped"/>.
    /// </para>
    /// </summary>
    public static bool CullOffscreenWhenClipped
    {
        get => CameraScissorExtensions.CullOffscreenWhenClipped;
        set => CameraScissorExtensions.CullOffscreenWhenClipped = value;
    }

    #region Fields


    List<Layer> _layers = new List<Layer>();
    ReadOnlyCollection<Layer> _layersReadOnly;

#if XNALIKE
    SpriteRenderer spriteRenderer = new SpriteRenderer();
#endif


    RenderStateVariables mRenderStateVariables = new RenderStateVariables();

    GraphicsDevice mGraphicsDevice;

    private RenderTargetService renderTargetService;
    private bool _renderTargetSweepCompletedForHostFrame;
    private bool _allLayersPreRenderedForHostFrame;
    private bool _referencedRenderTargetsCollectedForHostFrame;
    private readonly HashSet<IRenderableIpso> _referencedRenderTargetOwners = new();
    Camera mCamera;

    Texture2D mSinglePixelTexture;
    Texture2D mDottedLineTexture;

    public static object LockObject = new object();


    #endregion

    #region Properties

    internal float CurrentZoom
    {
        get
        {
            return spriteRenderer.CurrentZoom;
        }
        //private set;
    }

    public Layer MainLayer
    {
        get { return _layers[0]; }
    }

    internal List<Layer> LayersWritable
    {
        get
        {
            return _layers;
        }
    }

    public ReadOnlyCollection<Layer> Layers => _layersReadOnly;


    /// <summary>
    /// The texture used to render solid objects. If SinglePixelSourceRectangle is null, the entire texture is used. Otherwise
    /// the portion of SinglePixelTexture is applied.
    /// </summary>
    public Texture2D SinglePixelTexture
    {
        get
        {
#if FULL_DIAGNOSTICS && !TEST
            // This should always be available
            if (mSinglePixelTexture == null)
            {
                throw new InvalidOperationException("The single pixel texture is not set yet.  You must call Renderer.Initialize before accessing this property." +
                    "If running unit tests, be sure to run in UnitTest configuration");
            }
#endif
            return mSinglePixelTexture;
        }
        set
        {
            // Setter added to support rendering from sprite sheet.
            mSinglePixelTexture = value;
        }
    }

    /// <summary>
    /// Returns the SinglePixelTexture if it exists, or null if not. This tolerates nulls, unlike the property.
    /// </summary>
    /// <returns>The SinglePixelTexture if it is not null</returns>
    public Texture2D? TryGetSinglePixelTexture() => mSinglePixelTexture;

    /// <summary>
    /// The rectangle to use when rendering single-pixel texture objects, such as ColoredRectangles.
    /// By default this is null, indicating the entire texture is used.
    /// </summary>
    public Rectangle? SinglePixelSourceRectangle = null;

    public Texture2D DottedLineTexture
    {
        get
        {
#if FULL_DIAGNOSTICS && !TEST
            // This should always be available
            if (mDottedLineTexture == null)
            {
                throw new InvalidOperationException("The dotted line texture is not set yet.  You must call Renderer.Initialize before accessing this property." +
                    "If running unit tests, be sure to run in UnitTest configuration");
            }
#endif
            return mDottedLineTexture;
        }
    }

    internal Texture2D InternalShapesTexture { get; set; }

    public GraphicsDevice GraphicsDevice
    {
        get
        {
            return mGraphicsDevice;
        }
    }

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
            if(SystemManagers.Default == null)
            {
                throw new InvalidOperationException(
                    "The SystemManagers.Default is null. You must either specify the default SystemManagers, or use a custom SystemsManager if your app has multiple SystemManagers.");
            }
            return SystemManagers.Default.Renderer;

        }
    }

    public Camera Camera
    {
        get
        {
            return mCamera;
        }
    }

    public SpriteRenderer SpriteRenderer
    {
        get
        {
            return spriteRenderer;
        }
    }

    /// <summary>
    /// Per-frame counters for render-state changes that <see cref="SpriteRenderer.LastFrameDrawStates"/>
    /// does not capture — currently the Apos.Shapes <c>ShapeBatch</c> begins, which live on a
    /// separate GPU command stream. Reset at the start of each <see cref="Draw(SystemManagers)"/>.
    /// </summary>
    public RenderStateChangeStatistics RenderStateChangeStatistics { get; private set; }

    /// <summary>
    /// Builds a <see cref="DrawStateSummary"/> for the just-completed frame, bucketing
    /// <see cref="SpriteRenderer.LastFrameDrawStates"/> by begin cause (clip / state / texture) and folding in
    /// the frame's Apos.Shapes begin count. Call after <see cref="Draw(SystemManagers)"/> to diagnose what is
    /// driving the SpriteBatch.Begin count — for example, whether a high count is clipping rather than something
    /// <see cref="BatchKeyGroupedOrderer"/> could reduce.
    /// </summary>
    public DrawStateSummary GetDrawStateSummary()
    {
        return DrawStateSummary.FromDrawStates(
            spriteRenderer.LastFrameDrawStates,
            RenderStateChangeStatistics.ShapeBatchBeginCount);
    }

    /// <summary>
    /// Controls which XNA BlendState is used for the Rendering Library's Blend.Normal value.
    /// </summary>
    /// <remarks>
    /// This should be either NonPremultiplied (if textures do not use premultiplied alpha), or
    /// AlphaBlend if using premultiplied alpha textures.
    /// </remarks>
    public static BlendState NormalBlendState
    {
        get;
        set;
    } = BlendState.NonPremultiplied;

    // Used only while baking a render target's children over a transparent clear
    // (RenderToRenderTarget). Color still premultiplies via (SourceAlpha, InverseSourceAlpha),
    // same as NonPremultiplied — but Alpha uses (One, InverseSourceAlpha) instead of
    // NonPremultiplied's (SourceAlpha, InverseSourceAlpha). The latter squares alpha when the
    // destination starts fully transparent (result = srcAlpha*srcAlpha instead of srcAlpha),
    // corrupting the baked texture's alpha channel — e.g. a 50%-alpha child bakes to 25% alpha
    // instead of 50%. This is the standard "premultiply on bake" blend used industry-wide when
    // rendering straight-alpha content onto a transparent render target (#1696).
    private static readonly BlendState _bakeToRenderTargetBlendState = new BlendState
    {
        ColorSourceBlend = Gum.Blend.SourceAlpha,
        ColorDestinationBlend = Gum.Blend.InverseSourceAlpha,
        AlphaSourceBlend = Gum.Blend.One,
        AlphaDestinationBlend = Gum.Blend.InverseSourceAlpha,
    };

    public bool IsUsingPremultipliedAlpha
    {
        get; set;
    }
    = false;

    /// <summary>
    /// Use the custom effect for rendering. This setting takes priority if 
    /// both UseCustomEffectRendering and UseBasicEffectRendering are enabled.
    /// </summary>
    public static bool UseCustomEffectRendering 
    {
        get => RendererSettings.UseCustomEffectRendering;
        set => RendererSettings.UseCustomEffectRendering = value;
    }
    public static bool UseBasicEffectRendering 
    { 
        get => RendererSettings.UseBasicEffectRendering;
        set => RendererSettings.UseBasicEffectRendering = value;
    }
    public static bool UsingEffect => RendererSettings.UsingEffect;

    public static CustomEffectManager CustomEffectManager { get; } = new CustomEffectManager();

    /// <summary>
    /// When this is enabled texture colors will be translated to linear space before 
    /// any other shader operations are performed. This is useful for games with 
    /// lighting and other special shader effects. If the colors are left in gamma 
    /// space the shader calculations will crush the colors and not look like natural 
    /// lighting. Delinearization must be done by the developer in the last render 
    /// step when rendering to the screen. This technique is called gamma correction.
    /// Requires using the custom effect. Disabled by default.
    /// </summary>
    public static bool LinearizeTextures { get; set; }

    // Vic says March 29 2020
    // For some reason the rendering
    // in the tool works differently than
    // in-game. Not sure if this is a DesktopGL
    // vs XNA thing, but I traced it down to the zoom thing.
    // I'm going to add a bool here to control it.
    public static bool ApplyCameraZoomOnWorldTranslation { get; set; } = false;

    public static TextureFilter TextureFilter { get; set; } = TextureFilter.Point;

    /// <summary>
    /// When true (the default), textures loaded from file bleed their edge color into
    /// fully-transparent texels on load (see <see cref="Content.TextureEdgeBleed"/>). This stops the
    /// non-premultiplied pipeline from darkening anti-aliased edges toward black when sampled with
    /// <see cref="Microsoft.Xna.Framework.Graphics.TextureFilter.Linear"/> — most visibly on font
    /// atlases (issue #3691). Only affects the RGB of texels whose alpha is 0, so it is visually a
    /// no-op under Point filtering. Set false to skip the per-load pass.
    /// </summary>
    public static bool BleedTransparentTextureEdgesOnLoad { get; set; } = true;

#endregion

    public Renderer()
    {

        _layers = new List<Layer>();
        _layersReadOnly = new ReadOnlyCollection<Layer>(_layers);
        mCamera = new RenderingLibrary.Camera();
        RenderStateChangeStatistics = new RenderStateChangeStatistics();

    }

    public void Initialize(GraphicsDevice graphicsDevice, SystemManagers managers)
    {
        renderTargetService = new RenderTargetService();

        if (graphicsDevice != null)
        {
            mCamera.ClientWidth = graphicsDevice.Viewport.Width;
            mCamera.ClientHeight = graphicsDevice.Viewport.Height;
            mCamera.ClientLeft = graphicsDevice.Viewport.X;
            mCamera.ClientTop = graphicsDevice.Viewport.Y;
        }

                // for open gl (desktop gl) this should be 0
                // for DirectX it should be 0.5 I believe....
#if DIRECTX_RENDERING
        Camera.PixelPerfectOffsetX = .5f;
        Camera.PixelPerfectOffsetY = .5f;
#else
        Camera.PixelPerfectOffsetX = .0f;
        Camera.PixelPerfectOffsetY = .0f;
#endif


        _layers.Add(new Layer());
        _layers[0].Name = "Main Layer";

        mGraphicsDevice = graphicsDevice;

        spriteRenderer.Initialize(graphicsDevice);
        CustomEffectManager.Initialize(graphicsDevice);

        mSinglePixelTexture = new Texture2D(mGraphicsDevice, 1, 1, false, SurfaceFormat.Color);
        Microsoft.Xna.Framework.Color[] pixels = new Microsoft.Xna.Framework.Color[1];
        pixels[0] = Microsoft.Xna.Framework.Color.White;
        mSinglePixelTexture.SetData<Microsoft.Xna.Framework.Color>(pixels);
        mSinglePixelTexture.Name = "Rendering Library Single Pixel Texture";

        mDottedLineTexture = new Texture2D(mGraphicsDevice, 2, 1, false, SurfaceFormat.Color);
        mDottedLineTexture.Name = "Renderer Dotted Line Texture";
        pixels = new Microsoft.Xna.Framework.Color[2];
        pixels[0] = Microsoft.Xna.Framework.Color.White;
        pixels[1] = Microsoft.Xna.Framework.Color.Transparent;
        mDottedLineTexture.SetData<Microsoft.Xna.Framework.Color>(pixels);

        if (GraphicsDevice != null)
        {
            mCamera.ClientWidth = GraphicsDevice.Viewport.Width;
            mCamera.ClientHeight = GraphicsDevice.Viewport.Height;
            mCamera.ClientLeft = GraphicsDevice.Viewport.X;
            mCamera.ClientTop = GraphicsDevice.Viewport.Y;
        }
    }

    public void Uninitialize()
    {
        mSinglePixelTexture?.Dispose();
        mSinglePixelTexture = null;

        mDottedLineTexture?.Dispose();
        mDottedLineTexture = null;

        CustomEffectManager.Reset();
    }

    #region Add/Remove Layers

    public Layer AddLayer()
    {
        Layer layer = new Layer();
        _layers.Add(layer);
        return layer;
    }

    public void AddLayer(Layer layer) => _layers.Add(layer);

    public void InsertLayer(int index, Layer layer) => _layers.Insert(index, layer);

    public void RemoveLayer(Layer layer) => _layers.Remove(layer);


    //public void AddLayer(SortableLayer sortableLayer, Layer masterLayer)
    //{
    //    if (masterLayer == null)
    //    {
    //        masterLayer = LayersWritable[0];
    //    }

    //    masterLayer.Add(sortableLayer);
    //}

    #endregion

    /// <summary>
    /// Opens a host-frame draw batch for render-target cache lifecycle. Sweeps unused GPU
    /// targets from prior frames exactly once. Hosts normally rely on
    /// <see cref="SystemManagers.Activity"/> advancing time each frame instead of calling
    /// this directly; use it when Gum draws without a preceding Activity pass.
    /// </summary>
    public void BeginFrame()
    {
        lock (LockObject)
        {
            TrySweepUnusedRenderTargetsAtFrameBoundary();
        }
    }

    /// <summary>
    /// Optional frame end for hosts that issue multiple Gum draw batches without advancing
    /// Activity time. Resets the once-per-frame sweep token so the next
    /// <see cref="BeginFrame"/> or draw call can sweep again.
    /// </summary>
    public void EndFrame()
    {
        lock (LockObject)
        {
            _renderTargetSweepCompletedForHostFrame = false;
            _allLayersPreRenderedForHostFrame = false;
            _referencedRenderTargetsCollectedForHostFrame = false;
        }
    }

    internal void NotifyHostFrameAdvanced()
    {
        lock (LockObject)
        {
            _renderTargetSweepCompletedForHostFrame = false;
            _allLayersPreRenderedForHostFrame = false;
            _referencedRenderTargetsCollectedForHostFrame = false;
        }
    }

    private void TrySweepUnusedRenderTargetsAtFrameBoundary()
    {
        if (_renderTargetSweepCompletedForHostFrame)
        {
            return;
        }

        renderTargetService.ClearUnusedRenderTargetsLastFrame();
        _renderTargetSweepCompletedForHostFrame = true;
    }

    /// <summary>
    /// Bakes render targets and binds <see cref="IRenderTargetTextureReferencer"/> textures for
    /// the supplied layers without compositing them. Per-layer hosts (or FRB drawing Gum layers
    /// across multiple draw calls) should call this once per frame with every layer that can
    /// supply a <see cref="IRenderTargetTextureReferencer.RenderTargetTextureSource"/> before
    /// any <see cref="Draw(SystemManagers, Layer)"/> compositing pass.
    /// </summary>
    public void PreRenderLayers(IReadOnlyList<Layer> layers)
    {
        lock (LockObject)
        {
            PreRenderLayersCore(layers);
            _allLayersPreRenderedForHostFrame = true;
        }
    }

    private void TryPreRenderAllLayersForHostFrame()
    {
        if (_allLayersPreRenderedForHostFrame)
        {
            return;
        }

        PreRenderLayersCore(_layers);
        _allLayersPreRenderedForHostFrame = true;
    }

    private void PreRenderLayersCore(IReadOnlyList<Layer> layers)
    {
        EnsureReferencedRenderTargetsCollected();

        for (int i = 0; i < layers.Count; i++)
        {
            SetFilteringForLayer(layers[i]);
            PreRender(layers[i].Renderables);
        }

        for (int i = 0; i < layers.Count; i++)
        {
            SetFilteringForLayer(layers[i]);
            PreRenderWithSourceRenderTargets(layers[i].Renderables);
        }
    }

    private void SetFilteringForLayer(Layer layer)
    {
        if (layer.IsLinearFilteringEnabled != null)
        {
            mRenderStateVariables.Filtering = layer.IsLinearFilteringEnabled.Value;
        }
        else
        {
            mRenderStateVariables.Filtering = TextureFilter == TextureFilter.Linear;
        }
    }

    public void Draw(SystemManagers managers)
    {
        ClearPerformanceRecordingVariables();

        if (managers == null)
        {
            managers = SystemManagers.Default;
        }

        Draw(managers, _layers);

        ForceEnd();
        EndFrame();
    }

    /// <summary>
    /// For integration tests — whether <paramref name="owner"/> still has a cached offscreen
    /// render target in this renderer's <see cref="RenderTargetService"/>.
    /// </summary>
    internal bool HasCachedRenderTarget(IRenderableIpso owner)
    {
        return renderTargetService.HasCachedRenderTarget(owner);
    }

    public void Draw(SystemManagers managers, Layer layer)
    {
        // So that 2 controls don't render at the same time.
        lock (LockObject)
        {
            if (GraphicsDevice != null)
            {
                mCamera.ClientWidth = GraphicsDevice.Viewport.Width;
                mCamera.ClientHeight = GraphicsDevice.Viewport.Height;
                mCamera.ClientLeft = GraphicsDevice.Viewport.X;
                mCamera.ClientTop = GraphicsDevice.Viewport.Y;
            }

            TrySweepUnusedRenderTargetsAtFrameBoundary();

            var oldSampler = GraphicsDevice.SamplerStates[0];

            mRenderStateVariables.BlendState = Renderer.NormalBlendState;
            mRenderStateVariables.Wrap = false;

            TryPreRenderAllLayersForHostFrame();

            SetFilteringForLayer(layer);
            PreRender(layer.Renderables);
            PreRenderWithSourceRenderTargets(layer.Renderables);

            SetFilteringForLayer(layer);
            RenderLayer(managers, layer, prerender:false);

            if (oldSampler != null)
            {
                GraphicsDevice.SamplerStates[0] = oldSampler;
            }
        }
    }

    public void Draw(SystemManagers managers, List<Layer> layers)
    {
        // So that 2 controls don't render at the same time.
        lock (LockObject)
        {
            if (GraphicsDevice != null)
            {
                mCamera.ClientWidth = GraphicsDevice.Viewport.Width;
                mCamera.ClientHeight = GraphicsDevice.Viewport.Height;
                mCamera.ClientLeft = GraphicsDevice.Viewport.X;
                mCamera.ClientTop = GraphicsDevice.Viewport.Y;
            }

            TrySweepUnusedRenderTargetsAtFrameBoundary();


            mRenderStateVariables.BlendState = Renderer.NormalBlendState;
            mRenderStateVariables.Wrap = false;

            PreRenderLayersCore(layers);

            for (int i = 0; i < layers.Count; i++)
            {
                Layer layer = layers[i];
                SetFilteringForLayer(layer);
                RenderLayer(managers, layer, prerender:false);
            }
        }
    }

    void IRenderer.RenderLayer(RenderingLibrary.ISystemManagers managers, RenderingLibrary.Graphics.Layer layer, bool prerender)
    {
        RenderLayer(managers as SystemManagers, layer, prerender);
    }
    


    internal void RenderLayer(SystemManagers managers, Layer layer, bool prerender = true)
    {
        //////////////////Early Out////////////////////////////////
        if (layer.Renderables.Count == 0)
        {
            return;
        }
        ///////////////End Early Out///////////////////////////////

        if (prerender)
        {
            PreRender(layer.Renderables);

            PreRenderWithSourceRenderTargets(layer.Renderables);
        }

        SpriteBatchStack.PerformStartOfLayerRenderingLogic();

        spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Push, mCamera, null);

        layer.SortRenderables();

        // Build phase: flatten the (already Z-sorted) layer into a sequence of DrawCommands.
        // Submit phase: walk that sequence, issuing the actual SpriteBatch / orchestrator
        // calls. Splitting these phases is what #2879's batch-grouped orderer needs — and
        // by itself it produces byte-identical output via HierarchicalOrderer (the default).
        // The recursive Render/Draw helpers below remain in use for the prerender path
        // (RenderToRenderTarget) and the GumBatch immediate-mode path (Renderer.Draw).
        SiblingOrdering.BuildDrawList(layer, _scratchCommands, mCamera);
        Submit(_scratchCommands, managers, layer);

        _batchOrchestrator.FlushAndReset(managers);

#if !NET8_0_OR_GREATER
        spriteRenderer.EndSpriteBatch();
#else
        // The full EndSpriteBatch pop (with its End()/currentParameters reset) is intentionally
        // skipped on NET8+ — the frame-level ForceEnd flushes instead. But the matching
        // BeginSpriteBatch(Push) above still adds a state-stack entry, so without balancing it here
        // mStateStack grows one entry per layer per frame on the Draw(Layer)/Draw(List<Layer>) paths
        // that never run the per-frame ClearPerformanceRecordingVariables reset — an unbounded leak
        // rooted by the long-lived Renderer (#3515). Remove just the pushed entry, preserving the
        // NET8+ currentParameters carry-over that the frame-level flush relies on.
        spriteRenderer.RemoveLastStateStackEntry();
#endif
    }


    // Immediate mode calls:
    public void Begin(Microsoft.Xna.Framework.Matrix? spriteBatchMatrix = null)
    {
        SpriteBatchStack.PerformStartOfLayerRenderingLogic();
        spriteRenderer.ForcedMatrix = spriteBatchMatrix;
        spriteRenderer.BeginSpriteBatch(mRenderStateVariables, _layers[0], BeginType.Push, mCamera, null);
    }


    public void Draw(IRenderableIpso renderable)
    {
        // The layered Draw paths run a full PreRender pass on layer.Renderables before
        // BeginSpriteBatch (see Draw(SystemManagers, Layer) and RenderLayer). That pass is
        // what fires hooks like RenderableShapeBase.PreRender -> AposShapeRuntime.PreRender,
        // which is where the runtime's StrokeWidth (and ScreenPixel resolution) gets pushed
        // onto the contained renderable.
        //
        // The GumBatch entry path (Renderer.Begin/Draw/End) had no equivalent walk, so any
        // GumBatch consumer (FRB2 GumRenderBatch, immediate-mode samples) saw shape runtimes
        // render with their renderable's default values. Invoke the PreRender walk here.
        //
        // We deliberately do NOT do the IsRenderTarget rendering pass here: BeginSpriteBatch
        // has already started the outer SpriteBatch by the time we arrive, and
        // RenderToRenderTarget would change the render target and start its own SpriteBatch
        // cycle, breaking the outer one. Render targets nested inside a GumBatch.Draw tree
        // are not supported on this path.
        InvokePreRenderRecursively(renderable);

        Draw(SystemManagers.Default, _layers[0], renderable, forceRenderHierarchy:false, isPreRender:false);
    }

    private void InvokePreRenderRecursively(IRenderableIpso renderable)
    {
        if (!renderable.Visible && !renderable.IsRenderTarget)
        {
            return;
        }

        renderable.PreRender();

        var children = renderable.Children;
        if (children != null)
        {
            var count = children.Count;
            for (int i = 0; i < count; i++)
            {
                InvokePreRenderRecursively(children[i]);
            }
        }
    }

    public void End()
    {
        spriteRenderer.ForcedMatrix = null;

        // Mirror RenderLayer's end-of-walk: flush any pending custom batch and reset state
        // before ending SpriteBatch. Without this, the next Renderer.Begin cycle inherits
        // the dangling owner/key — causing custom-batch draws (e.g. Apos.Shapes shapes)
        // to leak across cycles and flush at non-deterministic times. This matters most
        // for consumers that do many small Begin/End cycles (FRB2's GumRenderable, the
        // immediate-mode samples).
        _batchOrchestrator.FlushAndReset(SystemManagers.Default);

        spriteRenderer.EndSpriteBatch();
    }


    private void PreRender(IList<IRenderableIpso> renderables)
    {
#if FULL_DIAGNOSTICS
        if (renderables == null)
        {
            throw new ArgumentNullException("renderables");
        }
#endif

        var count = renderables.Count;
        if(count== 0)
        {
            return;
        }

        EnsureReferencedRenderTargetsCollected();

        for (int i = 0; i < count; i++)
        {
            var renderable = renderables[i];
            bool shouldBakeRenderTarget = ShouldRenderToRenderTarget(renderable);
            if (renderable.Visible || shouldBakeRenderTarget)
            {

                renderable.PreRender();

                // Some Gum objects, like GraphicalUiElements, may not have children if the object hasn't
                // yet been assigned a visual. Just skip over it...
                if ((renderable.Visible || shouldBakeRenderTarget) && renderable.Children != null)
                {
                    PreRender(renderable.Children);
                }
                if (shouldBakeRenderTarget)
                {
                    RenderToRenderTarget(renderable, SystemManagers.Default);
                }
            }
        }
    }

    private void EnsureReferencedRenderTargetsCollected()
    {
        if (_referencedRenderTargetsCollectedForHostFrame)
        {
            return;
        }

        _referencedRenderTargetOwners.Clear();
        for (int i = 0; i < _layers.Count; i++)
        {
            CollectReferencedRenderTargets(_layers[i].Renderables);
        }

        _referencedRenderTargetsCollectedForHostFrame = true;
    }

    private void CollectReferencedRenderTargets(IList<IRenderableIpso> renderables)
    {
        int count = renderables.Count;
        for (int i = 0; i < count; i++)
        {
            IRenderableIpso renderable = renderables[i];
            if (renderable.Visible && renderable is IRenderTargetTextureReferencer textureReferencer &&
                textureReferencer.RenderTargetTextureSource != null)
            {
                _referencedRenderTargetOwners.Add(
                    ResolveRenderTargetCacheOwner(textureReferencer.RenderTargetTextureSource));
            }

            if (renderable.Visible && renderable.Children != null)
            {
                CollectReferencedRenderTargets(renderable.Children);
            }
        }
    }

    private bool ShouldRenderToRenderTarget(IRenderableIpso renderable)
    {
        if (!renderable.IsRenderTarget)
        {
            return false;
        }

        if (renderable.Visible)
        {
            return true;
        }

        EnsureReferencedRenderTargetsCollected();
        return _referencedRenderTargetOwners.Contains(ResolveRenderTargetCacheOwner(renderable));
    }

    private void PreRenderWithSourceRenderTargets(IList<IRenderableIpso> renderables)
    {
        var count = renderables.Count;
        if (count == 0)
        {
            return;
        }


        for (int i = 0; i < count; i++)
        {
            var renderable = renderables[i];
            if (renderable.Visible && renderable is IRenderTargetTextureReferencer textureReferencer &&
                textureReferencer.RenderTargetTextureSource != null)
            {
                IRenderableIpso cacheOwner = ResolveRenderTargetCacheOwner(
                    textureReferencer.RenderTargetTextureSource);
                textureReferencer.Texture = renderTargetService.GetExistingRenderTarget(cacheOwner);
            }

            if (renderable.Visible && renderable.Children != null)
            {
                PreRenderWithSourceRenderTargets(renderable.Children);
            }
        }
    }

    private static IRenderableIpso ResolveRenderTargetCacheOwner(IRenderableIpso source)
    {
        if (source is GraphicalUiElement gue && gue.RenderableComponent is IRenderableIpso contained)
        {
            return contained;
        }

        return source;
    }

    GumBatch gumBatch;

    bool hasSaved = false;

    // True only while RenderToRenderTarget's nested Draw call is baking a subtree over a
    // transparent clear. Tells AdjustRenderStates/AdjustNonClipRenderStates to substitute
    // _bakeToRenderTargetBlendState wherever a renderable would otherwise resolve to
    // NormalBlendState (#1696).
    bool _isBakingRenderTarget = false;

    private void RenderToRenderTarget(IRenderableIpso renderable, SystemManagers systemManagers)
    {


        Texture oldRenderTarget = null;

        // RenderTargetCount isn't supported in raw XNA or KNI
        //if (GraphicsDevice.RenderTargetCount > 0)
        //{
            oldRenderTarget = GraphicsDevice.GetRenderTargets().FirstOrDefault().RenderTarget;
        //}
        var oldCameraWidth = Camera.ClientWidth;
        var oldCameraHeight = Camera.ClientHeight;
        var oldCameraClientLeft = Camera.ClientLeft;
        var oldCameraClientTop = Camera.ClientTop;

        var oldCameraX = Camera.X;
        var oldCameraY = Camera.Y;
        var oldViewport = GraphicsDevice.Viewport;

        // Cache key must match what PreRenderWithSourceRenderTargets resolves for a
        // RenderTargetTextureSource reference (#3451) — always the contained renderable, not the
        // GraphicalUiElement wrapper a nested container is walked as (#816). Unresolved, a nested
        // source bakes under the wrapper while the referencing Sprite looks up the raw renderable
        // and misses.
        var renderTarget = renderTargetService.GetRenderTargetFor(
            GraphicsDevice, ResolveRenderTargetCacheOwner(renderable), Camera);

        if(renderTarget != null)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);

            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);

            var oldX = renderable.GetAbsoluteLeft();
            var oldY = renderable.GetAbsoluteTop();

            var cameraLeft = Camera.AbsoluteLeft;
            var cameraTop = Camera.AbsoluteTop;
            

            Camera.ClientWidth = (int)renderTarget.Width;
            Camera.ClientHeight = (int)renderTarget.Height;
            Camera.ClientLeft = 0;
            Camera.ClientTop = 0;

            var left = System.Math.Max(cameraLeft, oldX);
            var top = System.Math.Max(cameraTop, oldY);

            Camera.X = left;
            Camera.Y = top;
            if(Camera.CameraCenterOnScreen == CameraCenterOnScreen.Center)
            {
                Camera.X += (renderTarget.Width / 2.0f)/Camera.Zoom;
                Camera.Y += (renderTarget.Height / 2.0f) / Camera.Zoom;
            }

            // Internally the sprite rendering system snaps to the pixel, so we need to do the same thing:

            var effectivePixelOffsetX = Camera.PixelPerfectOffsetX;
            var effectivePixelOffsetY = Camera.PixelPerfectOffsetY;

            Camera.X = Math.MathFunctions.RoundToInt(Camera.X * CurrentZoom) / CurrentZoom + effectivePixelOffsetX / CurrentZoom;
            Camera.Y = Math.MathFunctions.RoundToInt(Camera.Y * CurrentZoom) / CurrentZoom + effectivePixelOffsetY / CurrentZoom;


            gumBatch = gumBatch ?? new GumBatch();


            // todo  - rotations don't currently work:
            //var rotationRadians = MathHelper.ToRadians(renderable.Rotation);
            //var matrix = Matrix.CreateRotationZ(rotationRadians);
            //gumBatch.Begin(matrix);
            gumBatch.Begin();


            //gumBatch.Draw(renderable);
            //systemManagers.Renderer.Draw(renderable);
            // Children with the default (unconfigured) blend would otherwise resolve to
            // NormalBlendState, whose alpha factors square alpha over the transparent clear
            // above. _isBakingRenderTarget tells AdjustRenderStates/AdjustNonClipRenderStates to
            // substitute the bake-safe blend for that case instead (#1696). A child with an
            // explicitly custom BlendState is unaffected, same as the composite-back override.
            _isBakingRenderTarget = true;
            Draw(systemManagers, _layers[0], renderable, forceRenderHierarchy:true, isPreRender:true);
            _isBakingRenderTarget = false;

            gumBatch.End();
            GraphicsDevice.SetRenderTarget(oldRenderTarget as RenderTarget2D);

#if DEBUG
            if(!hasSaved)
            {
                hasSaved = true;
                // Uncomment this to test saving...
                //if (!System.IO.File.Exists("Output.png"))
                //{
                //    using var stream = System.IO.File.OpenWrite("Output.png");
                //    renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);
                //}
            }
#endif

            Camera.ClientWidth = oldCameraWidth;
            Camera.ClientHeight = oldCameraHeight;
            Camera.X = oldCameraX;
            Camera.Y = oldCameraY;
            Camera.ClientLeft = oldCameraClientLeft;
            Camera.ClientTop = oldCameraClientTop;

            GraphicsDevice.Viewport = oldViewport;

            // Uncomment this to test saving...
            //if (!System.IO.File.Exists("Output.png"))
            //{
            //    using var stream = System.IO.File.OpenWrite("Output.png");
            //    renderTarget.SaveAsPng(stream, renderTarget.Width, renderTarget.Height);
            //}

        }

    }

    private void Render(IList<IRenderableIpso> whatToRender, SystemManagers managers, Layer layer, bool isPreRender)
    {
        var count = whatToRender.Count;
        for (int i = 0; i < count; i++)
        {
            var renderable = whatToRender[i];
            Draw(managers, layer, renderable, forceRenderHierarchy:false, isPreRender:isPreRender);
        }
    }


    Sprite renderTargetRenderableSprite = new Sprite((Texture2D)null);

    readonly BatchOrchestrator _batchOrchestrator = new();

    // Reused across layers and frames so the build phase does not allocate per frame
    // after warm-up. The renderer owns the buffer; the orderer writes into it.
    readonly List<DrawCommand> _scratchCommands = new List<DrawCommand>();

    // Tracks clip state during Submit so EndClip can restore the rect that BeginClip saw.
    // Balanced by construction (the orderer always emits matched BeginClip/EndClip pairs),
    // so this is empty at the end of every Submit call.
    readonly Stack<Rectangle?> _clipScopeStack = new Stack<Rectangle?>();

    private void Draw(SystemManagers managers, Layer layer, IRenderableIpso renderable, bool forceRenderHierarchy, bool isPreRender)
    {
        if (renderable.Visible || ( renderable.IsRenderTarget && isPreRender))
        {
            var oldClip = mRenderStateVariables.ClipRectangle;
            AdjustRenderStates(mRenderStateVariables, layer, renderable, managers);
            bool didClipChange = oldClip != mRenderStateVariables.ClipRectangle;

            if (renderable.IsRenderTarget && !forceRenderHierarchy)
            {
                // Resolved cache key — see the matching comment in RenderToRenderTarget (#3451).
                var renderTarget = renderTargetService.GetRenderTargetFor(
                    GraphicsDevice, ResolveRenderTargetCacheOwner(renderable), Camera);

                if(renderTarget != null)
                {
                    DrawRenderTargetToScreen(renderable, renderTarget, managers, layer);
                }
            }
            else
            {
                _batchOrchestrator.OnRenderable(renderable, managers);

                renderable.Render(managers);


                if (RenderUsingHierarchy)
                {
                    Render(renderable.Children, managers, layer, isPreRender);
                }
            }

            if (didClipChange)
            {
                mRenderStateVariables.ClipRectangle = oldClip;

                _batchOrchestrator.FlushAndReset(managers);

                spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Begin, mCamera, $"Un-set {renderable} Clip");
            }
        }
    }

    // GetScissorRectangleFor moved to Camera.cs as an extension method
    // (CameraScissorExtensions.GetScissorRectangleFor) so Raylib/Sokol/Skia
    // backends can share the world->screen scissor math.


    /// <summary>
    /// Main-pass submit phase: walks the flat <see cref="DrawCommand"/> list produced by
    /// <see cref="SiblingOrdering"/> and issues the corresponding SpriteBatch / orchestrator
    /// calls. The orderer is responsible for visibility, ClipsChildren bracketing, and
    /// hierarchy traversal; this method only translates commands into device calls.
    /// </summary>
    private void Submit(IReadOnlyList<DrawCommand> commands, SystemManagers managers, Layer layer)
    {
        int count = commands.Count;
        for (int i = 0; i < count; i++)
        {
            DrawCommand command = commands[i];
            switch (command.Kind)
            {
                case DrawCommandKind.BeginClip:
                    BeginClipScope(layer, command.Target, managers);
                    break;
                case DrawCommandKind.DrawRenderable:
                    SubmitDrawRenderable(command.Target, managers, layer);
                    break;
                case DrawCommandKind.EndClip:
                    EndClipScope(layer, command.Target, managers);
                    break;
            }
        }
    }

    /// <summary>
    /// Blits a render-target container's cached texture to the screen. When the container has a
    /// <see cref="RenderableBase.RenderTargetEffect"/>, that shader is bound for this single
    /// draw so it post-processes the whole container; otherwise the texture is drawn normally.
    /// </summary>
    private void DrawRenderTargetToScreen(IRenderableIpso renderable, RenderTarget2D renderTarget, SystemManagers managers, Layer layer)
    {
        var renderableAlpha = renderable.Alpha;
        renderableAlpha = System.Math.Min(255, renderableAlpha);
        renderableAlpha = System.Math.Max(0, renderableAlpha);

        // RenderToRenderTarget always bakes children over a transparent clear, so the target's
        // contents are premultiplied regardless of Renderer.NormalBlendState. A straight-white
        // tint would re-lighten group alpha on top of that, so the tint must be premultiplied too
        // (#1696) — otherwise group alpha renders too light relative to the same content drawn
        // directly.
        var color = System.Drawing.Color.FromArgb(renderableAlpha, renderableAlpha, renderableAlpha, renderableAlpha);

        renderTargetRenderableSprite.X = System.Math.Max(renderable.GetAbsoluteX(), Camera.AbsoluteLeft);
        renderTargetRenderableSprite.Y = System.Math.Max(renderable.GetAbsoluteY(), Camera.AbsoluteTop);
        renderTargetRenderableSprite.Width = renderTarget.Width / Camera.Zoom;
        renderTargetRenderableSprite.Height = renderTarget.Height / Camera.Zoom;

        // The main-pass walk yields the contained renderable for a top-level render target, but the
        // GraphicalUiElement wrapper for a nested one. The effect slot lives on the shared
        // IRenderTargetRenderable interface (implemented by the runtime's RenderableBase containers
        // AND the editor's LineRectangle containers), so read it directly off the renderable, or off
        // the wrapper's contained RenderableComponent.
        var renderTargetEffect = ((renderable as IRenderTargetRenderable)
            ?? ((renderable as Gum.Wireframe.GraphicalUiElement)?.RenderableComponent as IRenderTargetRenderable))
            ?.RenderTargetEffect as Effect;

        // The blit must composite with a premultiplied blend since the target's contents are
        // premultiplied by construction (see the `color` comment above). Only override the
        // container's own default (unconfigured) blend — a container with an explicitly-configured
        // custom BlendState (e.g. Additive) is left alone; making that combination correct against
        // premultiplied content is that container's own concern, not this composite step's (#1696).
        // A nested render target composites into its parent's bake while _isBakingRenderTarget is
        // still true for that outer bake, so an unconfigured nested container's ambient blend has
        // already been substituted to _bakeToRenderTargetBlendState by AdjustRenderStates — treat
        // that the same as NormalBlendState here.
        bool needsPremultipliedBlend = mRenderStateVariables.BlendState == Renderer.NormalBlendState
            || mRenderStateVariables.BlendState == _bakeToRenderTargetBlendState;

        if (renderTargetEffect == null && !needsPremultipliedBlend)
        {
            Sprite.Render(managers, spriteRenderer, renderTargetRenderableSprite, renderTarget, color, rotationInDegrees: renderable.Rotation, objectCausingRendering: renderable);
        }
        else
        {
            // Bind the container's post-process shader and/or force a premultiplied blend for just
            // this blit. Flush whatever batch is open (sprites or a custom Apos.Shapes batch) so
            // prior draws paint first, re-begin the SpriteBatch with the override(s), draw the
            // target, then re-begin with the normal state so following renderables are unaffected.
            // Mirrors the mid-walk clip-change flush in Draw/AdjustRenderStates.
            _batchOrchestrator.FlushAndReset(managers);

            var previousBlendState = mRenderStateVariables.BlendState;
            if (needsPremultipliedBlend)
            {
                mRenderStateVariables.BlendState = BlendState.AlphaBlend;
            }

            spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Begin, mCamera, renderable, effectOverride: renderTargetEffect);
            Sprite.Render(managers, spriteRenderer, renderTargetRenderableSprite, renderTarget, color, rotationInDegrees: renderable.Rotation, objectCausingRendering: renderable);

            mRenderStateVariables.BlendState = previousBlendState;
            spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Begin, mCamera, renderable);
        }
    }

    private void SubmitDrawRenderable(IRenderableIpso renderable, SystemManagers managers, Layer layer)
    {
        // Non-clip state (blend / color / wrap) is reapplied here. Clip-scope handling is
        // factored out into BeginClipScope/EndClipScope, which Submit invokes via the
        // BeginClip / EndClip commands emitted by the orderer.
        AdjustNonClipRenderStates(mRenderStateVariables, layer, renderable, managers);

        if (renderable.IsRenderTarget)
        {
            // Main pass: draw the cached texture baked by the prerender phase. We never
            // call SetRenderTarget here — that only happens in PreRender / RenderToRenderTarget.
            // Resolved cache key — see the matching comment in RenderToRenderTarget (#3451).
            var renderTarget = renderTargetService.GetRenderTargetFor(
                GraphicsDevice, ResolveRenderTargetCacheOwner(renderable), Camera);

            if (renderTarget != null)
            {
                DrawRenderTargetToScreen(renderable, renderTarget, managers, layer);
            }
        }
        else
        {
            _batchOrchestrator.OnRenderable(renderable, managers);
            renderable.Render(managers);
        }
    }

    private void BeginClipScope(Layer layer, IRenderableIpso renderable, SystemManagers managers)
    {
        _clipScopeStack.Push(mRenderStateVariables.ClipRectangle);

        var clipRectangle = Camera.GetScissorRectangleFor(layer, renderable);
        var adjustedRectangle = mRenderStateVariables.ClipRectangle != null
            ? Rectangle.Intersect(clipRectangle, mRenderStateVariables.ClipRectangle.Value)
            : clipRectangle;

        if (mRenderStateVariables.ClipRectangle == null || adjustedRectangle != mRenderStateVariables.ClipRectangle.Value)
        {
            mRenderStateVariables.ClipRectangle = adjustedRectangle;

            // Mirror the clip-exit flush in EndClipScope: end any in-flight custom batch
            // before restarting SpriteBatch with the new scissor. Without this, an
            // Apos.Shapes batch opened by an earlier sibling stays open with stale scissor
            // state. See .claude/skills/gum-monogame-rendering "Mid-Walk Scissor Change
            // Must Flush the Open Custom Batch".
            _batchOrchestrator.FlushAndReset(managers);

            spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Begin, mCamera, renderable);
        }
    }

    // Diagnostic label for the clip-exit BeginSpriteBatch. A shared constant instead of a per-frame
    // interpolation ($"Un-set {renderable} Clip") — the latter allocated a fresh string every clip
    // exit, one of the idle Draw allocation sources (#1934). DrawStateSummary.FromDrawStates only
    // checks that ObjectChangingState is a string (not its content) to bucket a begin as a clip exit,
    // so a constant preserves that categorization exactly.
    private const string ClipExitStateLabel = "Un-set Clip";

    private void EndClipScope(Layer layer, IRenderableIpso renderable, SystemManagers managers)
    {
        var previousClip = _clipScopeStack.Pop();
        if (previousClip != mRenderStateVariables.ClipRectangle)
        {
            mRenderStateVariables.ClipRectangle = previousClip;

            _batchOrchestrator.FlushAndReset(managers);

            spriteRenderer.BeginSpriteBatch(mRenderStateVariables, layer, BeginType.Begin, mCamera, ClipExitStateLabel);
        }
    }

    private void AdjustNonClipRenderStates(RenderStateVariables renderState, Layer layer, IRenderableIpso renderable, SystemManagers managers)
    {
        BlendState renderBlendState = renderable.BlendState ?? Renderer.NormalBlendState;
        if (_isBakingRenderTarget && renderBlendState == Renderer.NormalBlendState)
        {
            renderBlendState = _bakeToRenderTargetBlendState;
        }
        bool wrap = renderable.Wrap;
        bool shouldResetStates = false;

        if (renderState.BlendState != renderBlendState)
        {
            renderState.BlendState = renderBlendState;
            shouldResetStates = true;
        }

        if (renderState.ColorOperation != renderable.ColorOperation)
        {
            renderState.ColorOperation = renderable.ColorOperation;
            shouldResetStates = true;
        }

        if (renderState.Wrap != wrap)
        {
            renderState.Wrap = wrap;
            shouldResetStates = true;
        }

        if (shouldResetStates)
        {
            spriteRenderer.BeginSpriteBatch(renderState, layer, BeginType.Begin, mCamera, renderable);
        }
    }

    private void AdjustRenderStates(RenderStateVariables renderState, Layer layer, IRenderableIpso renderable, SystemManagers managers)
    {
        BlendState renderBlendState = renderable.BlendState;
        bool wrap = renderable.Wrap;
        bool shouldResetStates = false;
        bool didClipChange = false;

        if (renderBlendState == null)
        {
            renderBlendState = Renderer.NormalBlendState;
        }
        if (_isBakingRenderTarget && renderBlendState == Renderer.NormalBlendState)
        {
            renderBlendState = _bakeToRenderTargetBlendState;
        }
        if (renderState.BlendState != renderBlendState)
        {
            // This used to set this, but not sure why...I think it should set the renderBlendState:
            //renderState.BlendState = renderable.BlendState;
            renderState.BlendState = renderBlendState;

            shouldResetStates = true;

        }

        if(renderState.ColorOperation != renderable.ColorOperation)
        {
            renderState.ColorOperation = renderable.ColorOperation;
            shouldResetStates = true;
        }

        if (renderState.Wrap != wrap)
        {
            renderState.Wrap = wrap;
            shouldResetStates = true;
        }

        if (renderable.ClipsChildren)
        {
            var clipRectangle = Camera.GetScissorRectangleFor(layer, renderable);

            if (renderState.ClipRectangle == null || clipRectangle != renderState.ClipRectangle.Value)
            {
                var adjustedRectangle = renderState.ClipRectangle != null
                    ? Rectangle.Intersect(clipRectangle, renderState.ClipRectangle.Value)
                    : clipRectangle;

                renderState.ClipRectangle = adjustedRectangle;
                shouldResetStates = true;
                didClipChange = true;
            }

        }


        if (shouldResetStates)
        {
            // Mirror the clip-exit flush in Draw: end any in-flight custom batch before
            // restarting SpriteBatch with the new scissor. Without this, an Apos.Shapes
            // batch opened earlier in the walk (e.g. a sibling scrollbar thumb) stays
            // open; the first shape descendant inside the clip has a matching BatchKey
            // so OnRenderable is a no-op and its draw queues into the stale-scissor
            // ShapeBatch, bleeding past the clip. Clip-only: blend / color / wrap don't
            // propagate to ShapeBatch state (each sb.Begin captures its own).
            //
            // Contract: BatchOrchestratorTests.ShapeAfterFlush_FiresFreshStartBatch_
            // EvenWhenKeyMatchesPreFlushKey. Architectural rationale + pitfall checklist:
            // .claude/skills/gum-monogame-rendering/SKILL.md "Mid-Walk Scissor Change
            // Must Flush the Open Custom Batch".
            if (didClipChange)
            {
                _batchOrchestrator.FlushAndReset(managers);
            }
            spriteRenderer.BeginSpriteBatch(renderState, layer, BeginType.Begin, mCamera, renderable);
        }
    }

    // ConstrainRectangle removed — replaced with System.Drawing.Rectangle.Intersect,
    // which has identical math for the overlapping case and returns Rectangle.Empty
    // (rather than negative dims) when rects don't overlap.

    // Made public to allow custom renderable objects to be removed:
    public void RemoveRenderable(IRenderableIpso renderable)
    {
        foreach (Layer layer in this.Layers)
        {
            if (layer.Renderables.Contains(renderable))
            {
                layer.Remove(renderable);
            }
        }
    }

    //public void RemoveLayer(SortableLayer sortableLayer)
    //{
    //    RemoveRenderable(sortableLayer);
    //}

    public void ClearPerformanceRecordingVariables()
    {
        spriteRenderer.ClearPerformanceRecordingVariables();
        RenderStateChangeStatistics.Reset();
    }

    /// <summary>
    /// Ends the current SpriteBatchif it hasn't yet been ended. This is needed for projects which may need the
    /// rendering to end itself so that they can start sprite batch.
    /// </summary>
    public void ForceEnd()
    {
        this.spriteRenderer.End();

    }

    public override bool Equals(object? obj)
    {
        return obj is Renderer renderer &&
               EqualityComparer<ReadOnlyCollection<Layer>>.Default.Equals(_layersReadOnly, renderer._layersReadOnly);
    }
}

#region RenderTargetService

class RenderTargetService : RenderTargetServiceBase<RenderTarget2D>
{
    private GraphicsDevice? _graphicsDeviceForCreate;

    protected override RenderTarget2D Create(int width, int height)
    {
        // _graphicsDeviceForCreate is staged by GetRenderTargetFor before delegating to the
        // base — RenderTarget2D's ctor requires a GraphicsDevice that the generic base can't
        // know about. Cleared after Create runs so a stale reference can't be reused.
        var device = _graphicsDeviceForCreate
            ?? throw new System.InvalidOperationException(
                "GraphicsDevice was not staged before Create — use GetRenderTargetFor.");
        return new RenderTarget2D(device, width, height);
    }

    protected override void Destroy(RenderTarget2D renderTarget) => renderTarget.Dispose();

    protected override int GetWidth(RenderTarget2D renderTarget) => renderTarget.Width;

    protected override int GetHeight(RenderTarget2D renderTarget) => renderTarget.Height;

    public RenderTarget2D? GetExistingRenderTarget(IRenderableIpso renderable)
    {
        var renderTarget = TryGetExisting(renderable);
        return renderTarget is { IsDisposed: false } ? renderTarget : null;
    }

    public RenderTarget2D? GetRenderTargetFor(GraphicsDevice graphicsDevice, IRenderableIpso renderable, Camera camera)
    {
        // Shared clamp+size helper (#3478) — the same one raylib's bake/composite path uses — so the
        // camera-visible-bounds math can't drift between backends. GetFor returns null for a
        // non-positive size (off-camera / degenerate), which the callers treat as "render nothing".
        var bounds = camera.GetRenderTargetBounds(renderable);

        _graphicsDeviceForCreate = graphicsDevice;
        try
        {
            return GetFor(renderable, bounds.Width, bounds.Height);
        }
        finally
        {
            _graphicsDeviceForCreate = null;
        }
    }
}

#endregion

#region GumBatch

public class GumBatch
{
    enum GumBatchState
    {
        NotRendering,
        BeginCalled
    }


    GumBatchState State;
    SystemManagers systemManagers;
    Text internalTextForRendering;

    /// <summary>
    /// The underlying MonoGame <see cref="SpriteBatch"/> that this GumBatch wraps. Use this
    /// to issue your own SpriteBatch draw calls between <see cref="Begin"/> and <see cref="End"/>
    /// without having to manage a separate SpriteBatch. The instance is stable for the lifetime
    /// of this GumBatch; Gum mutates its state (clip regions, blend, etc.) but never swaps the
    /// instance mid-frame. Callers are responsible for any state interaction with Gum's own draws.
    /// </summary>
    public SpriteBatch SpriteBatch => systemManagers.Renderer.SpriteRenderer.SpriteBatch;

    public GumBatch()
    {
        if(SystemManagers .Default == null)
        {
            throw new InvalidOperationException("SystemManagers is null - did you remember to initialize Gum?");
        }
        systemManagers = SystemManagers.Default;
        internalTextForRendering = new Text(systemManagers);
    }

    public void Begin(Matrix? spriteBatchMatrix = null)
    {
        if(State == GumBatchState.BeginCalled)
        {
            throw new InvalidOperationException("Begin has already been called. You must call End before calling Begin again.");
        }

        State = GumBatchState.BeginCalled;

        systemManagers.Renderer.Camera.ClientWidth = systemManagers.Renderer.GraphicsDevice.Viewport.Width;
        systemManagers.Renderer.Camera.ClientHeight = systemManagers.Renderer.GraphicsDevice.Viewport.Height;
        systemManagers.Renderer.Camera.ClientLeft = systemManagers.Renderer.GraphicsDevice.Viewport.X;
        systemManagers.Renderer.Camera.ClientTop = systemManagers.Renderer.GraphicsDevice.Viewport.Y;

        systemManagers.Renderer.Begin(spriteBatchMatrix);
    }

    public void DrawString(BitmapFont font, string text, Microsoft.Xna.Framework.Vector2 position, Microsoft.Xna.Framework.Color color)
    {
        if (State == GumBatchState.NotRendering)
        {
            throw new InvalidOperationException("You must call Begin before calling DrawString");
        }

        internalTextForRendering.BitmapFont = font;
        internalTextForRendering.Width = null;
        internalTextForRendering.RawText = text;
        internalTextForRendering.X = position.X;
        internalTextForRendering.Y = position.Y;
        internalTextForRendering.Color = color.ToSystemDrawing();
        Draw(internalTextForRendering);
    }

    public void Draw(IRenderableIpso renderable)
    {
        if(State == GumBatchState.NotRendering)
        {
            throw new InvalidOperationException("You must call Begin before calling Draw");
        }

        systemManagers.Renderer.Draw(renderable);
    }

    public void End()
    {
        if(State == GumBatchState.NotRendering)
        {
            throw new InvalidOperationException("You must call Begin before calling End");
        }
        State = GumBatchState.NotRendering;

        systemManagers.Renderer.End();
    }


}

#endregion

#region Custom effect support

/// <summary>
/// Manages custom effects from the custom shader file. Main purposes:
/// <list type="number">
/// <item><description>Caches effect parameters and techniques to avoid lookups during rendering.</description></item>
/// <item><description>Handles compatibility between old and new effect specifications with automatic fallback.</description></item>
/// <item><description>Provides methods to retrieve techniques based on:
/// <list type="bullet">
/// <item><description>Texture filtering (Point/Linear)</description></item>
/// <item><description>Color source (VertexColor/ColorModifier)</description></item>
/// <item><description>Color operation (Add, Subtract, Modulate, etc.)</description></item>
/// <item><description>Gamma correction (Linearize)</description></item>
/// </list>
/// </description></item>
/// </list>
/// This class is designed for use by renderers and custom graphics code.
/// </summary>
public class CustomEffectManager
{
    Effect _effect = null!;

    // Cached effect members to avoid list lookups while rendering
    public EffectParameter ParameterCurrentTexture = null!;
    public EffectParameter ParameterViewProj = null!;
    public EffectParameter? ParameterColorModifier;

    bool _effectHasNewformat;

    EffectTechnique? _techniqueTexture;
    EffectTechnique? _techniqueAdd;
    EffectTechnique? _techniqueSubtract;
    EffectTechnique? _techniqueModulate;
    EffectTechnique? _techniqueModulate2X;
    EffectTechnique? _techniqueModulate4X;
    EffectTechnique? _techniqueInverseTexture;
    EffectTechnique? _techniqueColor;
    EffectTechnique? _techniqueColorTextureAlpha;
    EffectTechnique? _techniqueInterpolateColor;

    EffectTechnique? _techniqueTexture_CM;
    EffectTechnique? _techniqueAdd_CM;
    EffectTechnique? _techniqueSubtract_CM;
    EffectTechnique? _techniqueModulate_CM;
    EffectTechnique? _techniqueModulate2X_CM;
    EffectTechnique? _techniqueModulate4X_CM;
    EffectTechnique? _techniqueInverseTexture_CM;
    EffectTechnique? _techniqueColor_CM;
    EffectTechnique? _techniqueColorTextureAlpha_CM;
    EffectTechnique? _techniqueInterpolateColor_CM;

    EffectTechnique? _techniqueTexture_LN;
    EffectTechnique? _techniqueAdd_LN;
    EffectTechnique? _techniqueSubtract_LN;
    EffectTechnique? _techniqueModulate_LN;
    EffectTechnique? _techniqueModulate2X_LN;
    EffectTechnique? _techniqueModulate4X_LN;
    EffectTechnique? _techniqueInverseTexture_LN;
    EffectTechnique? _techniqueColor_LN;
    EffectTechnique? _techniqueColorTextureAlpha_LN;
    EffectTechnique? _techniqueInterpolateColor_LN;

    EffectTechnique? _techniqueTexture_LN_CM;
    EffectTechnique? _techniqueAdd_LN_CM;
    EffectTechnique? _techniqueSubtract_LN_CM;
    EffectTechnique? _techniqueModulate_LN_CM;
    EffectTechnique? _techniqueModulate2X_LN_CM;
    EffectTechnique? _techniqueModulate4X_LN_CM;
    EffectTechnique? _techniqueInverseTexture_LN_CM;
    EffectTechnique? _techniqueColor_LN_CM;
    EffectTechnique? _techniqueColorTextureAlpha_LN_CM;
    EffectTechnique? _techniqueInterpolateColor_LN_CM;

    EffectTechnique? _techniqueTexture_Linear;
    EffectTechnique? _techniqueAdd_Linear;
    EffectTechnique? _techniqueSubtract_Linear;
    EffectTechnique? _techniqueModulate_Linear;
    EffectTechnique? _techniqueModulate2X_Linear;
    EffectTechnique? _techniqueModulate4X_Linear;
    EffectTechnique? _techniqueInverseTexture_Linear;
    EffectTechnique? _techniqueColor_Linear;
    EffectTechnique? _techniqueColorTextureAlpha_Linear;
    EffectTechnique? _techniqueInterpolateColor_Linear;

    EffectTechnique? _techniqueTexture_Linear_CM;
    EffectTechnique? _techniqueAdd_Linear_CM;
    EffectTechnique? _techniqueSubtract_Linear_CM;
    EffectTechnique? _techniqueModulate_Linear_CM;
    EffectTechnique? _techniqueModulate2X_Linear_CM;
    EffectTechnique? _techniqueModulate4X_Linear_CM;
    EffectTechnique? _techniqueInverseTexture_Linear_CM;
    EffectTechnique? _techniqueColor_Linear_CM;
    EffectTechnique? _techniqueColorTextureAlpha_Linear_CM;
    EffectTechnique? _techniqueInterpolateColor_Linear_CM;

    EffectTechnique? _techniqueTexture_Linear_LN;
    EffectTechnique? _techniqueAdd_Linear_LN;
    EffectTechnique? _techniqueSubtract_Linear_LN;
    EffectTechnique? _techniqueModulate_Linear_LN;
    EffectTechnique? _techniqueModulate2X_Linear_LN;
    EffectTechnique? _techniqueModulate4X_Linear_LN;
    EffectTechnique? _techniqueInverseTexture_Linear_LN;
    EffectTechnique? _techniqueColor_Linear_LN;
    EffectTechnique? _techniqueColorTextureAlpha_Linear_LN;
    EffectTechnique? _techniqueInterpolateColor_Linear_LN;

    EffectTechnique? _techniqueTexture_Linear_LN_CM;
    EffectTechnique? _techniqueAdd_Linear_LN_CM;
    EffectTechnique? _techniqueSubtract_Linear_LN_CM;
    EffectTechnique? _techniqueModulate_Linear_LN_CM;
    EffectTechnique? _techniqueModulate2X_Linear_LN_CM;
    EffectTechnique? _techniqueModulate4X_Linear_LN_CM;
    EffectTechnique? _techniqueInverseTexture_Linear_LN_CM;
    EffectTechnique? _techniqueColor_Linear_LN_CM;
    EffectTechnique? _techniqueColorTextureAlpha_Linear_LN_CM;
    EffectTechnique? _techniqueInterpolateColor_Linear_LN_CM;

    public Effect Effect
    {
        get { return _effect; }
        set
        {
            _effect = value;

            var parameterViewProj = GetParameterSafe("ViewProj");
            if (parameterViewProj == null) // ViewProj is required. Throw exception if null.
            {
                throw new InvalidOperationException("Shader.xnb must contain a parameter called ViewProj.");
            }

            ParameterViewProj = parameterViewProj;

            var parameterCurrentTexture = GetParameterSafe("CurrentTexture");
            if (parameterCurrentTexture == null) // CurrentTexture is required. Throw exception if null.
            {
                throw new InvalidOperationException("Shader.xnb must contain a parameter called CurrentTexture.");
            }

            ParameterCurrentTexture = parameterCurrentTexture;

            ParameterColorModifier = GetParameterSafe("ColorModifier");

            // Let's check if the shader has the new format (which includes
            // separate versions of techniques for Point and Linear filtering).
            // We try to cache the first technique in order to do so.
            _techniqueTexture = GetTechniqueSafe("Texture_Point");

            if (_techniqueTexture != null)
            {
                _effectHasNewformat = true;

                //_techniqueTexture = GetTechniqueSafe("Texture_Point"); // This has been already cached
                _techniqueAdd = GetTechniqueSafe("Add_Point");
                _techniqueSubtract = GetTechniqueSafe("Subtract_Point");
                _techniqueModulate = GetTechniqueSafe("Modulate_Point");
                _techniqueModulate2X = GetTechniqueSafe("Modulate2X_Point");
                _techniqueModulate4X = GetTechniqueSafe("Modulate4X_Point");
                _techniqueInverseTexture = GetTechniqueSafe("InverseTexture_Point");
                _techniqueColor = GetTechniqueSafe("Color_Point");
                _techniqueColorTextureAlpha = GetTechniqueSafe("ColorTextureAlpha_Point");
                _techniqueInterpolateColor = GetTechniqueSafe("InterpolateColor_Point");

                _techniqueTexture_CM = GetTechniqueSafe("Texture_Point_CM");
                _techniqueAdd_CM = GetTechniqueSafe("Add_Point_CM");
                _techniqueSubtract_CM = GetTechniqueSafe("Subtract_Point_CM");
                _techniqueModulate_CM = GetTechniqueSafe("Modulate_Point_CM");
                _techniqueModulate2X_CM = GetTechniqueSafe("Modulate2X_Point_CM");
                _techniqueModulate4X_CM = GetTechniqueSafe("Modulate4X_Point_CM");
                _techniqueInverseTexture_CM = GetTechniqueSafe("InverseTexture_Point_CM");
                _techniqueColor_CM = GetTechniqueSafe("Color_Point_CM");
                _techniqueColorTextureAlpha_CM = GetTechniqueSafe("ColorTextureAlpha_Point_CM");
                _techniqueInterpolateColor_CM = GetTechniqueSafe("InterpolateColor_Point_CM");

                _techniqueTexture_LN = GetTechniqueSafe("Texture_Point_LN");
                _techniqueAdd_LN = GetTechniqueSafe("Add_Point_LN");
                _techniqueSubtract_LN = GetTechniqueSafe("Subtract_Point_LN");
                _techniqueModulate_LN = GetTechniqueSafe("Modulate_Point_LN");
                _techniqueModulate2X_LN = GetTechniqueSafe("Modulate2X_Point_LN");
                _techniqueModulate4X_LN = GetTechniqueSafe("Modulate4X_Point_LN");
                _techniqueInverseTexture_LN = GetTechniqueSafe("InverseTexture_Point_LN");
                _techniqueColor_LN = GetTechniqueSafe("Color_Point_LN");
                _techniqueColorTextureAlpha_LN = GetTechniqueSafe("ColorTextureAlpha_Point_LN");
                _techniqueInterpolateColor_LN = GetTechniqueSafe("InterpolateColor_Point_LN");

                _techniqueTexture_LN_CM = GetTechniqueSafe("Texture_Point_LN_CM");
                _techniqueAdd_LN_CM = GetTechniqueSafe("Add_Point_LN_CM");
                _techniqueSubtract_LN_CM = GetTechniqueSafe("Subtract_Point_LN_CM");
                _techniqueModulate_LN_CM = GetTechniqueSafe("Modulate_Point_LN_CM");
                _techniqueModulate2X_LN_CM = GetTechniqueSafe("Modulate2X_Point_LN_CM");
                _techniqueModulate4X_LN_CM = GetTechniqueSafe("Modulate4X_Point_LN_CM");
                _techniqueInverseTexture_LN_CM = GetTechniqueSafe("InverseTexture_Point_LN_CM");
                _techniqueColor_LN_CM = GetTechniqueSafe("Color_Point_LN_CM");
                _techniqueColorTextureAlpha_LN_CM = GetTechniqueSafe("ColorTextureAlpha_Point_LN_CM");
                _techniqueInterpolateColor_LN_CM = GetTechniqueSafe("InterpolateColor_Point_LN_CM");

                _techniqueTexture_Linear = GetTechniqueSafe("Texture_Linear");
                _techniqueAdd_Linear = GetTechniqueSafe("Add_Linear");
                _techniqueSubtract_Linear = GetTechniqueSafe("Subtract_Linear");
                _techniqueModulate_Linear = GetTechniqueSafe("Modulate_Linear");
                _techniqueModulate2X_Linear = GetTechniqueSafe("Modulate2X_Linear");
                _techniqueModulate4X_Linear = GetTechniqueSafe("Modulate4X_Linear");
                _techniqueInverseTexture_Linear = GetTechniqueSafe("InverseTexture_Linear");
                _techniqueColor_Linear = GetTechniqueSafe("Color_Linear");
                _techniqueColorTextureAlpha_Linear = GetTechniqueSafe("ColorTextureAlpha_Linear");
                _techniqueInterpolateColor_Linear = GetTechniqueSafe("InterpolateColor_Linear");

                _techniqueTexture_Linear_CM = GetTechniqueSafe("Texture_Linear_CM");
                _techniqueAdd_Linear_CM = GetTechniqueSafe("Add_Linear_CM");
                _techniqueSubtract_Linear_CM = GetTechniqueSafe("Subtract_Linear_CM");
                _techniqueModulate_Linear_CM = GetTechniqueSafe("Modulate_Linear_CM");
                _techniqueModulate2X_Linear_CM = GetTechniqueSafe("Modulate2X_Linear_CM");
                _techniqueModulate4X_Linear_CM = GetTechniqueSafe("Modulate4X_Linear_CM");
                _techniqueInverseTexture_Linear_CM = GetTechniqueSafe("InverseTexture_Linear_CM");
                _techniqueColor_Linear_CM = GetTechniqueSafe("Color_Linear_CM");
                _techniqueColorTextureAlpha_Linear_CM = GetTechniqueSafe("ColorTextureAlpha_Linear_CM");
                _techniqueInterpolateColor_Linear_CM = GetTechniqueSafe("InterpolateColor_Linear_CM");

                _techniqueTexture_Linear_LN = GetTechniqueSafe("Texture_Linear_LN");
                _techniqueAdd_Linear_LN = GetTechniqueSafe("Add_Linear_LN");
                _techniqueSubtract_Linear_LN = GetTechniqueSafe("Subtract_Linear_LN");
                _techniqueModulate_Linear_LN = GetTechniqueSafe("Modulate_Linear_LN");
                _techniqueModulate2X_Linear_LN = GetTechniqueSafe("Modulate2X_Linear_LN");
                _techniqueModulate4X_Linear_LN = GetTechniqueSafe("Modulate4X_Linear_LN");
                _techniqueInverseTexture_Linear_LN = GetTechniqueSafe("InverseTexture_Linear_LN");
                _techniqueColor_Linear_LN = GetTechniqueSafe("Color_Linear_LN");
                _techniqueColorTextureAlpha_Linear_LN = GetTechniqueSafe("ColorTextureAlpha_Linear_LN");
                _techniqueInterpolateColor_Linear_LN = GetTechniqueSafe("InterpolateColor_Linear_LN");

                _techniqueTexture_Linear_LN_CM = GetTechniqueSafe("Texture_Linear_LN_CM");
                _techniqueAdd_Linear_LN_CM = GetTechniqueSafe("Add_Linear_LN_CM");
                _techniqueSubtract_Linear_LN_CM = GetTechniqueSafe("Subtract_Linear_LN_CM");
                _techniqueModulate_Linear_LN_CM = GetTechniqueSafe("Modulate_Linear_LN_CM");
                _techniqueModulate2X_Linear_LN_CM = GetTechniqueSafe("Modulate2X_Linear_LN_CM");
                _techniqueModulate4X_Linear_LN_CM = GetTechniqueSafe("Modulate4X_Linear_LN_CM");
                _techniqueInverseTexture_Linear_LN_CM = GetTechniqueSafe("InverseTexture_Linear_LN_CM");
                _techniqueColor_Linear_LN_CM = GetTechniqueSafe("Color_Linear_LN_CM");
                _techniqueColorTextureAlpha_Linear_LN_CM = GetTechniqueSafe("ColorTextureAlpha_Linear_LN_CM");
                _techniqueInterpolateColor_Linear_LN_CM = GetTechniqueSafe("InterpolateColor_Linear_LN_CM");
            }
            else
            {
                _effectHasNewformat = false;

                _techniqueTexture = GetTechniqueSafe("Texture");
                _techniqueAdd = GetTechniqueSafe("Add");
                _techniqueSubtract = GetTechniqueSafe("Subtract");
                _techniqueModulate = GetTechniqueSafe("Modulate");
                _techniqueModulate2X = GetTechniqueSafe("Modulate2X");
                _techniqueModulate4X = GetTechniqueSafe("Modulate4X");
                _techniqueInverseTexture = GetTechniqueSafe("InverseTexture");
                _techniqueColor = GetTechniqueSafe("Color");
                _techniqueColorTextureAlpha = GetTechniqueSafe("ColorTextureAlpha");
                _techniqueInterpolateColor = GetTechniqueSafe("InterpolateColor");
            }
        }
    }

    EffectParameter? GetParameterSafe(string parameterName)
    {
        if (_effect == null)
            return null;

        for (int i = 0; i < _effect.Parameters.Count; i++)
        {
            var parameter = _effect.Parameters[i];
            if (parameter.Name == parameterName)
                return parameter;
        }

        return null;
    }

    EffectTechnique? GetTechniqueSafe(string techniqueName)
    {
        if (_effect == null)
            return null;

        for (int i = 0; i < _effect.Techniques.Count; i++)
        {
            var technique = _effect.Techniques[i];
            if (technique.Name == techniqueName)
                return technique;
        }

        return null;
    }

    public class ServiceContainer : IServiceProvider
    {
        #region Fields

        Dictionary<Type, object> services = new Dictionary<Type, object>();

        #endregion

        #region Methods

        public void AddService<T>(T service)
        {
            services.Add(typeof(T), service);
        }

        public object GetService(Type serviceType)
        {
            object service;

            services.TryGetValue(serviceType, out service);

            return service;
        }

        #endregion
    }

    static ContentManager mContentManager;

    // The optional custom shader is probed for existence to avoid a noisy load exception when
    // no shader ships. The path must be anchored to baseDirectory (the app base directory, where
    // ContentManager loads title-relative content from) and NOT the process working directory:
    // launching via a Windows file association sets the working directory to the opened file's
    // folder, which made a working-directory-relative probe miss a shipped Content/Shader.xnb (#3694).
    internal static bool CustomShaderFileExists(string baseDirectory) =>
        System.IO.File.Exists(System.IO.Path.Combine(baseDirectory, "Content", "Shader.xnb"));

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        if (mContentManager == null)
        {
            mContentManager = new ContentManager(
                              new ServiceProvider(
                                   new DeviceManager(graphicsDevice)));
        }

        // Loads the Shader.xnb effect file. The shader is optional; if missing,
        // the application won't be able to use custom effects.
        // On desktop OSes we can check File.Exists to avoid a noisy exception.
        // On other platforms (e.g., consoles/mobile using TitleContainer), skip the
        // check and fall through to the try-catch.
        // Shader should be capitalized.
        var _canCheckFileExists = OperatingSystem.IsWindows()
            || OperatingSystem.IsLinux()
            || OperatingSystem.IsMacOS();

        if (_canCheckFileExists && !CustomShaderFileExists(AppContext.BaseDirectory))
        {
            Debug.WriteLine("'Content/Shader.xnb' not found. Custom rendering is not available.");
        }
        else
        {
            // On platforms where we can't probe the filesystem (Blazor WASM, console targets),
            // we have to attempt the load to find out if a shader is present. If none is shipped
            // (the common case), the load fails and the browser logs the underlying 404. Surface
            // a heads-up so developers seeing that 404 in DevTools recognize it as benign.
            Console.WriteLine(
                "[Gum] Attempting to load optional 'Content/Shader.xnb'. If your project does " +
                "not ship a custom shader, any 404 for this file is expected and benign.");

            try
            {
                Effect = mContentManager.Load<Effect>("Content/Shader");
            }
            catch
            {
                Debug.WriteLine("'Content/Shader.xnb' could not be loaded. Custom rendering is not available.");
            }
        }
    }

    public void Reset()
    {
        _effect?.Dispose();
        _effect = null!;

        ParameterCurrentTexture = null!;
        ParameterViewProj = null!;
        ParameterColorModifier = null;

        _techniqueTexture = null;
        _techniqueAdd = null;
        _techniqueSubtract = null;
        _techniqueModulate = null;
        _techniqueModulate2X = null;
        _techniqueModulate4X = null;
        _techniqueInverseTexture = null;
        _techniqueColor = null;
        _techniqueColorTextureAlpha = null;
        _techniqueInterpolateColor = null;

        _techniqueTexture_CM = null;
        _techniqueAdd_CM = null;
        _techniqueSubtract_CM = null;
        _techniqueModulate_CM = null;
        _techniqueModulate2X_CM = null;
        _techniqueModulate4X_CM = null;
        _techniqueInverseTexture_CM = null;
        _techniqueColor_CM = null;
        _techniqueColorTextureAlpha_CM = null;
        _techniqueInterpolateColor_CM = null;

        _techniqueTexture_LN = null;
        _techniqueAdd_LN = null;
        _techniqueSubtract_LN = null;
        _techniqueModulate_LN = null;
        _techniqueModulate2X_LN = null;
        _techniqueModulate4X_LN = null;
        _techniqueInverseTexture_LN = null;
        _techniqueColor_LN = null;
        _techniqueColorTextureAlpha_LN = null;
        _techniqueInterpolateColor_LN = null;

        _techniqueTexture_LN_CM = null;
        _techniqueAdd_LN_CM = null;
        _techniqueSubtract_LN_CM = null;
        _techniqueModulate_LN_CM = null;
        _techniqueModulate2X_LN_CM = null;
        _techniqueModulate4X_LN_CM = null;
        _techniqueInverseTexture_LN_CM = null;
        _techniqueColor_LN_CM = null;
        _techniqueColorTextureAlpha_LN_CM = null;
        _techniqueInterpolateColor_LN_CM = null;

        _techniqueTexture_Linear = null;
        _techniqueAdd_Linear = null;
        _techniqueSubtract_Linear = null;
        _techniqueModulate_Linear = null;
        _techniqueModulate2X_Linear = null;
        _techniqueModulate4X_Linear = null;
        _techniqueInverseTexture_Linear = null;
        _techniqueColor_Linear = null;
        _techniqueColorTextureAlpha_Linear = null;
        _techniqueInterpolateColor_Linear = null;

        _techniqueTexture_Linear_CM = null;
        _techniqueAdd_Linear_CM = null;
        _techniqueSubtract_Linear_CM = null;
        _techniqueModulate_Linear_CM = null;
        _techniqueModulate2X_Linear_CM = null;
        _techniqueModulate4X_Linear_CM = null;
        _techniqueInverseTexture_Linear_CM = null;
        _techniqueColor_Linear_CM = null;
        _techniqueColorTextureAlpha_Linear_CM = null;
        _techniqueInterpolateColor_Linear_CM = null;

        _techniqueTexture_Linear_LN = null;
        _techniqueAdd_Linear_LN = null;
        _techniqueSubtract_Linear_LN = null;
        _techniqueModulate_Linear_LN = null;
        _techniqueModulate2X_Linear_LN = null;
        _techniqueModulate4X_Linear_LN = null;
        _techniqueInverseTexture_Linear_LN = null;
        _techniqueColor_Linear_LN = null;
        _techniqueColorTextureAlpha_Linear_LN = null;
        _techniqueInterpolateColor_Linear_LN = null;

        _techniqueTexture_Linear_LN_CM = null;
        _techniqueAdd_Linear_LN_CM = null;
        _techniqueSubtract_Linear_LN_CM = null;
        _techniqueModulate_Linear_LN_CM = null;
        _techniqueModulate2X_Linear_LN_CM = null;
        _techniqueModulate4X_Linear_LN_CM = null;
        _techniqueInverseTexture_Linear_LN_CM = null;
        _techniqueColor_Linear_LN_CM = null;
        _techniqueColorTextureAlpha_Linear_LN_CM = null;
        _techniqueInterpolateColor_Linear_LN_CM = null;

        mContentManager = null;
    }

    static EffectTechnique GetTechniqueVariant(bool useDefaultOrPointFilter, EffectTechnique point, EffectTechnique pointLinearized, EffectTechnique linear, EffectTechnique linearLinearized)
    {
        return useDefaultOrPointFilter ?
            (Renderer.LinearizeTextures ? pointLinearized : point) :
            (Renderer.LinearizeTextures ? linearLinearized : linear);
    }

    public EffectTechnique GetVertexColorTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
    {
        if (_effect == null)
            throw new InvalidOperationException("The effect hasn't been set.");

        EffectTechnique technique = null!;

        bool useDefaultOrPointFilterInternal;

        if (_effectHasNewformat)
        {
            // If the shader has the new format both point and linear are available
            if (!useDefaultOrPointFilter.HasValue)
            {
                // Filter not specified, we don't seem to have general setting for
                // filtering in Gum so we'll use the default.
                useDefaultOrPointFilterInternal = true;
            }
            else
            {
                // Filter specified
                useDefaultOrPointFilterInternal = useDefaultOrPointFilter.Value;
            }
        }
        else
        {
            // If the shader doesn't have the new format only one version of
            // the techniques are available, probably using point filtering.
            useDefaultOrPointFilterInternal = true;
        }

        // Only Modulate and ColorTextureAlpha are available in Gum at the moment
        switch (value)
        {
            //case ColorOperation.Texture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueTexture, _techniqueTexture_LN, _techniqueTexture_Linear, _techniqueTexture_Linear_LN); break;

            //case ColorOperation.Add:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueAdd, _techniqueAdd_LN, _techniqueAdd_Linear, _techniqueAdd_Linear_LN); break;

            //case ColorOperation.Subtract:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueSubtract, _techniqueSubtract_LN, _techniqueSubtract_Linear, _techniqueSubtract_Linear_LN); break;

            case ColorOperation.Modulate:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, _techniqueModulate, _techniqueModulate_LN, _techniqueModulate_Linear, _techniqueModulate_Linear_LN); break;

            //case ColorOperation.Modulate2X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueModulate2X, _techniqueModulate2X_LN, _techniqueModulate2X_Linear, _techniqueModulate2X_Linear_LN); break;

            //case ColorOperation.Modulate4X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueModulate4X, _techniqueModulate4X_LN, _techniqueModulate4X_Linear, _techniqueModulate4X_Linear_LN); break;

            //case ColorOperation.InverseTexture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueInverseTexture, _techniqueInverseTexture_LN, _techniqueInverseTexture_Linear, _techniqueInverseTexture_Linear_LN); break;

            //case ColorOperation.Color:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueColor, _techniqueColor_LN, _techniqueColor_Linear, _techniqueColor_Linear_LN); break;

            case ColorOperation.ColorTextureAlpha:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, _techniqueColorTextureAlpha, _techniqueColorTextureAlpha_LN, _techniqueColorTextureAlpha_Linear, _techniqueColorTextureAlpha_Linear_LN); break;

            //case ColorOperation.InterpolateColor:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueInterpolateColor, _techniqueInterpolateColor_LN, _techniqueInterpolateColor_Linear, _techniqueInterpolateColor_Linear_LN); break;

            default: throw new InvalidOperationException();
        }

        return technique;
    }

    public EffectTechnique GetColorModifierTechniqueFromColorOperation(ColorOperation value, bool? useDefaultOrPointFilter = null)
    {
        if (_effect == null)
            throw new InvalidOperationException("The effect hasn't been set.");

        EffectTechnique technique = null!;

        bool useDefaultOrPointFilterInternal;

        if (_effectHasNewformat)
        {
            // If the shader has the new format both point and linear are available
            if (!useDefaultOrPointFilter.HasValue)
            {
                // Filter not specified, we don't seem to have general setting for
                // filtering in Gum so we'll use the default.
                useDefaultOrPointFilterInternal = true;
            }
            else
            {
                // Filter specified
                useDefaultOrPointFilterInternal = useDefaultOrPointFilter.Value;
            }
        }
        else
        {
            // If the shader doesn't have the new format only one version of
            // the techniques are available, probably using point filtering.
            useDefaultOrPointFilterInternal = true;
        }

        switch (value)
        {
            //case ColorOperation.Texture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueTexture_CM, _techniqueTexture_LN_CM, _techniqueTexture_Linear_CM, _techniqueTexture_Linear_LN_CM); break;

            //case ColorOperation.Add:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueAdd_CM, _techniqueAdd_LN_CM, _techniqueAdd_Linear_CM, _techniqueAdd_Linear_LN_CM); break;

            //case ColorOperation.Subtract:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueSubtract_CM, _techniqueSubtract_LN_CM, _techniqueSubtract_Linear_CM, _techniqueSubtract_Linear_LN_CM); break;

            case ColorOperation.Modulate:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, _techniqueModulate_CM, _techniqueModulate_LN_CM, _techniqueModulate_Linear_CM, _techniqueModulate_Linear_LN_CM); break;

            //case ColorOperation.Modulate2X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueModulate2X_CM, _techniqueModulate2X_LN_CM, _techniqueModulate2X_Linear_CM, _techniqueModulate2X_Linear_LN_CM); break;

            //case ColorOperation.Modulate4X:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueModulate4X_CM, _techniqueModulate4X_LN_CM, _techniqueModulate4X_Linear_CM, _techniqueModulate4X_Linear_LN_CM); break;

            //case ColorOperation.InverseTexture:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueInverseTexture_CM, _techniqueInverseTexture_LN_CM, _techniqueInverseTexture_Linear_CM, _techniqueInverseTexture_Linear_LN_CM); break;

            //case ColorOperation.Color:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueColor_CM, _techniqueColor_LN_CM, _techniqueColor_Linear_CM, _techniqueColor_Linear_LN_CM); break;

            case ColorOperation.ColorTextureAlpha:
                technique = GetTechniqueVariant(
                useDefaultOrPointFilterInternal, _techniqueColorTextureAlpha_CM, _techniqueColorTextureAlpha_LN_CM, _techniqueColorTextureAlpha_Linear_CM, _techniqueColorTextureAlpha_Linear_LN_CM); break;

            //case ColorOperation.InterpolateColor:
            //    technique = GetTechniqueVariant(
            //    useDefaultOrPointFilterInternal, _techniqueInterpolateColor_CM, _techniqueInterpolateColor_LN_CM, _techniqueInterpolateColor_Linear_CM, _techniqueInterpolateColor_Linear_LN_CM); break;

            default: throw new InvalidOperationException();
        }

        return technique;
    }
}

public class DeviceManager : IGraphicsDeviceService
{
    public DeviceManager(GraphicsDevice device)
    {
        GraphicsDevice = device;
    }

    public GraphicsDevice GraphicsDevice { get; }

    public event EventHandler<EventArgs>? DeviceCreated;
    public event EventHandler<EventArgs>? DeviceDisposing;

    private EventHandler<EventArgs> deviceReset;
    event EventHandler<EventArgs> IGraphicsDeviceService.DeviceReset
    {
        add
        {
            lock (this)
            {
                deviceReset += value;
            }
        }
        remove
        {
            lock (this)
            {
                deviceReset -= value;
            }
        }
    }

    public event EventHandler<EventArgs> DeviceResetting;
}

public class ServiceProvider : IServiceProvider
{
    private readonly IGraphicsDeviceService deviceService;

    public ServiceProvider(IGraphicsDeviceService deviceService)
    {
        this.deviceService = deviceService;
    }

    public object GetService(Type serviceType)
    {
        return deviceService;
    }
}

#endregion
