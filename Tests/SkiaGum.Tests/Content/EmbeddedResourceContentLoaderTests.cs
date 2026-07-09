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

    [Fact]
    public void TryLoadContent_ForUnsupportedType_ShouldReturnDefaultWithoutThrowing()
    {
        EmbeddedResourceContentLoader loader = new EmbeddedResourceContentLoader();

        string loaded = "not-yet-overwritten";
        Should.NotThrow(() => loaded = loader.TryLoadContent<string>("whatever.png"));

        loaded.ShouldBeNull();
    }
}
