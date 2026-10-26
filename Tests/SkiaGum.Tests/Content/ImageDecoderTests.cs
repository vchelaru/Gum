using RenderingLibrary.Content;
using Shouldly;
using SkiaSharp;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace SkiaGum.Tests.Content;

/// <summary>
/// Issue #6829: SkiaGum runs the registered <see cref="IImageDecoder"/>s before SKBitmap.Decode, so
/// a decoder for a format Skia cannot read works the same as on the other runtimes.
/// </summary>
public class ImageDecoderTests : BaseTestClass
{
    private static readonly byte[] FileBytes = { 7, 6, 5, 4 };

    public override void Dispose()
    {
        LoaderManager.Self.ImageDecoders.Clear();
        base.Dispose();
    }

    private static string Serve(string name)
    {
        string path = Path.Combine(Path.GetTempPath(), "GumSkia6829_" + Guid.NewGuid().ToString("N"), name);
        FileManager.CustomGetStreamFromFile = incoming => incoming.EndsWith(name) ? new MemoryStream(FileBytes) : null!;
        return path;
    }

    [Theory]
    [InlineData(false, SKAlphaType.Unpremul)]
    [InlineData(true, SKAlphaType.Premul)]
    public void LoadContent_DecoderAccepts_ReturnsItsPixelsOnceWithItsAlphaType(bool isPremultiplied, SKAlphaType expected)
    {
        string path = Serve($"accepts_{isPremultiplied}_6829.fake6829");
        FakeDecoder decoder = new FakeDecoder(
            new DecodedImage(new byte[] { 100, 50, 25, 128, 1, 2, 3, 255 }, 2, 1, isPremultiplied));
        LoaderManager.Self.ImageDecoders.Add(decoder);
        ContentLoader loader = new ContentLoader();

        SKBitmap? bitmap = loader.LoadContent<SKBitmap>(path);
        loader.LoadContent<SKBitmap>(path);

        bitmap.ShouldNotBeNull();
        bitmap.Width.ShouldBe(2);
        bitmap.Height.ShouldBe(1);
        bitmap.AlphaType.ShouldBe(expected);
        bitmap.Bytes[4..8].ShouldBe(new byte[] { 1, 2, 3, 255 });
        decoder.CallCount.ShouldBe(1);
        decoder.ReceivedBytes.ShouldBe(FileBytes);
    }

    [Fact]
    public void LoadContent_DecoderThrows_LoadFailsWithDecoderError()
    {
        string path = Serve("throws_6829.fake6829");
        LoaderManager.Self.ImageDecoders.Add(new FakeDecoder(throwMessage: "decoder broke 6829"));

        Should.Throw<InvalidDataException>(() => new ContentLoader().LoadContent<SKBitmap>(path))
            .Message.ShouldBe("decoder broke 6829");
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
