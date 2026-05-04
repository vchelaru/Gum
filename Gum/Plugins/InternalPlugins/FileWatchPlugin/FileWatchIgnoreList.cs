using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

/// <summary>
/// Holds the set of file paths whose next change(s) should be ignored by
/// the file watcher. Extracted from FileWatchManager so that components
/// which only need to mute the watcher (e.g. FileCommands when saving an
/// element) can depend on this small surface without introducing a DI
/// cycle through FileWatchManager.
/// </summary>
public interface IFileWatchIgnoreList
{
    IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore { get; }

    void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null);
    void ClearIgnoredFiles();
    bool TryGetIgnoreFileChange(FilePath fileName);
}

public class FileWatchIgnoreList : IFileWatchIgnoreList
{
    private readonly ConcurrentDictionary<FilePath, int> _changesToIgnore = new();
    private readonly ConcurrentDictionary<FilePath, DateTime> _timedChangesToIgnore = new();

    public IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore => _timedChangesToIgnore;

    public void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null)
    {
        time = time ?? DateTime.Now.AddSeconds(5);
        _timedChangesToIgnore.AddOrUpdate(
            filePath,
            time.Value,
            (key, existing) => time.Value > existing ? time.Value : existing);
    }

    public void ClearIgnoredFiles()
    {
        _changesToIgnore.Clear();
    }

    public bool TryGetIgnoreFileChange(FilePath fileName)
    {
        if (_changesToIgnore.TryGetValue(fileName, out int timesToIgnore))
        {
            _changesToIgnore[fileName] = Math.Max(0, timesToIgnore - 1);
            if (timesToIgnore > 0)
            {
                return true;
            }
        }
        if (_timedChangesToIgnore.TryGetValue(fileName, out DateTime timeToIgnoreUntil))
        {
            if (timeToIgnoreUntil > DateTime.Now)
            {
                return true;
            }
        }
        return false;
    }
}
