using SkiaSharp;

namespace SkiaGum.Renderables
{
    /// <summary>
    /// Represents an object that can draw itself to a Skia surface.
    /// This is the shape concern only - no knowledge of Texture2D or MonoGame.
    /// </summary>
    public interface ISkiaSurfaceDrawable
    {
        float Width { get; }
        float Height { get; }
        bool AbsoluteVisible { get; }

        /// <summary>
        /// Extra pixels needed on each horizontal side to accommodate effects like dropshadow.
        /// </summary>
        float XSizeSpillover { get; }

        /// <summary>
        /// Extra pixels needed on each vertical side to accommodate effects like dropshadow.
        /// </summary>
        float YSizeSpillover { get; }

        void DrawToSurface(SKSurface surface);
    }
}
