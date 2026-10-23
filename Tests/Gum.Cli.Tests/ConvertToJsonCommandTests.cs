using Shouldly;

namespace Gum.Cli.Tests;

public class ConvertToJsonCommandTests : BaseTestClass
{
    [Fact]
    public void ConvertToJson_FormsProject_ShouldWriteGumjSiblingAndLeaveGumxUntouched()
    {
        string filePath = Path.Combine(TempDirectory, "MyProject", "MyProject.gumx");
        CliTestHelper.Run("new", filePath);
        byte[] gumxBytesBefore = File.ReadAllBytes(filePath);

        CliTestHelper result = CliTestHelper.Run("convert-to-json", filePath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("MyProject.gumj");
        File.Exists(Path.Combine(TempDirectory, "MyProject", "MyProject.gumj")).ShouldBeTrue();
        File.ReadAllBytes(filePath).ShouldBe(gumxBytesBefore);
    }

    [Fact]
    public void ConvertToJson_MissingProjectFile_ShouldReturnExitCode2()
    {
        string fakePath = Path.Combine(TempDirectory, "nonexistent.gumx");

        CliTestHelper result = CliTestHelper.Run("convert-to-json", fakePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not found");
    }
}
