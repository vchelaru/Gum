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

/// <summary>What the tool tells the user when the Preview process exits after it was started.</summary>
public class PreviewLauncherExitReportingTests : IDisposable
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IOutputManager> _outputManager = new();
    private readonly Mock<IPreviewGumxProjectionService> _projection = new();
    private readonly ExitableStarter _starter = new ExitableStarter();

    public void Dispose()
    {
        string? file = _starter.StartedWith?.ArgumentList.SkipWhile(a => a != "--selection-file").Skip(1).FirstOrDefault();
        if (file != null)
        {
            File.Delete(file);
        }
    }

    private PreviewLauncher LaunchedLauncher()
    {
        _projectManager.SetupGet(p => p.GumProjectSave).Returns(new GumProjectSave { FullFileName = "/MyGame/GumProject.gumx" });
        _selectedState.SetupGet(s => s.SelectedElement).Returns(new ScreenSave { Name = "MainMenu" });
        PreviewLauncher launcher = new PreviewLauncher(_selectedState.Object, _projectManager.Object, _outputManager.Object, _projection.Object, _starter, () => false) { PostToUiThread = action => action() };
        launcher.Launch();
        return launcher;
    }

    [Fact]
    public void Exit_WithNonZeroCodeAndStderr_ReportsTheMessageAndCodeOnce()
    {
        LaunchedLauncher();

        _starter.Process.RaiseExit(1, "GumPreview: could not load /x.gumx: missing");

        _outputManager.Verify(
            o => o.AddError(It.Is<string>(m => m.Contains("could not load /x.gumx: missing") && m.Contains("1"))),
            Times.Once);
    }

    [Fact]
    public void Exit_WithZeroCode_ReportsNothing()
    {
        LaunchedLauncher();

        _starter.Process.RaiseExit(0, "");

        _outputManager.Verify(o => o.AddError(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Exit_AfterTheToolClosedThePreview_ReportsNothing()
    {
        PreviewLauncher launcher = LaunchedLauncher();

        launcher.Close();
        _starter.Process.RaiseExit(1, "killed");

        _outputManager.Verify(o => o.AddError(It.IsAny<string>()), Times.Never);
        _starter.Process.HasExitSubscribers.ShouldBeFalse();
    }

    [Fact]
    public void Exit_OfAnOldPreviewAfterRelaunch_ReportsOnlyOnce()
    {
        PreviewLauncher launcher = LaunchedLauncher();
        FakeProcess first = _starter.Process;
        first.HasExited = true;
        launcher.Launch();

        first.RaiseExit(2, "boom");
        first.RaiseExit(2, "boom");

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("boom"))), Times.Once);
    }

    [Fact]
    public void Launch_WhenStartThrowsUnauthorizedAccess_ReportsAndDeletesTheSelectionFile()
    {
        _starter.StartThrows = new UnauthorizedAccessException("Access is denied");

        LaunchedLauncher();

        _outputManager.Verify(o => o.AddError(It.Is<string>(m => m.Contains("Could not launch") && m.Contains("Access is denied"))), Times.Once);
        string file = _starter.StartedWith!.ArgumentList.SkipWhile(a => a != "--selection-file").Skip(1).First();
        File.Exists(file).ShouldBeFalse();
    }

    private sealed class FakeProcess : IPreviewProcess
    {
        private Action<PreviewProcessExit>? _exited;

        public bool HasExited { get; set; }

        public bool HasExitSubscribers => _exited != null;

        public event Action<PreviewProcessExit>? Exited
        {
            add => _exited += value;
            remove => _exited -= value;
        }

        public void Close() => HasExited = true;

        public void RaiseExit(int code, string error)
        {
            HasExited = true;
            _exited?.Invoke(new PreviewProcessExit(code, error));
        }
    }

    private sealed class ExitableStarter : IPreviewProcessStarter
    {
        public FakeProcess Process { get; private set; } = new FakeProcess();

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
            Process = new FakeProcess();
            return Process;
        }
    }
}
