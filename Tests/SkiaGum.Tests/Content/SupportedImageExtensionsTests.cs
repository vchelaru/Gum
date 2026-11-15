using RenderingLibrary.Content;
using Shouldly;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;
using Xunit;

namespace SkiaGum.Tests.Content;

/// <summary>
/// Issue #7970: <see cref="ContentLoader.SupportedImageExtensions"/> lists what SkiaSharp decodes,
/// plus the extensions registered <see cref="IImageDecoder"/>s declare.
/// </summary>
public class SupportedImageExtensionsTests : BaseTestClass
{
    public override void Dispose()
    {
        LoaderManager.Self.ImageDecoders.Clear();
        FileManager.CustomGetStreamFromFile = null;
        base.Dispose();
    }

    [Fact]
    public void SupportedImageExtensions_NoDecoders_AreTheSkiaSharpFormats()
    {
        ContentLoader.SupportedImageExtensions.ShouldBe(
            new[] { "png", "jpg", "jpeg", "gif", "bmp", "webp", "ico", "wbmp" }, ignoreOrder: true);
    }

    [Fact]
    public void SupportedImageExtensions_DecoderRegistered_IncludesItsExtensions()
    {
        LoaderManager.Self.ImageDecoders.Add(new ExtensionOnlyDecoder("qoi", "png"));

        ContentLoader.SupportedImageExtensions.ShouldContain("qoi");
        ContentLoader.SupportedImageExtensions.Count(item => item == "png").ShouldBe(1);
        ContentLoader.SupportedImageExtensions.ShouldContain("webp");
    }

    // Proves the list against SkiaSharp: each listed format loads a 1x1 file through the loader.
    [Fact]
    public void LoadContent_EachSupportedExtension_Decodes()
    {
        byte[] png = Encode(SKEncodedImageFormat.Png);
        Dictionary<string, byte[]> samples = new Dictionary<string, byte[]>
        {
            ["png"] = png,
            ["jpg"] = Encode(SKEncodedImageFormat.Jpeg),
            ["jpeg"] = Encode(SKEncodedImageFormat.Jpeg),
            ["webp"] = Encode(SKEncodedImageFormat.Webp),
            ["gif"] = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"),
            ["bmp"] = Bmp(),
            ["ico"] = IcoWrapping(png),
            // Type 0, fixed header 0, width 1, height 1, one row of one bit.
            ["wbmp"] = new byte[] { 0, 0, 1, 1, 0x80 },
        };

        foreach (string extension in ContentLoader.SupportedImageExtensions)
        {
            string name = "pixel_7970." + extension;
            string path = Path.Combine(Path.GetTempPath(), "GumSkia7970_" + Guid.NewGuid().ToString("N"), name);
            byte[] bytes = samples[extension];
            FileManager.CustomGetStreamFromFile = incoming => incoming.EndsWith(name) ? new MemoryStream(bytes) : null!;

            SKBitmap? bitmap = new ContentLoader().LoadContent<SKBitmap>(path);

            bitmap.ShouldNotBeNull(extension);
            bitmap.Width.ShouldBe(1, extension);
            bitmap.Height.ShouldBe(1, extension);
        }
    }

    private static byte[] Encode(SKEncodedImageFormat format)
    {
        using SKBitmap bitmap = new SKBitmap(1, 1);
        bitmap.SetPixel(0, 0, SKColors.Red);
        using SKData data = bitmap.Encode(format, 100);
        return data.ToArray();
    }

    // 1x1, 24 bits per pixel, one padded row.
    private static byte[] Bmp()
    {
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(58);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(1);
        writer.Write(1);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(4);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);
        writer.Write(new byte[] { 0, 0, 255, 0 });
        return stream.ToArray();
    }

    // An icon file holding one PNG image.
    private static byte[] IcoWrapping(byte[] png)
    {
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((short)0);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write((byte)1);
        writer.Write((byte)1);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(png.Length);
        writer.Write(22);
        writer.Write(png);
        return stream.ToArray();
    }

    private sealed class ExtensionOnlyDecoder : IImageDecoder
    {
        public ExtensionOnlyDecoder(params string[] extensions) => SupportedExtensions = extensions;

        public IEnumerable<string>? SupportedExtensions { get; }

        public bool TryDecode(string fileName, Stream stream, out DecodedImage? image)
        {
            image = null;
            return false;
        }
    }
}
