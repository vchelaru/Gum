using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Moq;
using OrphanCodeFilePlugin;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for <see cref="OrphanCodeFileReporter"/> — the headless half of the tool's orphaned code
/// file reporting: turning scan results into Errors tab entries and resolving them (issue #4422).
/// </summary>
public class OrphanCodeFileReporterTests : BaseTestClass
{
    private readonly Mock<IOrphanCodeFileScanService> _scanService;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IDialogService> _dialogService;
    private readonly OrphanCodeFileReporter _sut;

    public OrphanCodeFileReporterTests()
    {
        _scanService = new Mock<IOrphanCodeFileScanService>();
        _fileCommands = new Mock<IFileCommands>();
        _dialogService = new Mock<IDialogService>();
        _sut = new OrphanCodeFileReporter(_scanService.Object, _fileCommands.Object, _dialogService.Object);
    }

    [Fact]
    public void CreateErrors_ShouldIncludeFilePathAndActionPerOrphan()
    {
        OrphanCodeFile orphan = new OrphanCodeFile(
            new FilePath("/game/Screens/DeletedScreen.Generated.cs"), OrphanCodeFileKind.Generated, "DeletedScreen");
        ArrangeScan(orphan);
        _sut.Refresh(new GumProjectSave(), new CodeOutputProjectSettings());

        List<ErrorViewModel> errors = _sut.CreateErrors().ToList();

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("DeletedScreen.Generated.cs");
        errors[0].Code.ShouldBe("GUM0005");
        errors[0].HasAction.ShouldBeTrue();
    }

    [Fact]
    public void OrphansChanged_ShouldRaise_OnRefreshAndOnResolve()
    {
        FilePath filePath = new FilePath("/game/Screens/DeletedScreen.Generated.cs");
        OrphanCodeFile orphan = new OrphanCodeFile(filePath, OrphanCodeFileKind.Generated, "DeletedScreen");
        ArrangeScan(orphan);
        int raiseCount = 0;
        _sut.OrphansChanged += () => raiseCount++;

        _sut.Refresh(new GumProjectSave(), new CodeOutputProjectSettings());
        _sut.Resolve(orphan);

        raiseCount.ShouldBe(2);
    }

    [Fact]
    public void Refresh_ShouldClearOrphans_WhenProjectIsNull()
    {
        ArrangeScan(new OrphanCodeFile(
            new FilePath("/game/Screens/DeletedScreen.Generated.cs"), OrphanCodeFileKind.Generated, "DeletedScreen"));
        _sut.Refresh(new GumProjectSave(), new CodeOutputProjectSettings());

        _sut.Refresh(project: null, new CodeOutputProjectSettings());

        _sut.Orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_ShouldMoveGeneratedFileToRecycleBin_WithoutPrompting()
    {
        FilePath filePath = new FilePath("/game/Screens/DeletedScreen.Generated.cs");
        OrphanCodeFile orphan = new OrphanCodeFile(filePath, OrphanCodeFileKind.Generated, "DeletedScreen");
        ArrangeScan(orphan);
        _sut.Refresh(new GumProjectSave(), new CodeOutputProjectSettings());

        _sut.Resolve(orphan);

        _fileCommands.Verify(x => x.MoveToRecycleBin(filePath), Times.Once);
        _dialogService.Verify(
            x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle?>()), Times.Never);
        _sut.Orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_ShouldNotRemoveCustomCodeFile_WhenUserDeclines()
    {
        FilePath filePath = new FilePath("/game/Screens/DeletedScreen.cs");
        OrphanCodeFile orphan = new OrphanCodeFile(filePath, OrphanCodeFileKind.CustomCode, "DeletedScreen");
        ArrangeScan(orphan);
        _sut.Refresh(new GumProjectSave(), new CodeOutputProjectSettings());
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Negative);

        _sut.Resolve(orphan);

        _fileCommands.Verify(x => x.MoveToRecycleBin(It.IsAny<FilePath>()), Times.Never);
        _sut.Orphans.Count.ShouldBe(1);
    }

    [Fact]
    public void Resolve_ShouldPromptBeforeRemovingCustomCodeFile()
    {
        FilePath filePath = new FilePath("/game/Screens/DeletedScreen.cs");
        OrphanCodeFile orphan = new OrphanCodeFile(filePath, OrphanCodeFileKind.CustomCode, "DeletedScreen");
        ArrangeScan(orphan);
        _sut.Refresh(new GumProjectSave(), new CodeOutputProjectSettings());
        _dialogService
            .Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Affirmative);

        _sut.Resolve(orphan);

        _fileCommands.Verify(x => x.MoveToRecycleBin(filePath), Times.Once);
        _sut.Orphans.ShouldBeEmpty();
    }

    private void ArrangeScan(params OrphanCodeFile[] orphans) =>
        _scanService
            .Setup(x => x.Scan(It.IsAny<GumProjectSave>(), It.IsAny<CodeOutputProjectSettings>()))
            .Returns(orphans);
}
