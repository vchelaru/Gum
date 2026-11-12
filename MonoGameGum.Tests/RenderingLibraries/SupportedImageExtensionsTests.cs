using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Issue #7970: <see cref="ContentLoader.SupportedImageExtensions"/> lists the formats
/// Texture2D.FromStream loads, plus the extensions registered <see cref="IImageDecoder"/>s declare.
/// </summary>
public class SupportedImageExtensionsTests : BaseTestClass
{
    private static readonly byte[] FileBytes = { 7, 6, 5, 4, 3, 2, 1 };

    public override void Dispose()
    {
        LoaderManager.Self.ImageDecoders.Clear();
        base.Dispose();
    }

    [Fact]
    public void SupportedImageExtensions_NoDecoders_AreTheTexture2DFromStreamFormats()
    {
        ContentLoader.SupportedImageExtensions.ShouldBe(
            new[] { "png", "jpg", "jpeg", "bmp", "gif", "tga", "tif", "tiff", "dds" }, ignoreOrder: true);
    }

    [Fact]
    public void SupportedImageExtensions_DecoderRegistered_IncludesItsExtensions()
    {
        LoaderManager.Self.ImageDecoders.Add(new ExtensionOnlyDecoder("webp", "png"));

        ContentLoader.SupportedImageExtensions.ShouldContain("webp");
        ContentLoader.SupportedImageExtensions.Count(item => item == "png").ShouldBe(1);
        ContentLoader.SupportedImageExtensions.ShouldContain("dds");
    }

    // Headless, so FromStream fails for every file; a listed format keeps FromStream's own error.
    [Fact]
    public void LoadContent_ListedExtensionFails_ErrorDoesNotSuggestADecoder()
    {
        FileManager.CustomGetStreamFromFile = path => path.EndsWith("broken_7970.tga") ? new MemoryStream(FileBytes) : null!;

        Exception exception = Should.Throw<Exception>(() =>
            new ContentLoader().LoadContent<Texture2D>("broken_7970.tga"));

        exception.Message.ShouldNotContain(nameof(LoaderManager.ImageDecoders));
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
