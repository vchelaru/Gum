using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.Services.Fonts;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using ToolsUtilities;

namespace GumToolUnitTests.Fonts;

public class ToolFontGenerationCallbacksTests
{
    private readonly AutoMocker _mocker;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileWatchIgnoreList> _fileWatchIgnoreList;
    private readonly Mock<ISpinner> _spinner;
    private readonly ToolFontGenerationCallbacks _callbacks;

    public ToolFontGenerationCallbacksTests()
    {
        _mocker = new AutoMocker();

        _spinner = new Mock<ISpinner>();
        _guiCommands = _mocker.GetMock<IGuiCommands>();
        _guiCommands.Setup(g => g.ShowSpinner()).Returns(_spinner.Object);
        _fileWatchIgnoreList = _mocker.GetMock<IFileWatchIgnoreList>();

        _callbacks = _mocker.CreateInstance<ToolFontGenerationCallbacks>();
    }

    [Fact]
    public void OnFontProgress_DoesNothing_WhenSpinnerNotShown()
    {
        _callbacks.OnFontProgress(0, 5);

        _spinner.Verify(s => s.SetTotal(It.IsAny<int>()), Times.Never);
        _spinner.Verify(s => s.IncrementProgress(), Times.Never);
    }

    [Fact]
    public void OnFontProgress_IncrementsProgress_WhenCompletedIsNonZero()
    {
        _callbacks.ShowSpinner();

        _callbacks.OnFontProgress(2, 5);

        _spinner.Verify(s => s.IncrementProgress(), Times.Once);
        _spinner.Verify(s => s.SetTotal(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void OnFontProgress_SetsTotal_WhenCompletedIsZero()
    {
        _callbacks.ShowSpinner();

        _callbacks.OnFontProgress(0, 5);

        _spinner.Verify(s => s.SetTotal(5), Times.Once);
        _spinner.Verify(s => s.IncrementProgress(), Times.Never);
    }

    [Fact]
    public void OnIgnoreFileChange_RoutesToFileWatchIgnoreList()
    {
        FilePath filePath = new FilePath("C:/temp/MyFont.fnt");

        _callbacks.OnIgnoreFileChange(filePath);

        _fileWatchIgnoreList.Verify(f => f.IgnoreNextChangeUntil(filePath, null), Times.Once);
    }

    [Fact]
    public void OnOutput_RoutesToGuiCommandsPrintOutput()
    {
        _callbacks.OnOutput("generating fonts");

        _guiCommands.Verify(g => g.PrintOutput("generating fonts"), Times.Once);
    }

    [Fact]
    public void ShowSpinner_Dispose_HidesSpinnerAndStopsFurtherProgress()
    {
        IDisposable? handle = _callbacks.ShowSpinner();
        handle.ShouldNotBeNull();
        _callbacks.OnFontProgress(0, 5);

        handle!.Dispose();
        _callbacks.OnFontProgress(0, 5);

        _spinner.Verify(s => s.Hide(), Times.Once);
        // SetTotal ran once (before dispose); the post-dispose call is a no-op because the
        // handle cleared the spinner reference.
        _spinner.Verify(s => s.SetTotal(5), Times.Once);
    }
}
