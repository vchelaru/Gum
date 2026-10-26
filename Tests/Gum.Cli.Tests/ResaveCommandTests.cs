using Shouldly;

namespace Gum.Cli.Tests;

public class ResaveCommandTests : BaseTestClass
{
    // The Forms template's files are hand-maintained and not all in saved form (trailing newlines,
    // blank lines), so the first save may reformat them; every save after that must be stable.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resave_FreshFormsProject_SecondSaveShouldMatchTheFirst(bool raw)
    {
        string filePath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", filePath);
        string[] arguments = raw ? new[] { "resave", filePath, "--raw" } : new[] { "resave", filePath };
        CliTestHelper.Run(arguments).ExitCode.ShouldBe(0);
        Dictionary<string, byte[]> afterFirstSave = ReadAllFiles();

        CliTestHelper result = CliTestHelper.Run(arguments);

        result.ExitCode.ShouldBe(0, result.StandardError);
        foreach (KeyValuePair<string, byte[]> file in afterFirstSave)
        {
            File.ReadAllBytes(file.Key).ShouldBe(file.Value, file.Key);
        }
    }

    [Fact]
    public void Resave_ShouldRewriteElementFiles()
    {
        string filePath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", filePath, "--template", "empty");
        string standardFile = Directory.GetFiles(Path.Combine(TempDirectory, "MyProject", "Standards"), "*.gutx").First();
        string savedContent = File.ReadAllText(standardFile);
        File.WriteAllText(standardFile, savedContent + "<!-- not part of the saved format -->");

        CliTestHelper result = CliTestHelper.Run("resave", filePath);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.ReadAllText(standardFile).ShouldBe(savedContent);
    }

    [Fact]
    public void Resave_ShouldRewriteBehaviorFiles()
    {
        string filePath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", filePath);
        string behaviorFile = Directory.GetFiles(Path.Combine(TempDirectory, "MyProject", "Behaviors"), "*.behx").First();
        CliTestHelper.Run("resave", filePath).ExitCode.ShouldBe(0);
        string savedContent = File.ReadAllText(behaviorFile);
        File.WriteAllText(behaviorFile, savedContent + "<!-- not part of the saved format -->");

        CliTestHelper result = CliTestHelper.Run("resave", filePath);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.ReadAllText(behaviorFile).ShouldBe(savedContent);
    }

    [Fact]
    public void Resave_BehaviorLinkedFromSharedPath_ShouldLeaveSharedFileUntouched()
    {
        string formsPath = Path.Combine(TempDirectory, "Forms", "Forms.gumx");
        CliTestHelper.Run("new", formsPath);
        string sharedFile = Path.Combine(TempDirectory, "Shared", "ButtonBehavior.behx");
        Directory.CreateDirectory(Path.GetDirectoryName(sharedFile)!);
        string sourceBehavior = Directory.GetFiles(Path.Combine(TempDirectory, "Forms", "Behaviors"), "*.behx").First();
        File.WriteAllText(sharedFile, File.ReadAllText(sourceBehavior) + "<!-- not part of the saved format -->");
        string linkedPath = Path.Combine(TempDirectory, "Linked", "Linked.gumx");
        CliTestHelper.Run("new", linkedPath, "--template", "empty");
        string project = File.ReadAllText(linkedPath);
        string reference = "<BehaviorReference Name=\"ButtonBehavior\" SourcePath=\"../Shared/ButtonBehavior.behx\" />";
        File.WriteAllText(linkedPath, project.Replace("</GumProjectSave>", reference + "</GumProjectSave>"));
        string sharedBefore = File.ReadAllText(sharedFile);

        CliTestHelper result = CliTestHelper.Run("resave", linkedPath);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.ReadAllText(sharedFile).ShouldBe(sharedBefore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resave_MissingProjectFile_ShouldReturnExitCode2(bool raw)
    {
        string fakePath = Path.Combine(TempDirectory, "nonexistent.gumx");

        CliTestHelper result = raw
            ? CliTestHelper.Run("resave", fakePath, "--raw")
            : CliTestHelper.Run("resave", fakePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not found");
    }

    private Dictionary<string, byte[]> ReadAllFiles()
    {
        return Directory.GetFiles(TempDirectory, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, File.ReadAllBytes);
    }
}
