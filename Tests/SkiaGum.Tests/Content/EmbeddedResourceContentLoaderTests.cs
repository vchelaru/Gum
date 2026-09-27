using SkiaGum.Content;
using SkiaSharp;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace SkiaGum.Tests.Content;

// #3567: TryLoadContent used to unconditionally throw NotImplementedException, breaking the
// IContentLoader contract (TryXxx should never throw) and diverging from every other backend's
// ContentLoader (MonoGame-family, Raylib, Sokol), which all implement it as a non-throwing
// mirror of LoadContent.
public class EmbeddedResourceContentLoaderTests
{
    [Fact]
    public void TryLoadContent_WhenContentExists_ShouldReturnLoadedContent()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumSkiaTryLoadTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        string savedRelativeDirectory = FileManager.RelativeDirectory;

        try
        {
            string absolutePath = Path.Combine(tempRoot, "try_load_pixel.png");
            using (SKBitmap source = new SKBitmap(4, 5))
            using (SKImage image = SKImage.FromBitmap(source))
            using (SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100))
            using (FileStream fileStream = File.OpenWrite(absolutePath))
            {
                encoded.SaveTo(fileStream);
            }

            EmbeddedResourceContentLoader loader = new EmbeddedResourceContentLoader();

            SKBitmap loaded = loader.TryLoadContent<SKBitmap>(absolutePath);

            loaded.ShouldNotBeNull();
            loaded.Width.ShouldBe(4);
            loaded.Height.ShouldBe(5);
        }
        finally
        {
            FileManager.RelativeDirectory = savedRelativeDirectory;
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryLoadContent_WhenContentMissing_ShouldReturnDefaultWithoutThrowing()
    {
        string missingPath = Path.Combine(Path.GetTempPath(),
            "GumSkiaTryLoadMissingTest_" + Guid.NewGuid().ToString("N"), "does_not_exist.png");

        EmbeddedResourceContentLoader loader = new EmbeddedResourceContentLoader();

        SKBitmap loaded = null;
        Should.NotThrow(() => loaded = loader.TryLoadContent<SKBitmap>(missingPath));

        loaded.ShouldBeNull();
    }

    // #5221: a .gumpkg bundle, zip-backed asset store or mobile host serves content only through
    // FileManager.CustomGetStreamFromFile. Every Skia content type must read through that hook, as the
    // MonoGame and raylib loaders do, instead of probing the real disk and then embedded resources.
    [Theory]
    [InlineData("image.png")]
    [InlineData("vector.svg")]
    [InlineData("animation.json")]
    [InlineData("font.ttf")]
    public void LoadContent_WhenFileIsOnlyReachableThroughTheStreamHook_ShouldLoadFromTheHook(string fileName)
    {
        byte[] contentBytes = CreateContentBytes(fileName);
        string offDiskDirectory = "NotOnDisk_" + Guid.NewGuid().ToString("N");
        string offDiskPath = Path.Combine(Path.GetTempPath(), offDiskDirectory, fileName);
        Func<string, Stream>? previousHook = FileManager.CustomGetStreamFromFile;

        try
        {
            FileManager.CustomGetStreamFromFile = requestedPath =>
                requestedPath.Contains(offDiskDirectory)
                    ? new MemoryStream(contentBytes)
                    : throw new FileNotFoundException($"Unexpected read of '{requestedPath}'.", requestedPath);

            EmbeddedResourceContentLoader loader = new EmbeddedResourceContentLoader();

            object? loaded = Path.GetExtension(fileName) switch
            {
                ".png" => loader.LoadContent<SKBitmap>(offDiskPath),
                ".svg" => loader.LoadContent<Svg.Skia.SKSvg>(offDiskPath),
                ".json" => loader.LoadContent<SkiaSharp.Skottie.Animation>(offDiskPath),
                _ => loader.LoadContent<SKTypeface>(offDiskPath),
            };

            loaded.ShouldNotBeNull();
        }
        finally
        {
            FileManager.CustomGetStreamFromFile = previousHook;
        }
    }

    private static byte[] CreateContentBytes(string fileName)
    {
        switch (Path.GetExtension(fileName))
        {
            case ".png":
                using (SKBitmap source = new SKBitmap(2, 2))
                using (SKImage image = SKImage.FromBitmap(source))
                using (SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return encoded.ToArray();
                }
            case ".svg":
                return System.Text.Encoding.UTF8.GetBytes(
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"4\" height=\"4\"><rect width=\"4\" height=\"4\" fill=\"red\"/></svg>");
            case ".json":
                return System.Text.Encoding.UTF8.GetBytes(
                    "{\"v\":\"5.7.0\",\"fr\":30,\"ip\":0,\"op\":30,\"w\":4,\"h\":4,\"layers\":[]}");
            default:
                return File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "TestFont.ttf"));
        }
    }

    [Fact]
    public void TryLoadContent_ForUnsupportedType_ShouldReturnDefaultWithoutThrowing()
    {
        EmbeddedResourceContentLoader loader = new EmbeddedResourceContentLoader();

        string loaded = "not-yet-overwritten";
        Should.NotThrow(() => loaded = loader.TryLoadContent<string>("whatever.png"));

        loaded.ShouldBeNull();
    }
}
