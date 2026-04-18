using System.Collections.ObjectModel;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Gum.Wireframe;
using SokolGum.Renderables;
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

    public Camera Camera => _camera;
    public Layer MainLayer => _layers[0];
    public ReadOnlyCollection<Layer> Layers => _layersReadOnly;

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

    public Renderer()
    {
        _layers = [new Layer()];
        _layersReadOnly = new ReadOnlyCollection<Layer>(_layers);
    }

    /// <summary>
    /// Configure sokol_gp for screen-space (top-left origin, +Y down) and
    /// project the world-space visible region defined by <see cref="Camera"/>.
    /// The Camera's <c>AbsoluteLeft/Right/Top/Bottom</c> properties already
    /// fold in Zoom + Position + CameraCenterOnScreen, so the returned
    /// projection matches Gum's MonoGame/XNA camera semantics: <c>Zoom=2</c>
    /// makes everything render 2× larger; moving <c>Camera.Position</c> pans.
    /// </summary>
    public void BeginFrame(int viewportWidth, int viewportHeight)
    {
        _camera.ClientWidth = viewportWidth;
        _camera.ClientHeight = viewportHeight;

        sgp_begin(viewportWidth, viewportHeight);
        sgp_viewport(0, 0, viewportWidth, viewportHeight);
        sgp_project(_camera.AbsoluteLeft, _camera.AbsoluteRight,
                    _camera.AbsoluteTop,  _camera.AbsoluteBottom);
        _currentBlendMode = DefaultBlendMode;
        sgp_set_blend_mode(_currentBlendMode);
        _scissorStack.Clear();
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
        // here and look for Sprites with an active chain.
        if (element is GraphicalUiElement gue && gue.RenderableComponent is Sprite sprite)
            sprite.AnimateSelf(dt);
        else if (element is Sprite plainSprite)
            plainSprite.AnimateSelf(dt);

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
    /// </summary>
    public void EndFrame()
    {
        (SystemManagers.Default as SystemManagers)?.Fonts?.FlushPendingUpload();
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
            // instead of being clipped by integer truncation.
            var left   = (int)MathF.Floor  (element.GetAbsoluteLeft());
            var top    = (int)MathF.Floor  (element.GetAbsoluteTop());
            var right  = (int)MathF.Ceiling(element.GetAbsoluteRight());
            var bottom = (int)MathF.Ceiling(element.GetAbsoluteBottom());

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
