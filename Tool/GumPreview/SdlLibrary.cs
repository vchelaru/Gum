using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GumPreview;

/// <summary>
/// Maps GumPreview's <c>[DllImport("SDL2")]</c> to the SDL file MonoGame ships on each OS (#5557).
/// Default probing looks for <c>libSDL2.dylib</c>/<c>libSDL2.so</c>, which don't exist outside Windows.
/// </summary>
internal static class SdlLibrary
{
    public const string ImportName = "SDL2";

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
}
