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
    /// <summary>The backup's own folder under the backup root.</summary>
    public string Folder { get; }

    /// <summary>The files backed up, by original path.</summary>
    public IReadOnlyList<CodeFileBackupEntry> Entries { get; }

    public CodeFileBackup(string folder, IReadOnlyList<CodeFileBackupEntry> entries)
    {
        Folder = folder;
        Entries = entries;
    }
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

/// <summary>What a <see cref="CodeFileBackupService.Restore"/> did.</summary>
public class CodeFileRestoreResult
{
    /// <summary>Files now holding their backed-up content, including ones that already did.</summary>
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

        Manifest manifest = new Manifest
        {
            ProjectFile = projectFile.FullPath,
            Entries = entries.Select(entry => new ManifestEntry
            {
                OriginalPath = entry.OriginalPath.FullPath,
                BackupFileName = entry.BackupFileName,
                Sha256 = entry.Sha256,
            }).ToList(),
        };
        File.WriteAllText(Path.Combine(folder, ManifestFileName), JsonConvert.SerializeObject(manifest, Formatting.Indented));

        foreach (CodeFileBackup old in List(projectFile).Skip(MaxBackupsPerProject))
        {
            Directory.Delete(old.Folder, recursive: true);
        }

        return new CodeFileBackup(folder, entries);
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
    /// Copies each backed-up file back to its original path. A file that exists there with
    /// different content was changed after the backup, so it is left as it is and reported.
    /// </summary>
    public CodeFileRestoreResult Restore(CodeFileBackup backup)
    {
        List<FilePath> restored = new List<FilePath>();
        List<FilePath> skipped = new List<FilePath>();

        foreach (CodeFileBackupEntry entry in backup.Entries)
        {
            string original = entry.OriginalPath.FullPath;
            if (File.Exists(original))
            {
                if (HashFile(original) == entry.Sha256)
                {
                    restored.Add(entry.OriginalPath);
                }
                else
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
            return new CodeFileBackup(folder, entries);
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
    }

    private sealed class ManifestEntry
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string BackupFileName { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
    }
}
