using System;
using System.Runtime.InteropServices;

namespace GumPreview;

/// <summary>
/// Thin Windows-only read of XInput, independent of SDL, used by <c>--log</c> to tell whether the OS delivers
/// pad input when SDL does not. All formatting lives in <see cref="PreviewGamepadLog"/>. Blittable structs and
/// plain DllImports, so it needs no runtime marshalling and is Native-AOT safe.
/// </summary>
internal static class XInputProbe
{
    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepad Gamepad;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint GetState14(uint userIndex, out XInputState state);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
    private static extern uint GetState910(uint userIndex, out XInputState state);

    private static int _flavor; // 0 = untried, 1 = xinput1_4, 2 = xinput9_1_0, -1 = unavailable

    /// <summary>Which DLL answered, for the startup line.</summary>
    public static string Library => _flavor switch { 1 => "xinput1_4", 2 => "xinput9_1_0", -1 => "unavailable", _ => "untried" };

    /// <summary>
    /// The formatted state of XInput user <paramref name="index"/> (0-3), or null when XInput is unavailable
    /// (not Windows, or no XInput DLL loads). Never throws.
    /// </summary>
    public static string? Read(int index)
    {
        if (!OperatingSystem.IsWindows() || _flavor < 0)
        {
            return null;
        }
        try
        {
            XInputState state;
            uint result;
            if (_flavor == 2)
            {
                result = GetState910((uint)index, out state);
            }
            else
            {
                try
                {
                    result = GetState14((uint)index, out state);
                    _flavor = 1;
                }
                catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException)
                {
                    result = GetState910((uint)index, out state);
                    _flavor = 2;
                }
            }
            if (result != 0)
            {
                return PreviewGamepadLog.FormatXInput(false, 0, 0, 0, 0, 0, 0, 0, 0);
            }
            XInputGamepad g = state.Gamepad;
            return PreviewGamepadLog.FormatXInput(true, state.PacketNumber, g.Buttons, g.LeftTrigger, g.RightTrigger,
                g.ThumbLX, g.ThumbLY, g.ThumbRX, g.ThumbRY);
        }
        catch (Exception)
        {
            _flavor = -1;
            return null;
        }
    }
}
