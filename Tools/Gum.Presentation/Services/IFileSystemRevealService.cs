namespace Gum.Services;

/// <summary>
/// Opens files, folders, and URLs in the operating system's own applications, and reveals a file
/// in its file manager. Every "open in explorer" or "view docs" action in the tool goes through
/// this so the per-OS commands live in one place (Windows: explorer, macOS: open, Linux: xdg-open).
/// See <see cref="FileSystemRevealService"/> for the implementation.
/// </summary>
public interface IFileSystemRevealService
{
    /// <summary>Opens the file manager with <paramref name="filePath"/> selected, where the OS supports that.</summary>
    void RevealFile(string filePath);

    /// <summary>Opens <paramref name="folderPath"/> in the file manager.</summary>
    void OpenFolder(string folderPath);

    /// <summary>Opens <paramref name="filePath"/> with its default application.</summary>
    void OpenFile(string filePath);

    /// <summary>Opens <paramref name="url"/> in the default browser.</summary>
    void OpenUrl(string url);
}
