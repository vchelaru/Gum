using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.ToolStates;
using Moq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// What the tool tells the user, and leaves behind, when the Preview process does not start.
/// </summary>
public class PreviewLauncherStartFailureTests : IDisposable
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IOutputManager> _outputManager = new();
    private readonly Mock<IPreviewGumxProjectionService> _projection = new();
    private readonly FailingStarter _starter = new FailingStarter();
    private string? _selectionFile;

    public void Dispose()
    {
        if (_selectionFile != null)
        {
            File.Delete(_selectionFile);
        }
    }

    private PreviewLauncher CreateLauncher()
    {
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(new GumProjectSave { FullFileName = "/MyGame/GumProject.gumx" });
        _selectedState.SetupGet(s => s.SelectedElement).Returns(new ScreenSave { Name = "MainMenu" });
        return new PreviewLauncher(_selectedState.Object, _projectManager.Object, _outputManager.Object, _projection.Object, _starter, () => false);
    }

    private void LaunchAndRememberSelectionFile(PreviewLauncher launcher)
    {
        try
        {
            launcher.Launch();
        }
        finally
        {
            _selectionFile = _starter.StartedWith?.ArgumentList.SkipWhile(a => a != "--selection-file").Skip(1).FirstOrDefault();
        }
    }

    [Fact]
    public void Launch_WhenTheProcessCouldNotBeStarted_ReportsAnErrorInsteadOfLaunched()
    {
        _starter.StartReturnsNull = true;

        LaunchAndRememberSelectionFile(CreateLauncher());

        _outputManager.Verify(o => o.AddOutput(It.Is<string>(m => m.Contains("Launched"))), Times.Never);
        _outputManager.Verify(o => o.AddError(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Launch_WhenStartingThrows_LeavesNoSelectionFileBehind()
    {
        _starter.StartThrows = new System.ComponentModel.Win32Exception("The system cannot find the file specified");

        LaunchAndRememberSelectionFile(CreateLauncher());

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("Could not launch"))), Times.Once);
        _selectionFile.ShouldNotBeNull();
        File.Exists(_selectionFile).ShouldBeFalse();
    }

    private sealed class FailingStarter : IPreviewProcessStarter
    {
        public bool StartReturnsNull { get; set; }

        public Exception? StartThrows { get; set; }

        public ProcessStartInfo? StartedWith { get; private set; }

        public ResolvedPreviewExecutable? Resolve() => new ResolvedPreviewExecutable("GumPreview.exe", IsNativeAot: false);

        public IPreviewProcess? Start(ProcessStartInfo startInfo)
        {
            StartedWith = startInfo;
            if (StartThrows != null)
            {
                throw StartThrows;
            }
            return StartReturnsNull ? null : throw new InvalidOperationException("unused");
        }
    }
}
