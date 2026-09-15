using System;
using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.Logic.FileWatch;

/// <summary>
/// No-op <see cref="IFileWatchIgnoreList"/> for headless callers (gumcli, headless project
/// creation) with no running file watcher to mute. Every member is a no-op.
/// </summary>
public class NullFileWatchIgnoreList : IFileWatchIgnoreList
{
    /// <inheritdoc/>
    public IReadOnlyDictionary<FilePath, DateTime> TimedChangesToIgnore { get; } =
        new Dictionary<FilePath, DateTime>();

    /// <inheritdoc/>
    public void IgnoreNextChangeUntil(FilePath filePath, DateTime? time = null) { }

    /// <inheritdoc/>
    public void ClearIgnoredFiles() { }

    /// <inheritdoc/>
    public bool TryGetIgnoreFileChange(FilePath fileName) => false;
}
