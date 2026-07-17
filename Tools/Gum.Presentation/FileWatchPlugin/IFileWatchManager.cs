using System;
using System.Collections.Generic;
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
