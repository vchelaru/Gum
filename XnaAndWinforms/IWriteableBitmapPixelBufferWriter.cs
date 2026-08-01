using Microsoft.Xna.Framework.Graphics;
using System.Windows.Media.Imaging;

namespace XnaAndWinforms;

/// <summary>
/// Converts a raw pixel buffer read back from a GPU render target (via <c>RenderTarget2D.GetData</c>)
/// into a <see cref="WriteableBitmap"/>'s backing memory, so a render surface can be displayed by a
/// plain WPF <c>Image</c> element. Kept separate from the render surface that owns the bitmap so the
/// conversion can be constructed and tested without a live GPU or window.
/// </summary>
public interface IWriteableBitmapPixelBufferWriter
{
    /// <summary>
    /// Writes <paramref name="rawImage"/> (as read back in <paramref name="sourceFormat"/>) into
    /// <paramref name="bitmap"/>'s backing memory, converting pixel byte order if needed.
    /// </summary>
    /// <exception cref="System.NotSupportedException">
    /// Thrown when <paramref name="sourceFormat"/> has no supported conversion to a BGRA-ordered
    /// destination.
    /// </exception>
    void WriteToBitmap(byte[] rawImage, SurfaceFormat sourceFormat, WriteableBitmap bitmap);
}
