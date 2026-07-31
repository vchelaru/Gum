using Shouldly;

namespace Gum.Cli.Tests;

public class ConvertToJsonCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public ConvertToJsonCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliConvertToJsonTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ConvertToJson_FormsProject_ShouldWriteGumjSiblingAndLeaveGumxUntouched()
    {
        string filePath = Path.Combine(_tempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", filePath);
        byte[] gumxBytesBefore = File.ReadAllBytes(filePath);

        CliTestHelper result = CliTestHelper.Run("convert-to-json", filePath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("MyProject.gumj");
        File.Exists(Path.Combine(_tempDirectory, "MyProject", "MyProject.gumj")).ShouldBeTrue();
        File.ReadAllBytes(filePath).ShouldBe(gumxBytesBefore);
    }

    [Fact]
    public void ConvertToJson_MissingProjectFile_ShouldReturnExitCode2()
    {
        string fakePath = Path.Combine(_tempDirectory, "nonexistent.gumx");

        CliTestHelper result = CliTestHelper.Run("convert-to-json", fakePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not found");
    }
}
