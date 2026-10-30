using Gum.ProjectServices;
using Shouldly;

namespace Gum.Cli.Tests;

public class NewCommandTests : BaseTestClass
{
    [Fact]
    public void New_WithGumxExtension_ShouldCreateProjectAtExactPath()
    {
        string filePath = Path.Combine(TempDirectory, "MyProject.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath);

        result.ExitCode.ShouldBe(0);
        File.Exists(filePath).ShouldBeTrue();
        result.StandardOutput.ShouldContain("Created project:");
    }

    [Fact]
    public void New_WithDirectoryName_ShouldCreateProjectInsideDirectory()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir);

        result.ExitCode.ShouldBe(0);

        // Defaults to .gumj (JSON, AOT-safe) when no extension is given (#4705).
        string expectedGumj = Path.Combine(projectDir, "MyGame.gumj");
        File.Exists(expectedGumj).ShouldBeTrue();
    }

    [Fact]
    public void New_WithDirectoryNameAndTrailingSeparator_ShouldNameProjectAfterDirectory()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir + Path.DirectorySeparatorChar);

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(projectDir, "MyGame.gumj")).ShouldBeTrue();
        File.Exists(Path.Combine(projectDir, ".gumj")).ShouldBeFalse();
    }

    [Fact]
    public void New_WithGumjExtension_ShouldCreateProjectAtExactPath()
    {
        string filePath = Path.Combine(TempDirectory, "MyProject.gumj");

        CliTestHelper result = CliTestHelper.Run("new", filePath);

        result.ExitCode.ShouldBe(0);
        File.Exists(filePath).ShouldBeTrue();
        File.Exists(Path.Combine(TempDirectory, "MyProject.gumx")).ShouldBeFalse();
    }

    [Fact]
    public void New_ShouldCreateStandardSubfolders()
    {
        string filePath = Path.Combine(TempDirectory, "SubfolderTest.gumx");

        CliTestHelper.Run("new", filePath);

        Directory.Exists(Path.Combine(TempDirectory, "Screens")).ShouldBeTrue();
        Directory.Exists(Path.Combine(TempDirectory, "Components")).ShouldBeTrue();
        Directory.Exists(Path.Combine(TempDirectory, "Standards")).ShouldBeTrue();
        Directory.Exists(Path.Combine(TempDirectory, "Behaviors")).ShouldBeTrue();
    }

    [Fact]
    public void New_DefaultTemplate_ShouldBeFormsTemplate()
    {
        string filePath = Path.Combine(TempDirectory, "FormsDefault.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath);

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(TempDirectory, "Components", "Controls", "ButtonStandard.gucx")).ShouldBeTrue();
    }

    [Fact]
    public void New_WithTheme_ShouldCreateProjectFromTheme()
    {
        string filePath = Path.Combine(TempDirectory, "Themed.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "--theme", "meadow");

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(TempDirectory, "Components", "Meadow", "Controls", "Label.gucx")).ShouldBeTrue();
        File.Exists(Path.Combine(TempDirectory, "Behaviors", "ButtonBehavior.behx")).ShouldBeTrue();
    }

    [Fact]
    public void New_WithUnknownTheme_ShouldReturnExitCode2AndListThemes()
    {
        string filePath = Path.Combine(TempDirectory, "Themed.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "--theme", "NoSuchTheme");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("NoSuchTheme");
        result.StandardError.ShouldContain("Meadow");
        File.Exists(filePath).ShouldBeFalse();
    }

    [Fact]
    public void New_WithThemeAndEmptyTemplate_ShouldReturnExitCode2()
    {
        string filePath = Path.Combine(TempDirectory, "Themed.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "--template", "empty", "--theme", "Meadow");

        result.ExitCode.ShouldBe(2);
        File.Exists(filePath).ShouldBeFalse();
    }

    [Fact]
    public void New_WithStandardTheme_ShouldCreateTheDefaultFormsTemplate()
    {
        string filePath = Path.Combine(TempDirectory, "Standard.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "--theme", "Standard");

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(TempDirectory, "Components", "Controls", "ButtonStandard.gucx")).ShouldBeTrue();
    }

    [Fact]
    public void New_WhenProjectAlreadyExists_ShouldReturnExitCode2()
    {
        string filePath = Path.Combine(TempDirectory, "Existing.gumx");
        CliTestHelper.Run("new", filePath);

        CliTestHelper result = CliTestHelper.Run("new", filePath);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("already exists");
    }

    [Fact]
    public void New_WithInvalidTemplate_ShouldReturnExitCode2()
    {
        string filePath = Path.Combine(TempDirectory, "InvalidTemplate.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "--template", "bogus");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("bogus");
    }

    [Fact]
    public void New_WithTemplateEmpty_ShouldCreateStandardFiles()
    {
        string filePath = Path.Combine(TempDirectory, "EmptyTemplate.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "-t", "empty");

        result.ExitCode.ShouldBe(0);
        File.Exists(Path.Combine(TempDirectory, "Standards", "Text.gutx")).ShouldBeTrue();
    }

    [Fact]
    public void New_WithTemplateEmpty_ShouldNotCreateFormsControls()
    {
        string filePath = Path.Combine(TempDirectory, "EmptyNoForms.gumx");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "-t", "empty");

        result.ExitCode.ShouldBe(0);
        Directory.Exists(Path.Combine(TempDirectory, "Components", "Controls")).ShouldBeFalse();
    }

    [Fact]
    public void New_WithoutPath_ShouldCreateProjectInDefaultSubdirectoryUnderCurrentDirectory()
    {
        string originalCurrentDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(TempDirectory);

            CliTestHelper result = CliTestHelper.Run("new");

            result.ExitCode.ShouldBe(0);
            // Defaults to .gumj (JSON, AOT-safe) when no path is given at all (#4705).
            string expectedGumj = Path.Combine(TempDirectory, "GumProject", "GumProject.gumj");
            File.Exists(expectedGumj).ShouldBeTrue();
            result.StandardOutput.ShouldContain("Created project:");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
        }
    }

    [Fact]
    public void New_WithoutPath_WhenDefaultSubdirectoryAlreadyExists_ShouldReturnExitCode2()
    {
        string originalCurrentDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(TempDirectory);

            CliTestHelper.Run("new");
            CliTestHelper result = CliTestHelper.Run("new");

            result.ExitCode.ShouldBe(2);
            result.StandardError.ShouldContain("already exists");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
        }
    }

    [Fact]
    public void New_DefaultTemplateAndExtension_ShouldProduceLoadableJsonProject()
    {
        // The default "forms" template is extracted from an XML-only embedded resource (#4705);
        // this proves the end-to-end gumcli path actually converts it, not just the lower-level
        // FormsTemplateCreator unit.
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir);

        result.ExitCode.ShouldBe(0);
        string gumjPath = Path.Combine(projectDir, "MyGame.gumj");
        File.Exists(gumjPath).ShouldBeTrue();

        ProjectLoadResult loadResult = new ProjectLoader().Load(gumjPath);
        loadResult.Success.ShouldBeTrue();
        loadResult.LoadErrors.ShouldBeEmpty();
    }

    [Fact]
    public void New_WithPlatform_ShouldCreateHostProjectWithGumProjectAndCodegenSettings()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "monogame", "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.Exists(Path.Combine(projectDir, "MyGame.csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(projectDir, "Game1.cs")).ShouldBeTrue();
        string gumFolder = Path.Combine(projectDir, "Content", "GumProject");
        File.Exists(Path.Combine(gumFolder, "GumProject.gumj")).ShouldBeTrue();
        File.Exists(Path.Combine(gumFolder, "ProjectCodeSettings.codsj")).ShouldBeTrue();
        result.StandardOutput.ShouldContain("MyGame.csproj");
    }

    [Fact]
    public void New_WithPlatform_ShouldProduceProjectThatCodegenAcceptsAndChecksClean()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");
        CliTestHelper.Run("new", projectDir, "--platform", "monogame", "--no-restore");
        string gumjPath = Path.Combine(projectDir, "Content", "GumProject", "GumProject.gumj");

        CliTestHelper checkResult = CliTestHelper.Run("check", gumjPath);
        CliTestHelper codegenResult = CliTestHelper.Run("codegen", gumjPath);

        checkResult.ExitCode.ShouldBe(0, checkResult.StandardOutput + checkResult.StandardError);
        codegenResult.ExitCode.ShouldBe(0, codegenResult.StandardOutput + codegenResult.StandardError);
        File.Exists(Path.Combine(projectDir, "Screens", "DemoScreenGum.Generated.cs")).ShouldBeTrue();
    }

    [Fact]
    public void New_WithStridePlatform_ShouldCreateHostProjectWithoutGameClass()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "-p", "stride", "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.ReadAllText(Path.Combine(projectDir, "MyGame.csproj")).ShouldContain("Gum.Stride");
        File.Exists(Path.Combine(projectDir, "Program.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(projectDir, "Game1.cs")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("silknet")]
    [InlineData("silk.net")]
    public void New_WithSilkNetPlatform_ShouldCreateHostProjectWithoutGameClass(string platform)
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "-p", platform, "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        File.ReadAllText(Path.Combine(projectDir, "MyGame.csproj")).ShouldContain("Gum.SilkNet");
        File.Exists(Path.Combine(projectDir, "Game1.cs")).ShouldBeFalse();
    }

    [Fact]
    public void New_WithKniSkiaPlatform_ShouldCreateKniGameDrawingGumThroughSkia()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "-p", "kniskia", "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        string csproj = File.ReadAllText(Path.Combine(projectDir, "MyGame.csproj"));
        csproj.ShouldContain("<PackageReference Include=\"Gum.KniSkia\"");
        csproj.ShouldContain("<PackageReference Include=\"SkiaGameRendering.Kni.DesktopGL\"");
        csproj.ShouldContain("<PackageReference Include=\"nkast.Kni.Platform.SDL2.GL\"");
        csproj.ShouldNotContain("Gum.KNI\"");
        File.Exists(Path.Combine(projectDir, "Program.cs")).ShouldBeTrue();
        string game = File.ReadAllText(Path.Combine(projectDir, "Game1.cs"));
        game.ShouldContain("namespace MyGame;");
        game.ShouldContain("GumUI.Initialize(this, \"Content/GumProject/GumProject.gumj\")");
        game.ShouldContain("SkiaRenderTarget2D");
        game.ShouldContain("GumUI.Draw(_canvas.Canvas)");
    }

    [Fact]
    public void New_WithUnknownPlatform_ShouldReturnExitCode2NamingValidValues()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "bogus");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("bogus");
        result.StandardError.ShouldContain("monogame");
        result.StandardError.ShouldContain("silknet");
        result.StandardError.ShouldContain("kniskia");
        Directory.Exists(projectDir).ShouldBeFalse();
    }

    [Fact]
    public void New_WithFnaPlatform_ShouldReturnExitCode2ExplainingItNeedsSource()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "fna");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("FNA");
        Directory.Exists(projectDir).ShouldBeFalse();
    }

    [Fact]
    public void New_WithPlatformAndProjectFilePath_ShouldReturnExitCode2()
    {
        string filePath = Path.Combine(TempDirectory, "MyGame.gumj");

        CliTestHelper result = CliTestHelper.Run("new", filePath, "--platform", "monogame", "--no-restore");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("folder");
        File.Exists(filePath).ShouldBeFalse();
    }

    [Fact]
    public void New_WithPlatformAndExistingProject_ShouldReturnExitCode2()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");
        CliTestHelper.Run("new", projectDir, "--platform", "raylib", "--no-restore");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "raylib", "--no-restore");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("already exists");
    }

    [Fact]
    public void New_WithPlatformAndSourceLinked_ShouldReferenceRuntimeProjectInsteadOfPackage()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "monogame", "--source-linked", "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        string csproj = File.ReadAllText(Path.Combine(projectDir, "MyGame.csproj"));
        csproj.ShouldContain("<ProjectReference Include=");
        csproj.ShouldContain("MonoGameGum.csproj");
        csproj.ShouldNotContain("Gum.MonoGame\"");
    }

    [Fact]
    public void New_WithSourceLinkedButNoPlatform_ShouldReturnExitCode2()
    {
        string projectDir = Path.Combine(TempDirectory, "MyGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--source-linked");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--platform");
        Directory.Exists(projectDir).ShouldBeFalse();
    }

    #region Output text pinned for the move into Gum.ProjectServices (#7208)

    [Fact]
    public void New_GumOnly_ShouldPrintCreatedProjectLineWithFullPath()
    {
        string filePath = Path.Combine(TempDirectory, "Pinned.gumj");

        CliTestHelper result = CliTestHelper.Run("new", filePath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.Trim().ShouldBe($"Created project: {filePath}");
    }

    [Fact]
    public void New_WithInvalidTemplate_ShouldPrintValidValues()
    {
        CliTestHelper result = CliTestHelper.Run("new", Path.Combine(TempDirectory, "X.gumx"), "--template", "bogus");

        result.ExitCode.ShouldBe(2);
        result.StandardError.Trim().ShouldEndWith("Unknown template 'bogus'. Valid values are: forms, empty.");
    }

    [Fact]
    public void New_WithThemeAndEmptyTemplate_ShouldExplainThemesStyleForms()
    {
        CliTestHelper result = CliTestHelper.Run(
            "new", Path.Combine(TempDirectory, "X.gumx"), "--template", "empty", "--theme", "Meadow");

        result.StandardError.Trim().ShouldEndWith("--theme needs the 'forms' template: themes style the Forms controls.");
    }

    [Fact]
    public void New_WithUnknownPlatform_ShouldPrintExactValidValues()
    {
        CliTestHelper result = CliTestHelper.Run("new", Path.Combine(TempDirectory, "G"), "--platform", "nope");

        result.ExitCode.ShouldBe(2);
        result.StandardError.Trim().ShouldEndWith(
            "Unknown platform 'nope'. Valid values are: monogame, kni, kniskia, raylib, stride, silknet.");
    }

    [Fact]
    public void New_WithFnaPlatform_ShouldPrintNuGetExplanation()
    {
        CliTestHelper result = CliTestHelper.Run("new", Path.Combine(TempDirectory, "G"), "--platform", "fna");

        result.StandardError.Trim().ShouldEndWith(
            "FNA projects can't be scaffolded: FNA is not published to NuGet, so it has to be linked from source.");
    }

    [Fact]
    public void New_WithPlatformAndProjectFilePath_ShouldPrintFolderExplanation()
    {
        CliTestHelper result = CliTestHelper.Run(
            "new", Path.Combine(TempDirectory, "G.gumj"), "--platform", "monogame");

        result.StandardError.Trim().ShouldEndWith("With --platform, <path> names the project folder, not a Gum project file.");
    }

    [Fact]
    public void New_WithPlatformAndInvalidName_ShouldPrintNameRules()
    {
        CliTestHelper result = CliTestHelper.Run("new", Path.Combine(TempDirectory, "9bad"), "--platform", "monogame", "--no-restore");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("'9bad' is not a valid project name.");
        Directory.Exists(Path.Combine(TempDirectory, "9bad")).ShouldBeFalse();
    }

    [Fact]
    public void New_WithPlatform_ShouldPrintBothCreatedLines()
    {
        string projectDir = Path.Combine(TempDirectory, "PinnedGame");

        CliTestHelper result = CliTestHelper.Run("new", projectDir, "--platform", "raylib", "--no-restore");

        result.ExitCode.ShouldBe(0, result.StandardError);
        string[] lines = result.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        lines.ShouldBe(new[]
        {
            $"Created project: {Path.Combine(projectDir, "PinnedGame.csproj")}",
            $"Created Gum project: {Path.Combine(projectDir, "Content", "GumProject", "GumProject.gumj")}"
        });
    }

    #endregion
}
