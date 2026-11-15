#nullable enable
using Gum;
using Gum.Forms.Controls;
using GumSamples.Benchmark;
using Microsoft.Xna.Framework;

namespace MonoGameGumFormsSample;

/// <summary>Runs the shared benchmark scene (#8052) on MonoGame DesktopGL, then exits. Gum is initialized with no
/// .gumx, like the other hosts' code-built benchmark, so the scene uses the default Forms visuals.</summary>
internal class BenchmarkGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly bool _hidden;
    private readonly string? _outputPath;

    public BenchmarkGame(string[] args)
    {
        int index = Array.IndexOf(args, "--benchmark");
        _hidden = args.Contains("--hidden");
        // Resolved against the launch directory, which is where the caller expects the file.
        _outputPath = index + 1 < args.Length && !args[index + 1].StartsWith("--") ? Path.GetFullPath(args[index + 1]) : Path.GetFullPath("benchmark-result.md"); // WinExe: stdout is invisible, so with no path the row goes to benchmark-result.md in the launch directory
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = false,
        };
        IsFixedTimeStep = false;
    }

    protected override void Initialize()
    {
        BenchmarkRunner.RequireRelease();
        if (_hidden)
        {
            BenchmarkHost.HideWindow(Window.Handle);
        }
        GumService gumUI = GumService.Default;
        gumUI.Initialize(this);
        BenchmarkScreen screen = new BenchmarkScreen();
        screen.AddToRoot();
        Console.WriteLine("GL_RENDERER: " + BenchmarkHost.Renderer());
        BenchmarkRunner.Run("MonoGame (DesktopGL)", screen,
            update: seconds => gumUI.Update(new GameTime(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(1.0 / 60))),
            draw: () =>
            {
                GraphicsDevice.Clear(Color.CornflowerBlue);
                gumUI.Draw();
            },
            sync: () =>
            {
                GraphicsDevice.Present();
                BenchmarkHost.Finish();
            },
            _outputPath);
        Exit();
    }

    protected override void Update(GameTime gameTime) { } // the run finished inside Initialize
    protected override void Draw(GameTime gameTime) { }
}
