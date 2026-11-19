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
    void RecordWrittenContent(FilePath filePath);

    /// <summary>
    /// Call when an edit may have changed which files the project's elements reference, so the next
    /// rename or delete of a file is judged against the project as it is now.
    /// </summary>
    void InvalidateReferencedFiles();

    /// <summary>
    /// True when a change to <paramref name="file"/> can change what the project reports or shows: a Gum,
    /// localization, animation or font-range file, or a file the project's elements reference. False for
    /// anything else in a watched folder, such as the tool's own event export.
    /// </summary>
    bool IsRelevantToProject(FilePath file);
    void Flush();
}
