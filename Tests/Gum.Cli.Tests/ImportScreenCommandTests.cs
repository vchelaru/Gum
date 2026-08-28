using Shouldly;

namespace Gum.Cli.Tests;

public class ImportScreenCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public ImportScreenCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliImportScreenTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ImportScreen_ShouldAddReferenceAndWriteScreenFile()
    {
        string projectPath = CreateTestProject("Basic");
        string screenPath = WriteStagedScreen("Basic", "Imported");

        CliTestHelper result = CliTestHelper.Run("import-screen", projectPath, screenPath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("Imported screen \"Imported\"");

        string projectDirectory = Path.GetDirectoryName(projectPath)!;
        File.Exists(Path.Combine(projectDirectory, "Screens", "Imported.gusx")).ShouldBeTrue();
        File.ReadAllText(projectPath).ShouldContain("Imported");
    }

    [Fact]
    public void ImportScreen_ShouldUniquifyName_WhenScreenAlreadyExists()
    {
        string projectPath = CreateTestProject("Conflict");
        string firstScreenPath = WriteStagedScreen("Conflict", "Dupe", stagingFolderName: "staged1");
        CliTestHelper.Run("import-screen", projectPath, firstScreenPath).ExitCode.ShouldBe(0);

        string secondScreenPath = WriteStagedScreen("Conflict", "Dupe", stagingFolderName: "staged2");
        CliTestHelper result = CliTestHelper.Run("import-screen", projectPath, secondScreenPath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("Imported screen \"Dupe_2\"");

        string projectDirectory = Path.GetDirectoryName(projectPath)!;
        File.Exists(Path.Combine(projectDirectory, "Screens", "Dupe_2.gusx")).ShouldBeTrue();
    }

    [Fact]
    public void ImportScreen_ShouldQualifyNameWithSubfolder()
    {
        string projectPath = CreateTestProject("Subfoldered");
        string screenPath = WriteStagedScreen("Subfoldered", "MyScreen");

        CliTestHelper result = CliTestHelper.Run(
            "import-screen", projectPath, screenPath, "--subfolder", "Section1");

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("Imported screen \"Section1/MyScreen\"");

        string projectDirectory = Path.GetDirectoryName(projectPath)!;
        File.Exists(Path.Combine(projectDirectory, "Screens", "Section1", "MyScreen.gusx")).ShouldBeTrue();
    }

    [Fact]
    public void ImportScreen_ShouldCopyStagedAssets_WhenAssetsOptionProvided()
    {
        string projectPath = CreateTestProject("WithAssets");
        string screenPath = WriteStagedScreen("WithAssets", "AssetScreen", stagingFolderName: "staged");

        string stagingDirectory = Path.Combine(Path.GetDirectoryName(projectPath)!, "..", "staged");
        stagingDirectory = Path.GetFullPath(stagingDirectory);
        Directory.CreateDirectory(Path.Combine(stagingDirectory, "Images"));
        File.WriteAllText(Path.Combine(stagingDirectory, "Images", "icon.png"), "fake-png");

        CliTestHelper result = CliTestHelper.Run(
            "import-screen", projectPath, screenPath, "--assets", stagingDirectory);

        result.ExitCode.ShouldBe(0);

        string projectDirectory = Path.GetDirectoryName(projectPath)!;
        File.Exists(Path.Combine(projectDirectory, "Images", "icon.png")).ShouldBeTrue();
    }

    [Fact]
    public void ImportScreen_NonexistentProject_ExitCode2()
    {
        string fakeProjectPath = Path.Combine(_tempDirectory, "nonexistent.gumx");
        string screenPath = WriteStagedScreen("Unused", "Screen1");

        CliTestHelper result = CliTestHelper.Run("import-screen", fakeProjectPath, screenPath);

        result.ExitCode.ShouldBe(2);
    }

    [Fact]
    public void ImportScreen_NonexistentScreenFile_ExitCode2()
    {
        string projectPath = CreateTestProject("MissingScreen");
        string fakeScreenPath = Path.Combine(_tempDirectory, "nonexistent.gusx");

        CliTestHelper result = CliTestHelper.Run("import-screen", projectPath, fakeScreenPath);

        result.ExitCode.ShouldBe(2);
    }

    private string CreateTestProject(string name)
    {
        string filePath = Path.Combine(_tempDirectory, name, name + ".gumx");
        CliTestHelper.Run("new", filePath);
        return filePath;
    }

    private string WriteStagedScreen(string projectName, string screenName, string stagingFolderName = "staged")
    {
        string stagingDirectory = Path.Combine(_tempDirectory, projectName, stagingFolderName);
        Directory.CreateDirectory(stagingDirectory);
        string screenPath = Path.Combine(stagingDirectory, screenName + ".gusx");
        File.WriteAllText(screenPath,
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <ScreenSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>{screenName}</Name>
            </ScreenSave>
            """);
        return screenPath;
    }
}
