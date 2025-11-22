using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;

namespace SkiaMonoGameRendering
{
    public interface ISkiaRenderable
    {
        int TargetWidth { get; }
        int TargetHeight { get; }
        SKColorType TargetColorFormat { get; }
        bool ShouldRender { get; }
        bool ClearCanvasOnRender { get; }
        void DrawToSurface(SKSurface surface);
        void NotifyDrawnTexture(Texture2D texture);
    }
}
