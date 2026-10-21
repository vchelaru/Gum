using Gum.Wireframe;
using System;

namespace Gum.Input;

/// <summary>
/// The key repeat delay and rate an operating system reports.
/// </summary>
internal readonly struct KeyRepeatSettings
{
    internal KeyRepeatSettings(TimeSpan delay, TimeSpan rate)
    {
        Delay = delay;
        Rate = rate;
    }

    internal TimeSpan Delay { get; }
    internal TimeSpan Rate { get; }
}

/// <summary>
/// Reads the key repeat settings of the machine the game runs on. Behind an interface so the code that
/// applies them can be tested without an operating system.
/// </summary>
internal interface IKeyRepeatSettingsProvider
{
    /// <summary>
    /// Returns the settings, or null when this machine's settings cannot be read. Must not throw.
    /// </summary>
    KeyRepeatSettings? GetSettings();
}

/// <summary>
/// Starts a keyboard's key repeat from the operating system's settings, once, when Gum initializes.
/// </summary>
internal static class KeyRepeatDefaults
{
    /// <summary>
    /// Where the settings come from. Null skips reading them, so every keyboard keeps the 500 ms / 70 ms
    /// defaults; tests set it to a fake or to null.
    /// </summary>
    internal static IKeyRepeatSettingsProvider? Provider { get; set; } = new OsKeyRepeatSettingsProvider();

    /// <summary>
    /// Sets <paramref name="keyboard"/>'s <see cref="IInputReceiverKeyboard.RepeatDelay"/> and
    /// <see cref="IInputReceiverKeyboard.RepeatRate"/> from <see cref="Provider"/>. Called once when Gum
    /// creates the keyboard, so a value the user sets afterwards wins. A property that is no longer at
    /// the shared default was set already and is left alone. Does nothing, and never throws, when the
    /// settings cannot be read.
    /// </summary>
    internal static void ApplyOsSettings(IInputReceiverKeyboard keyboard)
    {
        KeyRepeatSettings? settings;
        try
        {
            settings = Provider?.GetSettings();
        }
        catch (Exception)
        {
            return;
        }

        if (settings == null)
        {
            return;
        }

        if (keyboard.RepeatDelay == KeyRepeatTimer.DefaultDelay)
        {
            keyboard.RepeatDelay = settings.Value.Delay;
        }

        if (keyboard.RepeatRate == KeyRepeatTimer.DefaultRate)
        {
            keyboard.RepeatRate = settings.Value.Rate;
        }
    }
}
