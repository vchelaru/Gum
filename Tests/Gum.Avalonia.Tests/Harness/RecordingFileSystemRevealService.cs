using Gum.Services;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// The test container's <see cref="IFileSystemRevealService"/>: records what the tool asked the file
/// manager or browser to show instead of starting a process, so a test never opens a window on the
/// user's desktop.
/// </summary>
internal sealed class RecordingFileSystemRevealService : IFileSystemRevealService
{
    private readonly List<string> _requests = new List<string>();

    /// <summary>Each request in order, as "Reveal: path", "OpenFolder: path", "OpenFile: path" or "OpenUrl: url".</summary>
    public IReadOnlyList<string> Requests => _requests;

    /// <inheritdoc/>
    public void RevealFile(string filePath) => _requests.Add("Reveal: " + filePath);

    /// <inheritdoc/>
    public void OpenFolder(string folderPath) => _requests.Add("OpenFolder: " + folderPath);

    /// <inheritdoc/>
    public void OpenFile(string filePath) => _requests.Add("OpenFile: " + filePath);

    /// <inheritdoc/>
    public void OpenUrl(string url) => _requests.Add("OpenUrl: " + url);

    /// <summary>Forgets the recorded requests.</summary>
    public void Clear() => _requests.Clear();
}
