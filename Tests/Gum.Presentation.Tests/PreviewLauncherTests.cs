using System;
using System.IO;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Covers <see cref="PreviewLauncher"/>'s pure selection-file formatting and its validation/branch
/// logic. Every scenario here either returns before <c>Process.Start</c> or lets it fail against a
/// fake (non-executable) file, which <see cref="PreviewLauncher.Launch"/> already catches and
/// reports as an error - so none of this spawns a real preview process.
/// </summary>
public class PreviewLauncherTests : IDisposable
{
    private static readonly string ExeName = OperatingSystem.IsWindows() ? "GumPreview.exe" : "GumPreview";

    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IOutputManager> _outputManager = new();
    private readonly Mock<IPreviewGumxProjectionService> _previewGumxProjectionService = new();
    private readonly string _headBaseDirectory;

    public PreviewLauncherTests()
    {
        _headBaseDirectory = Path.Combine(Path.GetTempPath(), "GumPreviewLauncherTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_headBaseDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_headBaseDirectory))
        {
            Directory.Delete(_headBaseDirectory, recursive: true);
        }
    }

    private PreviewLauncher CreateLauncher() =>
        new PreviewLauncher(_selectedState.Object, _projectManager.Object, _outputManager.Object, _previewGumxProjectionService.Object, _headBaseDirectory);

    [Fact]
    public void BuildSelectionFileContent_WhenActivate_AppendsTheActivateMarker()
    {
        PreviewLauncher.BuildSelectionFileContent("MainMenu", activate: true).ShouldBe("MainMenu\nactivate");
    }

    [Fact]
    public void BuildSelectionFileContent_WhenNotActivate_IsJustTheElementName()
    {
        PreviewLauncher.BuildSelectionFileContent("MainMenu", activate: false).ShouldBe("MainMenu");
    }

    [Fact]
    public void Launch_WhenNoProjectIsLoaded_ReportsErrorAndDoesNotThrow()
    {
        _projectManager.SetupGet(p => p.GumProjectSave).Returns((GumProjectSave?)null);

        CreateLauncher().Launch();

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("saved Gum project"))), Times.Once);
    }

    [Fact]
    public void Launch_WhenProjectHasNoFullFileName_ReportsError()
    {
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(new GumProjectSave());

        CreateLauncher().Launch();

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("saved Gum project"))), Times.Once);
    }

    [Fact]
    public void Launch_WhenNoElementIsSelected_ReportsError()
    {
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(new GumProjectSave { FullFileName = "/MyGame/GumProject.gumx" });
        _selectedState.SetupGet(s => s.SelectedElement).Returns((ElementSave?)null);

        CreateLauncher().Launch();

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("Select a screen or component"))), Times.Once);
    }

    [Fact]
    public void Launch_WhenPreviewExecutableIsNotFound_ReportsError()
    {
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(new GumProjectSave { FullFileName = "/MyGame/GumProject.gumx" });
        _selectedState.SetupGet(s => s.SelectedElement).Returns(new ScreenSave { Name = "MainMenu" });

        CreateLauncher().Launch();

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("Preview executable"))), Times.Once);
    }

    [Fact]
    public void Launch_WithJsonProject_WhenPreviewExecutableIsNotFound_ReportsError()
    {
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(new GumProjectSave { FullFileName = "/MyGame/GumProject.gumj" });
        _selectedState.SetupGet(s => s.SelectedElement).Returns(new ScreenSave { Name = "MainMenu" });

        CreateLauncher().Launch();

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("Preview executable"))), Times.Once);
    }

    [Fact]
    public void Launch_WithGumxProject_WhenNativeAotBuildIsPresent_ConvertsProjectToJsonFirst()
    {
        // issue #4748: the Native AOT build can't load .gumx directly, so a .gumx project must be
        // converted to a temporary JSON copy before launch. The fake exe below isn't a real
        // executable, so Process.Start fails and is reported as an error afterward - that's fine,
        // this test only cares that the conversion happened first.
        string aotPreviewFolder = Path.Combine(_headBaseDirectory, "Preview-Aot");
        Directory.CreateDirectory(aotPreviewFolder);
        File.WriteAllText(Path.Combine(aotPreviewFolder, ExeName), "");
        GumProjectSave project = new GumProjectSave { FullFileName = "/MyGame/GumProject.gumx" };
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(project);
        _selectedState.SetupGet(s => s.SelectedElement).Returns(new ScreenSave { Name = "MainMenu" });
        _previewGumxProjectionService
            .Setup(s => s.Project(project))
            .Returns(new PreviewGumxProjection("/Temp/abc/GumProject.gumj", "/MyGame/"));

        CreateLauncher().Launch();

        _previewGumxProjectionService.Verify(s => s.Project(project), Times.Once);
    }

    [Fact]
    public void Launch_WithJsonProject_WhenNativeAotBuildIsPresent_DoesNotConvert()
    {
        string aotPreviewFolder = Path.Combine(_headBaseDirectory, "Preview-Aot");
        Directory.CreateDirectory(aotPreviewFolder);
        File.WriteAllText(Path.Combine(aotPreviewFolder, ExeName), "");
        GumProjectSave project = new GumProjectSave { FullFileName = "/MyGame/GumProject.gumj" };
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(project);
        _selectedState.SetupGet(s => s.SelectedElement).Returns(new ScreenSave { Name = "MainMenu" });

        CreateLauncher().Launch();

        _previewGumxProjectionService.Verify(s => s.Project(It.IsAny<GumProjectSave>()), Times.Never);
    }

    [Fact]
    public void RefreshIfRunning_WhenNoPreviewIsRunning_DoesNotConvert()
    {
        CreateLauncher().RefreshIfRunning();

        _previewGumxProjectionService.Verify(s => s.Project(It.IsAny<GumProjectSave>()), Times.Never);
    }

    [Fact]
    public void PushSelection_WhenNoPreviewIsRunning_DoesNothing()
    {
        CreateLauncher().PushSelection(new ScreenSave { Name = "MainMenu" });

        _outputManager.VerifyNoOtherCalls();
    }
}
