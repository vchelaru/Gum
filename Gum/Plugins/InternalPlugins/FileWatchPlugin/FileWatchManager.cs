using Gum.Commands;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

public interface IFileWatchManager
{
    IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore { get; }
    HashSet<FilePath> ChangedFilesWaitingForFlush { get; }
    bool Enabled { get; }
    IEnumerable<FilePath> CurrentFilePathsWatching { get; }
    TimeSpan TimeToNextFlush { get; }

    void EnableWithDirectories(HashSet<FilePath> directories);
    void Disable();
    void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null);
    void ClearIgnoredFiles();
    void Flush();
}

public class FileWatchManager : IFileWatchManager
{
    #region Fields/Properties

    Dictionary<FilePath, int> changesToIgnore = new ();
    Dictionary<FilePath, DateTime> timedChangesToIgnore = new ();

    public IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore => timedChangesToIgnore;

    /// <summary>
    /// These are the files that are waiting to be flushed by passing the
    /// change to the Gum main system and plugins. If a file is ignored (either
    /// directly or by time) then it should not appear here.
    /// </summary>
    public HashSet<FilePath> ChangedFilesWaitingForFlush { get; private set; } = new ();
    List<FilePath> filesCurrentlyFlushing = new List<FilePath>();

    List<FileSystemWatcher> fileSystemWatchers = new ();
    public bool Enabled =>
        fileSystemWatchers.FirstOrDefault()?.EnableRaisingEvents == true;

    DateTime LastFileChange;

    object LockObject=new object();

    bool IsFlushing;
    private readonly IGuiCommands _guiCommands;

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
        var gumProject = ProjectManager.Self.GumProjectSave;
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
        lock (LockObject)
        {
            bool wasIgnored = TryGetIgnoreFileChange(fileName);

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
                }
            }

            if (!wasIgnored)
            {
                // shuffle so that changes move to the end:
                if(ChangedFilesWaitingForFlush.Contains(fileName))
                {
                    ChangedFilesWaitingForFlush.Remove(fileName);
                }
                ChangedFilesWaitingForFlush.Add(fileName);

                LastFileChange = DateTime.Now;
            }
        }
    }

    bool TryGetIgnoreFileChange(FilePath fileName)
    {


        if (changesToIgnore.ContainsKey(fileName))
        {
            int timesToIgnore = 0;
            timesToIgnore = changesToIgnore[fileName];

            changesToIgnore[fileName] = System.Math.Max(0, timesToIgnore - 1);
            if( timesToIgnore > 0)
            {
                return true;
            }
        }
        if(timedChangesToIgnore.ContainsKey(fileName))
        {
            DateTime timeToIgnoreUntil = timedChangesToIgnore[fileName];
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
        lock (LockObject)
        {
            if (timedChangesToIgnore.ContainsKey(filePath))
            {
                if (time > timedChangesToIgnore[filePath])
                {
                    timedChangesToIgnore[filePath] = time.Value;
                }
            }
            else
            {
                timedChangesToIgnore.Add(filePath, time.Value);
            }
        }
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
        lock (LockObject)
        {
            IsFlushing = true;

            filesCurrentlyFlushing.AddRange(ChangedFilesWaitingForFlush);
            ChangedFilesWaitingForFlush.Clear();

            foreach (var file in filesCurrentlyFlushing)
            {
                FileChangeReactionLogic.Self.ReactToFileChanged(file);
            }
            filesCurrentlyFlushing.Clear();
            IsFlushing = false;
        }
    }

}
