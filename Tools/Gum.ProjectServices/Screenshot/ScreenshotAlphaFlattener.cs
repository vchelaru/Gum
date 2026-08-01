using System;

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
/// </summary>
public static class ScreenshotAlphaFlattener
{
    public static void FlattenToOpaque(Span<byte> rgba)
    {
        for (int i = 3; i < rgba.Length; i += 4)
        {
            rgba[i] = 255;
        }
    }
}
