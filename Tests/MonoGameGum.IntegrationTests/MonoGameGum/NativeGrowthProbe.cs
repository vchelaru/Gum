using System.Diagnostics;
using System.Text;
using Gum.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

// TEMPORARY (#6092): separates what a bare MonoGame Game leaves behind from what Gum leaves behind,
// as private-memory growth per iteration. Dropped before the PR.
public class NativeGrowthProbe : BaseTestClass
{
    private const int Iterations = 12;

    [Fact]
    public void MeasureGrowth()
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("A: bare Game (no Gum): create, one frame, dispose");
        Measure(report, () =>
        {
            using BareGame game = new BareGame(withGum: false);
            game.RunOneFrame();
        });

        report.AppendLine("B: Game + Gum Initialize + one frame + Gum Uninitialize, dispose");
        Measure(report, () =>
        {
            using BareGame game = new BareGame(withGum: true);
            game.RunOneFrame();
            game.RunOneFrame();
            Gum.GumService.Default.Uninitialize();
        });

        report.AppendLine("C: one shared Game, repeated Gum Initialize + frame + Uninitialize");
        using (BareGame shared = new BareGame(withGum: false))
        {
            shared.RunOneFrame();
            Measure(report, () =>
            {
                Gum.GumService.Default.Initialize(shared, DefaultVisualsVersion.V3);
                shared.RunOneFrame();
                Gum.GumService.Default.Uninitialize();
                LoaderManager.Self?.DisposeAndClear();
            });
        }

        // Is a disposed Game (and its device) still reachable after Gum used it?
        (WeakReference gameReference, WeakReference deviceReference) = CreateAndDisposeGameWithGum();
        for (int g = 0; g < 4; g++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        report.AppendLine($"D: disposed Game used with Gum still alive: {gameReference.IsAlive}; its GraphicsDevice alive: {deviceReference.IsAlive}");
        if (gameReference.Target is { } liveGame)
        {
            report.AppendLine(ProbeLog.PathTo(liveGame) ?? "(no static path found)");
        }
        else if (deviceReference.Target is { } liveDevice)
        {
            report.AppendLine(ProbeLog.PathTo(liveDevice) ?? "(no static path found)");
        }

        string directory = Path.Combine(
            Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? ".", "TestResults");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "native-growth.txt"), report.ToString());
    }

    private static (WeakReference, WeakReference) CreateAndDisposeGameWithGum()
    {
        BareGame game = new BareGame(withGum: true);
        game.RunOneFrame();
        game.RunOneFrame();
        WeakReference gameReference = new WeakReference(game);
        WeakReference deviceReference = new WeakReference(game.GraphicsDevice);
        Gum.GumService.Default.Uninitialize();
        game.Dispose();
        return (gameReference, deviceReference);
    }

    private static void Measure(StringBuilder report, Action iteration)
    {
        List<long> samples = new List<long>();
        for (int i = 0; i < Iterations; i++)
        {
            iteration();
            for (int g = 0; g < 3; g++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            using Process process = Process.GetCurrentProcess();
            samples.Add(process.PrivateMemorySize64 / (1024 * 1024));
        }
        report.AppendLine("   private MB after each iteration: " + string.Join(" ", samples));
        report.AppendLine($"   growth per iteration over the last {Iterations - 2}: {(samples[^1] - samples[1]) / (double)(Iterations - 2):F1} MB");
    }

    private sealed class BareGame : Game
    {
        private readonly bool _withGum;

        public BareGame(bool withGum)
        {
            _withGum = withGum;
            LoaderManager.Self?.DisposeAndClear();
            new GraphicsDeviceManager(this) { GraphicsProfile = GraphicsProfile.HiDef };
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (_withGum)
            {
                Gum.GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
            }
        }

        protected override void Update(GameTime gameTime) { }

        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

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
