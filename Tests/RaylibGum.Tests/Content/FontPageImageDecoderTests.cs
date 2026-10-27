using Raylib_cs;
using RaylibGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

/// <summary>
/// Issue #6891: bitmap font pages go through the registered <see cref="IImageDecoder"/>s, as sprite textures do.
/// </summary>
public class FontPageImageDecoderTests : BaseTestClass
{
    private static readonly byte[] CustomBytes = { 9, 8, 7, 6 };
    private readonly bool _savedCacheTextures = LoaderManager.Self.CacheTextures;

    public FontPageImageDecoderTests()
    {
        LoaderManager.Self.CacheTextures = false;
    }

    public override void Dispose()
    {
        LoaderManager.Self.ImageDecoders.Clear();
        LoaderManager.Self.CacheTextures = _savedCacheTextures;
        base.Dispose();
    }

    private static string FntText(string pageExtension, int pageCount)
    {
        StringBuilder text = new StringBuilder();
        text.Append("info face=\"Arial\" size=-18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n");
        text.Append($"common lineHeight=21 base=17 scaleW=8 scaleH=8 pages={pageCount} packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\n");
        for (int i = 0; i < pageCount; i++)
        {
            text.Append($"page id={i} file=\"Page{i}.{pageExtension}\"\n");
        }
        text.Append($"chars count={pageCount}\n");
        for (int i = 0; i < pageCount; i++)
        {
            text.Append($"char id={65 + i}   x=1     y=1     width=2     height=2     xoffset=0     yoffset=0     xadvance=3     page={i}  chnl=15\n");
        }
        return text.ToString();
    }

    // Serves the .fnt and its pages from the stream hook; returns the .fnt path (not on disk) and records requested paths.
    private static string ServeFont(string fntText, byte[] pageBytes, List<string> requested)
    {
        FileManager.CustomGetStreamFromFile = incomingPath =>
        {
            requested.Add(incomingPath);
            string fileNameOnly = Path.GetFileName(incomingPath);
            if (string.Equals(fileNameOnly, "Font6891.fnt", StringComparison.OrdinalIgnoreCase))
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(fntText));
            }
            if (fileNameOnly.StartsWith("Page", StringComparison.OrdinalIgnoreCase))
            {
                return new MemoryStream(pageBytes);
            }
            return null!;
        };
        return Path.Combine(Path.GetTempPath(), "GumRaylib6891_" + Guid.NewGuid().ToString("N"), "sub", "Font6891.fnt");
    }

    [Fact]
    public void LoadContent_MultiPageFont_DecoderAcceptsPages_LoadsFontWithMergedAtlas()
    {
        List<string> requested = new List<string>();
        string fntPath = ServeFont(FntText("fake6891", 2), CustomBytes, requested);
        FakeDecoder decoder = new FakeDecoder(new DecodedImage(new byte[8 * 8 * 4], 8, 8));
        LoaderManager.Self.ImageDecoders.Add(decoder);

        Font font = LoaderManager.Self.LoadContent<Font>(fntPath);

        decoder.Paths.Count.ShouldBe(2);
        decoder.Paths[0].ShouldEndWith(Path.Combine("sub", "Page0.fake6891"));
        decoder.Paths[1].ShouldEndWith(Path.Combine("sub", "Page1.fake6891"));
        decoder.ReceivedBytes.ShouldBe(CustomBytes);
        font.GlyphCount.ShouldBe(2);
        font.Texture.Width.ShouldBe(8);
        font.Texture.Height.ShouldBe(16);
        new ManagedFont(font).Dispose();
    }

    [Fact]
    public void LoadContent_SinglePageFont_DecoderAcceptsPage_LoadsFont()
    {
        List<string> requested = new List<string>();
        string fntPath = ServeFont(FntText("fake6891", 1), CustomBytes, requested);
        FakeDecoder decoder = new FakeDecoder(new DecodedImage(new byte[8 * 8 * 4], 8, 8));
        LoaderManager.Self.ImageDecoders.Add(decoder);

        Font font = LoaderManager.Self.LoadContent<Font>(fntPath);

        decoder.Paths.Count.ShouldBe(1);
        font.Texture.Width.ShouldBe(8);
        font.Texture.Height.ShouldBe(8);
        new ManagedFont(font).Dispose();
    }

    // The #6847/#6849 behavior: an undecodable page reports through PropertyAssignmentError and yields an empty Font.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void LoadContent_Font_NoDecoderAndUndecodablePage_ReportsErrorNamingPageAndReturnsEmptyFont(int pageCount)
    {
        string fntPath = ServeFont(FntText("fake6891", pageCount), CustomBytes, new List<string>());
        List<string> errors = new List<string>();
        Action<string> errorHandler = errors.Add;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += errorHandler;
        try
        {
            Font font = LoaderManager.Self.LoadContent<Font>(fntPath);

            font.BaseSize.ShouldBe(0);
            errors.ShouldContain(message => message.Contains("Page0.fake6891") && message.Contains("could not be decoded"));
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= errorHandler;
        }
    }

    [Fact]
    public void LoadContent_MultiPageFont_DecoderReturnsNull_FallsThroughToNative()
    {
        byte[] pngBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Content", "FontCache", "Font18Arial_0.png"));
        string fntPath = ServeFont(FntText("png", 2), pngBytes, new List<string>());
        FakeDecoder decoder = new FakeDecoder(null);
        LoaderManager.Self.ImageDecoders.Add(decoder);

        Font font = LoaderManager.Self.LoadContent<Font>(fntPath);

        decoder.Paths.Count.ShouldBe(2);
        font.Texture.Width.ShouldBe(256);
        font.Texture.Height.ShouldBe(512);
        new ManagedFont(font).Dispose();
    }

    private sealed class FakeDecoder : IImageDecoder
    {
        private readonly DecodedImage? _image;

        public FakeDecoder(DecodedImage? image) => _image = image;

        public List<string> Paths { get; } = new List<string>();
        public byte[]? ReceivedBytes { get; private set; }

        public bool TryDecode(string fileName, Stream stream, out DecodedImage? image)
        {
            Paths.Add(fileName);
            using MemoryStream copy = new MemoryStream();
            stream.CopyTo(copy);
            ReceivedBytes = copy.ToArray();
            image = _image;
            return _image != null;
        }
    }
}
