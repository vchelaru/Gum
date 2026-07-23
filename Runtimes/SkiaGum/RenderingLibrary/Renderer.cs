using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;

namespace RenderingLibrary.Graphics
{
    public class Renderer : IRenderer
    {

        List<Layer> _layers = new List<Layer>();
        ReadOnlyCollection<Layer> mLayersReadOnly;
        public ReadOnlyCollection<Layer> Layers
        {
            get
            {
                return mLayersReadOnly;
            }
        }

        public Layer MainLayer => 
            // Not sure if we have any layers in skia so do a FirstOrDefault
            _layers.FirstOrDefault();

        /// <summary>
        /// Whether renderable objects should call Render
        /// on contained children. This is true by default, 
        /// results in a hierarchical rendering order.
        /// </summary>
        public static bool RenderUsingHierarchy = true;

        /// <summary>
        /// Use the custom effect for rendering. This setting takes priority if
        /// both UseCustomEffectRendering and UseBasicEffectRendering are enabled.
        /// </summary>
        public static bool UseCustomEffectRendering { get; set; } = false;
        public static bool UseBasicEffectRendering { get; set; } = true;
        public static bool UsingEffect { get { return UseCustomEffectRendering || UseBasicEffectRendering; } }

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

        public Camera Camera { get; private set; }
        public bool ClearsCanvas { get; set; } = true;

        // Per-render-target-container offscreen surfaces (#3988), baked in a pre-pass and composited
        // in place during the main walk. Cache keyed by the container's contained renderable; swept
        // once per frame via RenderTargetServiceBase, exactly like the raylib/MonoGame renderers.
        readonly SkiaRenderTargetService _renderTargetService = new SkiaRenderTargetService();

        // This frame's baked snapshots, keyed by the same contained-renderable owner. Rebuilt each
        // top-level Draw (old snapshots disposed first) and read by both the composite pass and a
        // Sprite pulling its RenderTargetTextureSource.
        readonly Dictionary<IRenderableIpso, SKImage> _bakedImages = new Dictionary<IRenderableIpso, SKImage>();

        // Cache owners of render-target containers referenced by a visible Sprite's
        // RenderTargetTextureSource, so an invisible-but-referenced container still bakes (#1643).
        readonly HashSet<IRenderableIpso> _referencedRenderTargetOwners = new HashSet<IRenderableIpso>();

        public void Initialize(SystemManagers managers)
        {
            Camera = new Camera();

            mLayersReadOnly = new ReadOnlyCollection<Layer>(_layers);

            _layers.Add(new Layer());
            _layers[0].Name = "Main Layer";
        }

        public void Draw(SystemManagers managers)
        {
            //ClearPerformanceRecordingVariables();

            if (managers == null)
            {
                managers = SystemManagers.Default;
            }

            Draw(managers, _layers);

            //ForceEnd();
        }

        public void Draw(SystemManagers managers, List<Layer> layers)
        {
            foreach(var layer in layers)
            {
                Draw(layer.Renderables, managers, isTopLevelDraw: true);
            }
        }

        //public void Draw(SystemManagers systemManagers)
        //{
        //    var canvas = systemManagers.Canvas;
        //}


        // This syntax is a little different than standard Gum, but we're moving in that direction incrementally:
        public void Draw(IList<IRenderableIpso> whatToRender, SystemManagers managers)
        {
            Draw(whatToRender, managers, true);
        }

        public void Draw(ObservableCollection<IRenderableIpso> whatToRender, SystemManagers managers)
        {
            Draw(whatToRender, managers, true);
        }

        void Draw(IList<IRenderableIpso> whatToRender, SystemManagers managers, bool isTopLevelDraw = false)
        {
            if (isTopLevelDraw)
            {
                // Frame-boundary lifecycle: drop last frame's snapshots, then sweep offscreen surfaces
                // whose owner wasn't baked last frame (removed, hidden, or resized). Mirrors the
                // raylib/MonoGame RenderTargetService pattern (#3988).
                DisposeBakedImages();
                _renderTargetService.ClearUnusedRenderTargetsLastFrame();

                // Collect render-target containers referenced by a visible Sprite before baking, so an
                // invisible-but-referenced container still bakes (#1643).
                _referencedRenderTargetOwners.Clear();
                CollectReferencedRenderTargets(whatToRender);

                if (ClearsCanvas)
                {
                    managers.Canvas.Clear();
                }

                // Bake every render-target container's subtree into its offscreen surface before the
                // camera transform is applied to the screen canvas. Each bake swaps managers.Canvas to
                // its own offscreen canvas and restores it, so nothing leaks between the two.
                if (HasAnyRenderTarget(whatToRender))
                {
                    BakeRenderTargetsInSubtree(whatToRender, managers);
                }

                if (Camera.Zoom != 1)
                {
                    managers.Canvas.Scale(Camera.Zoom);
                }

                var translateX = -Camera.X;
                var translateY = -Camera.Y;

                if(Camera.CameraCenterOnScreen == CameraCenterOnScreen.Center)
                {
                    translateX += (Camera.ClientWidth / 2.0f);
                    translateY += (Camera.ClientHeight / 2.0f);
                }

                if(translateX != 0 || translateY != 0)
                {
                    managers.Canvas.Translate(translateX, translateY);
                }
            }

            PreRender(whatToRender);

            var count = whatToRender.Count;

            for (int i = 0; i < count; i++)
            {
                var renderable = whatToRender[i];
                if (renderable.Visible)
                {
                    // A render-target container composites its baked offscreen texture in place of
                    // walking its live children (its subtree was baked in the pre-pass, #3988). A
                    // hidden one is skipped by the Visible check above, so no stale texture ghosts.
                    if (renderable.IsRenderTarget)
                    {
                        CompositeRenderTarget(renderable, managers);
                        continue;
                    }

                    var canvas = (managers as SystemManagers).Canvas;

                    var isOnScreen = true;

                    if (renderable.ClipsChildren)
                    {
                        var absoluteX = renderable.GetAbsoluteX();
                        var absoluteY = renderable.GetAbsoluteY();

                        var width = renderable.Width;
                        var height = renderable.Height;

                        var rect = new SKRect(absoluteX, absoluteY, absoluteX + width, absoluteY + height);

                        isOnScreen =
                            rect.Bottom > canvas.LocalClipBounds.Top &&
                            rect.Top < canvas.LocalClipBounds.Bottom &&
                            rect.Right > canvas.LocalClipBounds.Left &&
                            rect.Left < canvas.LocalClipBounds.Right;

                        if (isOnScreen)
                        {
                            canvas.Save();
                            canvas.ClipRect(rect);
                            renderable.Render(managers);
                        }
                    }
                    else
                    {
                        renderable.Render(managers);
                    }

                    if (isOnScreen)
                    {
                        if (RenderUsingHierarchy)
                        {
                            Draw(renderable.Children, managers, false);
                        }

                        if (renderable.ClipsChildren)
                        {
                            canvas.Restore();
                        }
                    }
                }
            }
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
            for (int i = 0; i < count; i++)
            {
                var renderable = renderables[i];
                if (renderable.Visible)
                {
                    renderable.PreRender();

                    // Some Gum objects, like GraphicalUiElements, may not have children if the object hasn't
                    // yet been assigned a visual. Just skip over it...
                    if (renderable.Visible && renderable.Children != null)
                    {
                        PreRender(renderable.Children);
                    }
                }
            }
        }

        private void PreRender(ObservableCollection<IRenderableIpso> renderables)
        {
#if FULL_DIAGNOSTICS
            if (renderables == null)
            {
                throw new ArgumentNullException("renderables");
            }
#endif

            var count = renderables.Count;
            for (int i = 0; i < count; i++)
            {
                var renderable = renderables[i];
                if (renderable.Visible)
                {
                    renderable.PreRender();

                    // Some Gum objects, like GraphicalUiElements, may not have children if the object hasn't
                    // yet been assigned a visual. Just skip over it...
                    if (renderable.Visible && renderable.Children != null)
                    {
                        PreRender(renderable.Children);
                    }
                }
            }
        }

        /// <summary>
        /// Whether an offscreen texture is currently baked for the given render-target container.
        /// Resolves the container's cache owner internally. Intended for tests and diagnostics.
        /// </summary>
        public bool HasBakedRenderTargetFor(IRenderableIpso container)
            => _bakedImages.ContainsKey(ResolveRenderTargetCacheOwner(container));

        /// <summary>
        /// The offscreen texture baked for the given render-target container this frame, or null if
        /// none exists. Resolves the container's cache owner internally. Used by a Sprite pulling its
        /// <c>RenderTargetTextureSource</c>, and by tests.
        /// </summary>
        public SKImage TryGetBakedRenderTargetFor(IRenderableIpso container)
            => _bakedImages.TryGetValue(ResolveRenderTargetCacheOwner(container), out SKImage image)
                ? image
                : null;

        private void DisposeBakedImages()
        {
            foreach (SKImage image in _bakedImages.Values)
            {
                image?.Dispose();
            }
            _bakedImages.Clear();
        }

        // Populates _referencedRenderTargetOwners with the cache owner of every render-target
        // container referenced by a visible Sprite's RenderTargetTextureSource. Both a top-level
        // Sprite (handed directly) and a nested one (a SpriteRuntime wrapping the Sprite) implement
        // IRenderTargetTextureReferencer, so one interface test covers both — the same detection path
        // the xnalike and raylib Renderers use (#3992).
        private void CollectReferencedRenderTargets(IList<IRenderableIpso> renderables)
        {
            for (int i = 0; i < renderables.Count; i++)
            {
                IRenderableIpso renderable = renderables[i];
                if (!renderable.Visible)
                {
                    continue;
                }

                if (renderable is IRenderTargetTextureReferencer textureReferencer &&
                    textureReferencer.RenderTargetTextureSource != null)
                {
                    _referencedRenderTargetOwners.Add(ResolveRenderTargetCacheOwner(textureReferencer.RenderTargetTextureSource));
                }

                if (renderable.Children != null)
                {
                    CollectReferencedRenderTargets(renderable.Children);
                }
            }
        }

        private bool IsReferencedRenderTargetOwner(IRenderableIpso renderable)
            => _referencedRenderTargetOwners.Contains(ResolveRenderTargetCacheOwner(renderable));

        // Whether the tree contains any render-target container that needs baking this frame — a
        // visible one, or an invisible one referenced by a visible Sprite. Lets screens with no
        // render targets skip the bake walk entirely.
        private bool HasAnyRenderTarget(IList<IRenderableIpso> renderables)
        {
            for (int i = 0; i < renderables.Count; i++)
            {
                IRenderableIpso renderable = renderables[i];
                bool isReferenced = renderable.IsRenderTarget && IsReferencedRenderTargetOwner(renderable);
                if (!renderable.Visible && !isReferenced)
                {
                    continue;
                }

                if (renderable.IsRenderTarget)
                {
                    return true;
                }

                if (renderable.Children != null && HasAnyRenderTarget(renderable.Children))
                {
                    return true;
                }
            }
            return false;
        }

        // Post-order (innermost-first) bake of every render-target container, so a nested inner
        // target is baked before its outer one; the outer bake then composites the inner's finished
        // texture while walking its children.
        private void BakeRenderTargetsInSubtree(IList<IRenderableIpso> renderables, SystemManagers managers)
        {
            for (int i = 0; i < renderables.Count; i++)
            {
                IRenderableIpso renderable = renderables[i];
                bool isReferenced = renderable.IsRenderTarget && IsReferencedRenderTargetOwner(renderable);
                if (!renderable.Visible && !isReferenced)
                {
                    continue;
                }

                if (renderable.Children != null)
                {
                    BakeRenderTargetsInSubtree(renderable.Children, managers);
                }

                if (renderable.IsRenderTarget)
                {
                    BakeRenderTarget(renderable, managers);
                }
            }
        }

        // Bakes a single render-target container's child subtree into its cached offscreen surface,
        // then snapshots it for this frame. Children draw in the surface's local pixel space via an
        // offset translate; content outside the surface is clipped, truncating overflow to the
        // render-target size. A non-positive size bakes nothing (the composite then draws nothing).
        private void BakeRenderTarget(IRenderableIpso container, SystemManagers managers)
        {
            int width = MathFunctions.RoundToInt(container.Width);
            int height = MathFunctions.RoundToInt(container.Height);
            if (width <= 0 || height <= 0)
            {
                return;
            }

            IRenderableIpso owner = ResolveRenderTargetCacheOwner(container);
            SkiaRenderTarget renderTarget = _renderTargetService.GetFor(owner, width, height);
            if (renderTarget == null)
            {
                return;
            }

            float left = container.GetAbsoluteX();
            float top = container.GetAbsoluteY();

            SKCanvas offscreenCanvas = renderTarget.Surface.Canvas;
            offscreenCanvas.Clear(SKColors.Transparent);
            offscreenCanvas.Save();
            offscreenCanvas.Translate(-left, -top);

            SKCanvas screenCanvas = managers.Canvas;
            managers.Canvas = offscreenCanvas;
            Draw(container.Children, managers, isTopLevelDraw: false);
            managers.Canvas = screenCanvas;

            offscreenCanvas.Restore();

            // The snapshot stays valid across the frame via SkiaSharp copy-on-write; the persistent
            // surface is only re-cleared next frame, after DisposeBakedImages frees this snapshot.
            _bakedImages[owner] = renderTarget.Surface.Snapshot();
        }

        // Composites a render-target container's baked texture at the container's rectangle, honoring
        // group alpha and additive blend. No valid baked texture (degenerate size, or off-screen) =>
        // draws nothing, so the subtree simply does not appear.
        private void CompositeRenderTarget(IRenderableIpso container, SystemManagers managers)
        {
            IRenderableIpso owner = ResolveRenderTargetCacheOwner(container);
            if (!_bakedImages.TryGetValue(owner, out SKImage image) || image == null)
            {
                return;
            }

            int width = MathFunctions.RoundToInt(container.Width);
            int height = MathFunctions.RoundToInt(container.Height);
            if (width <= 0 || height <= 0)
            {
                return;
            }

            // Skia surfaces are premultiplied, so a plain SrcOver composite needs no straight-vs-
            // premultiplied correction (unlike the XNA/raylib bake blend handling). Group alpha tints
            // the whole texture. Additive-blend render-target containers aren't handled here because
            // ContainerRuntime exposes no Blend/BlendState on Skia (it's gated !SKIA) — nothing can set
            // a Skia container additive, so there is no additive case to composite (deferred: #3989).
            byte alpha = (byte)System.Math.Clamp(container.Alpha, 0, 255);
            using SKPaint paint = new SKPaint();
            paint.Color = new SKColor(255, 255, 255, alpha);

            float left = container.GetAbsoluteX();
            float top = container.GetAbsoluteY();
            SKRect destination = new SKRect(left, top, left + width, top + height);
            managers.Canvas.DrawImage(image, destination, paint);
        }

        // For a nested render-target container the walk hands the GraphicalUiElement wrapper; for a
        // top-level one it hands the contained renderable directly. The cache key is always the
        // contained renderable so both forms resolve to the same texture.
        private static IRenderableIpso ResolveRenderTargetCacheOwner(IRenderableIpso source)
        {
            if (source is GraphicalUiElement gue && gue.RenderableComponent is IRenderableIpso contained)
            {
                return contained;
            }
            return source;
        }

        public void RenderLayer(ISystemManagers managers, Layer layer, bool prerender = true)
        {
            throw new NotImplementedException();
        }
    }
}
