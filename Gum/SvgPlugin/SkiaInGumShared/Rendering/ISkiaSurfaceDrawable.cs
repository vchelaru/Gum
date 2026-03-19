using RenderingLibrary.Graphics;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace SkiaGum.Renderables
{
    /// <summary>
    /// Represents an object that can draw itself to a Skia surface.
    /// This is the shape concern only - no knowledge of Texture2D or MonoGame.
    /// </summary>
    public interface ISkiaSurfaceDrawable
    {
        float Width { get; set; }
        float Height { get; set; }

        bool NeedsUpdate { get; set; }

        Color Color { get; }

        bool ShouldApplyColorOnSpriteRender { get; }

        /// <summary>
        /// Extra pixels needed on each horizontal side to accommodate effects like dropshadow.
        /// </summary>
        float XSizeSpillover { get; }

        /// <summary>
        /// Extra pixels needed on each vertical side to accommodate effects like dropshadow.
        /// </summary>
        float YSizeSpillover { get; }

        ColorOperation ColorOperation { get; set; }

        /// <summary>
        /// Whether this drawable can render when Width or Height is zero or negative.
        /// When true, the texture renderer must still allocate enough space for the stroke.
        /// </summary>
        bool CanRenderAt0Dimension { get; }

        void DrawToSurface(SKSurface surface);

        void PreRender();
    }
}
