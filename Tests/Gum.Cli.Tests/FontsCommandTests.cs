using Shouldly;

namespace Gum.Cli.Tests;

public class FontsCommandTests : BaseTestClass
{
    [Fact]
    public void Fonts_MissingProjectFile_ShouldReturnExitCode2()
    {
        string fakePath = Path.Combine(TempDirectory, "nonexistent.gumx");

        CliTestHelper result = CliTestHelper.Run("fonts", fakePath);

        result.ExitCode.ShouldBe(2);
    }

    [Fact]
    public void Fonts_SizeWithoutFont_ShouldReturnExitCode2()
    {
        CliTestHelper result = CliTestHelper.Run("fonts", Path.Combine(TempDirectory, "any.gumx"), "--size", "18");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--font");
    }

    [Fact]
    public void Fonts_FontWithoutSize_ShouldReturnExitCode2()
    {
        CliTestHelper result = CliTestHelper.Run("fonts", Path.Combine(TempDirectory, "any.gumx"), "--font", "Arial");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--size");
    }

    [Fact]
    public void Fonts_StyleWithoutFont_ShouldReturnExitCode2()
    {
        CliTestHelper result = CliTestHelper.Run("fonts", Path.Combine(TempDirectory, "any.gumx"), "--bold");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--font");
    }

    [Fact]
    public void Fonts_NonPositiveSize_ShouldReturnExitCode2()
    {
        CliTestHelper result = CliTestHelper.Run(
            "fonts", Path.Combine(TempDirectory, "any.gumx"), "--font", "Arial", "--size", "0");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--size");
    }

    [Fact]
    public void Fonts_NonNumericSize_ShouldReturnExitCode2()
    {
        CliTestHelper result = CliTestHelper.Run(
            "fonts", Path.Combine(TempDirectory, "any.gumx"), "--font", "Arial", "--size", "abc");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--size");
    }

    [Fact]
    public void Fonts_NonNumericOutline_ShouldReturnExitCode2()
    {
        CliTestHelper result = CliTestHelper.Run(
            "fonts", Path.Combine(TempDirectory, "any.gumx"), "--font", "Arial", "--size", "14", "--outline", "abc");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--outline");
    }

    [Fact]
    public void Fonts_NegativeOutline_ShouldReturnExitCode2()
    {
        CliTestHelper result = CliTestHelper.Run(
            "fonts", Path.Combine(TempDirectory, "any.gumx"), "--font", "Arial", "--size", "14", "--outline=-2");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--outline");
    }
}
