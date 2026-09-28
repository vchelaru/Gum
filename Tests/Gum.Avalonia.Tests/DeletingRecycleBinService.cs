using Gum.Managers;
using ToolsUtilities;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Replaces the OS recycle bin in the test container: a test that deletes an element's code files
/// or a converted project's XML files deletes its temp files outright rather than filling the
/// user's recycle bin (or, on a CI runner, depending on a trash command).
/// </summary>
public class DeletingRecycleBinService : IRecycleBinService
{
    /// <inheritdoc/>
    public void MoveToRecycleBin(FilePath filePath) => File.Delete(filePath.FullPath);

    /// <inheritdoc/>
    public void MoveToRecycleBin(IReadOnlyList<FilePath> filePaths)
    {
        foreach (FilePath filePath in filePaths)
        {
            MoveToRecycleBin(filePath);
        }
    }
}
