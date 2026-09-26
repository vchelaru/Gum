using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services;
using Gum.Services.Dialogs;
using Moq;
using OrphanCodeFilePlugin;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for <see cref="OrphanCodeFileReporter"/> — the headless half of the tool's orphaned code
/// file reporting: running the scan off the UI thread, turning results into Errors tab entries and
/// resolving them (issues #4422, #5140).
/// </summary>
public class OrphanCodeFileReporterTests : BaseTestClass
{
    private readonly Mock<IOrphanCodeFileScanService> _scanService;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IOutputManager> _outputManager;
    private readonly QueuedDispatcher _dispatcher;
    private readonly OrphanCodeFileReporter _sut;

    public OrphanCodeFileReporterTests()
    {
        _scanService = new Mock<IOrphanCodeFileScanService>();
        _fileCommands = new Mock<IFileCommands>();
        _dialogService = new Mock<IDialogService>();
        _outputManager = new Mock<IOutputManager>();
        _dispatcher = new QueuedDispatcher();
        _scanService
            .Setup(x => x.CreatePlan(It.IsAny<GumProjectSave>(), It.IsAny<CodeOutputProjectSettings>()))
            .Returns(CreateEmptyPlan);
        _sut = new OrphanCodeFileReporter(_scanService.Object, _fileCommands.Object, _dialogService.Object,
            _dispatcher, _outputManager.Object);
    }

    [Fact]
    public async Task CreateErrors_ShouldIncludeFilePathAndActionPerOrphan()
    {
        OrphanCodeFile orphan = new OrphanCodeFile(
            new FilePath("/game/Screens/DeletedScreen.Generated.cs"), OrphanCodeFileKind.Generated, "DeletedScreen");
        ArrangeScan(orphan);
        await RefreshAndApplyAsync();

        List<ErrorViewModel> errors = _sut.CreateErrors().ToList();

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("DeletedScreen.Generated.cs");
        errors[0].Code.ShouldBe("GUM0005");
        errors[0].HasAction.ShouldBeTrue();
        errors[0].ElementName.ShouldBe("DeletedScreen");
    }

    [Fact]
    public async Task OrphansChanged_ShouldRaise_OnRefreshAndOnResolve()
    {
        FilePath filePath = new FilePath("/game/Screens/DeletedScreen.Generated.cs");
        OrphanCodeFile orphan = new OrphanCodeFile(filePath, OrphanCodeFileKind.Generated, "DeletedScreen");
        ArrangeScan(orphan);
        int raiseCount = 0;
        _sut.OrphansChanged += () => raiseCount++;

        await RefreshAndApplyAsync();
        _sut.Resolve(orphan);

        raiseCount.ShouldBe(2);
    }

    [Fact]
    public async Task RefreshAsync_ShouldWalkDiskOffTheCallingThread_AndApplyResultThroughDispatcher()
    {
        // Project load runs on the UI thread; a code root resolving to a huge folder froze the tool (#5140).
        int callingThread = Environment.CurrentManagedThreadId;
        int executeThread = callingThread;
        OrphanCodeFile orphan = new OrphanCodeFile(
            new FilePath("/game/Screens/DeletedScreen.Generated.cs"), OrphanCodeFileKind.Generated, "DeletedScreen");
        _scanService
            .Setup(x => x.Execute(It.IsAny<OrphanCodeFileScanPlan>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                executeThread = Environment.CurrentManagedThreadId;
                return new OrphanCodeFileScanResult(new[] { orphan }, isTruncated: false, codeRoot: "/game/");
            });

        await _sut.RefreshAsync(new GumProjectSave(), new CodeOutputProjectSettings());

        executeThread.ShouldNotBe(callingThread);
        _sut.Orphans.ShouldBeEmpty("the result must wait for the UI thread");
        _dispatcher.RunPending();
        _sut.Orphans.ShouldBe(new[] { orphan });
    }

    [Fact]
    public async Task RefreshAsync_ShouldDiscardAnOlderScan_WhenANewerOneStarted()
    {
        OrphanCodeFile staleOrphan = new OrphanCodeFile(
            new FilePath("/old/Stale.Generated.cs"), OrphanCodeFileKind.Generated, "Stale");
        OrphanCodeFile currentOrphan = new OrphanCodeFile(
            new FilePath("/new/Current.Generated.cs"), OrphanCodeFileKind.Generated, "Current");
        ArrangeScan(staleOrphan);
        int olderAppliedCount = 0;
        await _sut.RefreshAsync(new GumProjectSave(), new CodeOutputProjectSettings(), _ => olderAppliedCount++);
        ArrangeScan(currentOrphan);

        await _sut.RefreshAsync(new GumProjectSave(), new CodeOutputProjectSettings());
        _dispatcher.RunPending();

        _sut.Orphans.ShouldBe(new[] { currentOrphan });
        olderAppliedCount.ShouldBe(0);
    }

    [Fact]
    public async Task RefreshAsync_ShouldWriteError_WhenScanThrows()
    {
        _scanService
            .Setup(x => x.Execute(It.IsAny<OrphanCodeFileScanPlan>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("disk exploded"));

        await RefreshAndApplyAsync();

        _outputManager.Verify(x => x.AddError(It.Is<string>(message => message.Contains("disk exploded"))), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldWarnInOutput_WhenScanWasTruncated()
    {
        _scanService
            .Setup(x => x.Execute(It.IsAny<OrphanCodeFileScanPlan>(), It.IsAny<CancellationToken>()))
            .Returns(new OrphanCodeFileScanResult(Array.Empty<OrphanCodeFile>(), isTruncated: true,
                codeRoot: "C:/Users/me/AppData/Local/"));

        await RefreshAndApplyAsync();

        _outputManager.Verify(x => x.AddError(It.Is<string>(message =>
            message.Contains("C:/Users/me/AppData/Local/"))), Times.Once);
    }

    [Fact]
    public async Task Refresh_ShouldClearOrphans_WhenProjectIsNull()
    {
        ArrangeScan(new OrphanCodeFile(
            new FilePath("/game/Screens/DeletedScreen.Generated.cs"), OrphanCodeFileKind.Generated, "DeletedScreen"));
        await RefreshAndApplyAsync();

        await _sut.RefreshAsync(project: null, new CodeOutputProjectSettings());
        _dispatcher.RunPending();

        _sut.Orphans.ShouldBeEmpty();
    }

    [Fact]
    public async Task Resolve_ShouldMoveGeneratedFileToRecycleBin_WithoutPrompting()
    {
        FilePath filePath = new FilePath("/game/Screens/DeletedScreen.Generated.cs");
        OrphanCodeFile orphan = new OrphanCodeFile(filePath, OrphanCodeFileKind.Generated, "DeletedScreen");
        ArrangeScan(orphan);
        await RefreshAndApplyAsync();

        _sut.Resolve(orphan);

        _fileCommands.Verify(x => x.MoveToRecycleBin(filePath), Times.Once);
        _dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle?>()), Times.Never);
        _sut.Orphans.ShouldBeEmpty();
    }

    [Fact]
    public async Task Resolve_ShouldNotRemoveCustomCodeFile_WhenUserDeclines()
    {
        FilePath filePath = new FilePath("/game/Screens/DeletedScreen.cs");
        OrphanCodeFile orphan = new OrphanCodeFile(filePath, OrphanCodeFileKind.CustomCode, "DeletedScreen");
        ArrangeScan(orphan);
        await RefreshAndApplyAsync();
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Negative);

        _sut.Resolve(orphan);

        _fileCommands.Verify(x => x.MoveToRecycleBin(It.IsAny<FilePath>()), Times.Never);
        _sut.Orphans.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Resolve_ShouldPromptBeforeRemovingCustomCodeFile()
    {
        FilePath filePath = new FilePath("/game/Screens/DeletedScreen.cs");
        OrphanCodeFile orphan = new OrphanCodeFile(filePath, OrphanCodeFileKind.CustomCode, "DeletedScreen");
        ArrangeScan(orphan);
        await RefreshAndApplyAsync();
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Affirmative);

        _sut.Resolve(orphan);

        _fileCommands.Verify(x => x.MoveToRecycleBin(filePath), Times.Once);
        _sut.Orphans.ShouldBeEmpty();
    }

    private async Task RefreshAndApplyAsync()
    {
        await _sut.RefreshAsync(new GumProjectSave(), new CodeOutputProjectSettings());
        _dispatcher.RunPending();
    }

    private void ArrangeScan(params OrphanCodeFile[] orphans) =>
        _scanService
            .Setup(x => x.Execute(It.IsAny<OrphanCodeFileScanPlan>(), It.IsAny<CancellationToken>()))
            .Returns(new OrphanCodeFileScanResult(orphans, isTruncated: false, codeRoot: "/game/"));

    private static OrphanCodeFileScanPlan CreateEmptyPlan() => new OrphanCodeFileScanPlan(
        "/game/", new HashSet<FilePath>(), new HashSet<FilePath>(), new List<string>(), new HashSet<FilePath>());

    /// <summary>Holds posted work until <see cref="RunPending"/>, standing in for the UI thread.</summary>
    private sealed class QueuedDispatcher : IDispatcher
    {
        private readonly Queue<Action> _pending = new Queue<Action>();

        public void Invoke(Action action) => action();

        public void Post(Action action)
        {
            lock (_pending)
            {
                _pending.Enqueue(action);
            }
        }

        public void RunPending()
        {
            while (true)
            {
                Action action;
                lock (_pending)
                {
                    if (_pending.Count == 0)
                    {
                        return;
                    }
                    action = _pending.Dequeue();
                }
                action();
            }
        }
    }
}
