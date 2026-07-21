using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Moves a file to the OS recycle bin/trash instead of permanently deleting it. The only
/// implementation requires the Windows desktop runtime's recycle-bin UI APIs, so this interface
/// exists to keep that platform dependency out of headless callers (e.g. FileCommands).
/// </summary>
public interface IRecycleBinService
{
    /// <summary>
    /// Moves the file at <paramref name="filePath"/> to the OS recycle bin/trash.
    /// </summary>
    void MoveToRecycleBin(FilePath filePath);
}
