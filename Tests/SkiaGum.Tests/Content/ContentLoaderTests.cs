using RenderingLibrary.Content;
using Shouldly;
using SkiaSharp;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace SkiaGum.Tests.Content;

/// <summary>
/// The Skia <see cref="ContentLoader"/> has the same name and methods as the MonoGame/KNI and raylib
/// ones, so code that holds an <see cref="IContentLoader"/> or reads <c>GumService.ContentLoader</c>
/// moves between runtimes unchanged.
/// </summary>
public class ContentLoaderTests
{
    private static string WritePng(string directory, string fileName, int width, int height)
    {
        Directory.CreateDirectory(directory);
        string absolutePath = Path.Combine(directory, fileName);
        using (SKBitmap source = new SKBitmap(width, height))
        using (SKImage image = SKImage.FromBitmap(source))
        using (SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100))
        using (FileStream fileStream = File.OpenWrite(absolutePath))
        {
            encoded.SaveTo(fileStream);
        }
        return absolutePath;
    }

    [Fact]
    public void LoadContent_ForABitmapOnDisk_ReturnsTheBitmap()
    {
        string directory = Path.Combine(Path.GetTempPath(), "GumSkiaContentLoader_" + Guid.NewGuid().ToString("N"));
        try
        {
            string absolutePath = WritePng(directory, "loader_pixel.png", width: 4, height: 5);
            ContentLoader loader = new ContentLoader();

            SKBitmap? loaded = loader.LoadContent<SKBitmap>(absolutePath);

            loaded.ShouldNotBeNull();
            loaded.Width.ShouldBe(4);
            loaded.Height.ShouldBe(5);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TryLoadContent_WhenContentMissing_ReturnsDefaultWithoutThrowing()
    {
        string missingPath = Path.Combine(Path.GetTempPath(),
            "GumSkiaContentLoaderMissing_" + Guid.NewGuid().ToString("N"), "does_not_exist.png");
        ContentLoader loader = new ContentLoader();

        SKBitmap? loaded = null;
        Should.NotThrow(() => loaded = loader.TryLoadContent<SKBitmap>(missingPath));

        loaded.ShouldBeNull();
    }

    [Fact]
    public void TryLoadContent_ForAnUnsupportedType_ReturnsDefaultWithoutThrowing()
    {
        ContentLoader loader = new ContentLoader();

        string? loaded = "not-yet-overwritten";
        Should.NotThrow(() => loaded = loader.TryLoadContent<string>("whatever.png"));

        loaded.ShouldBeNull();
    }

    [Fact]
    public void LoadContent_AppliesAdjustContentName_BeforeLoading()
    {
        string directory = Path.Combine(Path.GetTempPath(), "GumSkiaContentLoaderAdjust_" + Guid.NewGuid().ToString("N"));
        Func<string, string>? previousAdjust = ContentLoader.AdjustContentName;
        try
        {
            string absolutePath = WritePng(directory, "adjusted_pixel.png", width: 3, height: 2);
            ContentLoader.AdjustContentName = contentName => contentName == "alias.png" ? absolutePath : contentName;
            ContentLoader loader = new ContentLoader();

            SKBitmap? loaded = loader.LoadContent<SKBitmap>("alias.png");

            loaded.ShouldNotBeNull();
            loaded.Width.ShouldBe(3);
        }
        finally
        {
            ContentLoader.AdjustContentName = previousAdjust;
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CustomResourceAssembly_IsTheSameSetting_AsTheObsoleteSkiaResourceManager()
    {
        System.Reflection.Assembly? previous = ContentLoader.CustomResourceAssembly;
        try
        {
            ContentLoader.CustomResourceAssembly = typeof(ContentLoaderTests).Assembly;

#pragma warning disable CS0618 // The obsolete type must keep honoring the setting.
            global::SkiaGum.Content.SkiaResourceManager.CustomResourceAssembly.ShouldBeSameAs(typeof(ContentLoaderTests).Assembly);
#pragma warning restore CS0618
        }
        finally
        {
            ContentLoader.CustomResourceAssembly = previous;
        }
    }

    [Fact]
    public void StandardizeCaseSensitive_PrefixesRelativePathsWithTheRelativeDirectory_AndKeepsCase()
    {
        string previousRelativeDirectory = FileManager.RelativeDirectory;
        try
        {
            FileManager.RelativeDirectory = "/game/Content/";

            string standardized = ContentLoader.StandardizeCaseSensitive("Images/../Hero.PNG");

            standardized.Replace('\\', '/').ShouldEndWith("/game/Content/Hero.PNG");
        }
        finally
        {
            FileManager.RelativeDirectory = previousRelativeDirectory;
        }
    }

    [Fact]
    public void EmbeddedResourceContentLoader_StillLoadsContent()
    {
        string directory = Path.Combine(Path.GetTempPath(), "GumSkiaContentLoaderEmbedded_" + Guid.NewGuid().ToString("N"));
        try
        {
            string absolutePath = WritePng(directory, "embedded_pixel.png", width: 2, height: 2);

            SKBitmap? loaded = new global::SkiaGum.Content.EmbeddedResourceContentLoader().LoadContent<SKBitmap>(absolutePath);

            loaded.ShouldNotBeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
