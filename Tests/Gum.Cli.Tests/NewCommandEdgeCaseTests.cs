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

    [Fact]
    public void New_WithPlatformAndANameThatIsAPackageTheProjectReferences_ShouldCreateNothingAndNameTheConflict()
    {
        string path = Path.Combine(TempDirectory, "Gum.raylib");

        CliTestHelper result = CliTestHelper.Run("new", path, "--platform", "raylib", "--no-restore");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("Gum.raylib");
        Directory.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public void New_WithAPathContainingSpaces_CreatesTheCodeProjectThere()
    {
        string projectDir = Path.Combine(TempDirectory, "Vic Personal", "Gum Projects", "My_Game");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "raylib", "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.Exists(Path.Combine(projectDir, "My_Game.csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(projectDir, "Content", "GumProject", "GumProject.gumj")).ShouldBeTrue();
    }

    [Fact]
    public void New_SourceLinkedFromAPathContainingSpaces_ReferencesARuntimeProjectThatExists()
    {
        string projectDir = Path.Combine(TempDirectory, "Vic Personal", "Gum Projects", "Linked_Game");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "monogame", "--source-linked", "--no-restore", "--template", "empty");

        result.ExitCode.ShouldBe(0, result.StandardError);
        string csproj = File.ReadAllText(Path.Combine(projectDir, "Linked_Game.csproj"));
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(csproj, "<ProjectReference Include=\"([^\"]+)\"");
        match.Success.ShouldBeTrue(csproj);
        File.Exists(Path.GetFullPath(Path.Combine(projectDir, match.Groups[1].Value.Replace((char)92, Path.DirectorySeparatorChar)))).ShouldBeTrue();
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("com1")]
    [InlineData("Nul.txt")]
    public void New_WithAWindowsDeviceName_IsRefusedWithTheSameMessageTheDialogShows(string name)
    {
        foreach (string[] extra in new[] { new string[0], new[] { "--platform", "raylib", "--no-restore" } })
        {
            string projectDir = Path.Combine(TempDirectory, name);

            CliTestHelper result = CliTestHelper.Run(new[] { "new", projectDir }.Concat(extra).ToArray());

            result.ExitCode.ShouldBe(2, result.StandardError);
            result.StandardError.ShouldContain(Gum.ProjectServices.NewProjectNameRules.GetProblem(name, true)!);
            Directory.Exists(projectDir).ShouldBeFalse();
        }
    }
}
