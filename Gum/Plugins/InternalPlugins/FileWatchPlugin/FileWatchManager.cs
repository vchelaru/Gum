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

    public IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore => _ignoreList.TimedChangesToIgnore;

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
    private readonly IProjectManager _projectManager;
    private readonly FileChangeReactionLogic _fileChangeReactionLogic;
    private readonly IFileWatchIgnoreList _ignoreList;

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

    public FileWatchManager(
        IGuiCommands guiCommands,
        IProjectManager projectManager,
        FileChangeReactionLogic fileChangeReactionLogic,
        IFileWatchIgnoreList ignoreList)
    {
        _guiCommands = guiCommands;
        _projectManager = projectManager;
        _fileChangeReactionLogic = fileChangeReactionLogic;
        _ignoreList = ignoreList;
    }

    public void EnableWithDirectories(HashSet<FilePath> directories)
    {
        var gumProject = _projectManager.GumProjectSave;
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
        // Atomic-save pattern: editors (Vim, JetBrains, some VS Code modes) and
        // tools that write files programmatically often write to a temp file
        // and rename it over the target. The FileSystemWatcher then fires a
        // Renamed event rather than a Changed event, so we forward renames for
        // every extension we know how to react to.
        var extension = fileName.Extension;
        if(extension is "png" or "csv" or "resx"
            or "gumx" or "gusx" or "gutx" or "gucx" or "ganx" or "behx" or "fnt"
            or "achx" or "gif" or "tga" or "bmp")
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
        bool wasIgnored = _ignoreList.TryGetIgnoreFileChange(fileName);
        string? skipReason = wasIgnored ? "on ignore list" : null;

        if(!wasIgnored && IsTransientTempFile(fileName))
        {
            wasIgnored = true;
            skipReason = "atomic-save temp file";
        }

        // Subdirectory create/rename events surface here as well; queuing a
        // directory path would cause File.Open in Flush to throw. Folder
        // paths almost never have an extension, so gate the disk hit on
        // that — file events (which dominate by far) skip the syscall.
        if(!wasIgnored
            && string.IsNullOrEmpty(fileName.Extension)
            && Directory.Exists(fileName.Standardized))
        {
            wasIgnored = true;
            skipReason = "path is a directory";
        }

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

    public void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null)
        => _ignoreList.IgnoreNextChangeUntil(filePath, time);

    public void ClearIgnoredFiles() => _ignoreList.ClearIgnoredFiles();

    public TimeSpan TimeToNextFlush => (LastFileChange + TimeSpan.FromMilliseconds(500)) - DateTime.Now;

    private enum FileReadiness
    {
        Ready,
        Locked,
        Drop,
    }

    /// <summary>
    /// Determines whether a queued file change should be processed now, retried
    /// next flush, or dropped from the queue entirely.
    /// </summary>
    /// <remarks>
    /// Drop covers the normal atomic-save aftermath: an editor writes to a
    /// temp file and renames it over the target, so the original temp path
    /// no longer exists by the time we flush. Returning Locked there would
    /// keep the entry in the queue forever.
    /// </remarks>
    private FileReadiness GetFileReadiness(FilePath file)
    {
        var path = file.Standardized;
        if (Directory.Exists(path) || !File.Exists(path))
        {
            return FileReadiness.Drop;
        }

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return FileReadiness.Ready;
        }
        catch (IOException)
        {
            return FileReadiness.Locked;
        }
        catch (UnauthorizedAccessException)
        {
            return FileReadiness.Drop;
        }
    }

    private static bool IsTransientTempFile(FilePath file)
    {
        // Covers patterns like "Foo.gucx.tmp.17756.1777860550017" emitted by
        // tools that do atomic writes (write to .tmp, rename onto target).
        if (file.Extension == "tmp")
        {
            return true;
        }
        var fullName = System.IO.Path.GetFileName(file.Standardized);
        return fullName.Contains(".tmp.", StringComparison.OrdinalIgnoreCase);
    }

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
        try
        {
            var candidateFiles = _changedFilesWaitingForFlush.Keys.ToList();
            var filesToProcess = new List<FilePath>();
            foreach (var file in candidateFiles)
            {
                var readiness = GetFileReadiness(file);
                if (readiness == FileReadiness.Ready)
                {
                    _changedFilesWaitingForFlush.TryRemove(file, out _);
                    filesToProcess.Add(file);
                }
                else if (readiness == FileReadiness.Drop)
                {
                    // File no longer exists (e.g. atomic-save temp that was
                    // renamed away) or is otherwise unprocessable. Drop it
                    // silently — this is the normal path during agent edits
                    // and doesn't warrant user-visible noise.
                    _changedFilesWaitingForFlush.TryRemove(file, out _);
                }
                // else Locked: leave in queue and retry next cycle (e.g. git is writing it).
            }

            foreach (var file in filesToProcess)
            {
                try
                {
                    if (PrintFileChangesToOutput)
                    {
                        var stopwatch = Stopwatch.StartNew();
                        _fileChangeReactionLogic.ReactToFileChanged(file);
                        stopwatch.Stop();
                        _guiCommands.PrintOutput($"File change processed: {file} (took {stopwatch.ElapsedMilliseconds}ms)");
                    }
                    else
                    {
                        _fileChangeReactionLogic.ReactToFileChanged(file);
                    }
                }
                catch (Exception ex)
                {
                    // One bad reload must not poison the rest of the queue.
                    // Only surface this when the user has opted in to file-change
                    // diagnostics; otherwise it would just be noise.
                    if (PrintFileChangesToOutput)
                    {
                        _guiCommands.PrintOutput($"Error reacting to file change for {file}: {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            IsFlushing = false;
        }
    }

}
