using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Math;
using SkiaGum.Renderables;
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
        readonly ReadOnlyCollection<Layer> mLayersReadOnly;
        public ReadOnlyCollection<Layer> Layers
        {
            get
            {
                return mLayersReadOnly;
            }
        }

        public Layer MainLayer => _layers.Count > 0
            ? _layers[0]
            : throw new InvalidOperationException("The Renderer has no layers. Call Initialize first.");

        public Layer AddLayer()
        {
            Layer layer = new Layer();
            _layers.Add(layer);
            return layer;
        }

        public void AddLayer(Layer layer) => _layers.Add(layer);

        public void RemoveLayer(Layer layer) => _layers.Remove(layer);

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

        /// <summary>
        /// The <see cref="BlendState"/> that <see cref="Gum.RenderingLibrary.Blend.Normal"/> resolves
        /// to via <see cref="Gum.RenderingLibrary.BlendExtensions.ToBlendState"/>. Mirrors the XNA and
        /// raylib renderers' own <c>NormalBlendState</c> so <c>ContainerRuntime.Blend</c> round-trips
        /// on Skia too (#3989).
        /// </summary>
        public static BlendState NormalBlendState { get; set; } = BlendState.NonPremultiplied;

        /// <summary>
        /// How Sprite and NineSlice textures are sampled when drawn at a size other than their own:
        /// <see cref="SKFilterMode.Nearest"/> (point, the default) or <see cref="SKFilterMode.Linear"/>.
        /// Mirrors the MonoGame renderer's <c>TextureFilter</c>. Set from the project's
        /// <c>TextureFilter</c> when GumService loads a project (issue #5231).
        /// </summary>
        public static SKFilterMode TextureFilter { get; set; } = SKFilterMode.Nearest;

        /// <summary>
        /// The sampling Sprite and NineSlice pass to their image draws, built from <see cref="TextureFilter"/>.
        /// </summary>
        internal static SKSamplingOptions TextureSampling => new SKSamplingOptions(TextureFilter, SKMipmapMode.None);

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

        public Renderer()
        {
            // The same origin MonoGame, raylib and Sokol use. Center only looked like top-left here
            // because nothing set the camera's client size.
            Camera = new Camera { CameraCenterOnScreen = CameraCenterOnScreen.TopLeft };
            mLayersReadOnly = new ReadOnlyCollection<Layer>(_layers);
        }
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

        SystemManagers? _ownManagers;

        public void Initialize(SystemManagers managers)
        {
            _ownManagers = managers;
            _layers.Add(new Layer());
            _layers[0].Name = "Main Layer";
        }

        public void Draw(SystemManagers? managers)
        {
            managers ??= SystemManagers.Default
                ?? throw new InvalidOperationException("No SystemManagers was passed and SystemManagers.Default is null.");

            Draw(managers, _layers);
        }

        public void Draw(SystemManagers managers, List<Layer> layers)
        {
            BeginHostFrame(managers);
            foreach(var layer in layers)
            {
                DrawTopLevel(layer.Renderables, managers, layer);
            }
        }

        // This syntax is a little different than standard Gum, but we're moving in that direction incrementally:
        public void Draw(IList<IRenderableIpso> whatToRender, SystemManagers managers)
        {
            BeginHostFrame(managers);
            DrawTopLevel(whatToRender, managers, layer: null);
        }

        public void Draw(ObservableCollection<IRenderableIpso> whatToRender, SystemManagers managers)
        {
            BeginHostFrame(managers);
            DrawTopLevel(whatToRender, managers, layer: null);
        }

        // Set by the once-per-frame work and reset by NotifyHostFrameAdvanced, so GumBatch cycles
        // (several Begin/End pairs per frame) run it at most once per host frame, and never after a
        // full-scene Draw already did. Mirrors the XNA Renderer's host-frame latches.
        bool _hostFrameStarted;

        // Set when a GumBatch Begin ran the frame work this frame, so the full-scene Draw that follows
        // does not sweep again: a second sweep finds nothing marked used since the first and destroys
        // every render-target surface. Draw consumes it, so a host that never calls Update still
        // sweeps once per frame.
        bool _batchStartedHostFrame;

        // Called once per host frame by GumService.Update.
        internal void NotifyHostFrameAdvanced()
        {
            _hostFrameStarted = false;
            _batchStartedHostFrame = false;
        }

        // Once per full-scene Draw: drop last frame's snapshots, sweep offscreen surfaces whose owner
        // wasn't baked last frame (removed, hidden, or resized), then clear. Mirrors the
        // raylib/MonoGame RenderTargetService pattern (#3988). Clearing here rather than per layer
        // keeps earlier layers of the same frame.
        private void BeginHostFrame(SystemManagers managers)
        {
            if (_batchStartedHostFrame)
            {
                _batchStartedHostFrame = false;
            }
            else
            {
                StartHostFrameRenderTargets();
            }

            if (ClearsCanvas)
            {
                managers.Canvas.Clear();
            }
        }

        private void StartHostFrameRenderTargets()
        {
            DisposeBakedImages();
            _referencedRenderTargetOwners.Clear();
            _renderTargetService.ClearUnusedRenderTargetsLastFrame();
            _hostFrameStarted = true;
        }

        // Per layer of a full-scene Draw: bake this layer's render targets, then walk it under the
        // layer's camera.
        private void DrawTopLevel(IList<IRenderableIpso> whatToRender, SystemManagers managers, Layer? layer)
        {
            _referencedRenderTargetOwners.Clear();
            // Before the camera transform is applied to the screen canvas.
            BakeRenderTargets(whatToRender, managers);

            ApplyCamera(managers.Canvas, layer, matrix: null);
            DrawList(whatToRender, managers, layer);
            // Undoes this layer's camera so the next layer (or the next frame) starts from a clean
            // matrix (#4330).
            RestoreCamera(managers.Canvas);
        }

        // The canvas matrix the active camera was applied on top of (the host's own transform) and
        // the caller matrix applied with it. RenderLayer swaps the active camera for its layer's
        // own on top of these, the way MonoGame's RenderLayer begins its layer's camera absolutely
        // rather than composing it with the camera of the layer it is drawn in.
        SKMatrix? _cameraBaseMatrix;
        SKMatrix? _cameraCallerMatrix;

        // Saves the canvas, then applies the camera and, when given, a caller matrix applied before it
        // (the same order MonoGame composes GumBatch's matrix with the camera view:
        // matrix * cameraView). Every caller undoes it with RestoreCamera.
        private void ApplyCamera(SKCanvas canvas, Layer? layer, SKMatrix? matrix)
        {
            // Issue #4330: SKCanvas is a persistent, mutate-in-place matrix stack -- unlike a fresh
            // matrix passed per frame (XNALIKE's SpriteBatch.Begin) or an explicit begin/end pair
            // (raylib's BeginMode2D/EndMode2D), Scale/Translate calls accumulate onto whatever the
            // previous cycle applied unless undone. Without this Save and the caller's Restore, a
            // non-1 Camera.Zoom compounded every frame.
            _cameraBaseMatrix = canvas.TotalMatrix;
            _cameraCallerMatrix = matrix;
            canvas.Save();

            // Each layer gets its own effective camera position/zoom from LayerCameraSettings
            // (falling back to the main camera when the layer has none set), so a screen-space
            // HUD layer renders fixed on screen (#4367). A null layer (the public Draw(IList<...>)/
            // Draw(ObservableCollection<...>) overloads, which have no Layer) uses the main camera.
            float effectiveZoom = Camera.Zoom;
            float effectiveCameraX = Camera.X;
            float effectiveCameraY = Camera.Y;
            if (layer != null)
            {
                layer.GetEffectiveCamera(Camera, out effectiveCameraX, out effectiveCameraY, out effectiveZoom);
            }

            if (effectiveZoom != 1)
            {
                canvas.Scale(effectiveZoom);
            }

            var translateX = -effectiveCameraX;
            var translateY = -effectiveCameraY;

            if(Camera.CameraCenterOnScreen == CameraCenterOnScreen.Center)
            {
                translateX += (Camera.ClientWidth / 2.0f);
                translateY += (Camera.ClientHeight / 2.0f);
            }

            if(translateX != 0 || translateY != 0)
            {
                canvas.Translate(translateX, translateY);
            }

            if (matrix.HasValue)
            {
                SKMatrix callerMatrix = matrix.Value;
                canvas.Concat(in callerMatrix);
            }
        }

        #region Immediate mode (GumBatch)

        /// <summary>
        /// How <see cref="Draw(IRenderableIpso)"/> calls between a <see cref="Begin"/>/<see cref="End"/>
        /// pair are submitted. Same shape as the MonoGame renderer's enum.
        /// </summary>
        public enum GumBatchDrawMode
        {
            /// <summary>
            /// Each <see cref="Draw(IRenderableIpso)"/> call draws immediately, in call order.
            /// </summary>
            Immediate,

            /// <summary>
            /// <see cref="Draw(IRenderableIpso)"/> calls accumulate and draw at <see cref="End"/>,
            /// stable-sorted by <c>IRenderableIpso.Z</c> (equal Z keeps call order), the same order
            /// MonoGame's Deferred mode produces.
            /// </summary>
            Deferred,
        }

        SKCanvas? _cycleCanvas;
        GumBatchDrawMode _cycleMode;
        readonly List<IRenderableIpso> _deferredRoots = new List<IRenderableIpso>();

        /// <summary>
        /// Starts an immediate-mode cycle on <see cref="SystemManagers.Canvas"/>: applies the camera
        /// and, when given, <paramref name="matrix"/> before it. Does not clear the canvas, and does
        /// not dispose render-target textures a full-scene Draw baked earlier in the same frame.
        /// </summary>
        public void Begin(SKMatrix? matrix = null, GumBatchDrawMode mode = GumBatchDrawMode.Immediate)
        {
            SKCanvas canvas = OwnManagers.Canvas;
            TryStartHostFrameForBatch();
            _cycleCanvas = canvas;
            _cycleMode = mode;
            ApplyCamera(canvas, MainLayer, matrix);
        }

        /// <summary>
        /// Draws <paramref name="renderable"/> and its children between <see cref="Begin"/> and
        /// <see cref="End"/>. Render-target containers drawn this way are not baked; bake them with
        /// <see cref="PreRenderRoots"/> before <see cref="Begin"/>.
        /// </summary>
        public void Draw(IRenderableIpso renderable)
        {
            if (renderable.Visible)
            {
                renderable.PreRender();
                if (renderable.Children != null)
                {
                    PreRender(renderable.Children);
                }
            }

            if (_cycleMode == GumBatchDrawMode.Deferred)
            {
                // Visibility is checked at End, like MonoGame's deferred flush.
                _deferredRoots.Add(renderable);
            }
            else if (renderable.Visible)
            {
                DrawRenderable(renderable, OwnManagers);
            }
        }

        /// <summary>
        /// Ends the cycle started by <see cref="Begin"/>: in <see cref="GumBatchDrawMode.Deferred"/>
        /// draws the accumulated renderables sorted by Z, then restores the canvas matrix Begin changed.
        /// </summary>
        public void End()
        {
            try
            {
                if (_deferredRoots.Count > 0)
                {
                    // The same stable Z sort MonoGame's deferred flush uses. Layer 0's
                    // SecondarySortOnY is not applied there either, so equal Z keeps call order.
                    Layer.SortByZ(_deferredRoots);
                    SystemManagers managers = OwnManagers;
                    for (int i = 0; i < _deferredRoots.Count; i++)
                    {
                        IRenderableIpso renderable = _deferredRoots[i];
                        if (renderable.Visible)
                        {
                            DrawRenderable(renderable, managers);
                        }
                    }
                }
            }
            finally
            {
                // Cleared and restored even when a Render throws, so the next cycle neither redraws
                // this cycle's roots (#4584) nor inherits its matrix.
                _deferredRoots.Clear();
                // A Draw outside a Begin/End pair draws immediately instead of queuing for an End
                // that may never come.
                _cycleMode = GumBatchDrawMode.Immediate;
                if (_cycleCanvas != null)
                {
                    RestoreCamera(_cycleCanvas);
                }
                _cycleCanvas = null;
            }
        }

        readonly List<IRenderableIpso> _preRenderRootsBuffer = new List<IRenderableIpso>();

        /// <summary>
        /// Bakes render targets for roots drawn through <see cref="GumBatch"/> (or
        /// <see cref="Begin"/>/<see cref="Draw(IRenderableIpso)"/>/<see cref="End"/>), which does not
        /// bake them itself. Bakes every <see cref="IRenderableIpso.IsRenderTarget"/> container under
        /// <paramref name="roots"/>, including targets referenced by a
        /// <see cref="IRenderTargetTextureReferencer.RenderTargetTextureSource"/> in the roots, and
        /// keeps their surfaces from being swept. The roots do not need to be in a <see cref="Layer"/>.
        /// Call it once per frame (or once per batch of roots) before the <c>Begin</c> that draws
        /// them, never between <c>Begin</c> and <c>End</c>.
        /// </summary>
        /// <param name="roots">The roots about to be drawn.</param>
        public void PreRenderRoots(IEnumerable<IRenderableIpso> roots)
        {
            _preRenderRootsBuffer.Clear();
            _preRenderRootsBuffer.AddRange(roots);
            try
            {
                TryStartHostFrameForBatch();
                // Adds to this frame's referenced owners rather than replacing them, like MonoGame,
                // so a sprite in an earlier call keeps its invisible source baking in a later one.
                BakeRenderTargets(_preRenderRootsBuffer, OwnManagers);
            }
            finally
            {
                _preRenderRootsBuffer.Clear();
            }
        }

        // The first GumBatch call of a host frame (Begin or PreRenderRoots) runs the frame work, so
        // the full-scene Draw that may follow does not sweep again.
        private void TryStartHostFrameForBatch()
        {
            if (!_hostFrameStarted)
            {
                StartHostFrameRenderTargets();
                _batchStartedHostFrame = true;
            }
        }

        SystemManagers OwnManagers => _ownManagers
            ?? throw new InvalidOperationException("The Renderer has not been initialized. Call SystemManagers.Initialize first.");

        #endregion

        // A layer's top-level list is sorted after PreRender, like MonoGame's RenderLayer: a stable
        // sort on Z (then Y with SecondarySortOnY), every frame, so a PreRender that moves an element
        // sorts on the moved value. Children are never sorted.
        void DrawList(IList<IRenderableIpso> whatToRender, SystemManagers managers, Layer? layerToSort = null, bool prerender = true)
        {
            if (prerender)
            {
                PreRender(whatToRender);
            }
            layerToSort?.SortRenderables();

            var count = whatToRender.Count;

            for (int i = 0; i < count; i++)
            {
                var renderable = whatToRender[i];
                if (renderable.Visible)
                {
                    DrawRenderable(renderable, managers);
                }
            }
        }

        // Draws one visible renderable and (with RenderUsingHierarchy) its children.
        private void DrawRenderable(IRenderableIpso renderable, SystemManagers managers)
        {
            // A render-target container composites its baked offscreen texture in place of
            // walking its live children (its subtree was baked in the pre-pass, #3988). A
            // hidden one is skipped by the caller's Visible check, so no stale texture ghosts.
            if (renderable.IsRenderTarget)
            {
                CompositeRenderTarget(renderable, managers);
                return;
            }

            var canvas = managers.Canvas;

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
                    DrawList(renderable.Children, managers);
                }

                if (renderable.ClipsChildren)
                {
                    canvas.Restore();
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
        public SKImage? TryGetBakedRenderTargetFor(IRenderableIpso container)
            => _bakedImages.TryGetValue(ResolveRenderTargetCacheOwner(container), out SKImage? image)
                ? image
                : null;

        // The cached offscreen surface for a render-target container, for tests.
        internal SKSurface? TryGetRenderTargetSurfaceFor(IRenderableIpso container)
            => _renderTargetService.TryGetExisting(ResolveRenderTargetCacheOwner(container))?.Surface;

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
            SkiaRenderTarget? renderTarget = _renderTargetService.GetFor(owner, width, height);
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
            DrawList(container.Children, managers);
            managers.Canvas = screenCanvas;

            offscreenCanvas.Restore();

            // The snapshot stays valid across the frame via SkiaSharp copy-on-write; the persistent
            // surface is only re-cleared next frame, after DisposeBakedImages frees this snapshot.
            // A second bake in the same frame (PreRenderRoots then a full-scene Draw) replaces it.
            if (_bakedImages.TryGetValue(owner, out SKImage? previous))
            {
                previous?.Dispose();
            }
            _bakedImages[owner] = renderTarget.Surface.Snapshot();
        }

        // Bakes every render-target container's subtree under whatToRender into its offscreen
        // surface, marking it used so the next sweep keeps it. Render-target containers referenced
        // by a visible Sprite are collected first, so an invisible-but-referenced container still
        // bakes (#1643). Each bake swaps managers.Canvas to its own offscreen canvas and restores it.
        private void BakeRenderTargets(IList<IRenderableIpso> whatToRender, SystemManagers managers)
        {
            CollectReferencedRenderTargets(whatToRender);
            if (HasAnyRenderTarget(whatToRender))
            {
                BakeRenderTargetsInSubtree(whatToRender, managers);
            }
        }

        // Composites a render-target container's baked texture at the container's rectangle, honoring
        // group alpha and the container's Blend. No valid baked texture (degenerate size, or
        // off-screen) => draws nothing, so the subtree simply does not appear.
        private void CompositeRenderTarget(IRenderableIpso container, SystemManagers managers)
        {
            IRenderableIpso owner = ResolveRenderTargetCacheOwner(container);
            if (!_bakedImages.TryGetValue(owner, out SKImage? image) || image == null)
            {
                return;
            }

            int width = MathFunctions.RoundToInt(container.Width);
            int height = MathFunctions.RoundToInt(container.Height);
            if (width <= 0 || height <= 0)
            {
                return;
            }

            // Skia surfaces are premultiplied, so the baked color composites with the same blend a
            // shape with that Blend uses (ApplyBlend; Normal is the default SrcOver, which needs no
            // straight-vs-premultiplied correction unlike the XNA/raylib bake blend handling). An
            // unconfigured or unrecognized BlendState composites as Normal. Group alpha tints the
            // whole texture via the paint's alpha channel either way.
            byte alpha = (byte)System.Math.Clamp(container.Alpha, 0, 255);
            using SKPaint paint = new SKPaint();
            paint.Color = new SKColor(255, 255, 255, alpha);
            paint.ApplyBlend(Gum.RenderingLibrary.BlendExtensions.ToBlend(owner.BlendState)
                ?? Gum.RenderingLibrary.Blend.Normal);

            float left = container.GetAbsoluteX();
            float top = container.GetAbsoluteY();
            SKRect destination = new SKRect(left, top, left + width, top + height);

            // Optional post-process shader (ContainerRuntime.RenderTargetEffect / SourceShaderFile,
            // #3998). The effect is image-independent (declares a `uniform shader SpriteTexture`
            // child, the same texture name MonoGame .fx/.slang shaders use) -- the baked image is
            // only known here, at composite time, so it is bound as that child shader input on this
            // call rather than cached on the effect.
            SKRuntimeEffect? effect = (owner as IRenderTargetRenderable)?.RenderTargetEffect as SKRuntimeEffect;
            if (effect == null)
            {
                managers.Canvas.DrawImage(image, destination, SKSamplingOptions.Default, paint);
                return;
            }

            // SKCanvas.DrawImage ignores paint.Shader -- a paint's shader only takes effect on a
            // shape-drawing call, so the shaded path switches to DrawRect and feeds the baked image
            // in as the effect's "SpriteTexture" child shader instead of passing it as the image arg.
            // A plain image.ToShader() has an identity local matrix, so it samples using the raw
            // canvas/device coordinate passed to the runtime effect's main() -- correct only when
            // destination starts at (0, 0). The baked image's local (0, 0) corresponds to this
            // container's absolute (left, top), so the child shader needs a matching translation;
            // without it, any non-origin container reads the wrong (typically out-of-bounds,
            // edge-clamped) region of the baked image instead of its own contents (#4001).
            using SKShader imageShader = image.ToShader(
                SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, SKMatrix.CreateTranslation(left, top));
            using SKRuntimeEffectUniforms uniforms = new SKRuntimeEffectUniforms(effect);
            using SKRuntimeEffectChildren children = new SKRuntimeEffectChildren(effect);
            children["SpriteTexture"] = imageShader;

            // ShadowDusk converts a shader that reads COLOR0 or does UV arithmetic into SkSL that
            // declares these two uniforms, and an unset uniform reads as zero. White reproduces the
            // untinted result, and the container size is the size of the image being drawn.
            if (effect.Uniforms.Contains("ShadowDusk_Color"))
            {
                uniforms["ShadowDusk_Color"] = new float[] { 1f, 1f, 1f, 1f };
            }
            if (effect.Uniforms.Contains("ShadowDusk_Resolution"))
            {
                uniforms["ShadowDusk_Resolution"] = new float[] { width, height };
            }
            using SKShader combinedShader = effect.ToShader(uniforms, children);
            paint.Shader = combinedShader;
            managers.Canvas.DrawRect(destination, paint);
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

        private void RestoreCamera(SKCanvas canvas)
        {
            canvas.Restore();
            _cameraBaseMatrix = null;
            _cameraCallerMatrix = null;
        }

        /// <summary>
        /// Draws <paramref name="layer"/> on the current canvas under the layer's own camera, like
        /// MonoGame's RenderLayer: PreRenders it (unless <paramref name="prerender"/> is false), sorts
        /// it by Z, then draws it. A <see cref="SortableLayer"/> drawn as a renderable inside another
        /// layer comes through here.
        /// </summary>
        public void RenderLayer(ISystemManagers managers, Layer layer, bool prerender = true)
        {
            if (layer.Renderables.Count == 0)
            {
                return;
            }

            SystemManagers skiaManagers = (SystemManagers)managers;
            SKCanvas canvas = skiaManagers.Canvas;
            SKMatrix? outerBaseMatrix = _cameraBaseMatrix;
            SKMatrix? outerCallerMatrix = _cameraCallerMatrix;

            if (prerender)
            {
                // MonoGame's PreRender bakes render targets too. The bake draws on its own offscreen
                // canvas, so it is safe in the middle of the outer layer's walk.
                BakeRenderTargets(layer.Renderables, skiaManagers);
            }

            canvas.Save();
            if (outerBaseMatrix.HasValue)
            {
                // Drops the camera of the layer this one is drawn in, keeping the host's transform.
                canvas.SetMatrix(outerBaseMatrix.Value);
            }
            ApplyCamera(canvas, layer, outerCallerMatrix);
            try
            {
                DrawList(layer.Renderables, skiaManagers, layer, prerender);
            }
            finally
            {
                canvas.Restore();
                canvas.Restore();
                _cameraBaseMatrix = outerBaseMatrix;
                _cameraCallerMatrix = outerCallerMatrix;
            }
        }
    }
}
