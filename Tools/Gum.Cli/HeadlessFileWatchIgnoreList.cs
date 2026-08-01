using System;
using System.Collections.Generic;
using Gum.Logic.FileWatch;
using ToolsUtilities;

namespace Gum.Cli;

/// <summary>
/// No-op file watch ignore list for headless/CLI use. <c>gumcli</c> has no running file watcher to
/// mute, so every member is a no-op.
/// </summary>
internal class HeadlessFileWatchIgnoreList : IFileWatchIgnoreList
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
