using Raylib_cs;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;
using Xunit;

namespace RaylibGum.Tests.Content;

/// <summary>
/// Issue #7970: <see cref="ContentLoader.SupportedImageExtensions"/> lists what the stock raylib native
/// decodes, plus the extensions registered <see cref="IImageDecoder"/>s declare.
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
    public void SupportedImageExtensions_NoDecoders_AreTheStockRaylibFormats()
    {
        ContentLoader.SupportedImageExtensions.ShouldBe(
            new[] { "png", "bmp", "gif", "qoi", "dds" }, ignoreOrder: true);
    }

    [Fact]
    public void SupportedImageExtensions_DecoderRegistered_IncludesItsExtensions()
    {
        LoaderManager.Self.ImageDecoders.Add(new ExtensionOnlyDecoder("webp", "jpg"));

        ContentLoader.SupportedImageExtensions.ShouldContain("webp");
        ContentLoader.SupportedImageExtensions.Count(item => item == "jpg").ShouldBe(1);
        ContentLoader.SupportedImageExtensions.ShouldContain("png");
    }

    // Proves the list against the native: each listed format loads a 1x1 file through the loader.
    [Fact]
    public void LoadContent_EachSupportedExtension_DecodesWithTheStockNative()
    {
        Dictionary<string, byte[]> samples = new Dictionary<string, byte[]>
        {
            ["png"] = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="),
            ["gif"] = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"),
            ["bmp"] = TinyImages.Bmp(),
            ["qoi"] = TinyImages.Qoi(),
            ["dds"] = TinyImages.Dds(),
        };
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        try
        {
            LoaderManager.Self.CacheTextures = false;
            foreach (string extension in ContentLoader.SupportedImageExtensions)
            {
                string fileName = "pixel_7970." + extension;
                byte[] bytes = samples[extension];
                FileManager.CustomGetStreamFromFile = path => path.EndsWith(fileName) ? new MemoryStream(bytes) : null!;

                Texture2D loaded = LoaderManager.Self.LoadContent<Texture2D>(fileName);

                loaded.Width.ShouldBe(1, extension);
                loaded.Height.ShouldBe(1, extension);
            }
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
        }
    }

    // An extension outside the list gets the convert-or-add-a-decoder message, like .jpg does.
    [Fact]
    public void LoadContent_UnlistedExtension_SuggestsPngOrDecoder()
    {
        bool savedCacheTextures = LoaderManager.Self.CacheTextures;
        try
        {
            LoaderManager.Self.CacheTextures = false;
            byte[] garbage = { 1, 2, 3, 4, 5, 6, 7, 8 };
            FileManager.CustomGetStreamFromFile = path => path.EndsWith("garbage_7970.webp") ? new MemoryStream(garbage) : null!;

            FileNotFoundException exception = Should.Throw<FileNotFoundException>(() =>
                LoaderManager.Self.LoadContent<Texture2D>("garbage_7970.webp"));

            exception.Message.ShouldContain("garbage_7970.webp");
            exception.Message.ShouldContain("PNG");
            exception.Message.ShouldContain("IImageDecoder");
        }
        finally
        {
            LoaderManager.Self.CacheTextures = savedCacheTextures;
        }
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

    private static class TinyImages
    {
        // 1x1, 24 bits per pixel, one padded row.
        public static byte[] Bmp()
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

        // 1x1 RGBA: header, one QOI_OP_RGBA, end marker.
        public static byte[] Qoi() => new byte[]
        {
            (byte)'q', (byte)'o', (byte)'i', (byte)'f',
            0, 0, 0, 1,
            0, 0, 0, 1,
            4, 0,
            0xFF, 255, 0, 0, 255,
            0, 0, 0, 0, 0, 0, 0, 1,
        };

        // 1x1 uncompressed 32-bit RGBA.
        public static byte[] Dds()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((byte)'D');
            writer.Write((byte)'D');
            writer.Write((byte)'S');
            writer.Write((byte)' ');
            writer.Write(124);
            writer.Write(0x100F);
            writer.Write(1);
            writer.Write(1);
            writer.Write(4);
            writer.Write(0);
            writer.Write(0);
            for (int i = 0; i < 11; i++)
            {
                writer.Write(0);
            }
            writer.Write(32);
            writer.Write(0x41);
            writer.Write(0);
            writer.Write(32);
            writer.Write(0x00FF0000);
            writer.Write(0x0000FF00);
            writer.Write(0x000000FF);
            writer.Write(unchecked((int)0xFF000000));
            writer.Write(0x1000);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(new byte[] { 255, 0, 0, 255 });
            return stream.ToArray();
        }
    }
}
