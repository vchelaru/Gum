using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Moq;
using OrphanCodeFilePlugin;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for <see cref="CodeFileMigrator"/>, the Content menu's Migrate Code Files and Restore Last
/// Code File Migration commands (issue #5846).
/// </summary>
public class CodeFileMigratorTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _codeRoot;
    private readonly FilePath _projectFile;
    private readonly GumProjectSave _project = new GumProjectSave();
    private readonly CodeOutputProjectSettings _settings = new CodeOutputProjectSettings();
    private readonly Mock<ICodeFileMigrationPlanner> _planner = new();
    private readonly Mock<ICodeFileMigrationApplier> _applier = new();
    private readonly Mock<IElementCodeRegenerator> _regenerator = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly CodeFileBackupService _backupService;

    public CodeFileMigratorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumMigratorTests_" + Guid.NewGuid().ToString("N"));
        _codeRoot = Path.Combine(_tempDirectory, "Game") + Path.DirectorySeparatorChar;
        _projectFile = new FilePath(Path.Combine(_tempDirectory, "Game", "Game.gumx"));
        _backupService = new CodeFileBackupService(Path.Combine(_tempDirectory, "Backups"), () => DateTime.UtcNow);
    }

    [Fact]
    public void Migrate_ShowsThePlan_ThenAppliesItAndRegenerates_WhenConfirmed()
    {
        OrphanCodeFileScanResult scan = GivenScanPlannedAs(new CodeFileMigrationStep(
            "Controls/Button", CodeFileMigrationAction.RemoveGenerated, new FilePath(_codeRoot + "ButtonRuntime.Generated.cs")),
            isTruncated: true);
        CodeFileBackup backup = new CodeFileBackup(Path.Combine(_tempDirectory, "Backups", "1"), _projectFile,
            Array.Empty<CodeFileBackupEntry>(), Array.Empty<CodeFileCreatedEntry>());
        _applier.Setup(a => a.Apply(_projectFile, _settings, It.IsAny<CodeFileMigrationPlan>()))
            .Returns(new CodeFileMigrationResult(backup, null));
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), "Migrate Code Files", It.Is<MessageDialogStyle>(s => s != null && s.AffirmativeText == "Migrate")))
            .Returns(MessageDialogResult.Affirmative);

        CreateMigrator().Migrate(_project, _projectFile, _settings, scan);

        // Paths relative to the code root, and a scan that stopped early says so before anything changes.
        _dialogService.Verify(d => d.ShowMessage(It.Is<string>(text => text.Contains("  ButtonRuntime.Generated.cs")
                && text.Contains(OrphanCodeFileScanService.GetTruncatedMessage(_codeRoot))),
            "Migrate Code Files", It.IsAny<MessageDialogStyle>()));
        _regenerator.Verify(r => r.Regenerate("Controls/Button", _settings));
        _dialogService.Verify(d => d.ShowMessage(
            It.Is<string>(text => text.Contains(backup.Folder) && text.Contains("Content > Restore Last Code File Migration")),
            "Migrate Code Files", null));
    }

    [Fact]
    public void Migrate_ChangesNothing_WhenCancelled()
    {
        OrphanCodeFileScanResult scan = GivenScanPlannedAs(new CodeFileMigrationStep(
            "Controls/Button", CodeFileMigrationAction.RemoveGenerated, new FilePath(_codeRoot + "ButtonRuntime.Generated.cs")));
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle>()))
            .Returns(MessageDialogResult.Negative);

        CreateMigrator().Migrate(_project, _projectFile, _settings, scan);

        _applier.Verify(a => a.Apply(It.IsAny<FilePath>(), It.IsAny<CodeOutputProjectSettings>(), It.IsAny<CodeFileMigrationPlan>()), Times.Never);
        _regenerator.Verify(r => r.Regenerate(It.IsAny<string>(), It.IsAny<CodeOutputProjectSettings>()), Times.Never);
    }

    [Fact]
    public void Migrate_ReportsTheError_AndRegeneratesNothing_WhenApplyStops()
    {
        OrphanCodeFileScanResult scan = GivenScanPlannedAs(new CodeFileMigrationStep(
            "Controls/Button", CodeFileMigrationAction.RemoveGenerated, new FilePath(_codeRoot + "ButtonRuntime.Generated.cs")));
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), "Migrate Code Files", It.Is<MessageDialogStyle>(s => s != null)))
            .Returns(MessageDialogResult.Affirmative);
        _applier.Setup(a => a.Apply(_projectFile, _settings, It.IsAny<CodeFileMigrationPlan>()))
            .Returns(new CodeFileMigrationResult(null, "The file is locked."));

        CreateMigrator().Migrate(_project, _projectFile, _settings, scan);

        _dialogService.Verify(d => d.ShowMessage("The migration stopped: The file is locked.", "Migrate Code Files", null));
        _regenerator.Verify(r => r.Regenerate(It.IsAny<string>(), It.IsAny<CodeOutputProjectSettings>()), Times.Never);
    }

    [Fact]
    public void RestoreLast_RestoresTheNewestBackup_AndListsFilesChangedSince()
    {
        Directory.CreateDirectory(_codeRoot);
        string removed = _codeRoot + "ButtonRuntime.Generated.cs";
        string edited = _codeRoot + "IconRuntime.cs";
        File.WriteAllText(removed, "generated");
        File.WriteAllText(edited, "original");
        _backupService.Create(_projectFile, new[] { new FilePath(removed), new FilePath(edited) });
        File.Delete(removed);
        File.WriteAllText(edited, "edited since");
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), "Restore Last Code File Migration", It.Is<MessageDialogStyle>(s => s != null)))
            .Returns(MessageDialogResult.Affirmative);

        CreateMigrator().RestoreLast(_projectFile);

        File.ReadAllText(removed).ShouldBe("generated");
        File.ReadAllText(edited).ShouldBe("edited since");
        _dialogService.Verify(d => d.ShowMessage(
            It.Is<string>(text => text.Contains("Restored 1 file(s).") && text.Contains(new FilePath(edited).FullPath)),
            "Restore Last Code File Migration", null));
    }

    [Fact]
    public void RestoreLast_SaysSo_WhenTheProjectHasNoBackups()
    {
        CreateMigrator().RestoreLast(_projectFile);

        _dialogService.Verify(d => d.ShowMessage("This project has no code file migration to restore.",
            "Restore Last Code File Migration", null));
    }

    private OrphanCodeFileScanResult GivenScanPlannedAs(CodeFileMigrationStep step, bool isTruncated = false)
    {
        OrphanCodeFileScanResult scan = new OrphanCodeFileScanResult(
            new[] { new OrphanCodeFile(step.Source, OrphanCodeFileKind.Generated, step.ElementName) }, isTruncated, _codeRoot);
        _planner.Setup(p => p.CreatePlan(_project, _settings, scan.Orphans)).Returns(new CodeFileMigrationPlan(new[] { step }));
        return scan;
    }

    private CodeFileMigrator CreateMigrator() => new CodeFileMigrator(
        _planner.Object, new CodeFileMigrationPlanFormatter(), _applier.Object, _backupService, _regenerator.Object, _dialogService.Object);

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
