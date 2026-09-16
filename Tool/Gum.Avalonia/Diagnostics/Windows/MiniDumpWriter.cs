using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Gum.Avalonia.Diagnostics.Windows;

/// <summary>
/// Windows-only minidump capture via dbghelp's MiniDumpWriteDump. This is the head's sanctioned
/// per-OS home for the P/Invoke that is otherwise banned in this project (see
/// BannedSymbols.CrossPlatform.txt) - callers must check <see cref="OperatingSystem.IsWindows"/>
/// before calling <see cref="TryWrite"/>.
/// </summary>
internal static class MiniDumpWriter
{
    [Flags]
    private enum MiniDumpType : uint
    {
        Normal = 0x00000000,
        WithThreadInfo = 0x00001000,
    }

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport("dbghelp.dll", SetLastError = true)]
    private static extern bool MiniDumpWriteDump(
        IntPtr hProcess,
        uint processId,
        SafeHandle hFile,
        MiniDumpType dumpType,
        IntPtr exceptionParam,
        IntPtr userStreamParam,
        IntPtr callbackParam);
#pragma warning restore RS0030

    /// <summary>
    /// Writes a thread-stacks-focused minidump of the current process to <paramref name="path"/>.
    /// Returns false (without throwing) if the OS call fails - diagnostics must never crash the
    /// process they are trying to capture information about.
    /// </summary>
    public static bool TryWrite(string path)
    {
        using Process process = Process.GetCurrentProcess();
        using FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write);
        return MiniDumpWriteDump(
            process.Handle,
            (uint)process.Id,
            file.SafeFileHandle,
            MiniDumpType.WithThreadInfo,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);
    }
}
