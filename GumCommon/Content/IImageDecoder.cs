using System.Collections.Generic;
using System.IO;

namespace RenderingLibrary.Content;

/// <summary>
/// Decodes an image format Gum does not load on its own (for example WebP). Register instances in
/// <see cref="LoaderManager.ImageDecoders"/> before the project loads.
/// </summary>
public interface IImageDecoder
{
    /// <summary>
    /// Decodes <paramref name="stream"/>. Return false when the file is not this decoder's format, so
    /// the next decoder (and then Gum's built-in loading) gets a chance. Throw when the file is this
    /// decoder's format but cannot be decoded: the load fails with that exception and no other
    /// decoder runs.
    /// </summary>
    /// <param name="fileName">The file being loaded, as Gum resolved it. Useful for extension checks.</param>
    /// <param name="stream">A seekable stream positioned at the start of the file. Gum rewinds it
    /// before the next decoder and disposes it afterwards.</param>
    /// <param name="image">The decoded pixels when this returns true.</param>
    bool TryDecode(string fileName, Stream stream, out DecodedImage? image);

    /// <summary>
    /// Extensions (without the dot, for example "webp") this decoder handles. The Gum tool uses them
    /// to accept those files as a Source File. Null or empty declares none; the decoder still runs
    /// on every file.
    /// </summary>
    IEnumerable<string>? SupportedExtensions => null;
}
