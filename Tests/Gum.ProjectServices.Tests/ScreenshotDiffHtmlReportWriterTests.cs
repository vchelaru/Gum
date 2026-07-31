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
                    DiffX = 12,
                    DiffY = 34,
                    MaxChannelDifference = 200,
                },
            },
        };

        string reportPath = Path.Combine(_tempDirectory, "report.html");
        ScreenshotDiffHtmlReportWriter writer = new ScreenshotDiffHtmlReportWriter();

        writer.Write(result, reportPath);

        string html = File.ReadAllText(reportPath);
        html.ShouldContain("Controls/Button");
        html.ShouldContain("(12, 34)");
        html.ShouldContain("200");
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
                    DiffX = 1,
                    DiffY = 1,
                    MaxChannelDifference = 100,
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
