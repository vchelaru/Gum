using System.IO;

namespace RenderingLibrary.Content;

/// <summary>
/// Decodes an image file to straight-alpha RGBA bytes with StbImageSharp, the decoder KNI's BlazorGL
/// <c>Texture2D.FromStream</c> uses, so the bytes match what FromStream would upload (issue #6522).
/// </summary>
/// <remarks>
/// Compiled into KniGum only, for its browser path in <see cref="ContentLoader"/>.
/// </remarks>
internal static class StbTextureDecoder
{
    /// <summary>
    /// Decodes <paramref name="stream"/> to tightly packed row-major RGBA bytes.
    /// </summary>
    internal static byte[] Decode(Stream stream, out int width, out int height)
    {
        StbImageSharp.ImageResult image =
            StbImageSharp.ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        width = image.Width;
        height = image.Height;
        return image.Data;
    }
}
