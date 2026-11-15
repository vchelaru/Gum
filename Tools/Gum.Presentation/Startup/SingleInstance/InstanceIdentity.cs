using System;
using System.IO;
using System.Linq;
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

    /// <summary>Where lock files live for the current user; see <see cref="InstanceDirectoryFor"/>.</summary>
    public static string DefaultInstanceDirectory() => InstanceDirectoryFor(
        Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR"), Path.GetTempPath(), Environment.UserName, OperatingSystem.IsLinux());

    /// <summary>
    /// The folder for lock files. Linux prefers <paramref name="runtimeDirectory"/>
    /// (<c>XDG_RUNTIME_DIR</c>, private to the user). Otherwise a folder named for
    /// <paramref name="userName"/> inside <paramref name="tempPath"/>, because /tmp is shared on Linux.
    /// </summary>
    public static string InstanceDirectoryFor(string? runtimeDirectory, string tempPath, string userName, bool isLinux)
    {
        if (isLinux && !string.IsNullOrEmpty(runtimeDirectory))
        {
            return Path.Combine(runtimeDirectory, "Gum", "Instances");
        }

        char[] invalid = Path.GetInvalidFileNameChars();
        string safeUser = string.Concat(userName.Select(c => invalid.Contains(c) ? '_' : c));
        return Path.Combine(tempPath, "Gum-" + safeUser, "Instances");
    }

    /// <summary>
    /// Creates <paramref name="directory"/> and its parent so that only the current user can use them
    /// on macOS and Linux (mode 0700), tightening one that already exists. Throws
    /// <see cref="UnauthorizedAccessException"/> or <see cref="IOException"/> when a folder
    /// belongs to someone else. On Windows the user's temp folder is already private.
    /// </summary>
    public static void EnsurePrivateDirectory(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(directory);
            return;
        }

        const UnixFileMode privateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        string? parent = Path.GetDirectoryName(directory);
        foreach (string? folder in new[] { parent, directory })
        {
            if (folder == null)
            {
                continue;
            }

            Directory.CreateDirectory(folder, privateMode);
            if (File.GetUnixFileMode(folder) != privateMode)
            {
                File.SetUnixFileMode(folder, privateMode);
            }
        }
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
