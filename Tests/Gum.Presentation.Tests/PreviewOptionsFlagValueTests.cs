using GumPreview;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// A flag that takes a value but is given none, or is given another flag in place of one, must be a
/// readable error. Silently ignoring it turns an unattended run into one that never ends, or an
/// attended run into one that is never closed with its tool.
/// </summary>
public class PreviewOptionsFlagValueTests
{
    private static readonly string[] Required = { "--project", "a.gumx", "--element", "Main" };

    private static string[] With(params string[] extra)
    {
        string[] all = new string[Required.Length + extra.Length];
        Required.CopyTo(all, 0);
        extra.CopyTo(all, Required.Length);
        return all;
    }

    [Theory]
    [InlineData("--exit-after")]
    [InlineData("--parent-pid")]
    [InlineData("--screenshot")]
    [InlineData("--selection-file")]
    [InlineData("--content-root")]
    [InlineData("--focus")]
    [InlineData("--type")]
    public void Parse_TrailingFlagWithNoValue_IsAnError(string flag)
    {
        PreviewOptions options = PreviewOptions.Parse(With(flag));

        options.Error.ShouldNotBeNull();
        options.Error.ShouldContain(flag);
    }

    [Theory]
    [InlineData("--project")]
    [InlineData("--element")]
    public void Parse_FlagFollowedByAnotherFlag_DoesNotTakeTheFlagAsItsValue(string flag)
    {
        string[] args = flag == "--project"
            ? new[] { "--project", "--element", "Main" }
            : new[] { "--project", "a.gumx", "--element", "--parent-pid", "5" };

        PreviewOptions options = PreviewOptions.Parse(args);

        options.Error.ShouldNotBeNull();
    }

    [Fact]
    public void Parse_PathsWithSpacesAndUnicode_AreKeptExactly()
    {
        PreviewOptions options = PreviewOptions.Parse(new[]
        {
            "--project", "C:\\My Games\\Pr\u00e9view \u65e5\u672c\\Project.gumj",
            "--element", "Screens/Main Menu",
            "--selection-file", "C:\\Temp Dir\\sel.txt",
            "--content-root", "C:\\My Games\\Pr\u00e9view \u65e5\u672c",
        });

        options.Error.ShouldBeNull();
        options.ProjectPath.ShouldBe("C:\\My Games\\Pr\u00e9view \u65e5\u672c\\Project.gumj");
        options.ElementName.ShouldBe("Screens/Main Menu");
        options.SelectionFilePath.ShouldBe("C:\\Temp Dir\\sel.txt");
        options.ContentRootDirectory.ShouldBe("C:\\My Games\\Pr\u00e9view \u65e5\u672c");
    }

    [Fact]
    public void Parse_ErrorsComeWithTheUsageSoAUserCanFixTheCommandLine()
    {
        PreviewOptions options = PreviewOptions.Parse(new string[0]);

        options.Error.ShouldNotBeNull();
        PreviewOptions.Usage.ShouldContain("--project");
        PreviewOptions.Usage.ShouldContain("--element");
        PreviewOptions.Usage.ShouldContain("--selection-file");
        PreviewOptions.Usage.ShouldContain("--parent-pid");
    }
}
