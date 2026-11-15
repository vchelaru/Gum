using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Gum.Startup.SingleInstance;

/// <summary>
/// Which running Gum owns which project. An instance is identified by the normalized path of the
/// project it has open plus the user, never by its window title. The key names both the lock file
/// and the pipe.
/// </summary>
public static class InstanceIdentity
{
    /// <summary>
    /// A 16-character lowercase hex key for <paramref name="projectPath"/> and <paramref name="userName"/>.
    /// The path is made absolute and dot-free; case is ignored unless the file system
    /// is case sensitive (<see cref="IsFileSystemCaseSensitive"/>).
    /// </summary>
    public static string KeyFor(string projectPath, string userName, bool caseSensitive)
    {
        string normalized = Path.GetFullPath(projectPath);
        if (!caseSensitive)
        {
            normalized = normalized.ToUpperInvariant();
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("gum-open-v1\n" + userName + "\n" + normalized));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    /// <summary>The key for the current user on this machine.</summary>
    public static string KeyForCurrentUser(string projectPath) =>
        KeyFor(projectPath, Environment.UserName, IsFileSystemCaseSensitive());

    /// <summary>Linux file systems are case sensitive; Windows and macOS default to insensitive.</summary>
    public static bool IsFileSystemCaseSensitive() => OperatingSystem.IsLinux();

    /// <summary>The pipe name for <paramref name="key"/>.</summary>
    public static string PipeName(string key) => "gum-open-" + key;

    /// <summary>The lock file for <paramref name="key"/> inside <paramref name="instanceDirectory"/>.</summary>
    public static string LockFilePath(string instanceDirectory, string key) => Path.Combine(instanceDirectory, key + ".lock");

    /// <summary>
    /// Where lock files live: a per-user folder. Linux prefers <c>XDG_RUNTIME_DIR</c> (private to
    /// the user); elsewhere the user's temp folder, which is already per user.
    /// </summary>
    public static string DefaultInstanceDirectory()
    {
        string? runtimeDirectory = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        string root = OperatingSystem.IsLinux() && !string.IsNullOrEmpty(runtimeDirectory) ? runtimeDirectory : Path.GetTempPath();
        return Path.Combine(root, "Gum", "Instances");
    }

    /// <summary>
    /// The Gum element name for an element file: its path below the project's Screens, Components or
    /// Standards folder without the extension, with <c>/</c> separators. A file elsewhere falls back
    /// to its bare name. Null when the file is not a Gum element file.
    /// </summary>
    public static string? ElementNameFor(string projectPath, string elementFilePath)
    {
        string extension = Path.GetExtension(elementFilePath).ToLowerInvariant();
        string? folder = extension switch
        {
            ".gusx" or ".gusj" => "Screens",
            ".gucx" or ".gucj" => "Components",
            ".gutx" or ".gutj" => "Standards",
            _ => null,
        };
        if (folder == null)
        {
            return null;
        }

        string projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? "";
        string relative = Path.GetRelativePath(Path.Combine(projectDirectory, folder), Path.GetFullPath(elementFilePath));
        string withoutExtension = Path.ChangeExtension(relative, null);
        bool isInsideFolder = !withoutExtension.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(withoutExtension);
        string name = isInsideFolder ? withoutExtension : Path.GetFileNameWithoutExtension(elementFilePath);
        return name.Replace('\\', '/');
    }
}
