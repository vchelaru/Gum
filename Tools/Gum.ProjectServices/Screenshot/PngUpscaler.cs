using SkiaSharp;
using System;
using System.IO;

namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Enlarges a PNG file in place by a whole-number factor with nearest-neighbor sampling, so each source pixel
/// becomes a square block of identical pixels.
/// </summary>
public static class PngUpscaler
{
    /// <summary>
    /// Replaces the PNG at <paramref name="path"/> with a copy <paramref name="scale"/> times as wide and tall.
    /// A scale of 1 leaves the file untouched.
    /// </summary>
    public static void ScaleFile(string path, int scale)
    {
        if (scale < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be 1 or more.");
        }
        if (scale == 1)
        {
            return;
        }

        SKBitmap source;
        using (SKCodec codec = SKCodec.Create(path) ?? throw new InvalidOperationException($"Could not read {path} as an image."))
        {
            // Unpremultiplied, so pixels round-trip exactly; a premultiplied decode would round translucent colors.
            SKImageInfo info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            source = new SKBitmap(info);
            codec.GetPixels(info, source.GetPixels());
        }

        using (source)
        using (SKBitmap scaled = new SKBitmap(new SKImageInfo(
            source.Width * scale, source.Height * scale, SKColorType.Rgba8888, SKAlphaType.Unpremul)))
        {
            ReadOnlySpan<uint> from = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(source.GetPixelSpan());
            Span<uint> to = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(scaled.GetPixelSpan());
            int sourceWidth = source.Width;
            int scaledWidth = scaled.Width;
            for (int y = 0; y < scaled.Height; y++)
            {
                ReadOnlySpan<uint> sourceRow = from.Slice(y / scale * sourceWidth, sourceWidth);
                Span<uint> scaledRow = to.Slice(y * scaledWidth, scaledWidth);
                for (int x = 0; x < scaledWidth; x++)
                {
                    scaledRow[x] = sourceRow[x / scale];
                }
            }

            using SKData png = scaled.Encode(SKEncodedImageFormat.Png, 100);
            using FileStream stream = File.Create(path);
            png.SaveTo(stream);
        }
    }
}
