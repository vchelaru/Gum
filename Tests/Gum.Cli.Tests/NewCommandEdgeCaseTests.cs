using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>
/// What <c>gumcli new</c> prints and returns for unusual targets.
/// </summary>
public class NewCommandEdgeCaseTests : BaseTestClass
{
    [Fact]
    public void New_WhenAProjectOfTheOtherFormatIsAlreadyThere_ShouldReturnExitCode2()
    {
        string folder = Path.Combine(TempDirectory, "Proj");
        CliTestHelper.Run("new", Path.Combine(folder, "Proj.gumx")).ExitCode.ShouldBe(0);

        CliTestHelper result = CliTestHelper.Run("new", folder);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("already exists");
        File.Exists(Path.Combine(folder, "Proj.gumj")).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("raylib")]
    public void New_WhenTheTargetIsAnExistingFile_ShouldReturnExitCode2WithAMessageAndNoStackTrace(string? platform)
    {
        string taken = Path.Combine(TempDirectory, "Taken");
        File.WriteAllText(taken, "mine");

        CliTestHelper result = platform == null
            ? CliTestHelper.Run("new", taken)
            : CliTestHelper.Run("new", taken, "--platform", platform, "--no-restore");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldNotBeNullOrWhiteSpace();
        result.StandardError.ShouldNotContain("   at ");
        File.ReadAllText(taken).ShouldBe("mine");
    }

    [Fact]
    public void New_WithPlatformAndANameWithSpaces_ShouldReturnExitCode2NamingTheName()
    {
        string path = Path.Combine(TempDirectory, "My Game");

        CliTestHelper result = CliTestHelper.Run("new", path, "--platform", "raylib", "--no-restore");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("My Game");
        Directory.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public void New_GumOnlyWithANameWithSpaces_ShouldCreateTheProject()
    {
        string path = Path.Combine(TempDirectory, "My Game");

        CliTestHelper result = CliTestHelper.Run("new", path);

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(path, "My Game.gumj")).ShouldBeTrue();
    }

    [Fact]
    public void New_WithPlatformAndANameThatIsAReservedRuntimeName_ShouldCreateNothing()
    {
        string path = Path.Combine(TempDirectory, "raylibgum");

        CliTestHelper result = CliTestHelper.Run("new", path, "--platform", "raylib", "--no-restore");

        result.ExitCode.ShouldBe(2);
        Directory.Exists(path).ShouldBeFalse();
    }
}
