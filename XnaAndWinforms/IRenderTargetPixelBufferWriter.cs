using System.Drawing;
using Microsoft.Xna.Framework.Graphics;

namespace XnaAndWinforms;

/// <summary>
/// Converts a raw pixel buffer read back from a GPU render target (via <c>RenderTarget2D.GetData</c>)
/// into a <see cref="Bitmap"/>'s pixel format and writes it into that bitmap's backing memory. This
/// is the "produce a pixel buffer" half of <see cref="GraphicsDeviceControl"/>'s paint sequence,
/// deliberately kept separate from the actual blit of that bitmap onto a WinForms <see cref="Graphics"/>
/// surface - so the conversion can be constructed and tested without a live GPU or window handle.
/// </summary>
public interface IRenderTargetPixelBufferWriter
{
    /// <summary>
    /// Writes <paramref name="rawImage"/> (as read back in <paramref name="sourceFormat"/>) into
    /// <paramref name="bitmap"/>'s backing memory, converting pixel byte order if needed.
    /// </summary>
    /// <exception cref="System.NotSupportedException">
    /// Thrown when <paramref name="sourceFormat"/> and <paramref name="bitmap"/>'s pixel format have
    /// no supported conversion.
    /// </exception>
    void WriteToBitmap(byte[] rawImage, SurfaceFormat sourceFormat, Bitmap bitmap);
}
