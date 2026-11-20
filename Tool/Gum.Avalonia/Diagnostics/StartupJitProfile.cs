using System;
using System.IO;
using System.Runtime;

namespace Gum.Avalonia.Diagnostics;

/// <summary>
/// Turns on multicore JIT for startup: each launch records the methods it compiles to a profile in
/// the per-user folder, and the next launch compiles them on background threads ahead of the UI
/// thread needing them. A Debug build is not precompiled, so most of its startup is JIT.
/// </summary>
internal static class StartupJitProfile
{
    /// <summary>The profile's folder, under the per-user folder.</summary>
    public const string FolderName = "JitProfile";

    /// <summary>The profile's file name.</summary>
    public const string FileName = "Startup.jitprofile";

    /// <summary>Starts the runtime's startup JIT profile under <paramref name="appDataDirectory"/>.</summary>
    public static void Start(string appDataDirectory) =>
        Start(appDataDirectory, (folder, fileName) =>
        {
            ProfileOptimization.SetProfileRoot(folder);
            ProfileOptimization.StartProfile(fileName);
        });

    /// <summary>
    /// Creates the profile folder under <paramref name="appDataDirectory"/> and hands it to
    /// <paramref name="startProfile"/>. Never throws for a folder that cannot be created: startup
    /// then runs as it would without the profile.
    /// </summary>
    internal static void Start(string appDataDirectory, Action<string, string> startProfile)
    {
        string folder = Path.Combine(appDataDirectory, FolderName);
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        startProfile(folder, FileName);
    }
}
