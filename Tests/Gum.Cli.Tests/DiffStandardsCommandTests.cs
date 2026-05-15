using Shouldly;

namespace Gum.Cli.Tests;

public class DiffStandardsCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public DiffStandardsCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliDiffStandardsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void DiffStandards_ProjectWithDriftedFont_ShouldReturnExitCode1AndNameTheStandard()
    {
        // We don't assume any specific project produces a clean baseline (the bundled
        // Templates/Default/Standards/*.gutx files have known drift from
        // StandardElementsManager). Instead we force a specific drift and verify it
        // shows up in the output, regardless of any baseline drift that may also exist.
        string filePath = CreateTestProject("DriftedFont");

        string textPath = Path.Combine(Path.GetDirectoryName(filePath)!, "Standards", "Text.gutx");
        string content = File.ReadAllText(textPath);
        File.WriteAllText(textPath, content.Replace(">Arial<", ">DefinitelyNotArial<"));

        CliTestHelper result = CliTestHelper.Run("diff-standards", filePath);

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldContain("Text.gutx:");
        result.StandardOutput.ShouldContain("DefinitelyNotArial");
    }

    [Fact]
    public void DiffStandards_JsonFlag_ShouldEmitJson()
    {
        string filePath = CreateTestProject("Json");

        CliTestHelper result = CliTestHelper.Run("diff-standards", filePath, "--json");

        // We don't assert on drift presence/absence here — only that the output is JSON
        // with the expected top-level keys.
        result.StandardOutput.ShouldStartWith("{");
        result.StandardOutput.ShouldContain("\"hasDrift\":");
        result.StandardOutput.ShouldContain("\"differences\":");
        result.StandardOutput.ShouldContain("\"missingFromProject\":");
        result.StandardOutput.ShouldContain("\"projectOnlyStandards\":");
    }

    [Fact]
    public void DiffStandards_MissingProjectFile_ShouldReturnExitCode2()
    {
        string fakePath = Path.Combine(_tempDirectory, "nonexistent.gumx");

        CliTestHelper result = CliTestHelper.Run("diff-standards", fakePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not found");
    }

    private string CreateTestProject(string name)
    {
        string filePath = Path.Combine(_tempDirectory, name, name + ".gumx");
        CliTestHelper.Run("new", filePath, "--template", "empty");
        return filePath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
