using Microsoft.Xna.Framework.Graphics;
using System.Drawing.Imaging;

namespace XnaAndWinforms;

/// <summary>
/// The way a render target's raw pixel buffer must be transformed before it can be blitted into
/// a <see cref="System.Drawing.Bitmap"/> of a given <see cref="PixelFormat"/>.
/// </summary>
public enum PixelBufferConversionStrategy
{
    /// <summary>
    /// The source and destination byte orders differ (RGBA vs BGRA), so each pixel's color
    /// channels must be swapped while copying.
    /// </summary>
    ByteSwapRgbaToBgra,

    /// <summary>
    /// The source and destination byte orders already match, so the raw bytes can be copied
    /// straight across.
    /// </summary>
    DirectCopy
}

/// <summary>
/// Decides how to convert the raw byte buffer read back from a <see cref="RenderTarget2D"/>
/// (via <c>GetData</c>) into the pixel layout a <see cref="System.Drawing.Bitmap"/> expects.
/// Extracted from <c>GraphicsDeviceControl</c> so this format-driven decision - independent of
/// any live <see cref="GraphicsDevice"/> or bitmap - can be unit-tested directly.
/// </summary>
public static class RenderTargetPixelBufferConverter
{
    /// <summary>
    /// Returns the conversion strategy for the given source/destination format pair, or
    /// <see langword="null"/> if the combination isn't supported.
    /// </summary>
    public static PixelBufferConversionStrategy? GetStrategy(SurfaceFormat sourceFormat, PixelFormat destinationFormat)
    {
        bool destinationIsArgbOrPArgb =
            destinationFormat == PixelFormat.Format32bppArgb ||
            destinationFormat == PixelFormat.Format32bppPArgb;

        if (!destinationIsArgbOrPArgb)
        {
            return null;
        }

        return GetStrategyForBgraDestination(sourceFormat);
    }

    /// <summary>
    /// Returns the conversion strategy for a destination whose byte order is already known to be
    /// BGRA-ordered 32bpp (e.g. a GDI+ <see cref="PixelFormat.Format32bppPArgb"/>
    /// <see cref="System.Drawing.Bitmap"/>, or a WPF <c>WriteableBitmap</c> using
    /// <c>PixelFormats.Pbgra32</c>/<c>PixelFormats.Bgra32</c> - both share the same in-memory byte
    /// layout as their GDI+ counterparts), or <see langword="null"/> if <paramref name="sourceFormat"/>
    /// isn't supported.
    /// </summary>
    public static PixelBufferConversionStrategy? GetStrategyForBgraDestination(SurfaceFormat sourceFormat)
    {
        if (sourceFormat == SurfaceFormat.Color)
        {
            return PixelBufferConversionStrategy.ByteSwapRgbaToBgra;
        }

        if (sourceFormat == SurfaceFormat.Bgra32)
        {
            return PixelBufferConversionStrategy.DirectCopy;
        }

        return null;
    }
}
