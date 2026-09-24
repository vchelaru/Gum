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

    [Fact]
    public void FindCaseMismatch_SeesAFileRenamedAfterAnEarlierCheck()
    {
        FileNameCaseChecker checker = new FileNameCaseChecker();
        checker.FindCaseMismatch(_root, "Textures/hero.png").ShouldBeNull();

        File.Move(Path.Combine(_root, "Textures", "hero.png"), Path.Combine(_root, "Textures", "Hero.png"));

        checker.FindCaseMismatch(_root, "Textures/hero.png").ShouldBe("Textures/Hero.png");
    }

    [Fact]
    public void FindCaseMismatch_ReusesAListing_WhileItsDirectoryIsUnchanged()
    {
        string textures = Path.Combine(_root, "Textures");
        DateTime unchangedSince = DateTime.UtcNow.AddMinutes(-1);
        Directory.SetLastWriteTimeUtc(textures, unchangedSince);
        FileNameCaseChecker checker = new FileNameCaseChecker();
        checker.FindCaseMismatch(_root, "Textures/Other.png").ShouldBeNull();

        // A file added without the directory's last-write time moving is invisible to a cached listing.
        File.WriteAllText(Path.Combine(textures, "other.png"), string.Empty);
        Directory.SetLastWriteTimeUtc(textures, unchangedSince);

        checker.FindCaseMismatch(_root, "Textures/Other.png").ShouldBeNull();
    }

    [Fact]
    public void FindCaseMismatch_RereadsAListing_TakenTooSoonAfterItsDirectoryChanged()
    {
        // A coarse file-system clock can give a change made in the same tick as the listing an
        // unchanged last-write time, so a listing taken that soon is not trusted.
        string textures = Path.Combine(_root, "Textures");
        DateTime changedJustNow = DateTime.UtcNow;
        Directory.SetLastWriteTimeUtc(textures, changedJustNow);
        FileNameCaseChecker checker = new FileNameCaseChecker();
        checker.FindCaseMismatch(_root, "Textures/Other.png").ShouldBeNull();

        File.WriteAllText(Path.Combine(textures, "other.png"), string.Empty);
        Directory.SetLastWriteTimeUtc(textures, changedJustNow);

        checker.FindCaseMismatch(_root, "Textures/Other.png").ShouldBe("Textures/other.png");
    }
}
