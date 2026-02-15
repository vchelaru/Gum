using Gum.Managers;
using Gum.Settings;
using Moq;
using Shouldly;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GumToolUnitTests.Managers;

public class UserProjectSettingsManagerTests : BaseTestClass
{
    private readonly Mock<IOutputManager> _mockOutputManager;
    private readonly UserProjectSettingsManager _manager;
    private readonly string _testDirectory;
    private readonly string _testGumxPath;
    private readonly string _testSettingsPath;

    public UserProjectSettingsManagerTests()
    {
        _mockOutputManager = new Mock<IOutputManager>();
        _manager = new UserProjectSettingsManager(_mockOutputManager.Object);

        // Setup test paths
        _testDirectory = Path.Combine(Path.GetTempPath(), "GumTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _testGumxPath = Path.Combine(_testDirectory, "TestProject.gumx");
        _testSettingsPath = Path.ChangeExtension(_testGumxPath, ".user.setj");
    }

    public override void Dispose()
    {
        base.Dispose();

        // Cleanup test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public void Clear_ShouldResetCurrentSettings()
    {
        // Arrange
        var expandedNodes = new System.Collections.Generic.List<string> { "Components" };
        CreateSettingsFile(expandedNodes);
        _manager.LoadForProject(_testGumxPath);

        // Act
        _manager.Clear();

        // Assert
        _manager.CurrentSettings.ShouldBeNull();
    }

    [Fact]
    public void Clear_ShouldResetFilePath()
    {
        // Arrange
        var expandedNodes = new System.Collections.Generic.List<string> { "Screens" };
        CreateSettingsFile(expandedNodes);
        _manager.LoadForProject(_testGumxPath);

        // Act
        _manager.Clear();
        _manager.Save(); // Should not throw or create a file

        // Assert
        // No new files should be created (can't verify internal file path, but Save should be safe)
        _mockOutputManager.Verify(x => x.AddError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void LoadForProject_ShouldCreateEmptySettings_WhenFileDoesNotExist()
    {
        // Arrange - file does not exist

        // Act
        _manager.LoadForProject(_testGumxPath);

        // Assert
        _manager.CurrentSettings.ShouldNotBeNull();
        _manager.CurrentSettings.TreeViewState.ShouldBeNull();
    }

    [Fact]
    public void LoadForProject_ShouldCreateEmptySettings_WhenJsonIsCorrupted()
    {
        // Arrange
        File.WriteAllText(_testSettingsPath, "{ this is not valid json }", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        // Act
        _manager.LoadForProject(_testGumxPath);

        // Assert
        _manager.CurrentSettings.ShouldNotBeNull();
        _manager.CurrentSettings.TreeViewState.ShouldBeNull();
        _mockOutputManager.Verify(x => x.AddError(It.Is<string>(msg => msg.Contains("Error loading user settings"))), Times.Once);
    }

    [Fact]
    public void LoadForProject_ShouldCreateEmptySettings_WhenJsonDeserializesToNull()
    {
        // Arrange - write "null" as JSON content
        File.WriteAllText(_testSettingsPath, "null", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        // Act
        _manager.LoadForProject(_testGumxPath);

        // Assert
        _manager.CurrentSettings.ShouldNotBeNull();
        _mockOutputManager.Verify(x => x.AddOutput(It.Is<string>(msg => msg.Contains("Failed to deserialize"))), Times.Once);
    }

    [Fact]
    public void LoadForProject_ShouldLoadSettings_WhenFileExists()
    {
        // Arrange
        var expectedExpandedNodes = new System.Collections.Generic.List<string>
        {
            "Components",
            "Screens/MainMenu"
        };
        CreateSettingsFile(expectedExpandedNodes);

        // Act
        _manager.LoadForProject(_testGumxPath);

        // Assert
        _manager.CurrentSettings.ShouldNotBeNull();
        _manager.CurrentSettings.TreeViewState.ShouldNotBeNull();
        _manager.CurrentSettings.TreeViewState.ExpandedNodes.ShouldNotBeNull();
        _manager.CurrentSettings.TreeViewState.ExpandedNodes.Count.ShouldBe(expectedExpandedNodes.Count);
        _manager.CurrentSettings.TreeViewState.ExpandedNodes.ShouldBe(expectedExpandedNodes);
    }

    [Fact]
    public void Save_ShouldCreateFile_WithCorrectJsonStructure()
    {
        // Arrange
        _manager.LoadForProject(_testGumxPath);
        _manager.CurrentSettings!.TreeViewState = new TreeViewState
        {
            ExpandedNodes = new System.Collections.Generic.List<string> { "Components", "Screens" }
        };

        // Act
        _manager.Save();

        // Assert
        File.Exists(_testSettingsPath).ShouldBeTrue();

        var json = File.ReadAllText(_testSettingsPath);
        var loadedSettings = JsonSerializer.Deserialize<UserProjectSettings>(json);
        loadedSettings.ShouldNotBeNull();
        loadedSettings.TreeViewState.ShouldNotBeNull();
        loadedSettings.TreeViewState.ExpandedNodes.Count.ShouldBe(2);
        loadedSettings.TreeViewState.ExpandedNodes.ShouldContain("Components");
        loadedSettings.TreeViewState.ExpandedNodes.ShouldContain("Screens");
    }

    [Fact]
    public void Save_ShouldDoNothing_WhenCurrentSettingsIsNull()
    {
        // Arrange - don't load any settings
        _manager.Clear();

        // Act
        _manager.Save();

        // Assert
        File.Exists(_testSettingsPath).ShouldBeFalse();
    }

    [Fact]
    public void Save_ShouldHandleError_WhenFilePathIsInvalid()
    {
        // Arrange
        var invalidPath = "Z:\\InvalidPath\\DoesNotExist\\Project.gumx";
        _manager.LoadForProject(invalidPath);
        _manager.CurrentSettings!.TreeViewState = new TreeViewState();

        // Act
        _manager.Save();

        // Assert
        _mockOutputManager.Verify(x => x.AddError(It.Is<string>(msg => msg.Contains("Error saving user settings"))), Times.Once);
    }

    [Fact]
    public void Save_ShouldWriteIndentedJson()
    {
        // Arrange
        _manager.LoadForProject(_testGumxPath);
        _manager.CurrentSettings!.TreeViewState = new TreeViewState
        {
            ExpandedNodes = new System.Collections.Generic.List<string> { "Components" }
        };

        // Act
        _manager.Save();

        // Assert
        var json = File.ReadAllText(_testSettingsPath);
        json.ShouldContain("  "); // Should contain indentation
        json.ShouldContain("\n"); // Should contain newlines
    }

    [Fact]
    public void Save_WithEmptySettings_ShouldCreateValidFile()
    {
        // Arrange
        _manager.LoadForProject(_testGumxPath);
        // Leave settings empty

        // Act
        _manager.Save();

        // Assert
        File.Exists(_testSettingsPath).ShouldBeTrue();

        var json = File.ReadAllText(_testSettingsPath);
        var loadedSettings = JsonSerializer.Deserialize<UserProjectSettings>(json);
        loadedSettings.ShouldNotBeNull();
    }

    [Fact]
    public void SettingsFilePath_ShouldHaveCorrectExtension()
    {
        // Arrange
        var expandedNodes = new System.Collections.Generic.List<string> { "Components", "Screens" };
        CreateSettingsFile(expandedNodes);

        // Act
        _manager.LoadForProject(_testGumxPath);

        // Assert
        // Verify the file was read (indirect verification of correct path)
        _manager.CurrentSettings.ShouldNotBeNull();
        _manager.CurrentSettings.TreeViewState.ShouldNotBeNull();

        // Verify the extension is correct
        _testSettingsPath.ShouldEndWith(".user.setj");
    }

    private void CreateSettingsFile(System.Collections.Generic.List<string> expandedNodes)
    {
        var settings = new UserProjectSettings
        {
            TreeViewState = new TreeViewState
            {
                ExpandedNodes = expandedNodes
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(_testSettingsPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
