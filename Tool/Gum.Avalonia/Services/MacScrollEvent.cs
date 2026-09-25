using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Gum.Avalonia.Services;

/// <summary>
/// Tells a trackpad scroll from a mouse wheel on macOS. Avalonia reads
/// <c>NSEvent.hasPreciseScrollingDeltas</c> only to scale the delta and does not pass it on, but
/// it raises <c>PointerWheelChanged</c> synchronously inside AppKit's <c>scrollWheel:</c>, so
/// <c>[NSApp currentEvent]</c> is that scroll event while the handler runs. A Magic Mouse also
/// reports precise deltas and so pans too, as it does in native Mac apps.
/// This is the head's sanctioned per-OS P/Invoke file for the Objective-C runtime (see
/// BannedSymbols.CrossPlatform.txt); callers must check <see cref="OperatingSystem.IsMacOS"/>.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MacScrollEvent
{
    private const string ObjCPath = "/usr/lib/libobjc.A.dylib";

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport(ObjCPath)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCPath)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCPath, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendReturningPointer(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCPath, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SendReturningBool(IntPtr receiver, IntPtr selector);
#pragma warning restore RS0030

    private static readonly IntPtr NSApplicationClass = objc_getClass("NSApplication");
    private static readonly IntPtr SharedApplication = sel_registerName("sharedApplication");
    private static readonly IntPtr CurrentEvent = sel_registerName("currentEvent");
    private static readonly IntPtr HasPreciseScrollingDeltas = sel_registerName("hasPreciseScrollingDeltas");

    /// <summary>Whether the event AppKit is dispatching reports precise (trackpad) scrolling deltas.</summary>
    public static bool IsCurrentEventPrecise()
    {
        IntPtr app = SendReturningPointer(NSApplicationClass, SharedApplication);
        IntPtr currentEvent = app == IntPtr.Zero ? IntPtr.Zero : SendReturningPointer(app, CurrentEvent);
        return currentEvent != IntPtr.Zero && SendReturningBool(currentEvent, HasPreciseScrollingDeltas);
    }
}
