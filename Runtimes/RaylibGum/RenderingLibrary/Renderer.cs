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
            // Walk the visual tree calling PreRender on every visible IRenderable before
            // drawing the layer. Mirrors SkiaGum's Renderer.PreRender pattern. This is the
            // canonical hook for runtimes that need camera/zoom-aware resolution of
            // properties (e.g. StrokeWidthUnits = ScreenPixel on CircleRuntime /
            // RectangleRuntime / PolygonRuntime — see #2757). Without it those runtimes had
            // to push StrokeWidth immediately in the setter as a workaround.
            PreRender(layer.Renderables);
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

        element.Render(null);

        if (element.ClipsChildren)
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

        if (element.ClipsChildren)
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

}
