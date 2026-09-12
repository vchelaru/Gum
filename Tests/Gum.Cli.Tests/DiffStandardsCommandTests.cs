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
    public void DiffStandards_FreshEmptyProject_ShouldReturnExitCode0()
    {
        // `gumcli new --template empty` builds Standards/*.gutx live from
        // StandardElementsManager (#4676), so a fresh empty project is always drift-free.
        string filePath = CreateTestProject("FreshEmpty");

        CliTestHelper result = CliTestHelper.Run("diff-standards", filePath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("No drift found.");
    }

    [Fact]
    public void DiffStandards_ProjectWithDriftedFont_ShouldReturnExitCode1AndNameTheStandard()
    {
        // A fresh project is drift-free by construction (#4676), so we force a specific
        // drift here and verify it shows up in the output.
        string filePath = CreateTestProject("DriftedFont");

        string textPath = Path.Combine(Path.GetDirectoryName(filePath)!, "Standards", "Text.gutx");
        string content = File.ReadAllText(textPath);
        // Find the scaffolded Font variable's actual value and corrupt it, rather than hardcoding
        // a literal like the old default "Arial" - #4674 changed that default to a bundled .ttf
        // path, which silently no-op'd this Replace (nothing to find) and made the test pass with
        // exit code 0 instead of the drift it was supposed to force. The assertion below turns any
        // future default-value change into a loud, obvious failure here instead of a silent no-op.
        System.Text.RegularExpressions.Match fontValueMatch = System.Text.RegularExpressions.Regex.Match(
            content, @"(<Variable IsFont=""true""[^>]*>\s*<Value[^>]*>)([^<]*)(</Value>)");
        fontValueMatch.Success.ShouldBeTrue("could not locate the Font variable in the scaffolded Text.gutx");
        string driftedContent = content.Remove(fontValueMatch.Groups[2].Index, fontValueMatch.Groups[2].Length)
            .Insert(fontValueMatch.Groups[2].Index, "DefinitelyNotTheDefaultFont");
        driftedContent.ShouldNotBe(content);
        File.WriteAllText(textPath, driftedContent);

        CliTestHelper result = CliTestHelper.Run("diff-standards", filePath);

        result.ExitCode.ShouldBe(1);
        result.StandardOutput.ShouldContain("Text.gutx:");
        result.StandardOutput.ShouldContain("DefinitelyNotTheDefaultFont");
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
