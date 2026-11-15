using Gum.ProjectServices.Screenshot;
using Gum.Tests.Shared.CrossRuntime;
using Shouldly;
using SkiaSharp;

namespace Gum.ProjectServices.SkiaGum.Tests;

/// <summary>
/// <c>gumcli screenshot --backend skia</c> with no background color writes a straight-alpha PNG (#7992):
/// translucent elements keep their own color and alpha, drawn directly or through a render-target container.
/// The scene and the hand-computed values are in <see cref="ScreenshotAlphaScene"/>.
/// </summary>
public class SkiaGumScreenshotAlphaTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(), "GumSkiaScreenshotAlpha_" + Guid.NewGuid().ToString("N"));

    public SkiaGumScreenshotAlphaTests() => Directory.CreateDirectory(_tempDirectory);

    [Fact]
    public void TakeScreenshot_TranslucentElementsOnTransparentBackground_WritesStraightAlphaPng()
    {
        string projectPath = ScreenshotAlphaScene.CreateProject(_tempDirectory, WriteSolidPng);
        string outputPath = Path.Combine(_tempDirectory, "Screen.png");

        ScreenshotResult result = new SkiaGumScreenshotService().TakeScreenshot(new ScreenshotRequest
        {
            ProjectPath = projectPath,
            ElementName = ScreenshotAlphaScene.ScreenName,
            OutputPath = outputPath,
            Width = ScreenshotAlphaScene.Width,
            Height = ScreenshotAlphaScene.Height,
        });
        result.Success.ShouldBeTrue(result.ErrorMessage);

        // Decode into an unpremultiplied bitmap so the PNG's own straight-alpha bytes are read as stored.
        using SKCodec codec = SKCodec.Create(outputPath);
        SKImageInfo info = codec.Info.WithAlphaType(SKAlphaType.Unpremul);
        using SKBitmap image = new SKBitmap(info);
        codec.GetPixels(info, image.GetPixels()).ShouldBe(SKCodecResult.Success);

        (bool allMatch, List<string> report) = ScreenshotAlphaScene.Check((x, y) =>
        {
            SKColor c = image.GetPixel(x, y);
            return (c.Red, c.Green, c.Blue, c.Alpha);
        });
        ScreenshotAlphaScene.Keep(outputPath, "skia", report);
        allMatch.ShouldBeTrue(string.Join(Environment.NewLine, report));
    }

    private static void WriteSolidPng(string path, byte r, byte g, byte b)
    {
        using SKBitmap bitmap = new SKBitmap(60, 40);
        bitmap.Erase(new SKColor(r, g, b));
        using FileStream stream = File.Create(path);
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100).ShouldBeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
