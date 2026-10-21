using System;
using System.Collections.Generic;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Times held-key repeat for keyboards whose host reports which keys are pushed and held but leaves repeat
/// to the OS or does not report it. The keyboard tells the timer when a key is pushed and released, then
/// asks <see cref="IsRepeatDue"/> once a key is held. Shared by <see cref="PolledKeyboard"/> and the raylib
/// and Sokol keyboards so every host repeats the same way.
/// </summary>
internal sealed class KeyRepeatTimer
{
    /// <summary>
    /// The delay before repeat starts, shared by every keyboard and by the
    /// <see cref="IInputReceiverKeyboard.RepeatDelay"/> default.
    /// </summary>
    internal static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// The interval between repeats, shared by every keyboard and by the
    /// <see cref="IInputReceiverKeyboard.RepeatRate"/> default.
    /// </summary>
    internal static readonly TimeSpan DefaultRate = TimeSpan.FromMilliseconds(70);

    private readonly Dictionary<GumKeys, double> _downSince = new();
    private readonly Dictionary<GumKeys, double> _lastRepeat = new();

    /// <summary>
    /// Starts timing <paramref name="key"/> as held from <paramref name="gameTime"/> (seconds).
    /// </summary>
    internal void OnKeyPushed(GumKeys key, double gameTime)
    {
        _downSince[key] = gameTime;
        _lastRepeat.Remove(key);
    }

    /// <summary>
    /// Stops timing <paramref name="key"/>. Does nothing if it was not being timed.
    /// </summary>
    internal void OnKeyReleased(GumKeys key)
    {
        _downSince.Remove(key);
        _lastRepeat.Remove(key);
    }

    /// <summary>
    /// Returns whether a held <paramref name="key"/> repeats at <paramref name="gameTime"/>: once
    /// <paramref name="delay"/> has passed since the push, then every <paramref name="rate"/>. Asking again
    /// for the same <paramref name="gameTime"/> gives the same answer, so a frame can query a key more than
    /// once. False for a key that is not being timed.
    /// </summary>
    internal bool IsRepeatDue(GumKeys key, double gameTime, TimeSpan delay, TimeSpan rate)
    {
        if (!_downSince.TryGetValue(key, out double downSince))
        {
            return false;
        }

        if (gameTime - downSince < delay.TotalSeconds)
        {
            return false;
        }

        if (_lastRepeat.TryGetValue(key, out double lastRepeat))
        {
            if (lastRepeat == gameTime)
            {
                return true;
            }

            if (gameTime - lastRepeat < rate.TotalSeconds)
            {
                return false;
            }
        }

        _lastRepeat[key] = gameTime;
        return true;
    }
}
