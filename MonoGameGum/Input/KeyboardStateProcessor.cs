using Microsoft.Xna.Framework.Input;
using System;

namespace Gum.Input;

// Shared by link with the KNI Skia host (Runtimes/KniSkiaGum), which mirrors KniGum's keyboard API.
// Keep it free of #if platform symbols: the KNI Skia project defines none of them.

/// <summary>
/// Wraps MonoGame's <see cref="KeyboardState"/> to provide per-frame pushed, released, and held-down
/// queries. Typically not used directly — <see cref="Keyboard"/> owns an instance and delegates all
/// raw state reads to it. The property <see cref="Keyboard.KeyboardStateProcessor"/> is exposed
/// publicly to allow substitution in unit tests or advanced input scenarios.
/// </summary>
public class KeyboardStateProcessor
{
    KeyboardState mLastFrameKeyboardState = new KeyboardState();
    KeyboardState mKeyboardState;

    /// <summary>
    /// Reads the device in <see cref="Update"/>. Replaced in tests; <c>Keyboard.GetState</c> is a static device read.
    /// </summary>
    internal Func<KeyboardState> ReadKeyboardState { get; set; } = Microsoft.Xna.Framework.Input.Keyboard.GetState;

    /// <summary>
    /// Returns whether any key was pushed (down this frame, not down last frame).
    /// </summary>
    public virtual bool AnyKeyPushed()
    {
        // loop through all pressed keys...
        for (int i = 0; i < Keyboard.NumberOfKeys; i++)
        {
            // And see if it's pushed (was not down this frame, is down this frame)
            if (KeyPushed((Keys)i))
            {
                // if so, we can return true
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Clears the keyboard states, simulating the keyboard
    /// not having any values down or pressed
    /// </summary>
    public virtual void Clear()
    {
        mKeyboardState = new KeyboardState();
        mLastFrameKeyboardState = new KeyboardState();
    }

    /// <summary>
    /// Returns whether the specified key is currently held down.
    /// </summary>
    /// <param name="key">The key to query.</param>
    public virtual bool IsKeyDown(Keys key)
    {
        return mKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Returns whether the specified key was pressed this frame (down this frame, not down last frame).
    /// </summary>
    /// <param name="key">The key to query.</param>
    public virtual bool KeyPushed(Keys key)
    {
        return mKeyboardState.IsKeyDown(key) &&
            !mLastFrameKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Returns whether the specified key was released this frame (down last frame, not down this frame).
    /// </summary>
    /// <param name="key">The key to query.</param>
    public virtual bool KeyReleased(Keys key)
    {
        return mLastFrameKeyboardState.IsKeyDown(key) &&
            !mKeyboardState.IsKeyDown(key);
    }

    /// <summary>
    /// Advances the processor by one frame, capturing the current <see cref="KeyboardState"/>.
    /// Called internally by <c>Keyboard.Activity</c> — not typically called directly.
    /// </summary>
    public virtual void Update()
    {
        mLastFrameKeyboardState = mKeyboardState;
        mKeyboardState = ReadKeyboardState();
    }

}
