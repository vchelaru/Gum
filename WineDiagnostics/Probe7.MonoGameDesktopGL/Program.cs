using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Probe.Common;

namespace Probe7.MonoGameDesktopGL;

/// <summary>
/// Probe 7 - the MonoGame OpenGL path. MonoGame.Framework.DesktopGL renders via SDL2 + OpenGL, a
/// completely different graphics path from the tool's KNI Direct3D 11. Direct3D 11 under Wine on
/// macOS is limited to a low feature level (9_3), which is below what the tool asks for; OpenGL may
/// not have that ceiling. If KNI DX11 (Probe6) fails but this PASSES, moving the Gum tool to an
/// OpenGL backend is a viable fix for macOS/Wine.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        return ProbeLog.Run("Probe7.MonoGameDesktopGL", () =>
        {
            ProbeLog.Step("Starting MonoGame DesktopGL Game (creates an SDL2 window + OpenGL device)");
            using ProbeGame game = new ProbeGame();
            game.Run();
            ProbeLog.Step("Game.Run returned cleanly");
        });
    }
}

/// <summary>Minimal MonoGame game that creates an OpenGL device, draws a few frames, and exits.</summary>
internal sealed class ProbeGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private int _frames;

    public ProbeGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            GraphicsProfile = GraphicsProfile.HiDef,
            PreferredBackBufferWidth = 640,
            PreferredBackBufferHeight = 480,
        };
        IsFixedTimeStep = true;
    }

    /// <inheritdoc/>
    protected override void Initialize()
    {
        // Reaching Initialize means the OpenGL GraphicsDevice was created successfully.
        ProbeLog.Step("Game.Initialize - OpenGL GraphicsDevice created");
        ProbeLog.Info("Backend", "MonoGame DesktopGL (SDL2 / OpenGL)");
        ProbeLog.Info("Adapter", GraphicsAdapter.DefaultAdapter.Description ?? "(null)");
        base.Initialize();
    }

    /// <inheritdoc/>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        base.Draw(gameTime);

        _frames++;
        if (_frames == 1)
        {
            ProbeLog.Step("First frame drawn via OpenGL");
        }
        if (_frames >= 5)
        {
            Exit();
        }
    }
}
