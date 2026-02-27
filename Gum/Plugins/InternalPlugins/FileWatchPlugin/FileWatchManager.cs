using Gum.Commands;
using Gum.Managers;
using Gum.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

public interface IFileWatchManager
{
    IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore { get; }
    IEnumerable<FilePath> ChangedFilesWaitingForFlush { get; }
    bool Enabled { get; }
    IEnumerable<FilePath> CurrentFilePathsWatching { get; }
    TimeSpan TimeToNextFlush { get; }
    bool PrintFileChangesToOutput { get; set; }

    void EnableWithDirectories(HashSet<FilePath> directories);
    void Disable();
    void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null);
    void ClearIgnoredFiles();
    void Flush();
}

public class FileWatchManager : IFileWatchManager
{
    #region Fields/Properties

    ConcurrentDictionary<FilePath, int> changesToIgnore = new();
    ConcurrentDictionary<FilePath, DateTime> timedChangesToIgnore = new();

    public IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore => timedChangesToIgnore;

    /// <summary>
    /// These are the files that are waiting to be flushed by passing the
    /// change to the Gum main system and plugins. If a file is ignored (either
    /// directly or by time) then it should not appear here.
    /// </summary>
    ConcurrentDictionary<FilePath, byte> _changedFilesWaitingForFlush = new();
    public IEnumerable<FilePath> ChangedFilesWaitingForFlush => _changedFilesWaitingForFlush.Keys;

    List<FileSystemWatcher> fileSystemWatchers = new();
    public bool Enabled =>
        fileSystemWatchers.FirstOrDefault()?.EnableRaisingEvents == true;

    DateTime LastFileChange;

    bool IsFlushing;
    private readonly IGuiCommands _guiCommands;

    public bool PrintFileChangesToOutput { get; set; }

    public IEnumerable<FilePath> CurrentFilePathsWatching
    {
        get
        {
            foreach(var item in fileSystemWatchers)
            {
                yield return item.Path;
            }
        }
    }

    #endregion

    public FileWatchManager(IGuiCommands guiCommands)
    {
        _guiCommands = guiCommands;
    }

    public void EnableWithDirectories(HashSet<FilePath> directories)
    {
        var gumProject = Locator.GetRequiredService<IProjectManager>().GumProjectSave;
        if(gumProject == null)
        {
            return;
        }
        FilePath gumProjectFilePath = gumProject.FullFileName;

        foreach(var item in this.fileSystemWatchers)
        {
            item.EnableRaisingEvents = false;
        }
        fileSystemWatchers.Clear();

        foreach(var item in directories)
        {
            var filePathAsString = item.StandardizedCaseSensitive;

            var fileWatcher = CreateFileSystemWatcher();

            // Gum standard is to have a trailing slash,
            // but FileSystemWatcher expects no trailing slash:
            var pathToAssign = filePathAsString.Substring(0, filePathAsString.Length - 1);
            try
            {
                fileWatcher.Path = pathToAssign;
                fileWatcher.EnableRaisingEvents = true;
                fileSystemWatchers.Add(fileWatcher);
            }
            catch (Exception e)
            {
                _guiCommands.PrintOutput($"Error trying to watch {filePathAsString}:\n{e.Message}");
            }
        }
    }

    FileSystemWatcher CreateFileSystemWatcher()
    {
        var fileSystemWatcher = new FileSystemWatcher();
        fileSystemWatcher.Filter = "*.*";
        fileSystemWatcher.IncludeSubdirectories = true;
        fileSystemWatcher.NotifyFilter =
            NotifyFilters.LastWrite |
            NotifyFilters.DirectoryName
            // This causes 2 events to fire for changes on files like screens
            // ... but it's needed for file names on PNG
            | NotifyFilters.FileName;

        fileSystemWatcher.Deleted += new FileSystemEventHandler(HandleFileSystemDelete);
        fileSystemWatcher.Changed += new FileSystemEventHandler(HandleFileSystemChange);
        // Gum files get deleted and then created, rather than changed
        fileSystemWatcher.Created += HandleFileSystemChange;
        fileSystemWatcher.Renamed += HandleRename;

        return fileSystemWatcher;
    }

    public void Disable()
    {
        foreach (var item in this.fileSystemWatchers)
        {
            item.EnableRaisingEvents = false;
        }
        fileSystemWatchers.Clear();
    }

    private void HandleRename(object? sender, RenamedEventArgs e)
    {
        var fileName = new FilePath(e.FullPath);
        // for now only do texture files like PNG:
        // Update November 3, 2025 - Open Office renames
        // so allow CSV too:
        var extension = fileName.Extension;
        if(extension == "png" || extension == "csv")
        {
            HandleFileSystemChange(fileName);
        }
    }

    private void HandleFileSystemDelete(object? sender, FileSystemEventArgs e)
    {
        // do anything?
    }

    private void HandleFileSystemChange(object? sender, FileSystemEventArgs e)
    {
        var fileName = new FilePath(e.FullPath);
        var extension = fileName.Extension;

        var isGum = extension is "gumx" or "gusx" or "gutx" or "gucx" or "ganx" or "behx";

        // for some reason if we include created here, we'll get double-adds for XML files like screens...
        if (e.ChangeType != WatcherChangeTypes.Created || !isGum)
        {
            HandleFileSystemChange(fileName);
        }
    }

    private void HandleFileSystemChange(FilePath fileName)
    {
        bool wasIgnored = TryGetIgnoreFileChange(fileName);
        string? skipReason = wasIgnored ? "on ignore list" : null;

        if(!wasIgnored)
        {
            var directoryContainingThis = fileName.GetDirectoryContainingThis();
            var isFolderConsidered =
                CurrentFilePathsWatching.Any(item =>
                    item == directoryContainingThis ||
                    item.IsRootOf(fileName));

            if(!isFolderConsidered)
            {
                wasIgnored = true;
                skipReason = "directory not watched";
            }
        }

        if (wasIgnored)
        {
            if (PrintFileChangesToOutput)
            {
                _guiCommands.PrintOutput($"File change skipped ({skipReason}): {fileName}");
            }
        }
        else
        {
            _changedFilesWaitingForFlush[fileName] = 0;
            LastFileChange = DateTime.Now;
        }
    }

    bool TryGetIgnoreFileChange(FilePath fileName)
    {
        if (changesToIgnore.TryGetValue(fileName, out int timesToIgnore))
        {
            changesToIgnore[fileName] = Math.Max(0, timesToIgnore - 1);
            if (timesToIgnore > 0)
            {
                return true;
            }
        }
        if (timedChangesToIgnore.TryGetValue(fileName, out DateTime timeToIgnoreUntil))
        {
            if (timeToIgnoreUntil > DateTime.Now)
            {
                return true;
            }
        }

        return false;
    }

    public void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null)
    {
        time = time ?? DateTime.Now.AddSeconds(5);
        timedChangesToIgnore.AddOrUpdate(
            filePath,
            time.Value,
            (key, existing) => time.Value > existing ? time.Value : existing);
    }

    public void ClearIgnoredFiles()
    {
        changesToIgnore.Clear();
    }

    public TimeSpan TimeToNextFlush => (LastFileChange + TimeSpan.FromSeconds(2)) - DateTime.Now;

    /// <summary>
    /// Attempts to processes all queued file changes
    /// </summary>
    public void Flush()
    {
        // early out
        if(IsFlushing || TimeToNextFlush.TotalSeconds > 0)
        {
            return;
        }
        // endif

        IsFlushing = true;

        var filesToProcess = _changedFilesWaitingForFlush.Keys.ToList();
        foreach (var file in filesToProcess)
        {
            _changedFilesWaitingForFlush.TryRemove(file, out _);
        }

        foreach (var file in filesToProcess)
        {
            if (PrintFileChangesToOutput)
            {
                var stopwatch = Stopwatch.StartNew();
                FileChangeReactionLogic.Self.ReactToFileChanged(file);
                stopwatch.Stop();
                _guiCommands.PrintOutput($"File change processed: {file} (took {stopwatch.ElapsedMilliseconds}ms)");
            }
            else
            {
                FileChangeReactionLogic.Self.ReactToFileChanged(file);
            }
        }

        IsFlushing = false;
    }

}
