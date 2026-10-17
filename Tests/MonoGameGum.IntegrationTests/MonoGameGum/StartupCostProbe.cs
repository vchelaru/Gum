using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

// TEMPORARY (#6092): times each phase of creating and tearing down a Game, so the per-test floor
// seen on CI (~2.1 s) can be attributed. Delete before the final PR.
public class StartupCostProbe : BaseTestClass
{
    [Fact]
    public void ProbeStartupPhases()
    {
        List<string> lines = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            Stopwatch total = Stopwatch.StartNew();
            ProbeGame game = new ProbeGame();
            long ctor = total.ElapsedMilliseconds;
            game.RunOneFrame();
            long runOneFrame = total.ElapsedMilliseconds;
            game.Dispose();
            long dispose = total.ElapsedMilliseconds;

            lines.Add(
                $"iteration {i}: ctor={ctor} ms, RunOneFrame={runOneFrame - ctor} ms " +
                $"(of which base.Initialize={game.BaseInitializeMs}, GumService.Initialize={game.GumInitializeMs}), " +
                $"Dispose={dispose - runOneFrame} ms, total={dispose} ms");
        }

        string directory = Path.Combine(
            Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? ".", "TestResults");
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "startup-probe.txt"), lines);
    }

    private class ProbeGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public long BaseInitializeMs;
        public long GumInitializeMs;

        public ProbeGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this) { GraphicsProfile = GraphicsProfile.HiDef };
        }

        protected override void Initialize()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            base.Initialize();
            BaseInitializeMs = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            Gum.GumService.Default.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
            GumInitializeMs = stopwatch.ElapsedMilliseconds;
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.Black);

        protected override void Dispose(bool disposing)
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
