using System;
using System.IO;
using System.IO.Compression;

namespace PkgBench;

// SPIKE #6156 (not for merge). Blob encodings for raw premultiplied RGBA inside gum.pkg.
public static class PkgBlobs
{
    public static byte[] DecodePremultiplied(byte[] png, out int w, out int h)
    {
        var img = StbImageSharp.ImageResult.FromMemory(png, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        w = img.Width; h = img.Height;
        var d = img.Data;
        Premultiply(d);
        return d;
    }

    // Same rounding as RenderingLibrary TexturePremultiply (128 at alpha 128).
    public static void Premultiply(byte[] d)
    {
        for (int i = 0; i + 3 < d.Length; i += 4)
        {
            int a = d[i + 3];
            if (a == 255) continue;
            d[i] = (byte)((d[i] * a + 127) / 255);
            d[i + 1] = (byte)((d[i + 1] * a + 127) / 255);
            d[i + 2] = (byte)((d[i + 2] * a + 127) / 255);
        }
    }

    public static byte[] FilterUp(byte[] d, int w)
    {
        int stride = w * 4; var o = new byte[d.Length];
        for (int i = 0; i < d.Length; i++) o[i] = (byte)(d[i] - (i >= stride ? d[i - stride] : 0));
        return o;
    }

    public static byte[] FilterSub(byte[] d)
    {
        var o = new byte[d.Length];
        for (int i = 0; i < d.Length; i++) o[i] = (byte)(d[i] - (i >= 4 ? d[i - 4] : 0));
        return o;
    }

    public static byte[] Deflate(byte[] d, CompressionLevel level)
    {
        using var ms = new MemoryStream();
        using (var z = new DeflateStream(ms, level, true)) z.Write(d, 0, d.Length);
        return ms.ToArray();
    }

    public static byte[] Brotli(byte[] d, int quality)
    {
        var dst = new byte[BrotliEncoder.GetMaxCompressedLength(d.Length)];
        using var enc = new BrotliEncoder(quality, 22);
        using var ms = new MemoryStream();
        ReadOnlySpan<byte> src = d;
        System.Buffers.OperationStatus status;
        do
        {
            status = enc.Compress(src, dst, out int consumed, out int written, isFinalBlock: true);
            ms.Write(dst, 0, written);
            src = src.Slice(consumed);
        } while (status == System.Buffers.OperationStatus.DestinationTooSmall);
        if (status != System.Buffers.OperationStatus.Done) throw new Exception("brotli " + status);
        return ms.ToArray();
    }
}
