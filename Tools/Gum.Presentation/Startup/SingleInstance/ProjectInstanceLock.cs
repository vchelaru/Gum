using System;
using System.IO;

namespace Gum.Startup.SingleInstance;

/// <summary>
/// Marks a project as owned by this process for as long as the process lives. It is an exclusive
/// open of a file (<see cref="FileShare.None"/>), which the operating system releases when the
/// process dies, so a crash leaves no stale claim. .NET enforces it across processes on Windows
/// and, through advisory <c>flock</c> locks, on macOS and Linux.
/// </summary>
public sealed class ProjectInstanceLock : IDisposable
{
    private readonly FileStream _stream;

    private ProjectInstanceLock(FileStream stream)
    {
        _stream = stream;
    }

    /// <summary>The lock for <paramref name="key"/>, or null when another process holds it.</summary>
    public static ProjectInstanceLock? TryAcquire(string instanceDirectory, string key)
    {
        Directory.CreateDirectory(instanceDirectory);
        try
        {
            return new ProjectInstanceLock(new FileStream(
                InstanceIdentity.LockFilePath(instanceDirectory, key), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>True while some process holds the lock for <paramref name="key"/>.</summary>
    public static bool IsHeld(string instanceDirectory, string key)
    {
        using ProjectInstanceLock? probe = TryAcquire(instanceDirectory, key);
        return probe == null;
    }

    /// <inheritdoc/>
    public void Dispose() => _stream.Dispose();
}
