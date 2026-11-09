namespace Gum;

/// <summary>The rectangles for copying a Skia GL texture (row 0 is the top of the image) to the default framebuffer (row 0 is the bottom).</summary>
internal static class GlBlitMath
{
    /// <summary>
    /// The destination corners for glBlitFramebuffer: the full framebuffer with y reversed (y0 is the top edge,
    /// y1 the bottom edge), which makes the blit flip the image so it appears upright.
    /// </summary>
    public static (int X0, int Y0, int X1, int Y1) FlippedDestination(int framebufferWidth, int framebufferHeight) =>
        (0, framebufferHeight, framebufferWidth, 0);
}
