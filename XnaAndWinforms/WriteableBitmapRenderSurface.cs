using Microsoft.Xna.Framework.Graphics;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XnaAndWinforms;

/// <summary>
/// The real implementation of <see cref="IWriteableBitmapRenderSurface"/>. Delegates the actual
/// pixel conversion to <see cref="IWriteableBitmapPixelBufferWriter"/>; this class's own
/// responsibility is sizing the <see cref="WriteableBitmap"/>/raw buffer pair and avoiding
/// unnecessary reallocation when the size hasn't changed between frames.
/// </summary>
public class WriteableBitmapRenderSurface : IWriteableBitmapRenderSurface
{
    private readonly IWriteableBitmapPixelBufferWriter _pixelBufferWriter;

    /// <inheritdoc/>
    public WriteableBitmap? Bitmap { get; private set; }

    /// <inheritdoc/>
    public byte[] RawImageBuffer { get; private set; } = Array.Empty<byte>();

    /// <inheritdoc/>
    public int Width { get; private set; }

    /// <inheritdoc/>
    public int Height { get; private set; }

    public WriteableBitmapRenderSurface(IWriteableBitmapPixelBufferWriter pixelBufferWriter)
    {
        _pixelBufferWriter = pixelBufferWriter;
    }

    /// <inheritdoc/>
    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width), $"{nameof(width)} and {nameof(height)} must both be positive, but were {width}x{height}.");
        }

        if (Bitmap != null && Width == width && Height == height)
        {
            // Already the right size - avoid churning a new bitmap/buffer every frame.
            return;
        }

        Width = width;
        Height = height;
        Bitmap = new WriteableBitmap(width, height, dpiX: 96, dpiY: 96, PixelFormats.Pbgra32, palette: null);
        RawImageBuffer = new byte[width * height * 4];
    }

    /// <inheritdoc/>
    public void Push(SurfaceFormat sourceFormat)
    {
        if (Bitmap == null)
        {
            throw new InvalidOperationException($"{nameof(Resize)} must be called before {nameof(Push)}.");
        }

        _pixelBufferWriter.WriteToBitmap(RawImageBuffer, sourceFormat, Bitmap);
    }
}
