using System;
using Gum;
using Gum.DataTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// End-to-end check for issue #4105 (RegisterRuntimeTypesThroughReflection scanned every loaded
// assembly via unguarded reflection, crashing GumService.Initialize under Native AOT when it
// touched trimmed framework metadata) and issue #4180 (GumProjectSave.Load's JSON path, added for
// the Native AOT XmlSerializer crash in #4170/#4171, had never been proven under a real AOT
// publish - every prior check ran via `dotnet test`, which is always JIT). Both failure modes only
// a real `dotnet publish -p:PublishAot=true` reproduces. This harness drives Gum's real Initialize
// pipeline - real GraphicsDevice, loading a real .gumj project and its referenced standard
// elements - end to end, so a future regression anywhere in that path is caught automatically
// instead of waiting for another user report. The CI job forces software GL (Mesa llvmpipe) since
// the runners have no GPU. Exit code is the assertion.

try
{
    using SmokeGame game = new SmokeGame();
    game.RunOneFrame();

    Console.WriteLine($"[native-aot-smoke] Adapter = {game.GraphicsDevice.Adapter.Description}");

    GumProjectSave? loadedProject = game.LoadedProject;
    if (loadedProject == null)
    {
        throw new InvalidOperationException("GumService.Initialize did not return a loaded project.");
    }
    if (loadedProject.StandardElements.Count != 8)
    {
        throw new InvalidOperationException(
            $"Expected 8 standard elements from the JSON project, found {loadedProject.StandardElements.Count}.");
    }

    Texture2D texture = new Texture2D(game.GraphicsDevice, 4, 4);
    Color[] data = new Color[16];
    Array.Fill(data, Color.CornflowerBlue);
    texture.SetData(data);

    Console.WriteLine(
        "[native-aot-smoke] PASS: GumService.Initialize loaded a real .gumj project " +
        $"({loadedProject.StandardElements.Count} standard element(s)) under Native AOT with a real GraphicsDevice.");
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

    public GumProjectSave? LoadedProject { get; private set; }

    public SmokeGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        base.Initialize();
        LoadedProject = GumService.Default.Initialize(this, "GumProject/GumProject.gumj");
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
    }
}
