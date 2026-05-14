using Shouldly;

namespace Gum.Cli.Tests;

public class FontsCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public FontsCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliFontsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Fonts_MissingProjectFile_ShouldReturnExitCode2()
    {
        string fakePath = Path.Combine(_tempDirectory, "nonexistent.gumx");

        CliTestHelper result = CliTestHelper.Run("fonts", fakePath);

        result.ExitCode.ShouldBe(2);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
