using Raylib_cs;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

/// <summary>
/// Issue #6829: raylib runs the registered <see cref="IImageDecoder"/>s before LoadImageFromMemory.
/// </summary>
public class ImageDecoderTests : BaseTestClass
{
    private static readonly byte[] FileBytes = { 7, 6, 5, 4 };
    private readonly Action<Texture2D, TextureFilter> _savedApplier = ContentLoader.TextureFilterApplier;
    private readonly List<Texture2D> _uploaded = new List<Texture2D>();
    private readonly List<byte[]> _uploadedPixels = new List<byte[]>();
    private readonly Action<byte[]>? _savedUploadObserver = ContentLoader.DecodedPixelsUploaded;

    public ImageDecoderTests()
    {
        ContentLoader.TextureFilterApplier = (texture, _) => _uploaded.Add(texture);
        ContentLoader.DecodedPixelsUploaded = pixels => _uploadedPixels.Add((byte[])pixels.Clone());
    }

    public override void Dispose()
    {
        ContentLoader.TextureFilterApplier = _savedApplier;
        ContentLoader.DecodedPixelsUploaded = _savedUploadObserver;
        LoaderManager.Self.ImageDecoders.Clear();
        base.Dispose();
    }

    private static string Serve(string name)
    {
        FileManager.CustomGetStreamFromFile = path => path.EndsWith(name) ? new MemoryStream(FileBytes) : null!;
        return Path.Combine(Path.GetTempPath(), "GumRaylib6829_" + Guid.NewGuid().ToString("N"), name);
    }

    [Fact]
    public void LoadContent_DecoderAccepts_UploadsItsPixelsOnceAndCaches()
    {
        string path = Serve("accepts_6829.fake6829");
        FakeDecoder decoder = new FakeDecoder(new DecodedImage(new byte[2 * 3 * 4], 2, 3));
        LoaderManager.Self.ImageDecoders.Add(decoder);

        LoaderManager.Self.LoadContent<Texture2D>(path);
        LoaderManager.Self.LoadContent<Texture2D>(path);

        decoder.CallCount.ShouldBe(1);
        decoder.ReceivedBytes.ShouldBe(FileBytes);
        _uploaded.Count.ShouldBe(1);
    }

    [Fact]
    public void LoadContent_DecoderThrows_LoadFailsWithDecoderError()
    {
        string path = Serve("throws_6829.fake6829");
        LoaderManager.Self.ImageDecoders.Add(new FakeDecoder(throwMessage: "decoder broke 6829"));

        Should.Throw<InvalidDataException>(() => LoaderManager.Self.LoadContent<Texture2D>(path))
            .Message.ShouldBe("decoder broke 6829");
    }

    [Fact]
    public void LoadContent_Premultiplied_IsConvertedToStraightAlpha()
    {
        string path = Serve("premultiplied_6829.fake6829");
        DecodedImage image = new DecodedImage(new byte[] { 100, 50, 25, 128 }, 1, 1, isPremultiplied: true);
        LoaderManager.Self.ImageDecoders.Add(new FakeDecoder(image));

        LoaderManager.Self.LoadContent<Texture2D>(path);

        _uploadedPixels.Count.ShouldBe(1);
        _uploadedPixels[0].ShouldBe(new byte[] { 199, 100, 50, 128 });
        // A decoder may return a cached instance, so its image must not be converted in place.
        image.Rgba.ShouldBe(new byte[] { 100, 50, 25, 128 });
        image.IsPremultiplied.ShouldBeTrue();
    }

    [Fact]
    public void LoadContent_Straight_PixelsAreUnchanged()
    {
        string path = Serve("straight_6829.fake6829");
        DecodedImage image = new DecodedImage(new byte[] { 100, 50, 25, 128 }, 1, 1);
        FakeDecoder decoder = new FakeDecoder(image);
        LoaderManager.Self.ImageDecoders.Add(decoder);

        LoaderManager.Self.LoadContent<Texture2D>(path);

        decoder.CallCount.ShouldBe(1);
        image.Rgba.ShouldBe(new byte[] { 100, 50, 25, 128 });
        _uploaded.Count.ShouldBe(1);
    }

    private sealed class FakeDecoder : IImageDecoder
    {
        private readonly DecodedImage? _image;
        private readonly string? _throwMessage;

        public FakeDecoder(DecodedImage? image = null, string? throwMessage = null)
        {
            _image = image;
            _throwMessage = throwMessage;
        }

        public int CallCount { get; private set; }
        public byte[]? ReceivedBytes { get; private set; }

        public bool TryDecode(string fileName, Stream stream, out DecodedImage? image)
        {
            CallCount++;
            if (_throwMessage != null)
            {
                throw new InvalidDataException(_throwMessage);
            }
            using MemoryStream copy = new MemoryStream();
            stream.CopyTo(copy);
            ReceivedBytes = copy.ToArray();
            image = _image;
            return _image != null;
        }
    }
}
