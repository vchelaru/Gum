using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class FileNameCaseCheckerTests : IDisposable
{
    private readonly string _root;

    public FileNameCaseCheckerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumCaseChecker_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Textures"));
        File.WriteAllText(Path.Combine(_root, "Textures", "hero.png"), string.Empty);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Theory]
    [InlineData("Textures/Hero.png")]
    [InlineData("textures/hero.png")]
    [InlineData("TEXTURES\\HERO.PNG")]
    public void FindCaseMismatch_ReturnsTheOnDiskSpelling_WhenOnlyTheCaseDiffers(string referencedPath)
    {
        FileNameCaseChecker checker = new FileNameCaseChecker();

        string? actual = checker.FindCaseMismatch(_root, referencedPath);

        actual.ShouldBe("Textures/hero.png");
    }

    [Theory]
    [InlineData("Textures/hero.png")]
    [InlineData("Textures/other.png")]
    [InlineData("Missing/hero.png")]
    [InlineData("")]
    public void FindCaseMismatch_ReturnsNull_WhenThePathMatchesOrDoesNotExist(string referencedPath)
    {
        FileNameCaseChecker checker = new FileNameCaseChecker();

        string? actual = checker.FindCaseMismatch(_root, referencedPath);

        actual.ShouldBeNull();
    }
}
