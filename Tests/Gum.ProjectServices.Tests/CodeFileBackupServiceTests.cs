using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="CodeFileBackupService"/>, the undo for a code file migration: copies of the
/// files a migration touches, kept outside the repo, and a restore that never overwrites a file
/// changed since (issue #5846).
/// </summary>
public class CodeFileBackupServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _backupRoot;
    private readonly FilePath _projectFile;
    private DateTime _now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    public CodeFileBackupServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumBackupTests_" + Guid.NewGuid().ToString("N"));
        _backupRoot = Path.Combine(_tempDirectory, "AppData", "CodeFileBackups");
        _projectFile = new FilePath(Path.Combine(_tempDirectory, "Game", "Content", "GumProject", "Game.gumx"));
    }

    [Fact]
    public void Restore_PutsRemovedFilesBack_WithTheirContentAndFolders()
    {
        FilePath generated = WriteFile("Game/Components/Controls/ButtonRuntime.Generated.cs", "//Code for Controls/Button");
        FilePath custom = WriteFile("Game/Components/Controls/ButtonRuntime.cs", "partial class ButtonRuntime { int x; }");
        CodeFileBackupService service = CreateService();

        CodeFileBackup backup = service.Create(_projectFile, new[] { generated, custom });
        Directory.Delete(Path.Combine(_tempDirectory, "Game", "Components"), recursive: true);
        CodeFileRestoreResult result = service.Restore(backup);

        File.ReadAllText(generated.FullPath).ShouldBe("//Code for Controls/Button");
        File.ReadAllText(custom.FullPath).ShouldBe("partial class ButtonRuntime { int x; }");
        result.Restored.ShouldBe(new[] { generated, custom });
        result.SkippedBecauseChanged.ShouldBeEmpty();
    }

    [Fact]
    public void Restore_LeavesAFileChangedSinceTheBackup_AndReportsIt()
    {
        FilePath custom = WriteFile("Game/Components/ButtonRuntime.cs", "original");
        FilePath unchanged = WriteFile("Game/Components/IconRuntime.cs", "same");
        CodeFileBackupService service = CreateService();

        CodeFileBackup backup = service.Create(_projectFile, new[] { custom, unchanged });
        File.WriteAllText(custom.FullPath, "edited after the migration");
        CodeFileRestoreResult result = service.Restore(backup);

        File.ReadAllText(custom.FullPath).ShouldBe("edited after the migration");
        result.SkippedBecauseChanged.ShouldBe(new[] { custom });
        // A file still holding its backed-up content needs nothing done and is not a conflict.
        result.Restored.ShouldBe(new[] { unchanged });
    }

    [Fact]
    public void Restore_RemovesFilesTheMigrationCreated_UnlessEditedSince()
    {
        // A move: the old file is backed up, the new copy is recorded as created.
        FilePath oldPath = WriteFile("Game/ButtonRuntime.cs", "partial class ButtonRuntime { int x; }");
        CodeFileBackupService service = CreateService();
        CodeFileBackup backup = service.Create(_projectFile, new[] { oldPath });
        FilePath moved = WriteFile("Game/Button.cs", "partial class Button { int x; }");
        File.Delete(oldPath.FullPath);
        service.AddCreatedFile(backup, moved);
        FilePath edited = WriteFile("Game/Icon.cs", "written by the migration");
        service.AddCreatedFile(backup, edited);
        File.WriteAllText(edited.FullPath, "edited after the migration");

        // Read back from disk, as Restore Last does after Gum restarts.
        CodeFileRestoreResult result = service.Restore(service.List(_projectFile).Single());

        File.Exists(moved.FullPath).ShouldBeFalse();
        File.ReadAllText(oldPath.FullPath).ShouldBe("partial class ButtonRuntime { int x; }");
        File.ReadAllText(edited.FullPath).ShouldBe("edited after the migration");
        result.SkippedBecauseChanged.ShouldBe(new[] { edited });
    }

    [Fact]
    public void Create_KeepsOnlyTheTenNewestBackups_PerProject()
    {
        FilePath file = WriteFile("Game/A.cs", "a");
        FilePath otherProject = new FilePath(Path.Combine(_tempDirectory, "Other", "Other.gumx"));
        CodeFileBackupService service = CreateService();
        CodeFileBackup otherBackup = service.Create(otherProject, new[] { file });

        List<CodeFileBackup> created = new List<CodeFileBackup>();
        for (int i = 0; i < 11; i++)
        {
            _now = _now.AddMinutes(1);
            created.Add(service.Create(_projectFile, new[] { file }));
        }

        IReadOnlyList<CodeFileBackup> kept = service.List(_projectFile);
        kept.Select(backup => backup.Folder).ShouldBe(created.Skip(1).Reverse().Select(backup => backup.Folder));
        Directory.Exists(created[0].Folder).ShouldBeFalse();
        service.List(otherProject).Single().Folder.ShouldBe(otherBackup.Folder);
    }

    [Fact]
    public void Create_GivesTwoBackupsMadeAtTheSameMoment_SeparateFolders()
    {
        FilePath file = WriteFile("Game/A.cs", "a");
        CodeFileBackupService service = CreateService();

        CodeFileBackup first = service.Create(_projectFile, new[] { file });
        CodeFileBackup second = service.Create(_projectFile, new[] { file });

        second.Folder.ShouldNotBe(first.Folder);
        service.List(_projectFile).Count.ShouldBe(2);
    }

    private CodeFileBackupService CreateService() => new CodeFileBackupService(_backupRoot, () => _now);

    private FilePath WriteFile(string relativePath, string contents)
    {
        string fullPath = Path.Combine(_tempDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
        return new FilePath(fullPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
