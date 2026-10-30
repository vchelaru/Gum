using System.IO;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The <c>gumcli new</c> command and path the New Project dialog builds from its choices.</summary>
public class NewProjectCommandLineTests
{
    [Fact]
    public void Build_WithoutAPlatform_IgnoresSourceLinkedAndRestore()
    {
        string command = NewProjectCommandLine.Build("/p/MyGame.gumj", platform: null, includeForms: true, theme: null, sourceLinked: true, restore: false);

        command.ShouldBe("gumcli new \"/p/MyGame.gumj\"");
    }

    [Theory]
    [InlineData(true, "Standard", "")]
    [InlineData(true, null, "")]
    [InlineData(true, "", "")]
    [InlineData(true, "Bubblegum", " --theme Bubblegum")]
    [InlineData(false, "Bubblegum", " --template empty")]
    public void Build_AddsTheThemeOnlyForAFormsProjectWithANonDefaultTheme(bool includeForms, string? theme, string expectedOptions)
    {
        string command = NewProjectCommandLine.Build("/p/Game", platform: null, includeForms, theme, sourceLinked: false, restore: true);

        command.ShouldBe("gumcli new \"/p/Game\"" + expectedOptions);
    }

    [Fact]
    public void GetPath_ForAGumOnlyProject_IsTheProjectFileInsideAFolderOfTheSameName()
    {
        string location = Path.Combine(Path.GetTempPath(), "Games");

        string path = NewProjectCommandLine.GetPath(location, "MyGame", ".gumx", withCodeProject: false);

        path.ShouldBe(Path.Combine(location, "MyGame", "MyGame.gumx"));
    }

    [Fact]
    public void GetPath_ForACodeProject_IsTheGameFolder()
    {
        string location = Path.Combine(Path.GetTempPath(), "Games");

        string path = NewProjectCommandLine.GetPath(location, "MyGame", ".gumj", withCodeProject: true);

        path.ShouldBe(Path.Combine(location, "MyGame"));
    }

    [Fact]
    public void GetPath_IgnoresTrailingSeparatorsOnTheLocation()
    {
        string location = Path.Combine(Path.GetTempPath(), "Games");

        string path = NewProjectCommandLine.GetPath(
            location + Path.DirectorySeparatorChar + Path.AltDirectorySeparatorChar, "MyGame", ".gumj", withCodeProject: true);

        path.ShouldBe(Path.Combine(location, "MyGame"));
    }

    [Fact]
    public void GetPath_KeepsTheRootWhenTheLocationIsAFilesystemRoot()
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!;

        NewProjectCommandLine.GetPath(root, "MyGame", ".gumj", withCodeProject: true).ShouldBe(Path.Combine(root, "MyGame"));
        NewProjectCommandLine.GetPath(root, "MyGame", ".gumj", withCodeProject: false).ShouldBe(Path.Combine(root, "MyGame", "MyGame.gumj"));
    }
}
