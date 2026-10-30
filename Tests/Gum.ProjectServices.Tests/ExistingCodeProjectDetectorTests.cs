using Shouldly;

namespace Gum.ProjectServices.Tests;

public class ExistingCodeProjectDetectorTests : IDisposable
{
    private readonly string _root;
    private readonly ExistingCodeProjectDetector _detector = new ExistingCodeProjectDetector();

    public ExistingCodeProjectDetectorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumCodeProjectDetector_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void FindExistingCodeProject_CsprojInTheFolder_ReturnsIt()
    {
        string csproj = Path.Combine(_root, "Game.csproj");
        File.WriteAllText(csproj, "");

        _detector.FindExistingCodeProject(_root).ShouldBe(csproj);
    }

    [Fact]
    public void FindExistingCodeProject_SolutionInAParent_ReturnsIt()
    {
        string sln = Path.Combine(_root, "Game.sln");
        File.WriteAllText(sln, "");
        string deep = Path.Combine(_root, "a", "b");
        Directory.CreateDirectory(deep);

        _detector.FindExistingCodeProject(deep).ShouldBe(sln);
    }

    [Fact]
    public void FindExistingCodeProject_FolderThatDoesNotExistYet_LooksInItsParents()
    {
        string csproj = Path.Combine(_root, "Game.csproj");
        File.WriteAllText(csproj, "");

        _detector.FindExistingCodeProject(Path.Combine(_root, "NewGame", "Sub")).ShouldBe(csproj);
    }

    [Fact]
    public void FindExistingCodeProject_NearestFolderWins()
    {
        File.WriteAllText(Path.Combine(_root, "Outer.sln"), "");
        string inner = Path.Combine(_root, "inner");
        Directory.CreateDirectory(inner);
        string innerCsproj = Path.Combine(inner, "Inner.csproj");
        File.WriteAllText(innerCsproj, "");

        _detector.FindExistingCodeProject(inner).ShouldBe(innerCsproj);
    }

    [Fact]
    public void FindExistingCodeProject_IgnoresProjectFilesInBinAndObjFolders()
    {
        string obj = Path.Combine(_root, "obj", "sub");
        Directory.CreateDirectory(obj);
        string buildOutput = Path.Combine(_root, "obj", "Generated.csproj");
        File.WriteAllText(buildOutput, "");

        string? found = _detector.FindExistingCodeProject(obj);

        found.ShouldNotBe(buildOutput);
    }

    [Fact]
    public void FindExistingCodeProject_NoProjectFileAnywhereUnderTheTestFolder_DoesNotReturnOneInside()
    {
        string? found = _detector.FindExistingCodeProject(_root);

        (found == null || !found.StartsWith(_root)).ShouldBeTrue();
    }

    [Fact]
    public void TargetExistsAndIsNotEmpty_ReflectsTheTargetFolder()
    {
        _detector.TargetExistsAndIsNotEmpty(_root, "Missing").ShouldBeFalse();

        string empty = Path.Combine(_root, "Empty");
        Directory.CreateDirectory(empty);
        _detector.TargetExistsAndIsNotEmpty(_root, "Empty").ShouldBeFalse();

        File.WriteAllText(Path.Combine(empty, "a.txt"), "");
        _detector.TargetExistsAndIsNotEmpty(_root, "Empty").ShouldBeTrue();
    }
}
