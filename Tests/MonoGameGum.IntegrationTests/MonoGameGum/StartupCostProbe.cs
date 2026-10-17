using System.Diagnostics;
using Gum.Forms;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

// TEMPORARY (#6092): times each phase of creating a Game, drawing in it and tearing it down, so the
// per-test cost seen on CI can be attributed. Delete before the final PR.
public static class ProbeLog
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static long _last;

    // Appends "label: ms since the previous mark" to the probe file.
    public static void Mark(string label)
    {
        long now = Clock.ElapsedMilliseconds;
        string directory = Path.Combine(
            Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? ".", "TestResults");
        Directory.CreateDirectory(directory);
        File.AppendAllText(Path.Combine(directory, "marks.txt"), $"{label}: {now - _last} ms\n");
        _last = now;
    }
}

public class StartupCostProbe : BaseTestClass
{
    [Fact]
    public void ProbeStartupPhases()
    {
        List<string> lines = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            Stopwatch total = Stopwatch.StartNew();
            ProbeGame game = new ProbeGame();
            game.RunOneFrame();
            long created = total.ElapsedMilliseconds;

            long Phase(Action action)
            {
                long start = total.ElapsedMilliseconds;
                action();
                return total.ElapsedMilliseconds - start;
            }

#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
            ColoredRectangleRuntime rectangle = new ColoredRectangleRuntime { Width = 50, Height = 50 };
#pragma warning restore CS0618
            Gum.GumService.Default.Root.AddChild(rectangle);
            long firstRectFrame = Phase(() => game.RunOneFrame());
            long secondRectFrame = Phase(() => game.RunOneFrame());

            long shapeInit = Phase(() => ShapeRenderer.Self.Initialize(game.GraphicsDevice, game.Content));
            CircleRuntime circle = new CircleRuntime { Width = 50, Height = 50, X = 60 };
            Gum.GumService.Default.Root.AddChild(circle);
            long firstCircleFrame = Phase(() => game.RunOneFrame());
            long secondCircleFrame = Phase(() => game.RunOneFrame());

            ContainerRuntime renderTarget = new ContainerRuntime { Width = 50, Height = 50, Y = 60, IsRenderTarget = true };
#pragma warning disable CS0618
            renderTarget.AddChild(new ColoredRectangleRuntime { Width = 50, Height = 50 });
#pragma warning restore CS0618
            Gum.GumService.Default.Root.AddChild(renderTarget);
            long firstTargetFrame = Phase(() => game.RunOneFrame());
            long secondTargetFrame = Phase(() => game.RunOneFrame());

            TextRuntime text = new TextRuntime { Text = "Hello", Y = 120 };
            Gum.GumService.Default.Root.AddChild(text);
            long firstTextFrame = Phase(() => game.RunOneFrame());
            long secondTextFrame = Phase(() => game.RunOneFrame());

            Gum.Forms.Controls.Button button = new Gum.Forms.Controls.Button();
            long buttonCreate = Phase(() => Gum.GumService.Default.Root.AddChild(button.Visual));
            long firstButtonFrame = Phase(() => game.RunOneFrame());
            long secondButtonFrame = Phase(() => game.RunOneFrame());

            long dispose = Phase(() => game.Dispose());

            lines.Add(
                $"iteration {i}: text frame1={firstTextFrame} frame2={secondTextFrame} | button create={buttonCreate} frame1={firstButtonFrame} frame2={secondButtonFrame} | " +
                $"create+init={created} ms | rect frame1={firstRectFrame} frame2={secondRectFrame} | " +
                $"ShapeRenderer.Initialize={shapeInit} | circle frame1={firstCircleFrame} frame2={secondCircleFrame} | " +
                $"render target frame1={firstTargetFrame} frame2={secondTargetFrame} | dispose={dispose} | total={total.ElapsedMilliseconds} ms");
        }

        string directory = Path.Combine(
            Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? ".", "TestResults");
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, "startup-probe.txt"), lines);
    }

    private class ProbeGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public ProbeGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this) { GraphicsProfile = GraphicsProfile.HiDef };
        }

        protected override void Initialize()
        {
            base.Initialize();
            Gum.GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
        }

        protected override void Update(GameTime gameTime) { }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            Gum.GumService.Default.Draw();
        }

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
