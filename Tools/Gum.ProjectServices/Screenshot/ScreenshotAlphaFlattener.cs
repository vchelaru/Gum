using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Forces every pixel's alpha byte to fully opaque in an RGBA8-packed pixel buffer (4 bytes per
/// pixel, alpha last). A screenshot rendered against a requested <see
/// cref="ScreenshotRequest.BackgroundColor"/> should be fully opaque everywhere, but blending
/// translucent content (e.g. a semi-transparent panel) onto that opaque background does not
/// itself flatten the render target's alpha channel back to 255 - both the MonoGame and raylib
/// screenshot backends leave leftover partial alpha in those regions. That leftover alpha is
/// invisible when the PNG is only ever composited against the same background color it was
/// rendered with, but any other viewer or further compositing pass blends its own backdrop
/// through instead, making the requested background appear not to be there at all (#4172).
/// The tool's canvas also runs this on every opaque frame it shows.
/// </summary>
public static class ScreenshotAlphaFlattener
{
    public static void FlattenToOpaque(Span<byte> rgba)
    {
        // Sets the alpha byte of several pixels per operation: a byte-at-a-time loop over a
        // full canvas costs about 10 ms per frame in a Debug build.
        Span<uint> pixels = MemoryMarshal.Cast<byte, uint>(rgba);
        uint alphaMask = BitConverter.IsLittleEndian ? 0xFF000000u : 0x000000FFu;

        Span<Vector<uint>> vectors = MemoryMarshal.Cast<uint, Vector<uint>>(pixels);
        Vector<uint> vectorMask = new Vector<uint>(alphaMask);
        for (int i = 0; i < vectors.Length; i++)
        {
            vectors[i] |= vectorMask;
        }

        for (int i = vectors.Length * Vector<uint>.Count; i < pixels.Length; i++)
        {
            pixels[i] |= alphaMask;
        }
    }
}
