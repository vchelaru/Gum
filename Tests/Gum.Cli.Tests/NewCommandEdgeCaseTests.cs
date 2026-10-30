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

}
