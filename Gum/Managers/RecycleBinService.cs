using ToolsUtilities;

namespace Gum.Managers;

/// <summary>
/// Moves files to the OS recycle bin via <see cref="Microsoft.VisualBasic.FileIO.FileSystem"/>,
/// which requires the Windows desktop runtime (<c>UseWindowsForms</c>). See
/// <see cref="IRecycleBinService"/> for why this is kept behind an interface.
/// </summary>
public class RecycleBinService : IRecycleBinService
{
    /// <inheritdoc/>
    public void MoveToRecycleBin(FilePath filePath) =>
        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filePath.FullPath,
            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
}
