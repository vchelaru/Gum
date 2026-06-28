using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Probe.Common;

namespace Probe9.KniDesktopGL;

/// <summary>
/// Probe 9 - can KNI itself run over OpenGL? The tool uses nkast.Kni.Platform.WinForms.DX11
/// (Direct3D 11), which fails under Wine on macOS. KNI also ships nkast.Kni.Platform.SDL2.GL
/// (SDL2 + OpenGL). If this PASSES, the tool could keep KNI and just swap the platform package -
/// the smallest possible fix - rather than migrating to MonoGame.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main()
    {
        return ProbeLog.Run("Probe9.KniDesktopGL", () =>
        {
            ProbeLog.Step("Starting KNI SDL2.GL Game (creates an SDL2 window + OpenGL device)");
            using ProbeGame game = new ProbeGame();
            game.Run();
            ProbeLog.Step("Game.Run returned cleanly");
        });
    }
}

/// <summary>Minimal KNI game on the SDL2/OpenGL platform that draws a few frames and exits.</summary>
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
        ProbeLog.Step("Game.Initialize - KNI OpenGL GraphicsDevice created");
        ProbeLog.Info("Backend", "KNI SDL2.GL (OpenGL)");
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
            ProbeLog.Step("First frame drawn via KNI OpenGL");
        }
        if (_frames >= 5)
        {
            Exit();
        }
    }
}
