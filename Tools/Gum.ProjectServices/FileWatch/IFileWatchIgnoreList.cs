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
    void ClearIgnoredFiles();
    bool TryGetIgnoreFileChange(FilePath fileName);
}
