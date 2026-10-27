using System;
using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

/// <summary>
/// Holds the set of file paths whose next change(s) should be ignored by
/// the file watcher. Extracted from FileWatchManager so that components
/// which only need to mute the watcher (e.g. FileCommands when saving an
/// element) can depend on this small surface without introducing a DI
/// cycle through FileWatchManager. Lives in Gum.ProjectServices (rather
/// than Gum.Presentation, its original home) so lower-layer services like
/// ConvertProjectToJsonService can also depend on it without an upward
/// project reference.
/// </summary>
public interface IFileWatchIgnoreList
{
    IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore { get; }

    void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null);

    /// <summary>
    /// Records the bytes the tool just finished writing to <paramref name="filePath"/>. Call it after the
    /// write that followed <see cref="IgnoreNextChangeUntil"/>. From then on, a change event inside the
    /// ignore window is ignored only while the file still holds those bytes, so an outside edit shortly
    /// after the tool's save still reloads. Without a record the window ignores by time alone.
    /// </summary>
    void RecordWrittenContent(FilePath filePath);
    bool TryGetIgnoreFileChange(FilePath fileName);
}
