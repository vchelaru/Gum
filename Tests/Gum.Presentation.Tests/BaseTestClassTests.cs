using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
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
public class BaseTestClassTests : BaseTestClass
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

    [Fact]
    public void Dispose_RestoresCanvasSize_WhenATestChangedIt()
    {
        // The canvas is a process-wide static the wireframe layout reads, so a test that sets it
        // must not leave it for whichever test runs next.
        float originalWidth = GraphicalUiElement.CanvasWidth;
        float originalHeight = GraphicalUiElement.CanvasHeight;
        try
        {
            BaseTestClass test = new BaseTestClass();
            GraphicalUiElement.CanvasWidth = originalWidth + 123;
            GraphicalUiElement.CanvasHeight = originalHeight + 45;

            test.Dispose();

            GraphicalUiElement.CanvasWidth.ShouldBe(originalWidth);
            GraphicalUiElement.CanvasHeight.ShouldBe(originalHeight);
        }
        finally
        {
            GraphicalUiElement.CanvasWidth = originalWidth;
            GraphicalUiElement.CanvasHeight = originalHeight;
        }
    }
}
