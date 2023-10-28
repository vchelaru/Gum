using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using System.Collections.Generic;
using System;
using static SkiaMonoGameRendering.GlConstants;
using MgGl = SkiaMonoGameRendering.GlWrapper.MgGlFunctions;
using SkGl = SkiaMonoGameRendering.GlWrapper.SkGlFunctions;

namespace SkiaMonoGameRendering
{
    /// <summary>
    /// Skia renderer. You should call Draw() inside your game's draw call.
    /// </summary>
    public static class SkiaRenderer
    {
        public static int TextureCount
        {
            get
            {
                int count = 0;

                foreach (var info in _renderableInfos.Values)
                {
                    if (info.Texture != null)
                        count++;
                }

                return count;
            }
        }

        public static int RenderableCount { get { return _renderables.Count - _renderablesToRemove.Count; } }

        static readonly List<ISkiaRenderable> _renderables = new();
        static readonly List<ISkiaRenderable> _renderablesToRemove = new();
        static readonly Dictionary<ISkiaRenderable, SkiaRenderableInfo> _renderableInfos = new();
        static readonly List<SkiaRenderableInfo> _renderableInfosToClear = new();

        /// <summary>
        /// Checks if a renderable is being managed by this renderer.
        /// </summary>
        /// <param name="renderable">Renderable to check.</param>
        /// <returns>True if the renderable is being managed, false otherwise.</returns>
        public static bool IsManaging(ISkiaRenderable renderable)
        {
            return _renderables.Contains(renderable) && !_renderablesToRemove.Contains(renderable);
        }

        /// <summary>
        /// Adds a renderable to the render list.
        /// </summary>
        /// <param name="renderable">Renderable to be rendered. ISkiaRenderable properties will determine the rendering requirements.</param>
        /// <exception cref="ArgumentException">An exception is thrown if the renderable is already managed.</exception>
        public static void AddRenderable(ISkiaRenderable renderable)
        {
            if (IsManaging(renderable))
                throw new ArgumentException("The renderable is already being managed.", nameof(renderable));

            _renderables.Add(renderable);
        }

        /// <summary>
        /// Removes a renderable from the render list.
        /// </summary>
        /// <param name="renderable">Renderable to be removed.</param>
        /// <exception cref="ArgumentException">An exception is thrown if the renderable isn't managed.</exception>
        public static void RemoveRenderable(ISkiaRenderable renderable)
        {
            if (!IsManaging(renderable))
                throw new ArgumentException("Can't remove the renderable because it isn't managed.", nameof(renderable));

            // Skia objects need to be eliminated in the Skia context
            // so we store them in another list to process them later.
            if (!_renderablesToRemove.Contains(renderable))
                _renderablesToRemove.Add(renderable);
        }

        static SurfaceFormat SkColorFormatToMgColorFormat(SKColorType color)
        {
            switch (color)
            {
                case SKColorType.Rgba1010102:
                    return SurfaceFormat.Rgba1010102;
                case SKColorType.Rgba16161616:
                    return SurfaceFormat.Rgba64;
                case SKColorType.Alpha8:
                    return SurfaceFormat.Alpha8;
#if !FNA
                 case SKColorType.Bgra8888:
                     return SurfaceFormat.Bgra32;
#endif
                case SKColorType.Rg1616:
                    return SurfaceFormat.Rg32;
                default: // If no better match found use the default MonoGame format
                    return SurfaceFormat.Color;
            }
        }

        static SkiaRenderableInfo CreateNewTextureAndInfo(ISkiaRenderable renderable, SkiaRenderableInfo? oldInfo)
        {
            var graphicsDevice = SkiaGlManager.GraphicsDevice;

            if (oldInfo.HasValue)
                _renderableInfosToClear.Add(oldInfo.Value); // Mark the info for clearing

            // Create the new texture
            var texture = new Texture2D(graphicsDevice, renderable.TargetWidth, renderable.TargetHeight,
                false, SkColorFormatToMgColorFormat(renderable.TargetColorFormat));

            // Get the texture ID
            MgGl.GetInteger(GL_TEXTURE_BINDING_2D, out var textureId);

            return new SkiaRenderableInfo(textureId, texture, 0, 0, null, null);
        }

        /// <summary>
        /// Renders the active Skia renderables and delivers the generated textures.
        /// </summary>
        public static void Draw()
        {
            var doAnyNeedToRender = false;
            // Create textures in MonoGame and cache them along with their IDs
            for (int i = 0; i < _renderables.Count; i++)
            {
                var renderable = _renderables[i];

                if (renderable.ShouldRender && renderable.TargetWidth > 0 && renderable.TargetHeight > 0)
                {
                    if (_renderableInfos.TryGetValue(renderable, out SkiaRenderableInfo info))
                    {
                        if (info.Texture == null || renderable.TargetWidth != info.Texture.Width || renderable.TargetHeight != info.Texture.Height
                            || SkColorFormatToMgColorFormat(renderable.TargetColorFormat) != info.Texture.Format)
                        {
                            _renderableInfos[renderable] = CreateNewTextureAndInfo(renderable, info);
                        }
                    }
                    else
                    {
                        _renderableInfos.Add(renderable, CreateNewTextureAndInfo(renderable, null));
                    }
                }
                doAnyNeedToRender = doAnyNeedToRender || renderable.ShouldRender;
            }


            /////////////////////////Early Out///////////////////////////
            if(!doAnyNeedToRender)
            {
                return;
            }
            //////////////////////End Early Out//////////////////////////


            // Make the Skia OpenGL context current
            SkiaGlManager.SetSkiaContextAsCurrent();

            // Reset the Skia context object so Skia knows OpenGL state has been changed by MonoGame
            SkiaGlManager.SkiaGrContext.ResetContext();

            // For each renderable
            for (int i = 0; i < _renderables.Count; i++)
            {
                var renderable = _renderables[i];

                if (!renderable.ShouldRender)
                    continue;

                int textureWidth = renderable.TargetWidth;
                int textureHeight = renderable.TargetHeight;
                var skColor = renderable.TargetColorFormat;
                var info = _renderableInfos[renderable];
                var surface = info.Surface;
                var backendRenderTarget = info.BackendRenderTarget;

                bool isRenderTargetSet = false;

                if (surface == null || backendRenderTarget == null)
                {
                    // If the Skia objects are null it means the texture properties
                    // changed and the objects need to be recreated.

                    // Get the sample count
                    SkGl.GetInteger(GL_SAMPLES, out var samples);
                    var maxSamples = SkiaGlManager.SkiaGrContext.GetMaxSurfaceSampleCount(skColor);
                    if (samples > maxSamples)
                        samples = maxSamples;

                    int framebufferId = 0;
                    int renderbufferId = 0;

                    // Use a single buffer for stencil and depth
                    SkGl.GenRenderbuffers(1, out renderbufferId);
                    SkGl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbufferId);
                    SkGl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, textureWidth, textureHeight);
                    SkGl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

                    // Generate a framebuffer and bind it
                    SkGl.GenFramebuffers(1, out framebufferId);
                    SkGl.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

                    // Attach color to the texture we created in the MonoGame context
                    SkGl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                        FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, info.TextureId, 0);

                    // Attach depth buffer
                    SkGl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, renderbufferId);

                    // Attach stencil buffer
                    SkGl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                        FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, renderbufferId);

                    var framebufferStatus = SkGl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                    if (framebufferStatus != FramebufferErrorCode.FramebufferComplete && framebufferStatus != FramebufferErrorCode.FramebufferCompleteExt)
                        throw new Exception("Skia framebuffer creation failed.");

                    isRenderTargetSet = true;

                    var skiaFramebufferInfo = new GRGlFramebufferInfo((uint)framebufferId, skColor.ToGlSizedFormat());

                    // Create the new Skia objects
                    backendRenderTarget = new GRBackendRenderTarget(textureWidth, textureHeight, samples, 8, skiaFramebufferInfo);
                    surface = SKSurface.Create(SkiaGlManager.SkiaGrContext, backendRenderTarget, GRSurfaceOrigin.TopLeft, skColor);

                    // Update the info with the new Skia objects and OpenGL IDs.
                    // It's a struct so it shouldn't trigger GC collections.
                    _renderableInfos[renderable] = new SkiaRenderableInfo(info.TextureId, info.Texture, framebufferId, renderbufferId, surface, backendRenderTarget);
                }

                if (!isRenderTargetSet) // Bind the framebuffer if it wasn't already
                    SkGl.BindFramebuffer(FramebufferTarget.Framebuffer, info.FramebufferId);

                if(renderable.ClearCanvasOnRender)
                {
                    surface.Canvas.Clear(); // Clear the canvas
                }
                renderable.DrawToSurface(surface); // Perform all the drawing
                surface.Flush(); // Send the data to the GPU

                // Unbind the framebuffer
                SkGl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                // Deliver the texture to the renderable object
                renderable.NotifyDrawnTexture(info.Texture);
            }

            // Clear old Skia resources
            ManageSkiaDataToClear();

            // Make the MonoGame GL context current
            SkiaGlManager.SetMonoGameContextAsCurrent();

            // Clear old MonoGame resources
            ManageMonoGameDataToClear();
        }

        private static void ManageSkiaDataToClear()
        {
            // Don't clear the lists yet, we need them for the MonoGame context cleanup
            for (int i = 0; i < _renderablesToRemove.Count; i++)
            {
                var renderable = _renderablesToRemove[i];
                if (_renderableInfos.TryGetValue(renderable, out var info))
                {
                    info.Surface.Dispose();
                    info.BackendRenderTarget.Dispose();
                    DeleteBuffers(info);
                }
            }

            for (int i = 0; i < _renderableInfosToClear.Count; i++)
            {
                var info = _renderableInfosToClear[i];
                info.Surface.Dispose();
                info.BackendRenderTarget.Dispose();
                DeleteBuffers(info);
            }
        }

        private static void ManageMonoGameDataToClear()
        {
            for (int i = 0; i < _renderablesToRemove.Count; i++)
            {
                var renderable = _renderablesToRemove[i];
                if (_renderableInfos.TryGetValue(renderable, out var info))
                {
                    info.Texture.Dispose();
                    info.ClearReferences();

                    _renderableInfos.Remove(renderable);
                }

                _renderables.Remove(renderable);
            }

            for (int i = 0; i < _renderableInfosToClear.Count; i++)
            {
                var info = _renderableInfosToClear[i];
                info.Texture.Dispose();
                info.ClearReferences();
            }

            // Remove everything from the lists
            _renderablesToRemove.Clear();
            _renderableInfosToClear.Clear();
        }

        private static void DeleteBuffers(SkiaRenderableInfo info)
        {
            if (info.FramebufferId > 0)
            {
                // Detach and unbind everything just in case:
                // https://stackoverflow.com/questions/56254797/opengl-es-2-0-gldeleteframebuffers-after-drawing-to-texture
                SkGl.BindFramebuffer(FramebufferTarget.Framebuffer, info.FramebufferId);
                SkGl.InvalidateFramebuffer(FramebufferTarget.Framebuffer, 3, SkGl.FramebufferAttachements);
                SkGl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, 0, 0);
                SkGl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
                SkGl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, 0);
                SkGl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                SkGl.DeleteFramebuffers(1, ref info.FramebufferId);
            }

            if (info.RenderbufferId > 0)
                SkGl.DeleteRenderbuffers(1, ref info.RenderbufferId);
        }
    }
}
