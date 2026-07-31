using Gum.ProjectServices.Screenshot;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="ScreenshotDiffHtmlReportWriter"/>, which turns a
/// <see cref="ScreenshotDiffResult"/> into a side-by-side (MonoGame | raylib) HTML report for
/// visually triaging mismatches (#4174).
/// </summary>
public class ScreenshotDiffHtmlReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public ScreenshotDiffHtmlReportWriterTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumHtmlReportTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_MatchingElement_RendersBothImagesWithRelativePaths()
    {
        ScreenshotDiffResult result = new ScreenshotDiffResult
        {
            OutputDirectory = _tempDirectory,
            ElementDiffs = new[]
            {
                new ElementScreenshotDiff
                {
                    ElementName = "Screens/Main",
                    Matches = true,
                    BackendAPath = Path.Combine(_tempDirectory, "A", "Screens", "Main.png"),
                    BackendBPath = Path.Combine(_tempDirectory, "B", "Screens", "Main.png"),
                },
            },
        };

        string reportPath = Path.Combine(_tempDirectory, "report.html");
        ScreenshotDiffHtmlReportWriter writer = new ScreenshotDiffHtmlReportWriter();

        writer.Write(result, reportPath);

        string html = File.ReadAllText(reportPath);
        html.ShouldContain("Screens/Main");
        html.ShouldContain("src=\"A/Screens/Main.png\"");
        html.ShouldContain("src=\"B/Screens/Main.png\"");
    }

    [Fact]
    public void Write_MismatchedElement_IncludesDiffDetails()
    {
        ScreenshotDiffResult result = new ScreenshotDiffResult
        {
            OutputDirectory = _tempDirectory,
            ElementDiffs = new[]
            {
                new ElementScreenshotDiff
                {
                    ElementName = "Controls/Button",
                    Matches = false,
                    BackendAPath = Path.Combine(_tempDirectory, "A", "Controls", "Button.png"),
                    BackendBPath = Path.Combine(_tempDirectory, "B", "Controls", "Button.png"),
                    MismatchedPixelCount = 42,
                    TotalPixelCount = 1000,
                    MismatchPercentage = 4.2,
                    BoundingBoxMinX = 12,
                    BoundingBoxMinY = 34,
                    BoundingBoxMaxX = 56,
                    BoundingBoxMaxY = 78,
                },
            },
        };

        string reportPath = Path.Combine(_tempDirectory, "report.html");
        ScreenshotDiffHtmlReportWriter writer = new ScreenshotDiffHtmlReportWriter();

        writer.Write(result, reportPath);

        string html = File.ReadAllText(reportPath);
        html.ShouldContain("Controls/Button");
        html.ShouldContain("42");
        html.ShouldContain("4.2%");
        html.ShouldContain("(12, 34)");
        html.ShouldContain("(56, 78)");
    }

    [Fact]
    public void Write_ElementWithRenderError_ShowsErrorInsteadOfImages()
    {
        ScreenshotDiffResult result = new ScreenshotDiffResult
        {
            OutputDirectory = _tempDirectory,
            ElementDiffs = new[]
            {
                new ElementScreenshotDiff
                {
                    ElementName = "Broken/Element",
                    Matches = false,
                    ErrorMessage = "simulated render failure",
                },
            },
        };

        string reportPath = Path.Combine(_tempDirectory, "report.html");
        ScreenshotDiffHtmlReportWriter writer = new ScreenshotDiffHtmlReportWriter();

        writer.Write(result, reportPath);

        string html = File.ReadAllText(reportPath);
        html.ShouldContain("Broken/Element");
        html.ShouldContain("simulated render failure");
        html.ShouldNotContain("<img");
    }

    [Fact]
    public void Write_MismatchesAndMatches_ListsMismatchesFirst()
    {
        ScreenshotDiffResult result = new ScreenshotDiffResult
        {
            OutputDirectory = _tempDirectory,
            ElementDiffs = new[]
            {
                new ElementScreenshotDiff
                {
                    ElementName = "Matching",
                    Matches = true,
                    BackendAPath = Path.Combine(_tempDirectory, "A", "Matching.png"),
                    BackendBPath = Path.Combine(_tempDirectory, "B", "Matching.png"),
                },
                new ElementScreenshotDiff
                {
                    ElementName = "Mismatched",
                    Matches = false,
                    BackendAPath = Path.Combine(_tempDirectory, "A", "Mismatched.png"),
                    BackendBPath = Path.Combine(_tempDirectory, "B", "Mismatched.png"),
                    MismatchedPixelCount = 5,
                    TotalPixelCount = 100,
                    MismatchPercentage = 5.0,
                    BoundingBoxMinX = 1,
                    BoundingBoxMinY = 1,
                    BoundingBoxMaxX = 2,
                    BoundingBoxMaxY = 2,
                },
            },
        };

        string reportPath = Path.Combine(_tempDirectory, "report.html");
        ScreenshotDiffHtmlReportWriter writer = new ScreenshotDiffHtmlReportWriter();

        writer.Write(result, reportPath);

        string html = File.ReadAllText(reportPath);
        html.IndexOf("Mismatched", StringComparison.Ordinal)
            .ShouldBeLessThan(html.IndexOf("Matching", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
