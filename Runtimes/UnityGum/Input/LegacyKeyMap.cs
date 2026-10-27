using System;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Unity;

/// <summary>
/// Maps Unity's legacy Input Manager keys (<c>UnityEngine.KeyCode</c>) onto Gum's keys. KeyCode values
/// are held as integers because this assembly references no Unity assembly; the Unity package casts
/// them back. Several KeyCodes can map to one Gum key (both Enters).
/// </summary>
public static class LegacyKeyMap
{
    /// <summary>
    /// Every Gum key the legacy backend reports, with the <c>KeyCode</c> value that holds it. Gum keys
    /// with no KeyCode (media, browser, IME, F16 to F24, OemBackslash) are left out.
    /// </summary>
    public static readonly (GumKeys Gum, int KeyCode)[] Entries = Build();

    /// <summary>
    /// Reports each mapped Gum key as held when any of its KeyCodes is, according to
    /// <paramref name="isKeyDown"/>.
    /// </summary>
    public static void Push(Gum.Input.Keyboard keyboard, Func<int, bool> isKeyDown)
    {
        // Release everything first, then raise the held ones, so a Gum key with two KeyCodes stays
        // down when either is. Nothing reads the state until the next Activity.
        for (int i = 0; i < Entries.Length; i++)
        {
            keyboard.SetKeyDown(Entries[i].Gum, false);
        }
        for (int i = 0; i < Entries.Length; i++)
        {
            if (isKeyDown(Entries[i].KeyCode))
            {
                keyboard.SetKeyDown(Entries[i].Gum, true);
            }
        }
    }

    private static (GumKeys, int)[] Build()
    {
        var list = new System.Collections.Generic.List<(GumKeys, int)>
        {
            (GumKeys.Back, 8),
            (GumKeys.Tab, 9),
            (GumKeys.Enter, 13),
            (GumKeys.Enter, 271),
            (GumKeys.Pause, 19),
            (GumKeys.CapsLock, 301),
            (GumKeys.Escape, 27),
            (GumKeys.Space, 32),
            (GumKeys.PageUp, 280),
            (GumKeys.PageDown, 281),
            (GumKeys.End, 279),
            (GumKeys.Home, 278),
            (GumKeys.Left, 276),
            (GumKeys.Up, 273),
            (GumKeys.Right, 275),
            (GumKeys.Down, 274),
            (GumKeys.PrintScreen, 316),
            (GumKeys.Insert, 277),
            (GumKeys.Delete, 127),
            (GumKeys.LeftWindows, 311),
            (GumKeys.RightWindows, 312),
            (GumKeys.Apps, 319),
            (GumKeys.Multiply, 268),
            (GumKeys.Add, 270),
            (GumKeys.Subtract, 269),
            (GumKeys.Decimal, 266),
            (GumKeys.Divide, 267),
            (GumKeys.NumLock, 300),
            (GumKeys.Scroll, 302),
            (GumKeys.LeftShift, 304),
            (GumKeys.RightShift, 303),
            (GumKeys.LeftControl, 306),
            (GumKeys.RightControl, 305),
            (GumKeys.LeftAlt, 308),
            (GumKeys.RightAlt, 307),
            (GumKeys.OemSemicolon, 59),
            (GumKeys.OemPlus, 61),
            (GumKeys.OemComma, 44),
            (GumKeys.OemMinus, 45),
            (GumKeys.OemPeriod, 46),
            (GumKeys.OemQuestion, 47),
            (GumKeys.OemTilde, 96),
            (GumKeys.OemOpenBrackets, 91),
            (GumKeys.OemPipe, 92),
            (GumKeys.OemCloseBrackets, 93),
            (GumKeys.OemQuotes, 39),
        };

        // D0 to D9 follow Alpha0 to Alpha9 (48 to 57), A to Z follow a to z (97 to 122), NumPad0 to
        // NumPad9 follow Keypad0 to Keypad9 (256 to 265), F1 to F15 follow F1 (282).
        for (int i = 0; i < 10; i++)
        {
            list.Add(((GumKeys)((int)GumKeys.D0 + i), 48 + i));
            list.Add(((GumKeys)((int)GumKeys.NumPad0 + i), 256 + i));
        }
        for (int i = 0; i < 26; i++)
        {
            list.Add(((GumKeys)((int)GumKeys.A + i), 97 + i));
        }
        for (int i = 0; i < 15; i++)
        {
            list.Add(((GumKeys)((int)GumKeys.F1 + i), 282 + i));
        }
        return list.ToArray();
    }
}
