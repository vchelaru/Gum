using Microsoft.Xna.Framework.Graphics;
using System.Windows.Media.Imaging;

namespace XnaAndWinforms;

/// <summary>
/// Owns a <see cref="WriteableBitmap"/> sized to match a render target, plus the raw pixel buffer
/// used to read that render target back into it. Framework-neutral aside from the WPF bitmap type
/// itself - no WPF <c>Image</c> element, no timer, no <c>GraphicsDevice</c> - so it can be
/// constructed and unit tested without a live render target or window.
/// </summary>
public interface IWriteableBitmapRenderSurface
{
    /// <summary>
    /// The bitmap a WPF <c>Image</c> element's <c>Source</c> can bind to. <see langword="null"/>
    /// until <see cref="Resize"/> has been called at least once.
    /// </summary>
    WriteableBitmap? Bitmap { get; }

    /// <summary>
    /// The buffer to pass to <c>RenderTarget2D.GetData</c>; sized to exactly match
    /// <see cref="Width"/> x <see cref="Height"/> x 4 bytes-per-pixel.
    /// </summary>
    byte[] RawImageBuffer { get; }

    /// <summary>The current bitmap width in pixels.</summary>
    int Width { get; }

    /// <summary>The current bitmap height in pixels.</summary>
    int Height { get; }

    /// <summary>
    /// (Re)creates <see cref="Bitmap"/> and <see cref="RawImageBuffer"/> for the given pixel
    /// dimensions. A no-op if the dimensions already match the current <see cref="Bitmap"/>.
    /// </summary>
    void Resize(int width, int height);

    /// <summary>
    /// Converts <see cref="RawImageBuffer"/> (already filled by the caller, e.g. via
    /// <c>RenderTarget2D.GetData</c>) into <see cref="Bitmap"/>'s backing memory.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when called before <see cref="Resize"/>.
    /// </exception>
    void Push(SurfaceFormat sourceFormat);
}
