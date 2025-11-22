using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace SkiaMonoGameRendering
{
    internal struct SkiaRenderableInfo
    {
        internal int TextureId;
        internal Texture2D Texture;
        internal int FramebufferId;
        internal int RenderbufferId;
        internal SKSurface Surface;
        internal GRBackendRenderTarget BackendRenderTarget;

        internal SkiaRenderableInfo(int textureId, Texture2D texture)
        {
            TextureId = textureId;
            Texture = texture;
            FramebufferId = 0;
            RenderbufferId = 0;
            Surface = null;
            BackendRenderTarget = null;
        }

        internal SkiaRenderableInfo(int textureId, Texture2D texture, int framebufferId, int renderbufferId, SKSurface surface, GRBackendRenderTarget backendRenderTarget)
        {
            TextureId = textureId;
            Texture = texture;
            FramebufferId = framebufferId;
            RenderbufferId = renderbufferId;
            Surface = surface;
            BackendRenderTarget = backendRenderTarget;
        }

        internal void ClearReferences()
        {
            Texture = null;
            BackendRenderTarget = null;
            Surface = null;
        }
    }
}
