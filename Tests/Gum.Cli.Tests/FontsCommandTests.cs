using Shouldly;

namespace Gum.Cli.Tests;

public class FontsCommandTests : BaseTestClass
{
    [Fact]
    public void Fonts_MissingProjectFile_ShouldReturnExitCode2()
    {
        string fakePath = Path.Combine(TempDirectory, "nonexistent.gumx");

        CliTestHelper result = CliTestHelper.Run("fonts", fakePath);

        result.ExitCode.ShouldBe(2);
    }
}
