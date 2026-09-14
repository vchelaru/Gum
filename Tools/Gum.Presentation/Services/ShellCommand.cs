using System;
using System.Runtime.InteropServices;

namespace Gum.Services;

/// <summary>
/// Wraps a command line for the operating system's shell: <c>cmd.exe /c</c> on Windows,
/// <c>/bin/sh -c</c> elsewhere. Use it for tools that must be resolved through the shell, such as
/// <c>npm</c>, instead of naming <c>cmd.exe</c> at the call site.
/// </summary>
public static class ShellCommand
{
    /// <summary>Builds the shell invocation for the current operating system.</summary>
    public static (string fileName, string arguments) Build(string commandLine) =>
        Build(commandLine, OperatingSystem.IsWindows() ? OSPlatform.Windows : OSPlatform.Linux);

    /// <summary>Builds the shell invocation for <paramref name="platform"/>. Pure, for tests.</summary>
    public static (string fileName, string arguments) Build(string commandLine, OSPlatform platform)
    {
        if (platform == OSPlatform.Windows)
        {
            return ("cmd.exe", "/c " + commandLine);
        }

        return ("/bin/sh", "-c \"" + commandLine.Replace("\"", "\\\"") + "\"");
    }
}
