using CodeOutputPlugin.Manager;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace OrphanCodeFilePlugin;

/// <summary>What <see cref="CodeFileMigrationApplier.Apply"/> did.</summary>
public class CodeFileMigrationResult
{
    /// <summary>The backup holding every file the migration touched; null when there was nothing to do or the backup itself failed.</summary>
    public CodeFileBackup? Backup { get; }

    /// <summary>Why the migration stopped, or null when it finished. A stopped migration has been undone.</summary>
    public string? Error { get; }

    public CodeFileMigrationResult(CodeFileBackup? backup, string? error)
    {
        Backup = backup;
        Error = error;
    }
}

/// <summary>Carries out a <see cref="CodeFileMigrationPlan"/>; see <see cref="CodeFileMigrationApplier"/>.</summary>
public interface ICodeFileMigrationApplier
{
    /// <summary>Applies <paramref name="plan"/>, keeping its backup with <paramref name="projectFile"/>'s backups.</summary>
    CodeFileMigrationResult Apply(FilePath projectFile, CodeOutputProjectSettings projectSettings, CodeFileMigrationPlan plan);
}

/// <summary>
/// Carries out a <see cref="CodeFileMigrationPlan"/>: backs up every file it will touch, removes old
/// generated files, sends untouched custom stubs to the recycle bin, and moves custom code with real
/// code to its element's current path with the namespace and class name rewritten. If any step
/// fails, the backup is restored, so the project is never left half-migrated. Skip steps are not touched.
/// </summary>
public class CodeFileMigrationApplier : ICodeFileMigrationApplier
{
    private readonly CodeFileBackupService _backupService;
    private readonly IFileCommands _fileCommands;
    private readonly CustomCodeHeaderRewriter _headerRewriter;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;

    public CodeFileMigrationApplier(CodeFileBackupService backupService, IFileCommands fileCommands,
        CustomCodeHeaderRewriter headerRewriter, CodeOutputElementSettingsManager elementSettingsManager)
    {
        _backupService = backupService;
        _fileCommands = fileCommands;
        _headerRewriter = headerRewriter;
        _elementSettingsManager = elementSettingsManager;
    }

    /// <inheritdoc/>
    public CodeFileMigrationResult Apply(FilePath projectFile, CodeOutputProjectSettings projectSettings, CodeFileMigrationPlan plan)
    {
        List<CodeFileMigrationStep> removeGenerated = StepsFor(plan, CodeFileMigrationAction.RemoveGenerated);
        List<CodeFileMigrationStep> removeStubs = StepsFor(plan, CodeFileMigrationAction.RemoveUntouchedStub);
        List<CodeFileMigrationStep> moves = StepsFor(plan, CodeFileMigrationAction.MoveCustomCode);
        // A move lands on top of an untouched stub codegen already wrote there; it is replaced.
        List<FilePath> replacedStubs = moves
            .Select(move => move.Destination!)
            .Where(destination => File.Exists(destination.FullPath))
            .ToList();

        List<FilePath> toBackUp = removeGenerated.Concat(removeStubs).Concat(moves)
            .Select(step => step.Source)
            .Concat(replacedStubs)
            .ToList();

        ///////////////////Early Out///////////////////
        if (toBackUp.Count == 0)
        {
            return new CodeFileMigrationResult(null, null);
        }
        /////////////////End Early Out/////////////////

        CodeFileBackup backup;
        try
        {
            backup = _backupService.Create(projectFile, toBackUp);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            // Nothing was changed yet, so there is nothing to undo.
            return new CodeFileMigrationResult(null, "Could not back up the files, so nothing was changed: " + exception.Message);
        }

        try
        {
            foreach (CodeFileMigrationStep step in removeGenerated)
            {
                // Derived data, rebuilt from the element, and backed up besides.
                File.Delete(step.Source.FullPath);
            }

            List<FilePath> toRecycle = removeStubs.Select(step => step.Source).Concat(replacedStubs).ToList();
            if (toRecycle.Count > 0)
            {
                // Custom code files never take a plain delete, even untouched ones.
                _fileCommands.MoveToRecycleBin(toRecycle);
            }

            foreach (CodeFileMigrationStep move in moves)
            {
                Move(move, projectSettings, backup);
            }
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            _backupService.Restore(backup);
            return new CodeFileMigrationResult(backup, exception.Message + " Every file was put back as it was.");
        }

        RemoveEmptiedFolders(projectFile, projectSettings, toBackUp.Where(source => !File.Exists(source.FullPath)));
        return new CodeFileMigrationResult(backup, null);
    }

    // Moving code to a new location (a prefix, say) leaves the old Screens/Components folders behind
    // empty. Only folders under the code project root, and only empty ones, are removed.
    private static void RemoveEmptiedFolders(FilePath projectFile, CodeOutputProjectSettings projectSettings, IEnumerable<FilePath> removedFiles)
    {
        string? projectDirectory = Path.GetDirectoryName(projectFile.FullPath);
        if (projectDirectory == null || string.IsNullOrEmpty(projectSettings.CodeProjectRoot))
        {
            return;
        }

        string codeRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(projectDirectory, projectSettings.CodeProjectRoot)));
        foreach (FilePath removed in removedFiles)
        {
            string? folder = Path.GetDirectoryName(removed.FullPath);
            while (folder != null && IsUnder(folder, codeRoot) && Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
            {
                try
                {
                    Directory.Delete(folder);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    // A leftover empty folder is harmless; the migration itself succeeded.
                    break;
                }
                folder = Path.GetDirectoryName(folder);
            }
        }
    }

    private static bool IsUnder(string folder, string root) =>
        folder.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private void Move(CodeFileMigrationStep move, CodeOutputProjectSettings projectSettings, CodeFileBackup backup)
    {
        FilePath destination = move.Destination!;
        string contents = File.ReadAllText(move.Source.FullPath);

        ElementSave? element = ObjectFinder.Self.GetElementSave(move.ElementName);
        if (element != null)
        {
            contents = _headerRewriter.Rewrite(contents, element, _elementSettingsManager.LoadOrCreateSettingsFor(element), projectSettings);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination.FullPath)!);
        File.WriteAllText(destination.FullPath, contents);
        // Recorded before the source goes, so a failure from here on still restores cleanly.
        _backupService.AddCreatedFile(backup, destination);
        File.Delete(move.Source.FullPath);
    }

    private static List<CodeFileMigrationStep> StepsFor(CodeFileMigrationPlan plan, CodeFileMigrationAction action) =>
        plan.Steps.Where(step => step.Action == action).ToList();
}
