using System.IO;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The <c>gumcli new</c> command and path the New Project dialog builds from its choices.</summary>
public class NewProjectCommandLineTests
{
    [Fact]
    public void BuildChained_KeepsSourceLinked_WhenOnlyTheCodegenStepIsChained()
    {
        string command = NewProjectCommandLine.BuildChained(
            "/p/Game", "kniskia", includeForms: true, theme: null, restore: true, packages: new string[0], csprojPath: "/p/Game/Game.csproj",
            gumProjectPath: "/p/Game/Content/GumProject/GumProject.gumj", sourceLinked: true);

        command.ShouldBe("gumcli new \"/p/Game\" --platform kniskia --source-linked && gumcli codegen \"/p/Game/Content/GumProject/GumProject.gumj\"");
    }

    [Fact]
    public void BuildChained_LinkedProjects_AreAddedAsReferences_NotPackages_BeforeTheRestore()
    {
        string command = NewProjectCommandLine.BuildChained(
            "/p/My Game", "monogame", includeForms: false, theme: null, restore: true, packages: new string[0],
            csprojPath: "/p/My Game/Game.csproj", sourceLinked: true,
            linkedProjects: new[] { "/c/Gum Src/Runtimes/GumExpressions/GumExpressions.csproj" });

        command.ShouldBe(
            "gumcli new \"/p/My Game\" --platform monogame --template empty --source-linked --no-restore" +
            " && dotnet add \"/p/My Game/Game.csproj\" reference \"/c/Gum Src/Runtimes/GumExpressions/GumExpressions.csproj\"" +
            " && dotnet restore \"/p/My Game/Game.csproj\"");
    }

    [Fact]
    public void BuildChained_KeepsSourceLinked_BeforeTheChainedSteps()
    {
        string command = NewProjectCommandLine.BuildChained(
            "/p/Game", "monogame", includeForms: true, theme: null, restore: false, packages: new[] { "Gum.Expressions" },
            csprojPath: "/p/Game/Game.csproj", gumProjectPath: "/p/Game/Content/GumProject/GumProject.gumj", sourceLinked: true);

        command.ShouldStartWith("gumcli new \"/p/Game\" --platform monogame --source-linked --no-restore && dotnet add");
        command.ShouldEndWith(" && gumcli codegen \"/p/Game/Content/GumProject/GumProject.gumj\"");
    }

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
