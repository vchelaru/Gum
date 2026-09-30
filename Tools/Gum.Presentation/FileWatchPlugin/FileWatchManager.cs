using Gum.Commands;
using Gum.DataTypes;
using Gum.Diagnostics;
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

/// <summary>
/// Owns the tool's <see cref="FileSystemWatcher"/>s and queues external file changes for
/// <see cref="Flush"/>.
/// </summary>
/// <remarks>
/// Watchers raise events on thread-pool threads, but everything here (the watcher list, the
/// debounce timestamp, the project model that the reappearance check reads) belongs to the UI
/// thread. Each watcher callback therefore only posts its work through <see cref="IDispatcher"/>.
/// An exception from a callback is reported instead of escaping, because one escaping a
/// thread-pool thread ends the process.
/// </remarks>
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
    private readonly IDispatcher _dispatcher;
    private readonly ICrashReporter? _crashReporter;

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
        IFileWatchIgnoreList ignoreList,
        IDispatcher dispatcher,
        // Only the Avalonia head registers a crash reporter; without one, errors go to Output only.
        ICrashReporter? crashReporter = null)
    {
        _guiCommands = guiCommands;
        _projectManager = projectManager;
        _fileChangeReactionLogic = fileChangeReactionLogic;
        _ignoreList = ignoreList;
        _dispatcher = dispatcher;
        _crashReporter = crashReporter;
    }

    public void EnableWithDirectories(HashSet<FilePath> directories)
    {
        var gumProject = _projectManager.GumProjectSave;
        if(gumProject == null)
        {
            return;
        }

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
                // The directory may not exist yet (e.g. FontCache/ before the first font has been
                // generated) - create it rather than letting FileSystemWatcher throw (#4259).
                if (!Directory.Exists(pathToAssign))
                {
                    Directory.CreateDirectory(pathToAssign);
                }

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

        fileSystemWatcher.Deleted += HandleFileSystemDelete;
        fileSystemWatcher.Changed += HandleFileSystemChange;
        // Gum files get deleted and then created, rather than changed
        fileSystemWatcher.Created += HandleFileSystemChange;
        fileSystemWatcher.Renamed += HandleRename;
        fileSystemWatcher.Error += HandleWatcherError;

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

    // Watcher-thread entry points: each only hands its event to the UI thread.
    internal void HandleRename(object? sender, RenamedEventArgs e)
        => RunOnUiThread(isOnIgnoreList => ReactToRename(e, isOnIgnoreList), e.FullPath);

    internal void HandleFileSystemDelete(object? sender, FileSystemEventArgs e)
        => RunOnUiThread(isOnIgnoreList => ReactToDelete(e, isOnIgnoreList), e.FullPath);

    internal void HandleFileSystemChange(object? sender, FileSystemEventArgs e)
        => RunOnUiThread(isOnIgnoreList => ReactToChangeOrCreate(e, isOnIgnoreList), e.FullPath);

    // Raised on a watcher thread when the watcher overflows its buffer or loses a watch (e.g. the
    // Linux inotify limit), after which external changes can go unnoticed.
    internal void HandleWatcherError(object? sender, ErrorEventArgs e)
    {
        try
        {
            string path = (sender as FileSystemWatcher)?.Path ?? "a watched directory";
            _guiCommands.PrintOutput(
                $"File watching failed for {path}, so external changes there may not reload: {e.GetException().Message}");
        }
        catch
        {
            // Watcher thread: an escaping exception would end the process.
        }
    }

    private void RunOnUiThread(Action<bool> reaction, string path)
    {
        try
        {
            // The ignore window is measured from when the event fired, so read it here: checked on
            // a UI thread busy past the window, Gum's own save would reload as an external edit.
            // The ignore list is thread-safe.
            bool isOnIgnoreList = _ignoreList.TryGetIgnoreFileChange(new FilePath(path));
            _dispatcher.Post(() =>
            {
                try
                {
                    reaction(isOnIgnoreList);
                }
                catch (Exception e)
                {
                    ReportCallbackException(e, path);
                }
            });
        }
        catch (Exception e)
        {
            // The ignore check or Post itself failed (e.g. the dispatcher is shutting down). This is
            // still the watcher thread, so the exception must not propagate.
            ReportCallbackException(e, path);
        }
    }

    private void ReportCallbackException(Exception exception, string path)
    {
        try
        {
            _crashReporter?.ReportRecoverable(exception, "File watch");
            _guiCommands.PrintOutput($"Error handling file change for {path}:\n{exception}");
        }
        catch
        {
            // Reporting is the last line of defense on a watcher thread. If it fails as well there
            // is nowhere left to report to, and rethrowing would end the process.
        }
    }

    private void ReactToRename(RenamedEventArgs e, bool isOnIgnoreList)
    {
        var fileName = new FilePath(e.FullPath);
        // Atomic-save pattern: editors (Vim, JetBrains, some VS Code modes) and
        // tools that write files programmatically often write to a temp file
        // and rename it over the target. The FileSystemWatcher then fires a
        // Renamed event rather than a Changed event, so we forward renames for
        // every extension we know how to react to.
        var extension = fileName.Extension;
        if(extension is "png" or "csv" or "resx"
            or "gumx" or "gumj" or "gusx" or "gusj" or "gutx" or "gutj" or "gucx" or "gucj"
            or "ganx" or "ganj" or "behx" or "behj" or "fnt"
            or "achx" or "achj" or "gif" or "tga" or "bmp")
        {
            HandleFileSystemChange(fileName, isOnIgnoreList);
        }
    }

    private void ReactToDelete(FileSystemEventArgs e, bool isOnIgnoreList)
    {
        var fileName = new FilePath(e.FullPath);
        // Detect deletion of an element file so the tool can flag the element's source as missing
        // (red "!" / GUM0004) instead of silently diverging from disk (issue #3367). Enqueue it
        // like a change - the flush re-checks existence, so a delete-then-recreate (atomic save)
        // within the debounce window collapses back into a normal reload. Non-element deletes are
        // left unhandled, as before.
        if (IsElementFileExtension(fileName))
        {
            HandleFileSystemChange(fileName, isOnIgnoreList);
        }
    }

    // internal (not private) so tests can pin the recognized extension set directly.
    internal static bool IsElementFileExtension(FilePath file)
    {
        var extension = file.Extension;
        return extension == GumProjectSave.ScreenExtension
            || extension == GumProjectSave.ScreenJsonExtension
            || extension == GumProjectSave.ComponentExtension
            || extension == GumProjectSave.ComponentJsonExtension
            || extension == GumProjectSave.StandardExtension
            || extension == GumProjectSave.StandardJsonExtension;
    }

    private void ReactToChangeOrCreate(FileSystemEventArgs e, bool isOnIgnoreList)
    {
        var fileName = new FilePath(e.FullPath);
        var extension = fileName.Extension;

        var isGum = extension is "gumx" or "gumj" or "gusx" or "gusj" or "gutx" or "gutj"
            or "gucx" or "gucj" or "ganx" or "ganj" or "behx" or "behj";

        // for some reason if we include created here, we'll get double-adds for XML files like screens...
        if (e.ChangeType != WatcherChangeTypes.Created || !isGum)
        {
            HandleFileSystemChange(fileName, isOnIgnoreList);
        }
        // ...except when the Created file is the reappearance of an element we previously flagged
        // missing (issue #3367). A restore (e.g. Explorer's undo-delete) fires only a Created - no
        // Change - so without this the red "!" / GUM0004 would never clear. Limiting it to flagged
        // elements keeps normal saves (which Gum ignore-lists anyway) on the suppressed path.
        else if (_fileChangeReactionLogic.IsReappearanceOfMissingSourceElement(fileName))
        {
            HandleFileSystemChange(fileName, isOnIgnoreList);
        }
    }

    private void HandleFileSystemChange(FilePath fileName, bool isOnIgnoreList)
    {
        bool wasIgnored = isOnIgnoreList;
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
            && Directory.Exists(fileName.FullPath))
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
        // FullPath keeps the file's case; Standardized is lowercased and misses on a case-sensitive file system.
        var path = file.FullPath;
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
        var fullName = System.IO.Path.GetFileName(file.FullPath);
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
            var deletedElementFiles = new List<FilePath>();
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
                    _changedFilesWaitingForFlush.TryRemove(file, out _);

                    // Drop usually means an atomic-save temp that was renamed away. But an element
                    // file that's still gone at flush time is a real deletion the user should see
                    // (issue #3367) — route it to the delete reaction instead of dropping silently.
                    if (IsElementFileExtension(file) && !IsTransientTempFile(file))
                    {
                        deletedElementFiles.Add(file);
                    }
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

            foreach (var file in deletedElementFiles)
            {
                try
                {
                    _fileChangeReactionLogic.ReactToFileDeleted(file);
                    if (PrintFileChangesToOutput)
                    {
                        _guiCommands.PrintOutput($"Element file deletion processed: {file}");
                    }
                }
                catch (Exception ex)
                {
                    if (PrintFileChangesToOutput)
                    {
                        _guiCommands.PrintOutput($"Error reacting to file deletion for {file}: {ex.Message}");
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
