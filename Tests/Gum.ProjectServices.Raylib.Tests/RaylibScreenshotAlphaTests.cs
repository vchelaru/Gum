using Gum.ProjectServices.Screenshot;
using Gum.Tests.Shared.CrossRuntime;
using Raylib_cs;
using Shouldly;

namespace Gum.ProjectServices.Raylib.Tests;

/// <summary>
/// <c>gumcli screenshot --backend raylib</c> with no background color writes a straight-alpha PNG (#7992):
/// translucent elements keep their own color and alpha, drawn directly or through a render-target container.
/// The scene and the hand-computed values are in <see cref="ScreenshotAlphaScene"/>.
/// </summary>
public class RaylibScreenshotAlphaTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(), "GumRaylibScreenshotAlpha_" + Guid.NewGuid().ToString("N"));

    public RaylibScreenshotAlphaTests() => Directory.CreateDirectory(_tempDirectory);

    [Fact]
    public void TakeScreenshot_TranslucentElementsOnTransparentBackground_WritesStraightAlphaPng()
    {
        string projectPath = ScreenshotAlphaScene.CreateProject(_tempDirectory, WriteSolidPng);
        string outputPath = Path.Combine(_tempDirectory, "Screen.png");

        ScreenshotResult result = new RaylibScreenshotService().TakeScreenshot(new ScreenshotRequest
        {
            ProjectPath = projectPath,
            ElementName = ScreenshotAlphaScene.ScreenName,
            OutputPath = outputPath,
            Width = ScreenshotAlphaScene.Width,
            Height = ScreenshotAlphaScene.Height,
        });
        result.Success.ShouldBeTrue(result.ErrorMessage);

        // stb_image decodes the PNG's stored straight-alpha bytes as they are.
        Image image = Raylib_cs.Raylib.LoadImage(outputPath);
        try
        {
            (bool allMatch, List<string> report) = ScreenshotAlphaScene.Check((x, y) =>
            {
                Color c = Raylib_cs.Raylib.GetImageColor(image, x, y);
                return (c.R, c.G, c.B, c.A);
            });
            ScreenshotAlphaScene.Keep(outputPath, "raylib", report);
            allMatch.ShouldBeTrue(string.Join(Environment.NewLine, report));
        }
        finally
        {
            Raylib_cs.Raylib.UnloadImage(image);
        }
    }

    private static void WriteSolidPng(string path, byte r, byte g, byte b)
    {
        Image image = Raylib_cs.Raylib.GenImageColor(60, 40, new Color(r, g, b, (byte)255));
        try
        {
            ((bool)Raylib_cs.Raylib.ExportImage(image, path)).ShouldBeTrue();
        }
        finally
        {
            Raylib_cs.Raylib.UnloadImage(image);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
