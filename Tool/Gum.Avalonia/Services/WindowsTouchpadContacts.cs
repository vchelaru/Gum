using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Gum.Avalonia.Services;

/// <summary>
/// Tells a precision-touchpad scroll from a mouse wheel on Windows. Both arrive as
/// <c>WM_MOUSEWHEEL</c> with nothing to tell them apart, but a precision touchpad also sends HID
/// reports (usage page 0x0D, usage 0x05) for as long as fingers touch it, so a wheel event arriving
/// while those reports are flowing came from the touchpad. This registers for those reports as raw
/// input on one Gum window (with <c>RIDEV_INPUTSINK</c>, so it keeps receiving them while another
/// Gum window has focus) and records when the last one arrived.
/// This is the head's sanctioned per-OS P/Invoke file for user32 raw input (see
/// BannedSymbols.CrossPlatform.txt); callers must check <see cref="OperatingSystem.IsWindows"/>.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowsTouchpadContacts
{
    private const uint WM_INPUT = 0x00FF;
    private const uint RID_HEADER = 0x10000005;
    private const uint RIM_TYPEHID = 2;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const ushort HidUsagePageDigitizer = 0x0D;
    private const ushort HidUsageTouchPad = 0x05;

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RawInputDevice[] devices, uint count, uint size);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(IntPtr rawInput, uint command, out RawInputHeader data, ref uint size, uint headerSize);
#pragma warning restore RS0030

    private static TopLevel? _hookedTopLevel;

    /// <summary>
    /// <see cref="Environment.TickCount64"/> when the last touchpad report arrived, or null if none
    /// has since the app started.
    /// </summary>
    public static long? LastReportMs { get; private set; }

    /// <summary>
    /// Starts listening for touchpad reports through <paramref name="topLevel"/>'s window, unless a
    /// still-open window already is. Raw input for a device goes to one window per process.
    /// </summary>
    public static void EnsureListening(TopLevel topLevel)
    {
        if (_hookedTopLevel != null)
        {
            return;
        }

        IntPtr hwnd = topLevel.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        RawInputDevice[] devices =
        [
            new RawInputDevice { UsagePage = HidUsagePageDigitizer, Usage = HidUsageTouchPad, Flags = RIDEV_INPUTSINK, Target = hwnd },
        ];
        if (!RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf<RawInputDevice>()))
        {
            return;
        }

        Win32Properties.AddWndProcHookCallback(topLevel, HandleWindowMessage);
        _hookedTopLevel = topLevel;
        if (topLevel is Window window)
        {
            window.Closed += (_, _) => _hookedTopLevel = null;
        }
    }

    // Only reads the header; returning unhandled lets DefWindowProc release the raw input.
    private static IntPtr HandleWindowMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_INPUT)
        {
            uint headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
            uint size = headerSize;
            if (GetRawInputData(lParam, RID_HEADER, out RawInputHeader header, ref size, headerSize) != uint.MaxValue
                && header.Type == RIM_TYPEHID)
            {
                LastReportMs = Environment.TickCount64;
            }
        }
        return IntPtr.Zero;
    }
}
