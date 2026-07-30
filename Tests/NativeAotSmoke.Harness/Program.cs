using System;
using Gum;
using Gum.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// End-to-end check for issue #4105: RegisterRuntimeTypesThroughReflection scanned every loaded
// assembly via unguarded reflection, which crashed GumService.Initialize under Native AOT when it
// touched trimmed framework metadata - a failure mode only a real `dotnet publish
// -p:PublishAot=true` reproduces, not `dotnet test` (which always runs JIT). This harness drives
// Gum's real Initialize pipeline - real GraphicsDevice, default-visuals construction, a real
// Texture2D upload - end to end, so a future regression anywhere in that path is caught
// automatically instead of waiting for another user report. The CI job forces software GL (Mesa
// llvmpipe) since the runners have no GPU. Exit code is the assertion.

try
{
    using SmokeGame game = new SmokeGame();
    game.RunOneFrame();

    Console.WriteLine($"[native-aot-smoke] Adapter = {game.GraphicsDevice.Adapter.Description}");

    Texture2D texture = new Texture2D(game.GraphicsDevice, 4, 4);
    Color[] data = new Color[16];
    Array.Fill(data, Color.CornflowerBlue);
    texture.SetData(data);

    Console.WriteLine("[native-aot-smoke] PASS: GumService.Initialize completed under Native AOT with a real GraphicsDevice.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[native-aot-smoke] FAIL: {ex.GetType().FullName}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}

sealed class SmokeGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public SmokeGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        base.Initialize();
        GumService.Default.Initialize(this, DefaultVisualsVersion.V2);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
    }
}
