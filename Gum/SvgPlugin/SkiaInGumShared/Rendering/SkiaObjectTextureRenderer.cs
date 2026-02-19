using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;
using Color = System.Drawing.Color;

#if FAST_GL_SKIA_RENDERING
using SkiaMonoGameRendering;
#endif

namespace SkiaGum.Renderables
{
    /// <summary>
    /// Manages the Texture2D pipeline for a <see cref="ISkiaSurfaceDrawable"/>.
    /// Handles both the CPU-copy path (non-FAST_GL) and the OpenGL fast path (FAST_GL_SKIA_RENDERING).
    /// </summary>
    public class SkiaObjectTextureRenderer
#if FAST_GL_SKIA_RENDERING
        : ISkiaRenderable
#endif
    {
        readonly ISkiaSurfaceDrawable _drawable;

        /// <summary>
        /// When true, <see cref="PreRender"/> (or the GL fast-path) will redraw the surface.
        /// Callers should sync this from the owning object's needsUpdate flag before calling PreRender.
        /// </summary>
        public bool NeedsUpdate { get; set; }

        /// <summary>
        /// The texture produced by the last render. May be null before the first render.
        /// </summary>
        public Texture2D? Texture { get; private set; }

#if FAST_GL_SKIA_RENDERING
        public bool ClearCanvasOnRender { get; set; } = true;

        int ISkiaRenderable.TargetWidth => (int)Math.Min(2048, _drawable.Width + _drawable.XSizeSpillover * 2);
        int ISkiaRenderable.TargetHeight => (int)Math.Min(2048, _drawable.Height + _drawable.YSizeSpillover * 2);
        SKColorType ISkiaRenderable.TargetColorFormat => SKColorType.Rgba8888;
        bool ISkiaRenderable.ShouldRender => NeedsUpdate && _drawable.Width > 0 && _drawable.Height > 0;

        void ISkiaRenderable.NotifyDrawnTexture(Texture2D texture)
        {
            Texture = texture;
            NeedsUpdate = false;
        }

        void ISkiaRenderable.DrawToSurface(SKSurface surface) => _drawable.DrawToSurface(surface);
#endif

        public SkiaObjectTextureRenderer(ISkiaSurfaceDrawable drawable)
        {
            _drawable = drawable;
        }

        public void AddToManagers()
        {
#if FAST_GL_SKIA_RENDERING
            SkiaRenderer.AddRenderable(this);
#endif
        }

        public void RemoveFromManagers()
        {
#if FAST_GL_SKIA_RENDERING
            SkiaRenderer.RemoveRenderable(this);
#endif
        }

        /// <summary>
        /// CPU-copy pre-render path. Call this once per frame before the Render call.
        /// Under FAST_GL_SKIA_RENDERING this method is a no-op; the GL path is driven by <see cref="ISkiaRenderable"/>.
        /// </summary>
        public void PreRender()
        {
#if !FAST_GL_SKIA_RENDERING
            if (NeedsUpdate && _drawable.Width > 0 && _drawable.Height > 0)
            {
                if (Texture != null)
                {
                    Texture.Dispose();
                    Texture = null;
                }

                var colorType = SKImageInfo.PlatformColorType;

                var widthToUse = Math.Min(2048, _drawable.Width + _drawable.XSizeSpillover * 2);
                var heightToUse = Math.Min(2048, _drawable.Height + _drawable.YSizeSpillover * 2);

                var imageInfo = new SKImageInfo((int)widthToUse, (int)heightToUse, colorType, SKAlphaType.Premul);
                using (var surface = SKSurface.Create(imageInfo))
                {
                    // It's possible this can fail
                    if (surface != null)
                    {
                        _drawable.DrawToSurface(surface);

                        var skImage = surface.Snapshot();

                        Texture = RenderImageToTexture2D(skImage, SystemManagers.Default.Renderer.GraphicsDevice, colorType);
                        NeedsUpdate = false;
                    }
                }
            }
#endif
        }

        public static bool PremultiplyRenderToTexture { get; set; } = false;

        /// <summary>
        /// Renders an SKImage to a Texture2D using the argument graphics device and SKColorType.
        /// </summary>
        /// <remarks>
        /// The SKColorType parameter can have significant performance impacts. MonoGame (In FlatRedBall)
        /// uses a default color format of Rgba8888 on Windows. If the skiaColorType is Rgba8888, then the
        /// bytes can be copied directly from skia to a byte[] to be used on the Texture2D.SetData call. If a
        /// different format is used, then the data needs to be converted when copied, causing a much slower call.
        /// Note that using SKImageInfo.PlatformColorType on Windows will return Bgra8888 which requires a (slower) conversion.
        /// </remarks>
        /// <param name="image">The SKImage containing the rendered Skia objects.</param>
        /// <param name="graphicsDevice">The MonoGame Graphics Device</param>
        /// <param name="skiaColorType">The color type. See remarks for info on this parameter</param>
        /// <param name="forcedColor">Forced color to assign when rendering rather than the original color from Skia.</param>
        /// <returns>The new Texture2D instance.</returns>
        public static Texture2D RenderImageToTexture2D(SKImage image, GraphicsDevice graphicsDevice, SKColorType skiaColorType, Color? forcedColor = null)
        {
            var pixelMap = image.PeekPixels();
            var pointer = pixelMap.GetPixels();
            var originalPixels = new byte[image.Height * pixelMap.RowBytes];

            Marshal.Copy(pointer, originalPixels, 0, originalPixels.Length);

            var texture = new Texture2D(graphicsDevice, image.Width, image.Height);
            if (skiaColorType == SKColorType.Rgba8888)
            {
                texture.SetData(originalPixels);
            }
            else
            {
                // need a new byte[] to convert from BGRA to ARGB
                var convertedBytes = new byte[originalPixels.Length];

                if (PremultiplyRenderToTexture)
                {
                    for (int i = 0; i < convertedBytes.Length; i += 4)
                    {
                        var b = originalPixels[i + 0];
                        var g = originalPixels[i + 1];
                        var r = originalPixels[i + 2];
                        var a = originalPixels[i + 3];

                        if (forcedColor != null)
                        {
                            r = forcedColor.Value.R;
                            g = forcedColor.Value.G;
                            b = forcedColor.Value.B;
                        }

                        convertedBytes[i + 0] = r;
                        convertedBytes[i + 1] = g;
                        convertedBytes[i + 2] = b;
                        convertedBytes[i + 3] = a;
                    }
                }
                else
                {
                    for (int i = 0; i < convertedBytes.Length; i += 4)
                    {
                        var b = originalPixels[i + 0];
                        var g = originalPixels[i + 1];
                        var r = originalPixels[i + 2];
                        var a = originalPixels[i + 3];
                        var ratio = a / 255.0f;

                        if (forcedColor != null)
                        {
                            r = forcedColor.Value.R;
                            g = forcedColor.Value.G;
                            b = forcedColor.Value.B;
                        }

                        // output will always be premult so we need to unpremult
                        convertedBytes[i + 0] = (byte)(r / ratio + .5);
                        convertedBytes[i + 1] = (byte)(g / ratio + .5);
                        convertedBytes[i + 2] = (byte)(b / ratio + .5);
                        convertedBytes[i + 3] = a;
                    }
                }

                texture.SetData(convertedBytes);
            }

            return texture;
        }
    }
}
