using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System.Text.RegularExpressions;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The first minutes of adding a Gum project to an existing game: what code generation derives from the
/// game's project file name, what a project made on another OS or an older Gum looks like to the loader.
/// </summary>
public class BeginnerProjectSetupTests : BaseTestClass
{
    private readonly string _folder;

    public BeginnerProjectSetupTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumBeginnerSetup_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    #region Namespace from the game's project file

    [Theory]
    [InlineData("2DPlatformer")]
    [InlineData("My Game")]
    [InlineData("my-game")]
    [InlineData("Space Shooter 2")]
    [InlineData("Game.Client")]
    [InlineData("3D-Maze.Game")]
    [InlineData("Café")]
    public void Run_CsprojNamedTheWayABeginnerNamesAGame_DerivesANamespaceTheGeneratedCodeCanUse(string csprojName)
    {
        File.WriteAllText(Path.Combine(_folder, csprojName + ".csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        string gumx = Path.Combine(_folder, "Content", "GumProject", "GumProject.gumx");
        Directory.CreateDirectory(Path.GetDirectoryName(gumx)!);
        File.WriteAllText(gumx, "");

        AutoSetupResult result = new CodeGenerationAutoSetupService().Run(gumx);

        result.Success.ShouldBeTrue();
        foreach (string segment in result.Settings!.RootNamespace.Split('.'))
        {
            Regex.IsMatch(segment, @"^[\p{L}_][\p{L}\p{Nd}_]*$").ShouldBeTrue($"'{segment}' of '{result.Settings.RootNamespace}' is not a C# identifier");
        }
    }

    #endregion

    #region Loading a project

    [Fact]
    public void Load_SameProjectTwice_ReturnsTheSameElementsBothTimes()
    {
        string gumx = CreateProject("Content/GumProject");
        ProjectLoader loader = new ProjectLoader();

        ProjectLoadResult first = loader.Load(gumx);
        ProjectLoadResult second = loader.Load(gumx);

        first.Success.ShouldBeTrue(first.ErrorMessage);
        second.Success.ShouldBeTrue(second.ErrorMessage);
        second.Project!.Screens.Select(item => item.Name).ShouldBe(first.Project!.Screens.Select(item => item.Name));
        second.Project.StandardElements.Count.ShouldBe(first.Project.StandardElements.Count);
    }

    [Fact]
    public void Load_ProjectInAFolderWithASpace_Loads()
    {
        string gumx = CreateProject("Content/Gum Project");

        ProjectLoadResult result = new ProjectLoader().Load(gumx);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        result.Project!.Screens.Select(item => item.Name).ShouldContain("MainMenu");
    }

    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    public void Load_ProjectAndElementFilesWithEitherLineEnding_Loads(string newline)
    {
        string gumx = CreateProject("Content/GumProject");
        foreach (string file in Directory.GetFiles(Path.GetDirectoryName(gumx)!, "*.gu*", SearchOption.AllDirectories))
        {
            File.WriteAllText(file, File.ReadAllText(file).Replace("\r\n", "\n").Replace("\n", newline));
        }

        ProjectLoadResult result = new ProjectLoader().Load(gumx);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        result.LoadErrors.ShouldBeEmpty();
        result.Project!.Screens.Select(item => item.Name).ShouldContain("MainMenu");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void Load_ProjectSavedByAnOlderGumVersion_LoadsWithoutErrors(int olderVersion)
    {
        string gumx = CreateProject("Content/GumProject");
        File.WriteAllText(gumx, Regex.Replace(File.ReadAllText(gumx), "<Version>\\d+</Version>", $"<Version>{olderVersion}</Version>"));

        ProjectLoadResult result = new ProjectLoader().Load(gumx);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        result.LoadErrors.ShouldBeEmpty();
        result.Project!.Screens.Select(item => item.Name).ShouldContain("MainMenu");
    }

    [Fact]
    public void Load_ProjectWhoseScreenFileWasDeletedByHand_NamesTheMissingFile()
    {
        string gumx = CreateProject("Content/GumProject");
        File.Delete(Path.Combine(Path.GetDirectoryName(gumx)!, "Screens", "MainMenu.gusx"));

        ProjectLoadResult result = new ProjectLoader().Load(gumx);

        string message = result.ErrorMessage ?? string.Join("\n", result.LoadErrors.Select(item => item.Message));
        message.ShouldContain("MainMenu");
    }

    #endregion

    #region Files authored on Windows

    [Fact]
    public void GetErrorsFor_SpriteSourceFileSavedWithBackslashes_FindsTheFileOnAnyOs()
    {
        string tempDirectory = Path.Combine(_folder, "Content", "GumProject");
        Directory.CreateDirectory(Path.Combine(tempDirectory, "Textures"));
        File.WriteAllBytes(Path.Combine(tempDirectory, "Textures", "Icon.png"), new byte[] { 0 });
        Project.FullFileName = Path.Combine(tempDirectory, "GumProject.gumx");
        ComponentSave component = new ComponentSave { Name = "IconHolder" };
        component.Instances.Add(new InstanceSave { Name = "MySprite", BaseType = "Sprite" });
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "MySprite.SourceFile", Value = "Textures\\Icon.png", Type = "string", IsFile = true });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = new HeadlessErrorChecker(new Mock<ITypeResolver>().Object).GetErrorsFor(component, Project);

        errors.ShouldBeEmpty();
    }

    #endregion

    private string CreateProject(string relativeFolder)
    {
        string gumx = Path.Combine(_folder, relativeFolder.Replace('/', Path.DirectorySeparatorChar), "GumProject.gumx");
        new ProjectCreator().Create(gumx);
        string screen = Path.Combine(Path.GetDirectoryName(gumx)!, "Screens", "MainMenu.gusx");
        File.WriteAllText(screen,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>MainMenu</Name>
              <State>
                <Name>Default</Name>
              </State>
            </ScreenSave>
            """);
        File.WriteAllText(gumx, File.ReadAllText(gumx).Replace("</GumProjectSave>", "  <ScreenReference Name=\"MainMenu\" />\n</GumProjectSave>"));
        return gumx;
    }
}
