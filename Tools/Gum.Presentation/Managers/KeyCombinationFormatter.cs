using System;
using System.Text;
using Gum.Input;

namespace Gum.Managers;

/// <summary>Which platform's modifier names a <see cref="KeyCombination"/> is rendered with.</summary>
public enum KeyDisplayStyle
{
    /// <summary>"Ctrl+Shift+Z" - Windows and Linux.</summary>
    Windows,

    /// <summary>"⇧⌘Z" - the macOS symbols, in Apple's ⌥⇧⌘ order.</summary>
    MacOS,
}

/// <summary>Renders a <see cref="KeyCombination"/> for display in the platform's conventions.</summary>
public interface IKeyCombinationFormatter
{
    /// <summary>The user-facing text for <paramref name="combination"/>, e.g. "Ctrl+Z" or "⌘Z".</summary>
    string Format(KeyCombination combination);
}

/// <inheritdoc/>
public class KeyCombinationFormatter : IKeyCombinationFormatter
{
    private readonly KeyDisplayStyle _style;

    /// <summary>Creates a formatter for <paramref name="style"/>.</summary>
    public KeyCombinationFormatter(KeyDisplayStyle style)
    {
        _style = style;
    }

    /// <summary>Creates the formatter for the platform the tool is running on.</summary>
    public static KeyCombinationFormatter ForCurrentPlatform() =>
        new KeyCombinationFormatter(OperatingSystem.IsMacOS() ? KeyDisplayStyle.MacOS : KeyDisplayStyle.Windows);

    /// <inheritdoc/>
    public string Format(KeyCombination combination)
    {
        return _style == KeyDisplayStyle.MacOS ? FormatMacOS(combination) : FormatWindows(combination);
    }

    private static string FormatWindows(KeyCombination combination)
    {
        StringBuilder text = new StringBuilder();
        if (combination.IsCtrlDown)
        {
            AppendPart(text, "Ctrl");
        }
        if (combination.IsShiftDown)
        {
            AppendPart(text, "Shift");
        }
        if (combination.IsAltDown)
        {
            AppendPart(text, "Alt");
        }
        if (combination.Key is { } key)
        {
            AppendPart(text, KeyName(key, KeyDisplayStyle.Windows));
        }
        return text.ToString();

        static void AppendPart(StringBuilder text, string part)
        {
            if (text.Length != 0)
            {
                text.Append('+');
            }
            text.Append(part);
        }
    }

    private static string FormatMacOS(KeyCombination combination)
    {
        StringBuilder text = new StringBuilder();
        if (combination.IsAltDown)
        {
            text.Append('⌥');
        }
        if (combination.IsShiftDown)
        {
            text.Append('⇧');
        }
        if (combination.IsCtrlDown)
        {
            text.Append('⌘');
        }
        if (combination.Key is { } key)
        {
            text.Append(KeyName(key, KeyDisplayStyle.MacOS));
        }
        return text.ToString();
    }

    private static string KeyName(GumKey key, KeyDisplayStyle style)
    {
        bool isMacOS = style == KeyDisplayStyle.MacOS;
        return key switch
        {
            GumKey.Up when isMacOS => "↑",
            GumKey.Down when isMacOS => "↓",
            GumKey.Left when isMacOS => "←",
            GumKey.Right when isMacOS => "→",
            GumKey.Delete when isMacOS => "⌦",
            GumKey.Add => "Numpad +",
            GumKey.Subtract => "Numpad -",
            GumKey.Oemplus => "=",
            GumKey.OemMinus => "-",
            GumKey.OemQuestion => "/",
            _ => key.ToString(),
        };
    }
}
