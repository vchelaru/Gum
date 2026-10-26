using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Issue #6829: user-supplied <see cref="IImageDecoder"/>s run after the file is opened and before
/// the built-in texture creation. These tests are headless (no GraphicsDevice), so a decoder that
/// succeeds still fails at upload; they cover which decoders run and what reaches them.
/// </summary>
public class ImageDecoderTests : BaseTestClass
{
    private static readonly byte[] FileBytes = { 7, 6, 5, 4, 3, 2, 1 };

    public override void Dispose()
    {
        LoaderManager.Self.ImageDecoders.Clear();
        base.Dispose();
    }

    private static void ServeFile(string suffix, Func<Stream>? streamFactory = null)
    {
        FileManager.CustomGetStreamFromFile = path => path.EndsWith(suffix)
            ? (streamFactory?.Invoke() ?? new MemoryStream(FileBytes))
            : null!;
    }

    [Fact]
    public void Chain_Declines_ReturnsNullAndRewinds()
    {
        ImageDecoderChain chain = new ImageDecoderChain();
        FakeDecoder first = new FakeDecoder(result: false, readAll: true);
        FakeDecoder second = new FakeDecoder(result: false, readAll: true);
        chain.Add(first);
        chain.Add(second);
        Stream stream = new MemoryStream(FileBytes);

        DecodedImage? image = chain.TryDecode("a.fake6829", ref stream);

        image.ShouldBeNull();
        first.ReceivedBytes.ShouldBe(FileBytes);
        second.ReceivedBytes.ShouldBe(FileBytes);
        stream.Position.ShouldBe(0);
        stream.CanRead.ShouldBeTrue();
    }

    [Fact]
    public void Chain_FirstAccepting_StopsTheChain()
    {
        ImageDecoderChain chain = new ImageDecoderChain();
        FakeDecoder declines = new FakeDecoder(result: false);
        FakeDecoder accepts = new FakeDecoder(result: true);
        FakeDecoder neverAsked = new FakeDecoder(result: true);
        chain.Add(declines);
        chain.Add(accepts);
        chain.Add(neverAsked);
        Stream stream = new MemoryStream(FileBytes);

        DecodedImage? image = chain.TryDecode("a.fake6829", ref stream);

        image.ShouldBeSameAs(accepts.Image);
        declines.CallCount.ShouldBe(1);
        neverAsked.CallCount.ShouldBe(0);
    }

    [Fact]
    public void Chain_DecoderThrows_PropagatesAndStops()
    {
        ImageDecoderChain chain = new ImageDecoderChain();
        FakeDecoder after = new FakeDecoder(result: true);
        chain.Add(new FakeDecoder(throwMessage: "broken 6829"));
        chain.Add(after);
        Stream stream = new MemoryStream(FileBytes);

        Should.Throw<InvalidDataException>(() => chain.TryDecode("a.fake6829", ref stream))
            .Message.ShouldBe("broken 6829");
        after.CallCount.ShouldBe(0);
    }

    [Fact]
    public void Chain_NonSeekableStream_IsBufferedForEveryDecoderAndTheCaller()
    {
        ImageDecoderChain chain = new ImageDecoderChain();
        FakeDecoder first = new FakeDecoder(result: false, readAll: true);
        FakeDecoder second = new FakeDecoder(result: false, readAll: true);
        chain.Add(first);
        chain.Add(second);
        Stream stream = new NonSeekableStream(FileBytes);

        chain.TryDecode("a.fake6829", ref stream);

        first.ReceivedBytes.ShouldBe(FileBytes);
        second.ReceivedBytes.ShouldBe(FileBytes);
        stream.CanSeek.ShouldBeTrue();
        stream.Position.ShouldBe(0);
    }

    [Fact]
    public void Chain_DecoderDisposesStream_NextDecoderCanStillRead()
    {
        ImageDecoderChain chain = new ImageDecoderChain();
        FakeDecoder second = new FakeDecoder(result: false, readAll: true);
        chain.Add(new FakeDecoder(result: false, disposeStream: true));
        chain.Add(second);
        Stream stream = new MemoryStream(FileBytes);

        chain.TryDecode("a.fake6829", ref stream);

        second.ReceivedBytes.ShouldBe(FileBytes);
    }

    [Fact]
    public void DeclaredExtensions_AreLowerCaseWithoutDot()
    {
        ImageDecoderChain chain = new ImageDecoderChain();
        chain.Add(new FakeDecoder(result: false, extensions: new[] { ".WebP", "avif" }));
        chain.Add(new FakeDecoder(result: false, extensions: new[] { "webp" }));
        chain.Add(new FakeDecoder(result: false));

        chain.DeclaredExtensions.ShouldBe(new[] { "webp", "avif" });
    }

    [Fact]
    public void LoadContent_RegisteredDecoder_ReceivesTheOpenedFile()
    {
        ServeFile("served_6829.fake6829");
        FakeDecoder decoder = new FakeDecoder(result: false, readAll: true);
        LoaderManager.Self.ImageDecoders.Add(decoder);

        // Every decoder declines and there is no device, so the built-in path fails afterwards.
        Should.Throw<Exception>(() => new ContentLoader().LoadContent<Texture2D>("served_6829.fake6829"));

        decoder.ReceivedFileName.ShouldNotBeNull();
        decoder.ReceivedFileName!.ShouldEndWith("served_6829.fake6829");
        decoder.ReceivedBytes.ShouldBe(FileBytes);
    }

    [Fact]
    public void LoadContent_DecoderThrows_LoadFailsWithDecoderError()
    {
        ServeFile("throws_6829.fake6829");
        LoaderManager.Self.ImageDecoders.Add(new FakeDecoder(throwMessage: "decoder broke 6829"));

        Exception exception = Should.Throw<Exception>(() =>
            new ContentLoader().LoadContent<Texture2D>("throws_6829.fake6829"));

        exception.ShouldBeOfType<InvalidDataException>();
        exception.Message.ShouldBe("decoder broke 6829");
    }

    [Fact]
    public void LoadContent_MissingFile_NeverReachesDecoders()
    {
        FakeDecoder decoder = new FakeDecoder(result: true);
        LoaderManager.Self.ImageDecoders.Add(decoder);

        Texture2D? result = new ContentLoader().LoadContent<Texture2D>("missing_6829.fake6829");

        result.ShouldBeNull();
        decoder.CallCount.ShouldBe(0);
    }

    [Fact]
    public void LoadContent_UnknownExtensionNoDecoder_ErrorNamesHowToRegisterOne()
    {
        ServeFile("unknown_6829.fake6829");

        Exception exception = Should.Throw<Exception>(() =>
            new ContentLoader().LoadContent<Texture2D>("unknown_6829.fake6829"));

        exception.Message.ShouldContain("fake6829");
        exception.Message.ShouldContain(nameof(LoaderManager.ImageDecoders));
    }

    [Fact]
    public void PrepareDecodedPixels_Straight_PremultipliesWhenRendererIsPremultiplied()
    {
        byte[] rgba = { 200, 100, 50, 128 };

        bool unpremultiplied = ContentLoader.PrepareDecodedPixels(rgba, 1, 1, isPremultiplied: false,
            usePremultipliedAlpha: true, bleedTransparentEdges: false);

        rgba.ShouldBe(new byte[] { 100, 50, 25, 128 });
        unpremultiplied.ShouldBeFalse();
    }

    [Fact]
    public void PrepareDecodedPixels_Premultiplied_SkipsPremultiplyWhenRendererIsPremultiplied()
    {
        byte[] rgba = { 100, 50, 25, 128 };

        bool unpremultiplied = ContentLoader.PrepareDecodedPixels(rgba, 1, 1, isPremultiplied: true,
            usePremultipliedAlpha: true, bleedTransparentEdges: true);

        rgba.ShouldBe(new byte[] { 100, 50, 25, 128 });
        unpremultiplied.ShouldBeFalse();
    }

    [Fact]
    public void PrepareDecodedPixels_Premultiplied_UnpremultipliesWhenRendererIsStraight()
    {
        byte[] rgba = { 100, 50, 25, 128, 0, 0, 0, 0, 9, 8, 7, 255 };

        bool unpremultiplied = ContentLoader.PrepareDecodedPixels(rgba, 3, 1, isPremultiplied: true,
            usePremultipliedAlpha: false, bleedTransparentEdges: false);

        rgba.ShouldBe(new byte[] { 199, 100, 50, 128, 0, 0, 0, 0, 9, 8, 7, 255 });
        unpremultiplied.ShouldBeTrue();
    }

    private sealed class FakeDecoder : IImageDecoder
    {
        private readonly bool _result;
        private readonly bool _readAll;
        private readonly bool _disposeStream;
        private readonly string? _throwMessage;
        private readonly string[]? _extensions;

        public FakeDecoder(bool result = false, bool readAll = false, bool disposeStream = false,
            string? throwMessage = null, string[]? extensions = null)
        {
            _result = result;
            _readAll = readAll;
            _disposeStream = disposeStream;
            _throwMessage = throwMessage;
            _extensions = extensions;
        }

        public DecodedImage Image { get; } = new DecodedImage(new byte[] { 1, 2, 3, 4 }, 1, 1);
        public int CallCount { get; private set; }
        public string? ReceivedFileName { get; private set; }
        public byte[]? ReceivedBytes { get; private set; }
        public IEnumerable<string>? SupportedExtensions => _extensions;

        public bool TryDecode(string fileName, Stream stream, out DecodedImage? image)
        {
            CallCount++;
            ReceivedFileName = fileName;
            if (_throwMessage != null)
            {
                throw new InvalidDataException(_throwMessage);
            }
            if (_readAll)
            {
                using MemoryStream copy = new MemoryStream();
                stream.CopyTo(copy);
                ReceivedBytes = copy.ToArray();
            }
            if (_disposeStream)
            {
                stream.ReadByte();
                stream.Dispose();
            }
            image = _result ? Image : null;
            return _result;
        }
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] bytes) : base(bytes) { }
        public override bool CanSeek => false;
    }
}
