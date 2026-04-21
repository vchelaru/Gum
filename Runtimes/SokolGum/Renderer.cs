using System.Collections.ObjectModel;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.Wireframe;
using Gum.Renderables;
using static Sokol.SGP;

namespace SokolGum;

/// <summary>
/// Implements Gum's <see cref="IRenderer"/>. All paint operations — colored
/// rectangles, sprites, nine-slices, text — flow through sokol_gp. Text in
/// particular goes through <see cref="FontAtlas"/>, whose fontstash render
/// callbacks emit sgp draw commands, so text batches interleave naturally
/// with other renderables in scene-graph order.
///
/// Caller is responsible for opening an sg_pass, calling <see cref="BeginFrame"/>
/// before <see cref="Draw"/>, and calling <see cref="EndFrame"/> before sg_end_pass.
/// </summary>
public sealed class Renderer : IRenderer
{
    private readonly List<Layer> _layers;
    private readonly ReadOnlyCollection<Layer> _layersReadOnly;
    private readonly Camera _camera = new();

    /// <summary>
    /// Convenience accessor matching the <c>Renderer.Self</c> pattern used by
    /// MonoGame / Raylib. Delegates to <see cref="SystemManagers.Default"/>.
    /// </summary>
    public static Renderer Self
    {
        get
        {
            if (SystemManagers.Default == null)
            {
                throw new InvalidOperationException(
                    "SystemManagers.Default is null. Initialize the default SystemManagers first.");
            }
            return SystemManagers.Default.Renderer;
        }
    }

    public Camera Camera => _camera;
    public Layer MainLayer => _layers[0];
    public ReadOnlyCollection<Layer> Layers => _layersReadOnly;

    /// <summary>
    /// Creates a new <see cref="Layer"/>, appends it to the render list, and
    /// returns it so the caller can assign <see cref="Layer.LayerCameraSettings"/>
    /// or move renderables onto it via <c>MoveToLayer</c>.
    /// </summary>
    public Layer AddLayer()
    {
        var layer = new Layer();
        _layers.Add(layer);
        return layer;
    }

    /// <summary>
    /// Removes <paramref name="layer"/> from the render list.
    /// The <see cref="MainLayer"/> cannot be removed and the call is silently
    /// ignored when <paramref name="layer"/> is the main layer.
    /// </summary>
    public void RemoveLayer(Layer layer)
    {
        if (layer == MainLayer)
        {
            return;
        }

        _layers.Remove(layer);
    }

    /// <summary>
    /// Default blend mode applied at the start of each frame and between
    /// renderables that don't declare their own. sokol_gp's SGP_BLENDMODE_BLEND
    /// is standard alpha (src.a, 1-src.a) — matches Gum's NonPremultiplied default.
    /// </summary>
    public static sgp_blend_mode DefaultBlendMode { get; set; } = sgp_blend_mode.SGP_BLENDMODE_BLEND;

    private sgp_blend_mode _currentBlendMode = sgp_blend_mode.SGP_BLENDMODE_BLEND;

    /// <summary>
    /// One entry on the scissor stack. sokol_gp has no native scissor
    /// save/restore, so nested <see cref="IRenderableIpso.ClipsChildren"/>
    /// elements need us to remember the outer rect and re-apply it when
    /// the inner subtree unwinds. We store the effective (intersected)
    /// rect rather than the raw element rect so popping restores exactly
    /// what the parent had pushed.
    /// </summary>
    private readonly record struct ScissorRect(int X, int Y, int W, int H);

    private readonly List<ScissorRect> _scissorStack = [];

    // Logical-to-framebuffer scale factors. sgp_scissor works in framebuffer
    // pixels while GetAbsoluteLeft/Top/... return logical coordinates, so
    // clip rects need scaling by this ratio to land correctly when high-DPI
    // or stretch-to-fit is active. Default 1:1 is restored by the 2-arg
    // BeginFrame overload.
    private float _scissorScaleX = 1f;
    private float _scissorScaleY = 1f;

    public Renderer()
    {
        _layers = [new Layer()];
        _layersReadOnly = new ReadOnlyCollection<Layer>(_layers);
    }

    /// <summary>
    /// Convenience overload for the 1:1 case (no DPI scaling): logical and
    /// framebuffer dimensions are the same.
    /// </summary>
    public void BeginFrame(int viewportWidth, int viewportHeight)
        => BeginFrame(viewportWidth, viewportHeight, viewportWidth, viewportHeight);

    /// <summary>
    /// Configure sokol_gp for screen-space (top-left origin, +Y down) and
    /// project the world-space visible region defined by <see cref="Camera"/>.
    /// The Camera's <c>AbsoluteLeft/Right/Top/Bottom</c> properties already
    /// fold in Zoom + Position + CameraCenterOnScreen, so the returned
    /// projection matches Gum's MonoGame/XNA camera semantics: <c>Zoom=2</c>
    /// makes everything render 2× larger; moving <c>Camera.Position</c> pans.
    ///
    /// Splits the two dimensions so hi-DPI callers can project in logical
    /// pixels (UI layout units) while sokol_gp rasterizes into a physical-
    /// pixel framebuffer that's typically 2× larger on Retina. Logical
    /// coords drive Camera + sgp_project; framebuffer coords drive
    /// sgp_begin + sgp_viewport so the full native resolution gets filled.
    /// </summary>
    public void BeginFrame(int logicalWidth, int logicalHeight, int framebufferWidth, int framebufferHeight)
    {
        _camera.ClientWidth = logicalWidth;
        _camera.ClientHeight = logicalHeight;

        sgp_begin(framebufferWidth, framebufferHeight);
        sgp_viewport(0, 0, framebufferWidth, framebufferHeight);
        sgp_project(_camera.AbsoluteLeft, _camera.AbsoluteRight,
                    _camera.AbsoluteTop,  _camera.AbsoluteBottom);
        _currentBlendMode = DefaultBlendMode;
        sgp_set_blend_mode(_currentBlendMode);
        _scissorStack.Clear();
        // sgp_scissor operates in framebuffer pixels; ClipsChildren bounds
        // come from logical coords. Remember the ratio for each axis so
        // DrawGumRecursively can translate one to the other.
        _scissorScaleX = framebufferWidth  / (float)logicalWidth;
        _scissorScaleY = framebufferHeight / (float)logicalHeight;
    }

    /// <summary>
    /// Advances per-frame state for every animated Sprite across every
    /// layer. Call this once per frame before <see cref="Draw"/>. Passing
    /// dt is explicit (rather than baked into Draw) so non-real-time cases
    /// like editor scrubbing or paused menus can suppress the tick without
    /// skipping rendering.
    /// </summary>
    public void Update(double secondsSinceLastFrame)
    {
        for (int i = 0; i < _layers.Count; i++)
        {
            var layer = _layers[i];
            foreach (var renderable in layer.Renderables)
                TickRecursively(renderable, secondsSinceLastFrame);
        }
    }

    private static void TickRecursively(IRenderableIpso element, double dt)
    {
        // Gum's render walker only visits IRenderable.Render; animation
        // tickers have nowhere else to live, so we match the walk order
        // here. Any renderable implementing IAnimatable gets ticked —
        // Sprite + NineSlice compose SpriteAnimationLogic under the hood.
        // Text doesn't animate via chains (per-character reveal uses
        // MaxLettersToShow instead).
        //
        // Invisible subtrees still tick so that a paused-but-visible
        // element resumes in-sync when re-shown; this matches how Gum's
        // shared SpriteAnimationLogic behaves.
        var renderable = (element as GraphicalUiElement)?.RenderableComponent ?? element;
        if (renderable is IAnimatable animatable)
            animatable.AnimateSelf(dt);

        if (element.Children is null) return;
        foreach (var child in element.Children)
            TickRecursively(child, dt);
    }

    public void Draw(ISystemManagers? managers)
    {
        managers ??= SystemManagers.Default
            ?? throw new InvalidOperationException("No SystemManagers available to draw.");

        for (int i = 0; i < _layers.Count; i++)
            RenderLayer(managers, _layers[i]);
    }

    /// <summary>
    /// Renders a single layer. <paramref name="prerender"/> is part of the
    /// <see cref="IRenderer"/> contract used by backends that support an
    /// off-screen prerender pass — we don't, and match RaylibGum's behaviour
    /// of ignoring the flag. Present for signature compatibility so callers
    /// holding an IRenderer can invoke this uniformly across runtimes.
    /// </summary>
    public void RenderLayer(ISystemManagers managers, Layer layer, bool prerender = true)
    {
        layer.SortRenderables();
        foreach (var renderable in layer.Renderables)
            DrawGumRecursively(renderable, managers);
    }

    /// <summary>
    /// Flushes the glyph atlas FIRST (so sg_update_image enters the sokol_gfx
    /// command queue before any draws), then submits sgp's queued draws.
    /// GPU execution order: update atlas → draws sample the updated atlas.
    /// This means new glyphs appear in the same frame they're rasterized
    /// (no one-frame lag). Ordering matters because sgp buffers its commands
    /// internally and only submits them to sokol_gfx on sgp_flush.
    ///
    /// Accepts the <see cref="ISystemManagers"/> passed to <see cref="Draw"/>
    /// so a caller using a non-default SystemManagers instance still gets
    /// its own font atlas flushed. Falls back to <see cref="SystemManagers.Default"/>
    /// when called without an argument, for symmetry with <see cref="Draw"/>.
    /// </summary>
    public void EndFrame(ISystemManagers? managers = null)
    {
        var sm = managers as SystemManagers ?? SystemManagers.Default;
        sm?.Fonts?.FlushPendingUpload();
        sgp_flush();
        sgp_end();
    }

    /// <summary>
    /// Maps Gum's cross-backend <see cref="Gum.BlendState"/> to sokol_gp's
    /// equivalent. Opaque disables blending entirely; AlphaBlend is the
    /// premultiplied-alpha mode (XNA "AlphaBlend"); Additive adds src to dst;
    /// NonPremultiplied is standard straight-alpha, the Gum default.
    /// <see cref="Gum.BlendState"/> is a class with static readonly instances
    /// (XNA-style), so identity comparison is correct here — not value equality.
    /// </summary>
    private static sgp_blend_mode MapBlend(Gum.BlendState? blend)
    {
        if (blend is null)                              return sgp_blend_mode.SGP_BLENDMODE_BLEND;
        if (ReferenceEquals(blend, Gum.BlendState.Opaque))           return sgp_blend_mode.SGP_BLENDMODE_NONE;
        if (ReferenceEquals(blend, Gum.BlendState.AlphaBlend))       return sgp_blend_mode.SGP_BLENDMODE_BLEND_PREMULTIPLIED;
        if (ReferenceEquals(blend, Gum.BlendState.Additive))         return sgp_blend_mode.SGP_BLENDMODE_ADD;
        if (ReferenceEquals(blend, Gum.BlendState.NonPremultiplied)) return sgp_blend_mode.SGP_BLENDMODE_BLEND;
        return sgp_blend_mode.SGP_BLENDMODE_BLEND;
    }

    private void DrawGumRecursively(IRenderableIpso element, ISystemManagers managers)
    {
        // Skip the whole subtree when this element is invisible. The child
        // loop below already checks child.Visible, but the initial call from
        // RenderLayer goes straight past that guard — so a top-level
        // ContainerRuntime added directly to a Layer would otherwise render
        // its children with its own Visible = false ignored.
        if (element is IVisible visible && !visible.Visible) return;

        var desiredBlend = MapBlend(element.BlendState);
        if (desiredBlend != _currentBlendMode)
        {
            sgp_set_blend_mode(desiredBlend);
            _currentBlendMode = desiredBlend;
        }

        // Rotation is applied as an sgp transform around the element's
        // absolute top-left. Gum's Rotation field is in degrees with
        // positive rotating counter-clockwise (GraphicalUiElement docs);
        // sokol_gp's sgp_rotate_at positive direction appears clockwise
        // under our Y-down projection, hence the negation.
        //
        // Note: sgp_scissor is applied in framebuffer (screen) pixels
        // and is not affected by transforms, so a rotated element that
        // ClipsChildren will still scissor against an axis-aligned rect.
        // This matches MonoGame/XNA behavior and is a hardware limitation.
        var rotation = element.Rotation;
        bool rotating = rotation != 0f;
        if (rotating)
        {
            sgp_push_transform();
            sgp_rotate_at(-rotation * MathF.PI / 180f,
                          element.GetAbsoluteLeft(),
                          element.GetAbsoluteTop());
        }

        element.Render(managers);

        bool clipping = element.ClipsChildren;
        if (clipping)
        {
            // Floor top-left + ceil bottom-right so a sub-pixel-wide element
            // still gets a scissor rect that fully contains its drawn pixels
            // instead of being clipped by integer truncation. Multiply by
            // _scissorScaleX/Y to convert from logical coords (what
            // GetAbsolute*() returns) into framebuffer pixels (what
            // sgp_scissor takes) — they diverge under high-DPI +
            // stretch-to-fit projection.
            var left   = (int)MathF.Floor  (element.GetAbsoluteLeft()   * _scissorScaleX);
            var top    = (int)MathF.Floor  (element.GetAbsoluteTop()    * _scissorScaleY);
            var right  = (int)MathF.Ceiling(element.GetAbsoluteRight()  * _scissorScaleX);
            var bottom = (int)MathF.Ceiling(element.GetAbsoluteBottom() * _scissorScaleY);

            // Intersect with the parent scissor, if any, so nested ClipsChildren
            // accumulate correctly instead of the inner element widening the
            // clip region beyond the outer bound.
            if (_scissorStack.Count > 0)
            {
                var outer = _scissorStack[^1];
                int ox1 = outer.X, oy1 = outer.Y;
                int ox2 = outer.X + outer.W, oy2 = outer.Y + outer.H;
                left   = Math.Max(left,   ox1);
                top    = Math.Max(top,    oy1);
                right  = Math.Min(right,  ox2);
                bottom = Math.Min(bottom, oy2);
            }

            var rect = new ScissorRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
            _scissorStack.Add(rect);
            sgp_scissor(rect.X, rect.Y, rect.W, rect.H);
        }

        if (element.Children != null)
        {
            foreach (var child in element.Children)
            {
                // Descend into any IRenderableIpso child, not just GUEs.
                // Plain renderables added under a GUE (e.g. when an IRenderable
                // is parented directly) were previously silently dropped.
                if (child.Visible)
                    DrawGumRecursively(child, managers);
            }
        }

        if (clipping)
        {
            // Pop our frame and restore the outer scissor (or clear entirely
            // when nothing's on the stack). sgp_reset_scissor disables
            // scissoring outright, so we only call it at the very outer
            // boundary; inside a nested clip we re-push the parent rect.
            _scissorStack.RemoveAt(_scissorStack.Count - 1);
            if (_scissorStack.Count > 0)
            {
                var outer = _scissorStack[^1];
                sgp_scissor(outer.X, outer.Y, outer.W, outer.H);
            }
            else
            {
                sgp_reset_scissor();
            }
        }

        if (rotating)
            sgp_pop_transform();
    }
}
