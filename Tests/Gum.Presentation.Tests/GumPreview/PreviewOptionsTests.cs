using GumPreview;
using Shouldly;

namespace Gum.Presentation.Tests.GumPreview;

public class PreviewOptionsTests
{
    [Fact]
    public void Parse_LauncherArguments_AreReadWithNoUnattendedRun()
    {
        string[] args = { "--project", "/p/a.gumj", "--element", "Main", "--selection-file", "/p/sel.json", "--content-root", "/p/" };

        PreviewOptions options = PreviewOptions.Parse(args);

        options.Error.ShouldBeNull();
        options.ProjectPath.ShouldBe("/p/a.gumj");
        options.ElementName.ShouldBe("Main");
        options.SelectionFilePath.ShouldBe("/p/sel.json");
        options.ContentRootDirectory.ShouldBe("/p/");
        options.ExitAfterSeconds.ShouldBeNull();
        options.ScreenshotPath.ShouldBeNull();
    }

    [Fact]
    public void Parse_ExitAfterAndScreenshot_AreReadInvariantAndScreenshotMadeAbsolute()
    {
        string[] args = { "--project", "a.gumj", "--element", "Main", "--exit-after", "12.5", "--screenshot", "shot.png" };

        PreviewOptions options = PreviewOptions.Parse(args);

        options.Error.ShouldBeNull();
        options.ExitAfterSeconds.ShouldBe(12.5);
        options.ScreenshotPath.ShouldBe(System.IO.Path.GetFullPath("shot.png"));
    }

    [Theory]
    [InlineData("--project", "a.gumj")]
    [InlineData("--element", "Main")]
    public void Parse_MissingProjectOrElement_IsAnError(string flag, string value)
    {
        PreviewOptions options = PreviewOptions.Parse(new[] { flag, value });

        options.Error.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    public void Parse_ExitAfterNotAPositiveNumber_IsAnError(string value)
    {
        string[] args = { "--project", "a.gumj", "--element", "Main", "--exit-after", value };

        PreviewOptions options = PreviewOptions.Parse(args);

        options.Error.ShouldNotBeNull();
        options.Error.ShouldContain("--exit-after");
    }

    [Fact]
    public void Parse_ScreenshotWithoutExitAfter_IsAnError()
    {
        string[] args = { "--project", "a.gumj", "--element", "Main", "--screenshot", "shot.png" };

        PreviewOptions options = PreviewOptions.Parse(args);

        options.Error.ShouldNotBeNull();
        options.Error.ShouldContain("--screenshot");
    }
}
