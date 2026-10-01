using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GumPreview;

/// <summary>
/// GumPreview's own SDL calls. MonoGame ships SDL as <c>libSDL2-2.0.0.dylib</c>/<c>libSDL2-2.0.so.0</c>,
/// which default probing for <c>SDL2</c> never finds, so <see cref="Resolve"/> maps the name (#5557).
/// </summary>
internal static class SdlLibrary
{
    public const string ImportName = "SDL2";

    /// <summary>The hidden command-line flag that runs <see cref="Probe"/> instead of opening a window.</summary>
    public const string ProbeFlag = "--probe-sdl";

    public static string FileName =>
        OperatingSystem.IsWindows() ? "SDL2.dll"
        : OperatingSystem.IsMacOS() ? "libSDL2-2.0.0.dylib"
        : "libSDL2-2.0.so.0";

    /// <summary>
    /// A <see cref="DllImportResolver"/>. Returns <see cref="IntPtr.Zero"/> for any other library, or
    /// when SDL can't be loaded, so the runtime falls back to its default probing.
    /// </summary>
    public static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != ImportName)
        {
            return IntPtr.Zero;
        }
        NativeLibrary.TryLoad(FileName, assembly, searchPath, out IntPtr handle);
        return handle;
    }

    /// <summary>Brings the window forward. Returns false when SDL could not be loaded.</summary>
    public static bool TryRaiseWindow(IntPtr window)
    {
        try
        {
            SDL_RaiseWindow(window);
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Calls SDL_RaiseWindow through the same import <see cref="TryRaiseWindow"/> uses, with no window
    /// (SDL rejects it without touching video), so CI can check a published build resolves SDL.
    /// </summary>
    public static int Probe(TextWriter output)
    {
        if (!TryRaiseWindow(IntPtr.Zero))
        {
            output.WriteLine($"GumPreview: could not load SDL_RaiseWindow from {FileName}.");
            return 1;
        }
        output.WriteLine($"GumPreview: SDL_RaiseWindow resolved from {FileName}.");
        return 0;
    }

    [DllImport(ImportName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_RaiseWindow(IntPtr window);
}
