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
    public void DiffStandards_FreshProject_ShouldReturnExitCode0()
    {
        // A project created via `gumcli new` extracts the bundled Default Standards
        // verbatim, so diff-standards must report no drift.
        string filePath = CreateTestProject("Fresh");

        CliTestHelper result = CliTestHelper.Run("diff-standards", filePath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("No drift found.");
    }

    [Fact]
    public void DiffStandards_ModifiedStandard_ShouldReturnExitCode1()
    {
        string filePath = CreateTestProject("Modified");
        // Force drift by editing Text.gutx to change the Font value.
        string textPath = Path.Combine(Path.GetDirectoryName(filePath)!, "Standards", "Text.gutx");
        string content = File.ReadAllText(textPath);
        content = content.Replace(
            "<Variable IsFont=\"true\" Type=\"string\" Name=\"Font\" Category=\"Font\" SetsValue=\"true\">\r\n      <Value xsi:type=\"xsd:string\">Arial</Value>\r\n    </Variable>",
            "<Variable IsFont=\"true\" Type=\"string\" Name=\"Font\" Category=\"Font\" SetsValue=\"true\">\r\n      <Value xsi:type=\"xsd:string\">ComicSans</Value>\r\n    </Variable>");
        // Fallback for LF line endings.
        content = content.Replace(
            "<Value xsi:type=\"xsd:string\">Arial</Value>",
            "<Value xsi:type=\"xsd:string\">ComicSans</Value>");
        File.WriteAllText(textPath, content);

        CliTestHelper result = CliTestHelper.Run("diff-standards", filePath);

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldContain("Text.gutx:");
        result.StandardOutput.ShouldContain("Font:");
        result.StandardOutput.ShouldContain("Arial");
        result.StandardOutput.ShouldContain("ComicSans");
    }

    [Fact]
    public void DiffStandards_ModifiedStandard_JsonFlag_ShouldEmitJson()
    {
        string filePath = CreateTestProject("ModifiedJson");
        string textPath = Path.Combine(Path.GetDirectoryName(filePath)!, "Standards", "Text.gutx");
        string content = File.ReadAllText(textPath);
        content = content.Replace(
            "<Value xsi:type=\"xsd:string\">Arial</Value>",
            "<Value xsi:type=\"xsd:string\">ComicSans</Value>");
        File.WriteAllText(textPath, content);

        CliTestHelper result = CliTestHelper.Run("diff-standards", filePath, "--json");

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldStartWith("{");
        result.StandardOutput.ShouldContain("\"hasDrift\": true");
        result.StandardOutput.ShouldContain("\"standard\": \"Text\"");
        result.StandardOutput.ShouldContain("\"variable\": \"Font\"");
        result.StandardOutput.ShouldContain("\"kind\": \"Changed\"");
    }

    [Fact]
    public void DiffStandards_MissingProjectFile_ShouldReturnExitCode2()
    {
        string fakePath = Path.Combine(_tempDirectory, "nonexistent.gumx");

        CliTestHelper result = CliTestHelper.Run("diff-standards", fakePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not found");
    }

    /// <summary>
    /// Creates a project via the <c>empty</c> template so it extracts the bundled
    /// Default Standards verbatim. The <c>forms</c> template uses FormsTemplate, whose
    /// Standards carry historic drift the diff command is designed to surface.
    /// </summary>
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
