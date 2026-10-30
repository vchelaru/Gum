using System.Diagnostics;
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
    public void FindExistingCodeProject_TrailingSeparator_StillFindsTheProject()
    {
        string csproj = Path.Combine(_root, "Game.csproj");
        File.WriteAllText(csproj, "");

        _detector.FindExistingCodeProject(_root + Path.DirectorySeparatorChar).ShouldBe(csproj);
    }

    [Fact]
    public void FindExistingCodeProject_DriveRoot_DoesNotThrow()
    {
        string? found = _detector.FindExistingCodeProject(Path.GetPathRoot(_root)!);

        (found == null || File.Exists(found)).ShouldBeTrue();
    }

    [Fact]
    public void FindExistingCodeProject_PathThatDoesNotExistAtAnyLevel_DoesNotThrow()
    {
        string? found = _detector.FindExistingCodeProject(Path.Combine(_root, "no", "such", "folder"));

        (found == null || !found.StartsWith(_root)).ShouldBeTrue();
    }

    [Fact]
    public void FindExistingCodeProject_UncLookingPath_ReturnsWithinABoundedTime()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        string? found = _detector.FindExistingCodeProject(@"\\gum-no-such-server-7208\share\x");

        stopwatch.Elapsed.TotalSeconds.ShouldBeLessThan(10);
        found.ShouldBeNull();
    }

    [Fact]
    public void FindExistingCodeProject_ProjectFileOnlyInBin_IsIgnored()
    {
        string bin = Path.Combine(_root, "bin");
        Directory.CreateDirectory(bin);
        string buildOutput = Path.Combine(bin, "Generated.csproj");
        File.WriteAllText(buildOutput, "");

        _detector.FindExistingCodeProject(bin).ShouldNotBe(buildOutput);
    }

    [Theory]
    [InlineData("Only.sln")]
    [InlineData("Only.slnx")]
    [InlineData("Only.csproj")]
    public void FindExistingCodeProject_EachProjectKind_IsFound(string fileName)
    {
        string path = Path.Combine(_root, fileName);
        File.WriteAllText(path, "");

        _detector.FindExistingCodeProject(_root).ShouldBe(path);
    }

    [Fact]
    public void FindExistingCodeProject_FiveLevelsDeep_FindsTheProjectAtTheTop()
    {
        string csproj = Path.Combine(_root, "Game.csproj");
        File.WriteAllText(csproj, "");
        string deep = Path.Combine(_root, "a", "b", "c", "d", "e");
        Directory.CreateDirectory(deep);

        _detector.FindExistingCodeProject(deep).ShouldBe(csproj);
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
