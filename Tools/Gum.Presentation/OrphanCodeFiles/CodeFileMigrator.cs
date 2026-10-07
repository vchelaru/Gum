using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace OrphanCodeFilePlugin;

/// <summary>
/// The Content menu's <b>Migrate Code Files</b> and <b>Restore Last Code File Migration</b>
/// commands: shows the plan for a finished orphan scan, applies it on confirmation, regenerates
/// the affected elements, and undoes the newest migration on request (#5846).
/// </summary>
public class CodeFileMigrator
{
    private const string MigrateTitle = "Migrate Code Files";
    private const string RestoreTitle = "Restore Last Code File Migration";
    private const string RestoreMenuPath = "Content > Restore Last Code File Migration";

    private readonly ICodeFileMigrationPlanner _planner;
    private readonly CodeFileMigrationPlanFormatter _formatter;
    private readonly ICodeFileMigrationApplier _applier;
    private readonly CodeFileBackupService _backupService;
    private readonly IElementCodeRegenerator _regenerator;
    private readonly IDialogService _dialogService;

    public CodeFileMigrator(ICodeFileMigrationPlanner planner, CodeFileMigrationPlanFormatter formatter,
        ICodeFileMigrationApplier applier, CodeFileBackupService backupService, IElementCodeRegenerator regenerator,
        IDialogService dialogService)
    {
        _planner = planner;
        _formatter = formatter;
        _applier = applier;
        _backupService = backupService;
        _regenerator = regenerator;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Plans <paramref name="scan"/>'s orphans, shows the plan, and applies it if the user confirms.
    /// <paramref name="changeDescription"/> is set when a Code tab settings edit raised this; the
    /// prompt then names the edit, and shows nothing at all when no file needs to move.
    /// </summary>
    public void Migrate(GumProjectSave project, FilePath projectFile, CodeOutputProjectSettings projectSettings,
        OrphanCodeFileScanResult scan, string? changeDescription = null)
    {
        CodeFileMigrationPlan plan = _planner.CreatePlan(project, projectSettings, scan.Orphans);
        List<CodeFileMigrationStep> changing = plan.Steps.Where(step => !IsSkip(step.Action)).ToList();

        // With no code root the scan finds no code files, so the plan is empty and no path is formatted.
        string message = _formatter.Format(plan, scan.CodeRoot ?? string.Empty, changeDescription);
        if (scan.IsTruncated)
        {
            message += "\n" + OrphanCodeFileScanService.GetTruncatedMessage(scan.CodeRoot);
        }

        ///////////////////Early Out///////////////////
        if (changing.Count == 0)
        {
            // A settings edit raises this on every change, so one that moved nothing says nothing.
            if (changeDescription == null)
            {
                _dialogService.ShowMessage(message, MigrateTitle);
            }
            return;
        }

        MessageDialogStyle confirm = new MessageDialogStyle { AffirmativeText = "Migrate", NegativeText = "Cancel" };
        if (_dialogService.ShowMessage(message, MigrateTitle, confirm) != MessageDialogResult.Affirmative)
        {
            return;
        }
        /////////////////End Early Out/////////////////

        CodeFileMigrationResult result = _applier.Apply(projectFile, projectSettings, plan);
        if (result.Error != null)
        {
            _dialogService.ShowMessage("The migration stopped: " + result.Error, MigrateTitle);
            return;
        }

        // Old generated files are gone, so each affected element needs its file at the current path.
        foreach (string elementName in changing.Select(step => step.ElementName).Distinct())
        {
            _regenerator.Regenerate(elementName, projectSettings);
        }

        _dialogService.ShowMessage(
            $"Migrated {changing.Count} file(s) and regenerated the affected elements' code.\n\n" +
            $"A copy of every file it changed is in:\n{result.Backup?.Folder}\n\n" +
            $"To undo it, use {RestoreMenuPath}.",
            MigrateTitle);
    }

    /// <summary>After a confirmation, undoes the project's newest migration and reports files left as they were.</summary>
    public void RestoreLast(FilePath projectFile)
    {
        CodeFileBackup? backup = _backupService.List(projectFile).FirstOrDefault();

        ///////////////////Early Out///////////////////
        if (backup == null)
        {
            _dialogService.ShowMessage("This project has no code file migration to restore.", RestoreTitle);
            return;
        }

        string question = $"Put back the {backup.Entries.Count} code file(s) the last migration changed, and remove the " +
            $"{backup.CreatedFiles.Count} file(s) it wrote?\n\nAny file edited since the migration is left as it is.";
        if (_dialogService.ShowMessage(question, RestoreTitle, MessageDialogStyle.YesNo) != MessageDialogResult.Affirmative)
        {
            return;
        }
        /////////////////End Early Out/////////////////

        CodeFileRestoreResult result = _backupService.Restore(backup);

        StringBuilder message = new StringBuilder($"Restored {result.Restored.Count} file(s).");
        if (result.SkippedBecauseChanged.Count > 0)
        {
            message.Append("\n\nThese files changed after the migration, so they were left as they are:\n");
            foreach (FilePath skipped in result.SkippedBecauseChanged)
            {
                message.Append("  ").Append(skipped.FullPath).Append('\n');
            }
        }
        _dialogService.ShowMessage(message.ToString(), RestoreTitle);
    }

    private static bool IsSkip(CodeFileMigrationAction action) =>
        action == CodeFileMigrationAction.SkipConflict || action == CodeFileMigrationAction.SkipNoElement;
}
