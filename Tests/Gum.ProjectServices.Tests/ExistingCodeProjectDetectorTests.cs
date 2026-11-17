using Microsoft.Extensions.Time.Testing;
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

    // Backslash UNC paths are only UNC on Windows; elsewhere they are a relative file name.
    // How long it takes is bounded by the search timeout, which SearchThatNeverFinishes_ReturnsNullAtTheBound covers.
    [SkippableFact]
    public void FindExistingCodeProject_UncPathToAnUnreachableServer_ReturnsNull()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "UNC paths are Windows-only");

        string? found = _detector.FindExistingCodeProject(@"\\gum-no-such-server-7208\share\x");

        found.ShouldBeNull();
    }

    [Fact]
    public void FindExistingCodeProject_AbsolutePathUnderTheRootThatDoesNotExist_DoesNotReturnOneInside()
    {
        string missing = Path.Combine(Path.GetPathRoot(_root)!, "gum-nonexistent-7208", "share", "x");

        string? found = _detector.FindExistingCodeProject(missing);

        (found == null || !found.StartsWith(missing)).ShouldBeTrue();
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
    public async Task FindExistingCodeProject_SearchThatNeverFinishes_ReturnsNullAtTheBound()
    {
        using ManualResetEventSlim release = new ManualResetEventSlim();
        TimeSpan bound = TimeSpan.FromSeconds(2);
        TimerSignalingTimeProvider clock = new TimerSignalingTimeProvider();
        ExistingCodeProjectDetector detector = new ExistingCodeProjectDetector(
            bound,
            _ =>
            {
                release.Wait();
                return "never";
            },
            clock);

        Task<string?> find = Task.Run(() => detector.FindExistingCodeProject(_root));
        // Hang deadlines only: the result depends on the test's clock, never on how fast the machine is.
        clock.TimerCreated.Wait(TimeSpan.FromSeconds(60)).ShouldBeTrue("the search never started its timeout");
        clock.Advance(bound - TimeSpan.FromTicks(1));
        bool finishedBeforeTheBound = find.IsCompleted;
        clock.Advance(TimeSpan.FromTicks(1));
        bool finishedAtTheBound = await Task.WhenAny(find, Task.Delay(TimeSpan.FromSeconds(60))) == find;
        release.Set();

        finishedBeforeTheBound.ShouldBeFalse();
        finishedAtTheBound.ShouldBeTrue();
        (await find).ShouldBeNull();
    }

    [Fact]
    public void FindExistingCodeProject_RunsTheSearchOffTheThreadPool()
    {
        bool? ranOnThreadPool = null;
        ExistingCodeProjectDetector detector = new ExistingCodeProjectDetector(
            TimeSpan.FromSeconds(60),
            _ =>
            {
                ranOnThreadPool = Thread.CurrentThread.IsThreadPoolThread;
                return null;
            });

        detector.FindExistingCodeProject(_root);

        // A search that hangs on a pool thread holds that thread forever; enough of them starve the pool.
        ranOnThreadPool.ShouldBe(false);
    }

    private sealed class TimerSignalingTimeProvider : FakeTimeProvider
    {
        public ManualResetEventSlim TimerCreated { get; } = new ManualResetEventSlim();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ITimer timer = base.CreateTimer(callback, state, dueTime, period);
            TimerCreated.Set();
            return timer;
        }
    }

    [Fact]
    public void FindExistingCodeProject_SearchThatThrows_ReturnsNull()
    {
        ExistingCodeProjectDetector detector = new ExistingCodeProjectDetector(
            TimeSpan.FromSeconds(5), _ => throw new InvalidOperationException("boom"));

        detector.FindExistingCodeProject(_root).ShouldBeNull();
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

    [Theory]
    [InlineData("Game.csproj")]
    [InlineData("Game.sln")]
    [InlineData("Game.slnx")]
    public void FindExistingCodeProject_FindsEachKindOfCodeProjectFile(string fileName)
    {
        string path = Path.Combine(_root, fileName);
        File.WriteAllText(path, "");

        _detector.FindExistingCodeProject(_root).ShouldBe(path);
    }

    [Fact]
    public void FindExistingCodeProject_FolderNamedLikeAProjectFile_IsNotAProject()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Fake.csproj"));

        string? found = _detector.FindExistingCodeProject(_root);

        (found ?? "").ShouldNotStartWith(_root);
    }

    [Fact]
    public void TargetExistsAndIsNotEmpty_NameWithCharactersAPathCannotHave_IsFalse()
    {
        _detector.TargetExistsAndIsNotEmpty(_root, "a\0b").ShouldBeFalse();
    }

    [Fact]
    public void TargetExistsAndIsNotEmpty_FolderHoldingOnlyAnEmptyFolder_IsNotEmpty()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Game", "Sub"));

        _detector.TargetExistsAndIsNotEmpty(_root, "Game").ShouldBeTrue();
    }
}
