using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices;

/// <summary>
/// Remembers which folders and files exist before a creation run so a failed or canceled run can delete exactly
/// what it created. A file the run overwrote stays as written. Creation writes through shared code (project saves,
/// template extraction, theme copies), so the journal records the names in the folders the
/// <see cref="NewProjectLayout"/> says the run writes into instead of listening to each write.
/// </summary>
internal sealed class NewProjectJournal
{
    private static readonly StringComparer NameComparer =
        OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    private static readonly EnumerationOptions SkipLinks = new EnumerationOptions
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint
    };

    private readonly List<FolderSnapshot> _snapshots = new List<FolderSnapshot>();
    private string? _topMissingFolder;

    private NewProjectJournal()
    {
    }

    /// <summary>Records the starting state for a run that writes into <paramref name="rootDirectory"/>.</summary>
    /// <param name="rootDirectory">The folder the run creates the project in; it and its missing parents may not exist yet.</param>
    /// <param name="gumFolder">The folder the Gum project file goes in: the root, or a folder under it.</param>
    public static NewProjectJournal Begin(string rootDirectory, string gumFolder)
    {
        NewProjectJournal journal = new NewProjectJournal();
        try
        {
            journal.Record(rootDirectory, gumFolder);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best effort: a folder that cannot be read has nothing the run can be undone to.
        }

        return journal;
    }

    private void Record(string rootDirectory, string gumFolder)
    {
        if (!Directory.Exists(rootDirectory))
        {
            DirectoryInfo? directory = new DirectoryInfo(rootDirectory);
            while (directory != null && !directory.Exists)
            {
                _topMissingFolder = directory.FullName;
                directory = directory.Parent;
            }

            return;
        }

        // The root and every folder down to the Gum project's folder: only their own files and the names of their
        // folders matter, because everything else in them is not written to.
        List<string> chain = new List<string> { rootDirectory };
        string relative = Path.GetRelativePath(rootDirectory, gumFolder);
        string current = rootDirectory;
        if (relative != ".")
        {
            foreach (string part in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                current = Path.Combine(current, part);
                chain.Add(current);
            }
        }

        foreach (string folder in chain.Where(Directory.Exists))
        {
            Snapshot(folder);
        }

        if (!Directory.Exists(gumFolder))
        {
            return;
        }

        // The folders the templates fill are walked all the way down.
        foreach (string name in NewProjectLayout.GumFolders.Concat(NewProjectLayout.OptionalGumFolders))
        {
            string folder = Path.Combine(gumFolder, name);
            if (Directory.Exists(folder))
            {
                SnapshotTree(folder);
            }
        }
    }

    private void SnapshotTree(string folder)
    {
        Snapshot(folder);
        foreach (string child in Directory.EnumerateDirectories(folder, "*", SkipLinks))
        {
            SnapshotTree(child);
        }
    }

    private void Snapshot(string folder)
    {
        FolderSnapshot snapshot = new FolderSnapshot(folder, NameComparer);
        foreach (string directory in Directory.EnumerateDirectories(folder, "*", SkipLinks))
        {
            snapshot.Folders.Add(Path.GetFileName(directory));
        }

        foreach (string file in Directory.EnumerateFiles(folder))
        {
            snapshot.Files.Add(Path.GetFileName(file));
        }

        _snapshots.Add(snapshot);
    }

    /// <summary>Deletes the folders and files created since <see cref="Begin"/>. Best effort: what cannot be deleted is left.</summary>
    public void Undo()
    {
        if (_topMissingFolder != null)
        {
            TryRun(() =>
            {
                if (Directory.Exists(_topMissingFolder))
                {
                    Directory.Delete(_topMissingFolder, recursive: true);
                }
            });
            return;
        }

        foreach (FolderSnapshot snapshot in _snapshots)
        {
            TryRun(() => Undo(snapshot));
        }
    }

    private static void Undo(FolderSnapshot snapshot)
    {
        if (!Directory.Exists(snapshot.Path))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(snapshot.Path))
        {
            if (!snapshot.Files.Contains(Path.GetFileName(file)))
            {
                TryRun(() => File.Delete(file));
            }
        }

        foreach (string directory in Directory.EnumerateDirectories(snapshot.Path, "*", SkipLinks))
        {
            if (!snapshot.Folders.Contains(Path.GetFileName(directory)))
            {
                TryRun(() => Directory.Delete(directory, recursive: true));
            }
        }
    }

    private static void TryRun(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Cleanup is best effort; the creation error is what the caller needs to see.
        }
    }

    private class FolderSnapshot
    {
        public FolderSnapshot(string path, StringComparer comparer)
        {
            Path = path;
            Folders = new HashSet<string>(comparer);
            Files = new HashSet<string>(comparer);
        }

        public string Path { get; }

        /// <summary>The names of the folders directly inside.</summary>
        public HashSet<string> Folders { get; }

        /// <summary>The names of the files directly inside.</summary>
        public HashSet<string> Files { get; }
    }
}
