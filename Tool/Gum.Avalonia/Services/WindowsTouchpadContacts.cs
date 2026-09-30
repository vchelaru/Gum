using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Gum.Avalonia.Services;

/// <summary>
/// Reads a Windows precision touchpad's HID contact reports (usage page 0x0D, usage 0x05) as raw
/// input. A touchpad scroll and a mouse wheel both arrive as <c>WM_MOUSEWHEEL</c> with nothing to
/// tell them apart, but the touchpad sends these reports for as long as fingers touch it, so a wheel
/// event arriving while they flow came from the touchpad. The reports' finger positions also feed
/// <see cref="PanTracker"/>, because the wheel messages lock a diagonal swipe to one axis at first.
/// Raw input for a device goes to one window per process, so this listens on one Gum window, with
/// <c>RIDEV_INPUTSINK</c> so it keeps receiving reports while another Gum window has focus.
/// This is the head's sanctioned per-OS P/Invoke file for raw input and HID parsing (see
/// BannedSymbols.CrossPlatform.txt); callers must check <see cref="OperatingSystem.IsWindows"/>.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowsTouchpadContacts
{
    private const uint WM_INPUT = 0x00FF;
    private const uint RID_INPUT = 0x10000003;
    private const uint RIDI_PREPARSEDDATA = 0x20000005;
    private const uint RIM_TYPEHID = 2;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const int HidpInput = 0;
    private const int HidpStatusSuccess = 0x00110000;

    private const ushort UsagePageGenericDesktop = 0x01;
    private const ushort UsageX = 0x30;
    private const ushort UsageY = 0x31;
    private const ushort UsagePageDigitizer = 0x0D;
    private const ushort UsageTouchPad = 0x05;
    private const ushort UsageTipSwitch = 0x42;
    private const ushort UsageContactId = 0x51;
    private const ushort UsageContactCount = 0x54;

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

    [StructLayout(LayoutKind.Sequential)]
    private struct HidpCaps
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    // HIDP_VALUE_CAPS; the trailing union is read as its NotRange form (Usage first).
    [StructLayout(LayoutKind.Sequential)]
    private struct HidpValueCaps
    {
        public ushort UsagePage;
        public byte ReportId;
        public byte IsAlias;
        public ushort BitField;
        public ushort LinkCollection;
        public ushort LinkUsage;
        public ushort LinkUsagePage;
        public byte IsRange;
        public byte IsStringRange;
        public byte IsDesignatorRange;
        public byte IsAbsolute;
        public byte HasNull;
        public byte Reserved;
        public ushort BitSize;
        public ushort ReportCount;
        public ushort Reserved2A, Reserved2B, Reserved2C, Reserved2D, Reserved2E;
        public uint UnitsExp;
        public uint Units;
        public int LogicalMin;
        public int LogicalMax;
        public int PhysicalMin;
        public int PhysicalMax;
        public ushort Usage;
        public ushort Reserved3;
        public ushort StringIndex;
        public ushort Reserved4;
        public ushort DesignatorIndex;
        public ushort Reserved5;
        public ushort DataIndex;
        public ushort Reserved6;
    }

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RawInputDevice[] devices, uint count, uint size);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(IntPtr rawInput, uint command, IntPtr data, ref uint size, uint headerSize);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, IntPtr data, ref uint size);

    [DllImport("hid.dll")]
    private static extern int HidP_GetCaps(IntPtr preparsedData, out HidpCaps caps);

    [DllImport("hid.dll")]
    private static extern int HidP_GetValueCaps(int reportType, [Out] HidpValueCaps[] caps, ref ushort length, IntPtr preparsedData);

    [DllImport("hid.dll")]
    private static extern int HidP_GetUsageValue(int reportType, ushort usagePage, ushort linkCollection, ushort usage,
        out uint value, IntPtr preparsedData, byte[] report, uint reportLength);

    [DllImport("hid.dll")]
    private static extern int HidP_GetUsages(int reportType, ushort usagePage, ushort linkCollection, [Out] ushort[] usages,
        ref uint usageLength, IntPtr preparsedData, byte[] report, uint reportLength);
#pragma warning restore RS0030

    /// <summary>A finger collection of a touchpad's report: where its X and Y live and how to scale them.</summary>
    private sealed record Finger(ushort LinkCollection, int XMin, double XMmPerUnit, int YMin, double YMmPerUnit);

    /// <summary>A touchpad's parsed report layout; null fields mean it isn't one this can read.</summary>
    private sealed record TouchpadLayout(IntPtr PreparsedData, ushort ContactCountLink, IReadOnlyList<Finger> Fingers);

    private static readonly Dictionary<IntPtr, TouchpadLayout?> Layouts = new Dictionary<IntPtr, TouchpadLayout?>();
    private static readonly TouchpadFrameAssembler FrameAssembler = new TouchpadFrameAssembler();
    private static readonly ushort[] UsageBuffer = new ushort[32];
    private static TopLevel? _hookedTopLevel;

    /// <summary>
    /// <see cref="Environment.TickCount64"/> when the last touchpad report arrived, or null if none
    /// has since the app started.
    /// </summary>
    public static long? LastReportMs { get; private set; }

    /// <summary>The two-finger travel read from the touchpad's reports.</summary>
    public static TouchpadPanTracker PanTracker { get; } = new TouchpadPanTracker();

    /// <summary>
    /// Starts listening for touchpad reports through <paramref name="topLevel"/>'s window, unless a
    /// still-open window already is.
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
            new RawInputDevice { UsagePage = UsagePageDigitizer, Usage = UsageTouchPad, Flags = RIDEV_INPUTSINK, Target = hwnd },
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

    // Returning unhandled lets DefWindowProc release the raw input.
    private static IntPtr HandleWindowMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_INPUT)
        {
            ReadRawInput(lParam);
        }
        return IntPtr.Zero;
    }

    private static void ReadRawInput(IntPtr rawInput)
    {
        uint headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        uint size = 0;
        if (GetRawInputData(rawInput, RID_INPUT, IntPtr.Zero, ref size, headerSize) != 0 || size < headerSize + 8)
        {
            return;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetRawInputData(rawInput, RID_INPUT, buffer, ref size, headerSize) == uint.MaxValue)
            {
                return;
            }

            RawInputHeader header = Marshal.PtrToStructure<RawInputHeader>(buffer);
            if (header.Type != RIM_TYPEHID)
            {
                return;
            }
            LastReportMs = Environment.TickCount64;

            TouchpadLayout? layout = GetLayout(header.Device);
            if (layout == null)
            {
                return;
            }

            // RAWHID: dwSizeHid, dwCount, then dwCount reports of dwSizeHid bytes each.
            int reportSize = Marshal.ReadInt32(buffer, (int)headerSize);
            int reportCount = Marshal.ReadInt32(buffer, (int)headerSize + 4);
            if (reportSize <= 0 || headerSize + 8 + (long)reportSize * reportCount > size)
            {
                return;
            }
            for (int i = 0; i < reportCount; i++)
            {
                byte[] report = new byte[reportSize];
                Marshal.Copy(buffer + (int)headerSize + 8 + i * reportSize, report, 0, reportSize);
                ReadReport(layout, report);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void ReadReport(TouchpadLayout layout, byte[] report)
    {
        uint length = (uint)report.Length;
        // A report without the contact count is another of the device's reports, not a touch report.
        if (HidP_GetUsageValue(HidpInput, UsagePageDigitizer, layout.ContactCountLink, UsageContactCount,
            out uint contactCount, layout.PreparsedData, report, length) != HidpStatusSuccess)
        {
            return;
        }

        List<TouchpadSlot> slots = new List<TouchpadSlot>(layout.Fingers.Count);
        foreach (Finger finger in layout.Fingers)
        {
            bool read =
                HidP_GetUsageValue(HidpInput, UsagePageDigitizer, finger.LinkCollection, UsageContactId, out uint id,
                    layout.PreparsedData, report, length) == HidpStatusSuccess
                & HidP_GetUsageValue(HidpInput, UsagePageGenericDesktop, finger.LinkCollection, UsageX, out uint x,
                    layout.PreparsedData, report, length) == HidpStatusSuccess
                & HidP_GetUsageValue(HidpInput, UsagePageGenericDesktop, finger.LinkCollection, UsageY, out uint y,
                    layout.PreparsedData, report, length) == HidpStatusSuccess;
            slots.Add(new TouchpadSlot(
                (int)id,
                read && IsTouching(layout, finger, report),
                ((int)x - finger.XMin) * finger.XMmPerUnit,
                ((int)y - finger.YMin) * finger.YMmPerUnit));
        }

        IReadOnlyList<TouchpadContact>? frame = FrameAssembler.AddReport((int)contactCount, slots);
        if (frame != null)
        {
            PanTracker.OnFrame(frame);
        }
    }

    private static bool IsTouching(TouchpadLayout layout, Finger finger, byte[] report)
    {
        uint usageCount = (uint)UsageBuffer.Length;
        if (HidP_GetUsages(HidpInput, UsagePageDigitizer, finger.LinkCollection, UsageBuffer, ref usageCount,
            layout.PreparsedData, report, (uint)report.Length) != HidpStatusSuccess)
        {
            return false;
        }
        return Array.IndexOf(UsageBuffer, UsageTipSwitch, 0, (int)usageCount) >= 0;
    }

    private static TouchpadLayout? GetLayout(IntPtr device)
    {
        if (!Layouts.TryGetValue(device, out TouchpadLayout? layout))
        {
            layout = ReadLayout(device);
            Layouts[device] = layout;
        }
        return layout;
    }

    // The preparsed data is kept for the process's lifetime; a machine has one or two touchpads.
    private static TouchpadLayout? ReadLayout(IntPtr device)
    {
        uint size = 0;
        if (GetRawInputDeviceInfo(device, RIDI_PREPARSEDDATA, IntPtr.Zero, ref size) != 0 || size == 0)
        {
            return null;
        }
        IntPtr preparsed = Marshal.AllocHGlobal((int)size);
        if (GetRawInputDeviceInfo(device, RIDI_PREPARSEDDATA, preparsed, ref size) == uint.MaxValue
            || HidP_GetCaps(preparsed, out HidpCaps caps) != HidpStatusSuccess)
        {
            Marshal.FreeHGlobal(preparsed);
            return null;
        }

        ushort valueCapsLength = caps.NumberInputValueCaps;
        HidpValueCaps[] valueCaps = new HidpValueCaps[valueCapsLength];
        if (HidP_GetValueCaps(HidpInput, valueCaps, ref valueCapsLength, preparsed) != HidpStatusSuccess)
        {
            Marshal.FreeHGlobal(preparsed);
            return null;
        }

        ushort? contactCountLink = null;
        List<Finger> fingers = new List<Finger>();
        for (int i = 0; i < valueCapsLength; i++)
        {
            HidpValueCaps xCaps = valueCaps[i];
            if (xCaps.IsRange != 0)
            {
                continue;
            }
            if (xCaps.UsagePage == UsagePageDigitizer && xCaps.Usage == UsageContactCount)
            {
                contactCountLink = xCaps.LinkCollection;
            }
            else if (xCaps.UsagePage == UsagePageGenericDesktop && xCaps.Usage == UsageX
                && FindValueCaps(valueCaps, valueCapsLength, xCaps.LinkCollection, UsageY) is { } yCaps)
            {
                fingers.Add(new Finger(xCaps.LinkCollection, xCaps.LogicalMin, MillimetersPerUnit(xCaps),
                    yCaps.LogicalMin, MillimetersPerUnit(yCaps)));
            }
        }

        if (contactCountLink == null || fingers.Count == 0)
        {
            Marshal.FreeHGlobal(preparsed);
            return null;
        }
        fingers.Sort((a, b) => a.LinkCollection.CompareTo(b.LinkCollection));
        return new TouchpadLayout(preparsed, contactCountLink.Value, fingers);
    }

    private static HidpValueCaps? FindValueCaps(HidpValueCaps[] valueCaps, int length, ushort linkCollection, ushort usage)
    {
        for (int i = 0; i < length; i++)
        {
            if (valueCaps[i].IsRange == 0 && valueCaps[i].UsagePage == UsagePageGenericDesktop
                && valueCaps[i].Usage == usage && valueCaps[i].LinkCollection == linkCollection)
            {
                return valueCaps[i];
            }
        }
        return null;
    }

    // Precision touchpads must report physical extents; if one reports none or an implausible
    // size, assume a 100 mm pad.
    private static double MillimetersPerUnit(HidpValueCaps caps)
    {
        int logicalRange = caps.LogicalMax - caps.LogicalMin;
        if (logicalRange <= 0)
        {
            return 0;
        }

        int exponent = (int)(caps.UnitsExp & 0xF);
        if (exponent > 7)
        {
            exponent -= 16;
        }
        // The unit's low nibble is its system: 1 is SI (centimeters), 3 is English (inches).
        double unitMm = (caps.Units & 0xF) == 3 ? 25.4 : 10;
        double extentMm = (caps.PhysicalMax - caps.PhysicalMin) * Math.Pow(10, exponent) * unitMm;
        if (extentMm < 20 || extentMm > 300)
        {
            extentMm = 100;
        }
        return extentMm / logicalRange;
    }
}
