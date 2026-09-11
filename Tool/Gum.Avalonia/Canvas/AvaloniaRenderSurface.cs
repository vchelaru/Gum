using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Xna.Framework.Graphics;
using XnaAndWinforms;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// Owns the <see cref="WriteableBitmap"/> a canvas shows and the raw buffer its render target is
/// read back into, sized together. The bitmap is RGBA at 96 DPI so a <see cref="SurfaceFormat.Color"/>
/// target copies straight across and one bitmap pixel covers one device-independent unit, the
/// same unit system the WPF surface uses.
/// </summary>
public sealed class AvaloniaRenderSurface : IDisposable
{
    /// <summary>The bitmap to show. Null until the first <see cref="Resize"/>.</summary>
    public WriteableBitmap? Bitmap { get; private set; }

    /// <summary>The buffer to pass to <c>RenderTarget2D.GetData</c>; width x height x 4 bytes.</summary>
    public byte[] RawImageBuffer { get; private set; } = Array.Empty<byte>();

    /// <summary>Current width in pixels.</summary>
    public int Width { get; private set; }

    /// <summary>Current height in pixels.</summary>
    public int Height { get; private set; }

    /// <summary>(Re)creates the bitmap and buffer for the given size; a no-op when unchanged.</summary>
    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width), $"{nameof(width)} and {nameof(height)} must both be positive, but were {width}x{height}.");
        }
        if (Bitmap != null && Width == width && Height == height)
        {
            return;
        }

        Bitmap?.Dispose();
        Width = width;
        Height = height;
        Bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul);
        RawImageBuffer = new byte[width * height * 4];
    }

    /// <summary>Copies <see cref="RawImageBuffer"/> (already read back) into the bitmap.</summary>
    public void Push(SurfaceFormat sourceFormat)
    {
        if (Bitmap == null)
        {
            throw new InvalidOperationException($"{nameof(Resize)} must be called before {nameof(Push)}.");
        }
        if (sourceFormat != SurfaceFormat.Color)
        {
            throw new NotSupportedException($"No pixel buffer conversion from {sourceFormat} to RGBA.");
        }

        using ILockedFramebuffer framebuffer = Bitmap.Lock();
        RawPixelBufferCopy.CopyDirect(RawImageBuffer, framebuffer.Address, framebuffer.RowBytes, Width, Height);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Bitmap?.Dispose();
        Bitmap = null;
    }
}
