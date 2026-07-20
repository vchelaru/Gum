using Microsoft.Xna.Framework.Graphics;
using System.Windows.Media.Imaging;

namespace XnaAndWinforms;

/// <summary>
/// The WPF-native counterpart to <see cref="IRenderTargetPixelBufferWriter"/>: converts a raw pixel
/// buffer read back from a GPU render target (via <c>RenderTarget2D.GetData</c>) into a
/// <see cref="WriteableBitmap"/>'s backing memory, so a render surface can be displayed by a plain
/// WPF <c>Image</c> element without going through <c>System.Drawing.Bitmap</c>/GDI+.
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
