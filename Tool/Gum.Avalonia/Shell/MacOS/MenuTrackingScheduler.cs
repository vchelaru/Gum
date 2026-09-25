using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Threading;

namespace Gum.Avalonia.Shell.MacOS;

/// <summary>
/// Runs a macOS menu-bar action once AppKit has finished tracking the menu. An item's click
/// arrives while the menu bar is still tracking, and Avalonia's dispatcher also runs during
/// tracking (its run-loop hooks are in the common modes), so a posted action that opens a
/// synchronous dialog nests its dispatcher loop inside the tracking loop: the dialog is ordered
/// in but never made key or brought to the front until the app is activated again (#4982).
/// A run-loop timer added only to the default mode cannot fire until tracking has ended.
/// This is the head's sanctioned per-OS P/Invoke file for CoreFoundation (see
/// BannedSymbols.CrossPlatform.txt); callers must check <see cref="OperatingSystem.IsMacOS"/>.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MenuTrackingScheduler
{
    private const string CoreFoundationPath = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TimerCallback(IntPtr timer, IntPtr info);

    [StructLayout(LayoutKind.Sequential)]
    private struct TimerContext
    {
        public nint Version;
        public IntPtr Info;
        public IntPtr Retain;
        public IntPtr Release;
        public IntPtr CopyDescription;
    }

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport(CoreFoundationPath)]
    private static extern IntPtr CFRunLoopGetMain();

    [DllImport(CoreFoundationPath)]
    private static extern double CFAbsoluteTimeGetCurrent();

    [DllImport(CoreFoundationPath)]
    private static extern IntPtr CFRunLoopTimerCreate(IntPtr allocator, double fireDate, double interval,
        nuint flags, nint order, TimerCallback callout, ref TimerContext context);

    [DllImport(CoreFoundationPath)]
    private static extern void CFRunLoopAddTimer(IntPtr runLoop, IntPtr timer, IntPtr mode);

    [DllImport(CoreFoundationPath)]
    private static extern void CFRelease(IntPtr handle);
#pragma warning restore RS0030

    // Held for the process lifetime so the function pointer handed to CoreFoundation stays valid.
    private static readonly TimerCallback Callback = OnTimerFired;

    private static IntPtr _defaultMode;

    /// <summary>
    /// Runs <paramref name="action"/> on the dispatcher once the main run loop is back in its
    /// default mode. Falls back to a plain dispatcher post if CoreFoundation cannot be reached.
    /// </summary>
    public static void InvokeAfterTracking(Action action)
    {
        GCHandle handle = GCHandle.Alloc(action);
        try
        {
            TimerContext context = new TimerContext { Info = GCHandle.ToIntPtr(handle) };
            // A non-repeating timer (interval 0) invalidates itself after firing; the run loop
            // holds its own reference, so ours is released right away.
            IntPtr timer = CFRunLoopTimerCreate(IntPtr.Zero, CFAbsoluteTimeGetCurrent(), 0, 0, 0, Callback, ref context);
            CFRunLoopAddTimer(CFRunLoopGetMain(), timer, DefaultMode());
            CFRelease(timer);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            handle.Free();
            Dispatcher.UIThread.Post(action);
        }
    }

    private static void OnTimerFired(IntPtr timer, IntPtr info)
    {
        GCHandle handle = GCHandle.FromIntPtr(info);
        Action action = (Action)handle.Target!;
        handle.Free();
        Dispatcher.UIThread.Post(action);
    }

    private static IntPtr DefaultMode()
    {
        if (_defaultMode == IntPtr.Zero)
        {
            IntPtr library = NativeLibrary.Load(CoreFoundationPath);
            // kCFRunLoopDefaultMode is an exported CFStringRef variable, so read the pointer it holds.
            _defaultMode = Marshal.ReadIntPtr(NativeLibrary.GetExport(library, "kCFRunLoopDefaultMode"));
        }
        return _defaultMode;
    }
}
