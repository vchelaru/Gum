using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

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
