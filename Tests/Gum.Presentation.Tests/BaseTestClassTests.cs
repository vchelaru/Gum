using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins #4915's cross-test-order flake: a test that leaves <see cref="ObjectFinder"/>'s project
/// set (skips its own cleanup, throws before disposing, or never derives from
/// <see cref="BaseTestClass"/>) must not leak that state into whichever test runs next. Same for a
/// test that changes the app-data override.
/// </summary>
public class BaseTestClassTests
{
    [Fact]
    public void Constructor_ResetsGumProjectSave_EvenWhenAPriorTestLeftItSet()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave { FullFileName = "C:/leaked/project.gumx" };

        using BaseTestClass _ = new BaseTestClass();

        ObjectFinder.Self.GumProjectSave.ShouldBeNull();
    }

    [Fact]
    public void Dispose_RestoresUserApplicationDataOverride_WhenATestClearedIt()
    {
        // Clearing the override would send every later test to the shared %APPDATA%\testhost folder.
        string? processOverride = FileManager.UserApplicationDataFolderOverride;
        processOverride.ShouldNotBeNullOrEmpty();
        try
        {
            BaseTestClass test = new BaseTestClass();
            FileManager.UserApplicationDataFolderOverride = null;

            test.Dispose();

            FileManager.UserApplicationDataFolderOverride.ShouldBe(processOverride);
        }
        finally
        {
            FileManager.UserApplicationDataFolderOverride = processOverride;
        }
    }
}
