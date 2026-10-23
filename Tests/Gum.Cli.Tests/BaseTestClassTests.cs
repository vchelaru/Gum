using Gum.Tests.Shared;
using Shouldly;

namespace Gum.Cli.Tests;

// Not derived from BaseTestClass: these tests build one by hand to prove what it cleans up.
public class BaseTestClassTests
{
    [Fact]
    public void Dispose_RestoresFileManagerState_WhenATestChangedIt()
    {
        FileManagerStateAssert.DisposeRestores(() => new BaseTestClass());
    }

    // The screenshot and svg commands run SystemManagers.Initialize, which sets MissingFileBehavior
    // to ThrowException; a later test that loads a project with a missing file would throw (#6556).
    [Fact]
    public void Dispose_RestoresGraphicalUiElementState_WhenATestChangedIt()
    {
        GraphicalUiElementStateAssert.DisposeRestores(() => new BaseTestClass());
    }

    [Fact]
    public void Dispose_ResetsObjectFinder_WhenATestLeftAProjectLoaded()
    {
        ObjectFinderStateAssert.DisposeResets(() => new BaseTestClass());
    }

    [Fact]
    public void Dispose_DeletesTempDirectory()
    {
        TempDirectoryProbe test = new();
        string directory = test.Directory;
        File.WriteAllText(Path.Combine(directory, "file6556.txt"), "x");

        test.Dispose();

        Directory.Exists(directory).ShouldBeFalse();
    }

    private sealed class TempDirectoryProbe : BaseTestClass
    {
        public string Directory => TempDirectory;
    }
}
