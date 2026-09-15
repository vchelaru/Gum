using System;
using System.IO;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.EditorTab;

public class PreviewLauncherTests : IDisposable
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IOutputManager> _outputManager = new();
    private readonly string _headBaseDirectory;

    public PreviewLauncherTests()
    {
        // No Preview/ folder and no Samples/GumPreview dev build here, so every test in this class
        // exercises a validation branch that returns before touching the filesystem or a process.
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
        new PreviewLauncher(_selectedState.Object, _projectManager.Object, _outputManager.Object, _headBaseDirectory);

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
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(new GumProjectSave { FullFileName = @"C:\MyGame\GumProject.gumx" });
        _selectedState.SetupGet(s => s.SelectedElement).Returns((ElementSave?)null);

        CreateLauncher().Launch();

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("Select a screen or component"))), Times.Once);
    }

    [Fact]
    public void Launch_WhenPreviewExecutableIsNotFound_ReportsError()
    {
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(new GumProjectSave { FullFileName = @"C:\MyGame\GumProject.gumx" });
        _selectedState.SetupGet(s => s.SelectedElement).Returns(new ScreenSave { Name = "MainMenu" });

        CreateLauncher().Launch();

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("Preview executable"))), Times.Once);
    }

    [Fact]
    public void PushSelection_WhenNoPreviewIsRunning_DoesNothing()
    {
        CreateLauncher().PushSelection(new ScreenSave { Name = "MainMenu" });

        _outputManager.VerifyNoOtherCalls();
    }
}
