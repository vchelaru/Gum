using System.CommandLine;
using Gum.Cli.Commands;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

/// <summary>Splash-damage sweep of New Project (#7328): the gumcli side of the dialog's command line.</summary>
public class NewCommandSplashTests : BaseTestClass
{
    [Theory]
    [InlineData("--template")]
    [InlineData("-t")]
    [InlineData("--theme")]
    [InlineData("--platform")]
    [InlineData("-p")]
    [InlineData("--no-restore")]
    [InlineData("--source-linked")]
    public void New_HasEveryOptionTheDialogsCommandLineUses(string alias)
    {
        Command command = NewCommand.Create();

        command.Options.ShouldContain(option => option.HasAlias(alias));
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

    [Fact]
    public async Task New_WithThemePlatformAndNoRestore_WritesTheSameFilesTheServiceDoes()
    {
        string cliDir = Path.Combine(TempDirectory, "FromCli", "Parity_Game");
        string serviceDir = Path.Combine(TempDirectory, "FromService", "Parity_Game");

        CliTestHelper result = CliTestHelper.Run("new", cliDir, "--platform", "kni", "--theme", "Bubblegum", "--no-restore");
        NewProjectService service = new NewProjectService(
            FormsThemeLocator.CreateDefault(AppContext.BaseDirectory, Directory.GetCurrentDirectory()),
            new NoRestore(), new[] { AppContext.BaseDirectory });
        NewProjectResult serviceResult = await service.ExecuteAsync(new NewProjectRequest
        {
            Path = serviceDir, Platform = "kni", Theme = "Bubblegum", Restore = false
        }, null, CancellationToken.None);

        result.ExitCode.ShouldBe(0, result.StandardError);
        serviceResult.Files.Success.ShouldBeTrue(serviceResult.Files.ErrorMessage);
        string[] Files(string root) => Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(root, file)).Order().ToArray();
        Files(cliDir).ShouldBe(Files(serviceDir));
        File.ReadAllText(Path.Combine(cliDir, "Game1.cs")).ShouldBe(File.ReadAllText(Path.Combine(serviceDir, "Game1.cs")));
    }

    [Fact]
    public void New_WithPlatformAndRestoreOff_PrintsNoRestoreLine()
    {
        string projectDir = Path.Combine(TempDirectory, "Quiet_Game");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "stride", "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.ShouldNotContain("Restoring");
    }

    private class NoRestore : IRestoreRunner
    {
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
    }
}
