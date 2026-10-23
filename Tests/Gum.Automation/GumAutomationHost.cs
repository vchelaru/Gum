using System.Collections.Concurrent;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gum.Automation;

/// <summary>
/// Plays an NDJSON script against a running MonoGame app from inside its own loop: the script runs
/// on a worker thread and the game thread hands it one frame at a time, so the app renders and pumps
/// window messages normally while every Gum input comes from the script.
/// </summary>
/// <remarks>
/// Wiring (debug/automation builds only):
/// <code>
/// // Game constructor:   _automation = GumAutomationHost.FromArgs(Environment.GetCommandLineArgs(), this);
/// // top of Update:      _automation?.BeforeUpdate();
/// </code>
/// Launch with <c>--gum-automation script.ndjson</c>; add <c>--gum-automation-hide</c> to park the
/// window off-screen without focus (Windows). The report is written to stdout and, with
/// <c>--gum-automation-report path</c>, to a file; the process exit code is 0 when every line passed.
/// The game thread is blocked while a script line runs, so lines may read Gum state safely.
/// </remarks>
public sealed class GumAutomationHost
{
    /// <summary>Followed by the path of the NDJSON script to play.</summary>
    public const string ScriptFlag = "--gum-automation";

    /// <summary>Parks the window off-screen and returns focus to the user (Windows).</summary>
    public const string HideFlag = "--gum-automation-hide";

    /// <summary>Followed by a path the report is also written to.</summary>
    public const string ReportFlag = "--gum-automation-report";

    private readonly string _script;
    private readonly object? _game;
    private readonly AutomationWindowParker? _parker;
    private readonly Action<GumScriptResult> _onFinished;
    private readonly BlockingCollection<Func<bool>> _work = new();
    private readonly SemaphoreSlim _frameDone = new(0);
    private bool _frameInFlight;
    private Thread? _worker;

    /// <summary>Creates a host. Prefer <see cref="FromArgs"/> for a real game.</summary>
    /// <param name="script">The NDJSON script text.</param>
    /// <param name="roots">The Gum roots searched by name, topmost first.</param>
    /// <param name="game">The game kept active while unfocused, or null.</param>
    /// <param name="parker">Parks the window each frame, or null to leave it alone.</param>
    /// <param name="onFinished">Runs on the game thread once the script ends.</param>
    public GumAutomationHost(string script, Func<IEnumerable<GraphicalUiElement?>> roots, object? game,
        AutomationWindowParker? parker, Action<GumScriptResult> onFinished)
    {
        _script = script;
        _game = game;
        _parker = parker;
        _onFinished = onFinished;
        Driver = new GumDriver(RequestFrame, roots);
    }

    /// <summary>
    /// A host for <paramref name="game"/> when <paramref name="args"/> contain <see cref="ScriptFlag"/>,
    /// otherwise null. Uses <see cref="GumService.Default"/>'s roots and a screenshot writer that renders
    /// Gum into a render target.
    /// </summary>
    public static GumAutomationHost? FromArgs(string[] args, Game game)
    {
        string? scriptPath = ValueAfter(args, ScriptFlag);
        if (scriptPath == null)
        {
            return null;
        }
        string? reportPath = ValueAfter(args, ReportFlag);
        AutomationWindowParker? parker = args.Contains(HideFlag) ? AutomationWindowParker.CreateForCurrentPlatform() : null;
        IntPtr foregroundAtStart = AutomationWindowParker.ForegroundWindow();
        if (parker != null)
        {
            HideBeforeFirstShow(game);
        }

        GumAutomationHost? host = null;
        host = new GumAutomationHost(File.ReadAllText(scriptPath), () => GumDriver.RootsOf(GumService.Default),
            game, parker, result =>
            {
                string report = result.ToString() + (result.Passed ? "AUTOMATION PASSED" : "AUTOMATION FAILED");
                if (parker != null)
                {
                    // Read in-process: the hidden run must leave the user's foreground window alone.
                    bool unchanged = AutomationWindowParker.ForegroundWindow() == foregroundAtStart;
                    report += Environment.NewLine + (unchanged ? "FOREGROUND UNCHANGED" : "FOREGROUND CHANGED");
                }
                Console.WriteLine(report);
                if (reportPath != null)
                {
                    File.WriteAllText(reportPath, report);
                }
                Environment.ExitCode = result.Passed ? 0 : 1;
                game.Exit();
            });
        host.Driver.ScreenshotWriter = path => host.RunOnGameThread(() => SaveGumFrame(game.GraphicsDevice, path));
        return host;
    }

    /// <summary>
    /// Keeps the window off-screen and unfocused from its first frame. MonoGame DesktopGL creates the
    /// SDL window hidden, sizes and centers it when the device is created, and shows it when the loop
    /// starts. SDL's <c>SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN</c> hint (read from the environment when
    /// the window is shown) stops that show from taking focus, and moving the window once the device
    /// exists puts it off-screen before it is shown.
    /// </summary>
    private static void HideBeforeFirstShow(Game game)
    {
        Environment.SetEnvironmentVariable("SDL_WINDOW_NO_ACTIVATION_WHEN_SHOWN", "1");
        Point offScreen = new Point(-32000, -32000);
        game.Window.Position = offScreen;
        if (game.Services.GetService(typeof(IGraphicsDeviceService)) is IGraphicsDeviceService graphics)
        {
            graphics.DeviceCreated += (_, _) => game.Window.Position = offScreen;
            graphics.DeviceReset += (_, _) => game.Window.Position = offScreen;
        }
    }

    /// <summary>The driver the script runs through.</summary>
    public GumDriver Driver { get; }

    /// <summary>The script's result once it has finished, otherwise null.</summary>
    public GumScriptResult? Result { get; private set; }

    /// <summary>Whether the script has finished.</summary>
    public bool IsFinished => Result != null;

    /// <summary>
    /// Call at the top of the game's Update, on the game thread. Returns once the script wants this
    /// frame to run (or has finished); until then the game thread runs the script's game-thread work.
    /// </summary>
    public void BeforeUpdate()
    {
        _parker?.Tick();
        if (_parker != null)
        {
            AutomationWindowParker.ForceActive(_game);
        }
        if (IsFinished)
        {
            return;
        }

        if (_worker == null)
        {
            Driver.Install();
            _worker = new Thread(RunScript) { IsBackground = true, Name = "Gum automation script" };
            _worker.Start();
        }
        else if (_frameInFlight)
        {
            _frameInFlight = false;
            _frameDone.Release();
        }

        // Each item returns true when the game thread should go on and run a frame.
        while (!_work.Take()())
        {
        }
    }

    private void RunScript()
    {
        GumScriptResult result = GumScriptRunner.Run(Driver, _script);
        _work.Add(() =>
        {
            Result = result;
            _onFinished(result);
            return true;
        });
    }

    private void RequestFrame()
    {
        _work.Add(() =>
        {
            _frameInFlight = true;
            return true;
        });
        _frameDone.Wait();
    }

    /// <summary>Runs <paramref name="action"/> on the game thread between frames and waits for it.</summary>
    public void RunOnGameThread(Action action)
    {
        Exception? error = null;
        using SemaphoreSlim done = new(0);
        _work.Add(() =>
        {
            try { action(); }
            catch (Exception exception) { error = exception; }
            done.Release();
            return false;
        });
        done.Wait();
        if (error != null)
        {
            throw new GumAutomationException($"{error.GetType().Name}: {error.Message}");
        }
    }

    private static void SaveGumFrame(GraphicsDevice device, string path)
    {
        PresentationParameters parameters = device.PresentationParameters;
        using RenderTarget2D target = new RenderTarget2D(device, parameters.BackBufferWidth, parameters.BackBufferHeight);
        device.SetRenderTarget(target);
        device.Clear(Color.Black);
        GumService.Default.Draw();
        device.SetRenderTarget(null);
        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (folder != null)
        {
            Directory.CreateDirectory(folder);
        }
        using FileStream stream = File.Create(path);
        target.SaveAsPng(stream, target.Width, target.Height);
    }

    private static string? ValueAfter(string[] args, string flag)
    {
        int index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
