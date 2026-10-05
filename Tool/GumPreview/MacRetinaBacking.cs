using System;
using System.Runtime.InteropServices;

namespace GumPreview;

/// <summary>
/// Gives MonoGame's SDL window a full-resolution (Retina) OpenGL surface on macOS so Preview draws
/// 1:1 with the tool's canvas (#5571). MonoGame 3.8.x never passes SDL_WINDOW_ALLOW_HIGHDPI, and SDL2
/// has no hint that turns it on, so this sets the same NSView property SDL would have set. Remove it
/// once MonoGame ships an opt-in (MonoGame/MonoGame#9462).
/// </summary>
internal static class MacRetinaBacking
{
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";
    private const int SdlSysWmCocoa = 4;

    /// <summary>
    /// Switches the window's content view to a full-resolution surface. Returns false off macOS, or
    /// when SDL can't hand back the native window.
    /// </summary>
    public static bool TryEnable(IntPtr sdlWindow)
    {
        IntPtr nsWindow = GetNSWindow(sdlWindow);
        if (nsWindow == IntPtr.Zero)
        {
            return false;
        }
        IntPtr contentView = objc_msgSend_IntPtr(nsWindow, sel_registerName("contentView"));
        objc_msgSend_Byte(contentView, sel_registerName("setWantsBestResolutionOpenGLSurface:"), 1);
        return true;
    }

    /// <summary>The window's current points-to-pixels factor: 2 on a Retina display, 1 otherwise.</summary>
    public static BackingScale GetScale(IntPtr sdlWindow)
    {
        IntPtr nsWindow = GetNSWindow(sdlWindow);
        if (nsWindow == IntPtr.Zero)
        {
            return new BackingScale(1);
        }
        double factor = objc_msgSend_Double(nsWindow, sel_registerName("backingScaleFactor"));
        return new BackingScale(factor > 0 ? factor : 1);
    }

    /// <summary>Sizes the window in points, bypassing MonoGame, which ties window size to back-buffer size.</summary>
    public static void SetWindowSize(IntPtr sdlWindow, int pointWidth, int pointHeight) =>
        SDL_SetWindowSize(sdlWindow, pointWidth, pointHeight);

    private static IntPtr GetNSWindow(IntPtr sdlWindow)
    {
        if (!OperatingSystem.IsMacOS() || sdlWindow == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }
        SysWmInfo info = default;
        SDL_GetVersion(out info.Version);
        if (SDL_GetWindowWMInfo(sdlWindow, ref info) == 0 || info.Subsystem != SdlSysWmCocoa)
        {
            return IntPtr.Zero;
        }
        return info.Window;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SdlVersion
    {
        public byte Major;
        public byte Minor;
        public byte Patch;
    }

    // SDL_SysWMinfo: version, subsystem, then a 64-byte union whose Cocoa member starts with the
    // NSWindow pointer.
    [StructLayout(LayoutKind.Sequential, Size = 72)]
    private struct SysWmInfo
    {
        public SdlVersion Version;
        public int Subsystem;
        public IntPtr Window;
    }

    [DllImport(SdlLibrary.ImportName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_GetVersion(out SdlVersion version);

    [DllImport(SdlLibrary.ImportName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_GetWindowWMInfo(IntPtr window, ref SysWmInfo info);

    [DllImport(SdlLibrary.ImportName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_SetWindowSize(IntPtr window, int width, int height);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    // objc_msgSend must be declared once per exact signature; arm64 passes variadic arguments
    // differently, so a single catch-all declaration would break there.
    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_Byte(IntPtr receiver, IntPtr selector, byte argument);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern double objc_msgSend_Double(IntPtr receiver, IntPtr selector);
}
