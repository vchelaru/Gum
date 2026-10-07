using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>A backup made before a code file migration: its folder and what it holds.</summary>
public class CodeFileBackup
{
    private readonly List<CodeFileCreatedEntry> _createdFiles;

    /// <summary>The backup's own folder under the backup root.</summary>
    public string Folder { get; }

    /// <summary>The project the backup was made for.</summary>
    public FilePath ProjectFile { get; }

    /// <summary>The files backed up, by original path.</summary>
    public IReadOnlyList<CodeFileBackupEntry> Entries { get; }

    /// <summary>Files the migration wrote after the backup, which Restore removes.</summary>
    public IReadOnlyList<CodeFileCreatedEntry> CreatedFiles => _createdFiles;

    public CodeFileBackup(string folder, FilePath projectFile, IReadOnlyList<CodeFileBackupEntry> entries,
        IEnumerable<CodeFileCreatedEntry> createdFiles)
    {
        Folder = folder;
        ProjectFile = projectFile;
        Entries = entries;
        _createdFiles = createdFiles.ToList();
    }

    internal void AddCreatedFile(CodeFileCreatedEntry entry) => _createdFiles.Add(entry);
}

/// <summary>One backed-up file.</summary>
public class CodeFileBackupEntry
{
    /// <summary>Where the file was, and where Restore puts it back.</summary>
    public FilePath OriginalPath { get; }

    /// <summary>The copy's file name inside the backup folder.</summary>
    public string BackupFileName { get; }

    /// <summary>SHA-256 of the file's content when it was backed up.</summary>
    public string Sha256 { get; }

    public CodeFileBackupEntry(FilePath originalPath, string backupFileName, string sha256)
    {
        OriginalPath = originalPath;
        BackupFileName = backupFileName;
        Sha256 = sha256;
    }
}

/// <summary>A file the migration wrote, with the content it wrote.</summary>
public class CodeFileCreatedEntry
{
    /// <summary>Where the migration wrote it.</summary>
    public FilePath Path { get; }

    /// <summary>SHA-256 of the content the migration wrote; anything else means the user edited it since.</summary>
    public string Sha256 { get; }

    public CodeFileCreatedEntry(FilePath path, string sha256)
    {
        Path = path;
        Sha256 = sha256;
    }
}

/// <summary>What a <see cref="CodeFileBackupService.Restore"/> did.</summary>
public class CodeFileRestoreResult
{
    /// <summary>Files now back to their pre-migration state: restored, already restored, or a created file removed.</summary>
    public IReadOnlyList<FilePath> Restored { get; }

    /// <summary>Files that hold something else now. Left as they are.</summary>
    public IReadOnlyList<FilePath> SkippedBecauseChanged { get; }

    public CodeFileRestoreResult(IReadOnlyList<FilePath> restored, IReadOnlyList<FilePath> skippedBecauseChanged)
    {
        Restored = restored;
        SkippedBecauseChanged = skippedBecauseChanged;
    }
}

/// <summary>
/// Backs up the code files a migration is about to touch, outside the repo, and restores them.
/// Each backup is a folder under <c>&lt;backup root&gt;/&lt;project key&gt;/</c> holding the copies
/// and a <c>manifest.json</c>; only the newest <see cref="MaxBackupsPerProject"/> per project are kept.
/// The manifest is rewritten after every <see cref="AddCreatedFile"/>, so a migration that stops
/// partway (an error, a crash) still leaves a backup that Restore can fully undo.
/// </summary>
public class CodeFileBackupService
{
    /// <summary>How many backups a project keeps; creating one more deletes the oldest.</summary>
    public const int MaxBackupsPerProject = 10;

    private const string ManifestFileName = "manifest.json";

    private readonly string _backupRoot;
    private readonly Func<DateTime> _utcNow;

    /// <param name="backupRoot">The folder all projects' backups live under, outside any repo (the tool passes one under its app data).</param>
    /// <param name="utcNow">The clock; a backup folder is named for its creation time.</param>
    public CodeFileBackupService(string backupRoot, Func<DateTime> utcNow)
    {
        _backupRoot = backupRoot;
        _utcNow = utcNow;
    }

    /// <summary>
    /// Copies <paramref name="files"/> into a new backup for <paramref name="projectFile"/>, then
    /// deletes that project's backups beyond the newest <see cref="MaxBackupsPerProject"/>.
    /// </summary>
    public CodeFileBackup Create(FilePath projectFile, IReadOnlyList<FilePath> files)
    {
        string projectFolder = GetProjectFolder(projectFile);
        string folder = CreateUniqueFolder(projectFolder, _utcNow().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture));

        List<CodeFileBackupEntry> entries = new List<CodeFileBackupEntry>();
        for (int i = 0; i < files.Count; i++)
        {
            // Numbered, so two files with the same name from different folders can't collide.
            string backupFileName = i + Path.GetExtension(files[i].FullPath);
            File.Copy(files[i].FullPath, Path.Combine(folder, backupFileName));
            entries.Add(new CodeFileBackupEntry(files[i], backupFileName, HashFile(files[i].FullPath)));
        }

        CodeFileBackup backup = new CodeFileBackup(folder, projectFile, entries, Array.Empty<CodeFileCreatedEntry>());
        WriteManifest(backup);

        foreach (CodeFileBackup old in List(projectFile).Skip(MaxBackupsPerProject))
        {
            Directory.Delete(old.Folder, recursive: true);
        }

        return backup;
    }

    /// <summary>
    /// Records that the migration wrote <paramref name="file"/>, with its current content, so
    /// Restore removes it unless it was edited afterwards. Call it right after each write.
    /// </summary>
    public void AddCreatedFile(CodeFileBackup backup, FilePath file)
    {
        backup.AddCreatedFile(new CodeFileCreatedEntry(file, HashFile(file.FullPath)));
        WriteManifest(backup);
    }

    /// <summary><paramref name="projectFile"/>'s backups, newest first. A backup whose manifest can't be read is left out.</summary>
    public IReadOnlyList<CodeFileBackup> List(FilePath projectFile)
    {
        string projectFolder = GetProjectFolder(projectFile);
        if (!Directory.Exists(projectFolder))
        {
            return Array.Empty<CodeFileBackup>();
        }

        List<CodeFileBackup> backups = new List<CodeFileBackup>();
        foreach (string folder in Directory.GetDirectories(projectFolder).OrderByDescending(path => path, StringComparer.Ordinal))
        {
            CodeFileBackup? backup = TryReadBackup(folder);
            if (backup != null)
            {
                backups.Add(backup);
            }
        }
        return backups;
    }

    /// <summary>
    /// Undoes the migration: removes each file it created, then copies each backed-up file back to
    /// its original path. A file whose content changed after the migration (a created file edited
    /// since, or something new at an original path) is left as it is and reported. Created files go
    /// first because a move can replace a backed-up stub at the same path.
    /// </summary>
    public CodeFileRestoreResult Restore(CodeFileBackup backup)
    {
        List<FilePath> restored = new List<FilePath>();
        List<FilePath> skipped = new List<FilePath>();

        foreach (CodeFileCreatedEntry created in backup.CreatedFiles)
        {
            string path = created.Path.FullPath;
            if (!File.Exists(path))
            {
                continue;
            }

            if (HashFile(path) == created.Sha256)
            {
                // Exactly what the migration wrote; the backed-up original holds the user's code.
                File.Delete(path);
                restored.Add(created.Path);
            }
            else
            {
                skipped.Add(created.Path);
            }
        }

        foreach (CodeFileBackupEntry entry in backup.Entries)
        {
            string original = entry.OriginalPath.FullPath;
            if (File.Exists(original))
            {
                if (HashFile(original) == entry.Sha256)
                {
                    restored.Add(entry.OriginalPath);
                }
                else if (!skipped.Contains(entry.OriginalPath))
                {
                    skipped.Add(entry.OriginalPath);
                }
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(original)!);
            File.Copy(Path.Combine(backup.Folder, entry.BackupFileName), original);
            restored.Add(entry.OriginalPath);
        }

        return new CodeFileRestoreResult(restored, skipped);
    }

    // The project's name keeps the folder readable; a hash of its full path keeps two projects
    // with the same name apart.
    private string GetProjectFolder(FilePath projectFile)
    {
        string fullPath = projectFile.FullPath.ToLowerInvariant();
        string pathHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullPath))).Substring(0, 8);
        return Path.Combine(_backupRoot, projectFile.CaseSensitiveNoPathNoExtension + "-" + pathHash);
    }

    private static string CreateUniqueFolder(string parent, string name)
    {
        string folder = Path.Combine(parent, name);
        for (int suffix = 2; Directory.Exists(folder); suffix++)
        {
            folder = Path.Combine(parent, name + "-" + suffix);
        }
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void WriteManifest(CodeFileBackup backup)
    {
        Manifest manifest = new Manifest
        {
            ProjectFile = backup.ProjectFile.FullPath,
            Entries = backup.Entries.Select(entry => new ManifestEntry
            {
                OriginalPath = entry.OriginalPath.FullPath,
                BackupFileName = entry.BackupFileName,
                Sha256 = entry.Sha256,
            }).ToList(),
            CreatedFiles = backup.CreatedFiles.Select(created => new ManifestCreatedFile
            {
                Path = created.Path.FullPath,
                Sha256 = created.Sha256,
            }).ToList(),
        };
        File.WriteAllText(Path.Combine(backup.Folder, ManifestFileName), JsonConvert.SerializeObject(manifest, Formatting.Indented));
    }

    private static CodeFileBackup? TryReadBackup(string folder)
    {
        try
        {
            Manifest? manifest = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(Path.Combine(folder, ManifestFileName)));
            if (manifest?.Entries == null)
            {
                return null;
            }

            List<CodeFileBackupEntry> entries = manifest.Entries
                .Select(entry => new CodeFileBackupEntry(new FilePath(entry.OriginalPath), entry.BackupFileName, entry.Sha256))
                .ToList();
            IEnumerable<CodeFileCreatedEntry> createdFiles = (manifest.CreatedFiles ?? new List<ManifestCreatedFile>())
                .Select(created => new CodeFileCreatedEntry(new FilePath(created.Path), created.Sha256));
            return new CodeFileBackup(folder, new FilePath(manifest.ProjectFile), entries, createdFiles);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException)
        {
            return null;
        }
    }

    private static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private sealed class Manifest
    {
        public string ProjectFile { get; set; } = string.Empty;
        public List<ManifestEntry> Entries { get; set; } = new List<ManifestEntry>();
        public List<ManifestCreatedFile>? CreatedFiles { get; set; } = new List<ManifestCreatedFile>();
    }

    private sealed class ManifestEntry
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string BackupFileName { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
    }

    private sealed class ManifestCreatedFile
    {
        public string Path { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
    }
}
