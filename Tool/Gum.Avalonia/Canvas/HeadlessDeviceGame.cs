using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gum.Avalonia.Canvas;

/// <summary>
/// The process-wide KNI graphics device for the Avalonia head, owned by a <see cref="Game"/> with
/// a 1x1 window that is never presented to. The game is never run: it is initialized once, on the
/// UI thread, and from then on its device is used directly to draw into render targets that the
/// canvases read back. This is the path phase 10's spike proved on the SDL2/GL platform; a
/// window-handle device the way the WPF head creates one does not exist off Windows.
/// </summary>
internal sealed class HeadlessDeviceGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private bool _isInitialized;

    /// <summary>Creates the game; the device is created by <see cref="EnsureInitialized"/>.</summary>
    public HeadlessDeviceGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1,
            PreferredBackBufferHeight = 1,
            // The tool's shapes use an SM4 effect that the Reach profile cannot load.
            GraphicsProfile = GraphicsProfile.HiDef,
            SynchronizeWithVerticalRetrace = false,
        };
        IsFixedTimeStep = false;
        IsMouseVisible = true;
        // The hidden window is never the active window, and the XNA default sleeps 20 ms per
        // tick while inactive.
        InactiveSleepTime = TimeSpan.Zero;
    }

    /// <summary>
    /// Initializes the platform and creates the device by running exactly one frame. Safe to call
    /// more than once.
    /// </summary>
    public void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }
        RunOneFrame();
        _isInitialized = true;
    }

    /// <inheritdoc/>
    protected override void Update(GameTime gameTime)
    {
        // Nothing to update: the canvases own their per-frame logic.
    }

    /// <inheritdoc/>
    protected override void Draw(GameTime gameTime)
    {
        // Nothing to draw: the canvases draw into their own render targets.
    }

    /// <inheritdoc/>
    protected override void EndDraw()
    {
        // Never present the hidden window; presenting is where vsync would block.
    }
}
