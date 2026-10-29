using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RenderingLibrary.Content;

/// <summary>
/// The ordered list of <see cref="IImageDecoder"/>s that Gum's content loaders ask before their
/// built-in image loading. Reached through <see cref="LoaderManager.ImageDecoders"/>.
/// </summary>
public sealed class ImageDecoderChain : IEnumerable<IImageDecoder>
{
    private readonly List<IImageDecoder> _decoders = new List<IImageDecoder>();

    /// <summary>The number of registered decoders.</summary>
    public int Count => _decoders.Count;

    /// <summary>Adds a decoder after the existing ones.</summary>
    public void Add(IImageDecoder decoder) =>
        _decoders.Add(decoder ?? throw new ArgumentNullException(nameof(decoder)));

    /// <summary>Inserts a decoder at <paramref name="index"/>, so it is asked before later ones.</summary>
    public void Insert(int index, IImageDecoder decoder) =>
        _decoders.Insert(index, decoder ?? throw new ArgumentNullException(nameof(decoder)));

    /// <summary>Removes a decoder. Returns false if it was not registered.</summary>
    public bool Remove(IImageDecoder decoder) => _decoders.Remove(decoder);

    /// <summary>Removes every decoder.</summary>
    public void Clear() => _decoders.Clear();

    /// <summary>
    /// The extensions (lower case, no dot) the registered decoders declare through
    /// <see cref="IImageDecoder.SupportedExtensions"/>.
    /// </summary>
    public IReadOnlyCollection<string> DeclaredExtensions =>
        _decoders
            .SelectMany(item => item.SupportedExtensions ?? Enumerable.Empty<string>())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.TrimStart('.').ToLowerInvariant())
            .Distinct()
            .ToArray();

    /// <summary>
    /// Asks each decoder in order and returns the first image one produces, or null when every
    /// decoder declines. A decoder's exception propagates and stops the chain.
    /// </summary>
    /// <param name="fileName">The file being loaded.</param>
    /// <param name="stream">The opened file. A stream that cannot seek is replaced by an in-memory
    /// copy (and disposed), so the caller can still read it from the start when every decoder
    /// declines. On return it is positioned where it was on entry.</param>
    public DecodedImage? TryDecode(string fileName, ref Stream stream)
    {
        if (_decoders.Count == 0)
        {
            return null;
        }

        if (!stream.CanSeek)
        {
            var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            stream.Dispose();
            buffer.Position = 0;
            stream = buffer;
        }

        long start = stream.Position;
        var guarded = new NonClosingStream(stream);
        // Snapshot so a decoder that edits the list mid-load does not break enumeration.
        foreach (IImageDecoder decoder in _decoders.ToArray())
        {
            stream.Position = start;
            if (decoder.TryDecode(fileName, guarded, out DecodedImage? image))
            {
                return image ?? throw new InvalidOperationException(
                    $"{decoder.GetType().FullName} returned true from TryDecode for {fileName} but gave no image.");
            }
        }
        stream.Position = start;
        return null;
    }

    /// <inheritdoc/>
    public IEnumerator<IImageDecoder> GetEnumerator() => _decoders.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Passes reads and seeks through but ignores Dispose, so a decoder that disposes the stream
    /// does not close it for the next decoder or the built-in path.
    /// </summary>
    private sealed class NonClosingStream : Stream
    {
        private readonly Stream _inner;
        public NonClosingStream(Stream inner) => _inner = inner;
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
