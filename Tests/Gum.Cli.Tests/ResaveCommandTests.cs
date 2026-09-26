using Shouldly;

namespace Gum.Cli.Tests;

public class ResaveCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public ResaveCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliResaveTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    // The Forms template's files are hand-maintained and not all in saved form (trailing newlines,
    // blank lines), so the first save may reformat them; every save after that must be stable.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resave_FreshFormsProject_SecondSaveShouldMatchTheFirst(bool raw)
    {
        string filePath = Path.Combine(_tempDirectory, "MyProject", "MyProject.gumx");
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
        string filePath = Path.Combine(_tempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", filePath, "--template", "empty");
        string standardFile = Directory.GetFiles(Path.Combine(_tempDirectory, "MyProject", "Standards"), "*.gutx").First();
        string savedContent = File.ReadAllText(standardFile);
        File.WriteAllText(standardFile, savedContent + "<!-- not part of the saved format -->");

        CliTestHelper result = CliTestHelper.Run("resave", filePath);

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.ReadAllText(standardFile).ShouldBe(savedContent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resave_MissingProjectFile_ShouldReturnExitCode2(bool raw)
    {
        string fakePath = Path.Combine(_tempDirectory, "nonexistent.gumx");

        CliTestHelper result = raw
            ? CliTestHelper.Run("resave", fakePath, "--raw")
            : CliTestHelper.Run("resave", fakePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not found");
    }

    private Dictionary<string, byte[]> ReadAllFiles()
    {
        return Directory.GetFiles(_tempDirectory, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, File.ReadAllBytes);
    }
}
