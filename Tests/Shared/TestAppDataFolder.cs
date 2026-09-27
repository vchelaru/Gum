using System;
using System.IO;
using System.Runtime.CompilerServices;
using ToolsUtilities;

namespace GumTestSupport;

/// <summary>
/// Points the tool's per-user folder at a fresh temp folder for this test process before any test
/// runs, and deletes it when the process exits. Without it, services that save settings resolve
/// <c>%APPDATA%\testhost</c>, which every test run on the machine shares (#5337). Compiled into each
/// tool test project.
/// </summary>
internal static class TestAppDataFolder
{
    /// <summary>The folder, without a trailing separator.</summary>
    public static string Path { get; private set; } = "";

    // CA2255 warns against module initializers in libraries; a test assembly is only ever loaded by
    // its test host, and this has to run before any test class builds a service.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GumTestAppData",
            $"{Environment.ProcessId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
        FileManager.UserApplicationDataFolderOverride = Path;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Delete();
    }

    private static void Delete()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A file watcher may still hold a file; the OS temp cleanup gets it later.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
