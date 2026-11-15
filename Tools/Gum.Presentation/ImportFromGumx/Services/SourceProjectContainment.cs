using System;
using System.IO;

namespace ImportFromGumxPlugin.Services;

/// <summary>
/// The one owner of "is this file inside the source project" for the .gumx import. A file counts as inside
/// when its path stays under the source folder lexically (no <c>..</c> out of it) and its real path, with every
/// link (symlink or junction) in the path followed, is still under the real source folder (#7392). A link that
/// stays inside the project is fine.
/// </summary>
internal static class SourceProjectContainment
{
    private const int MaxLinkDepth = 40;

    public const string LeavesProjectReason = "leaves the source project, so it was not copied";
    public const string ThroughLinkReason = "resolves outside the source project (through a link) and was not copied";
    public const string BrokenLinkReason = "is a link whose target was not found, so it was not copied";

    /// <summary>
    /// Why <paramref name="relativePath"/> under <paramref name="sourceBase"/> must not be read, or null when it
    /// may be (including when it does not exist). Web sources are never refused.
    /// </summary>
    public static string? GetRefusal(string sourceBase, string relativePath)
    {
        if (sourceBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || sourceBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // A source saved on Windows can use backslashes, which are file-name characters on macOS/Linux.
        string normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        string root = TrimSeparators(Path.GetFullPath(sourceBase));
        string full = TrimSeparators(Path.GetFullPath(Path.Combine(root, normalized)));
        if (!IsUnder(root, full)) return LeavesProjectReason;

        string? realRoot = ResolveReal(root, 0);
        string? realFull = ResolveReal(full, 0);
        if (realRoot == null || realFull == null || !IsUnder(TrimSeparators(realRoot), TrimSeparators(realFull)))
            return ThroughLinkReason;

        // A link that points at nothing: File.Exists is true for it on Windows, and reading it throws.
        if (!string.Equals(full, realFull, PathComparison) && !File.Exists(realFull) && !Directory.Exists(realFull)
            && new FileInfo(full).LinkTarget != null)
        {
            return BrokenLinkReason;
        }
        return null;
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static string TrimSeparators(string path)
    {
        string root = Path.GetPathRoot(path) ?? "";
        return path.Length > root.Length ? path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : path;
    }

    private static bool IsUnder(string root, string path) =>
        path.Equals(root, PathComparison)
        || path.StartsWith(root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar, PathComparison);

    /// <summary>
    /// The path with every link along it followed, segment by segment, so a junction on any parent directory is
    /// resolved too. Null for a link loop.
    /// </summary>
    private static string? ResolveReal(string fullPath, int depth)
    {
        if (depth > MaxLinkDepth) return null;

        string current = Path.GetPathRoot(fullPath) ?? "";
        string[] segments = fullPath.Substring(current.Length)
            .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
        {
            current = Path.Combine(current, segments[i]);
            FileSystemInfo target;
            try
            {
                FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
                // LinkTarget is null for a segment that is not a link or does not exist; ResolveLinkTarget throws for the latter.
                if (info.LinkTarget == null) continue;
                FileSystemInfo? resolved = info.ResolveLinkTarget(returnFinalTarget: true);
                if (resolved == null) continue;
                target = resolved;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            // The target's own parents can be links too.
            string? realTarget = ResolveReal(Path.GetFullPath(target.FullName), depth + 1);
            if (realTarget == null) return null;
            current = realTarget;
        }
        return current;
    }
}
