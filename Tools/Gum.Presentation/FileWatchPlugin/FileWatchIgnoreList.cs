using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

public class FileWatchIgnoreList : IFileWatchIgnoreList
{
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

    public bool TryGetIgnoreFileChange(FilePath fileName) =>
        _timedChangesToIgnore.TryGetValue(fileName, out DateTime timeToIgnoreUntil)
        && timeToIgnoreUntil > DateTime.Now;
}
