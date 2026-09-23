using System.Collections.Generic;
using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Moves a file to the OS recycle bin/trash instead of permanently deleting it. Each head supplies
/// its own platform implementation, keeping that dependency out of headless callers (e.g. FileCommands).
/// </summary>
public interface IRecycleBinService
{
    /// <summary>
    /// Moves the file at <paramref name="filePath"/> to the OS recycle bin/trash.
    /// </summary>
    void MoveToRecycleBin(FilePath filePath);

    /// <summary>
    /// Moves every file in <paramref name="filePaths"/> to the OS recycle bin/trash, batching them
    /// into as few OS calls as the platform allows.
    /// </summary>
    void MoveToRecycleBin(IReadOnlyList<FilePath> filePaths);
}
