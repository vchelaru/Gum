using System.IO;

namespace Gum.Dialogs;

/// <summary>
/// The one place that turns a path into what the New Project dialog shows: only the OS directory separator,
/// never a mix of backslashes and slashes.
/// </summary>
public static class NewProjectDisplayPath
{
    /// <summary>Replaces both separators with <see cref="Path.DirectorySeparatorChar"/>.</summary>
    public static string Normalize(string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    /// <summary>The same with a trailing separator, for a folder.</summary>
    public static string AsFolder(string path)
    {
        string normalized = Normalize(path);
        return normalized.EndsWith(Path.DirectorySeparatorChar) ? normalized : normalized + Path.DirectorySeparatorChar;
    }
}
