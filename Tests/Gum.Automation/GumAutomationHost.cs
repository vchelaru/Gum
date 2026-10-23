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
    /// <param name="args">The process arguments.</param>
    /// <param name="game">The game the script drives.</param>
    /// <param name="extraRoots">
    /// Elements searched by name after GumService's roots, for visuals added with
    /// <c>AddToManagers</c> instead of <c>AddToRoot</c>. Enumerated on each search, so a list filled later works.
    /// </param>
    public static GumAutomationHost? FromArgs(string[] args, Game game, IEnumerable<GraphicalUiElement>? extraRoots = null)
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
        host = new GumAutomationHost(File.ReadAllText(scriptPath), () => GumDriver.RootsOf(GumService.Default).Concat(extraRoots ?? Enumerable.Empty<GraphicalUiElement>()),
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
#if KNISKIA
        // KNI's GameWindow has no Position, but its desktop backend is SDL too: it creates the SDL
        // window hidden with the device, pulls an off-screen position back onto a display when the
        // device change ends (after DeviceCreated), and shows the window when the loop starts.
        // Window.Handle is the SDL window, so it is moved through SDL from a component's Initialize,
        // which runs after the device change and before the show.
        Action moveOffScreen = () => SdlMoveOffScreen(game.Window.Handle);
        game.Components.Add(new RunOnInitialize(game, moveOffScreen));
#else
        Point offScreen = new Point(-32000, -32000);
        Action moveOffScreen = () => game.Window.Position = offScreen;
#endif
        moveOffScreen();
        if (game.Services.GetService(typeof(IGraphicsDeviceService)) is IGraphicsDeviceService graphics)
        {
            graphics.DeviceCreated += (_, _) => moveOffScreen();
            graphics.DeviceReset += (_, _) => moveOffScreen();
        }
    }

#if KNISKIA
    private static void SdlMoveOffScreen(IntPtr sdlWindow)
    {
        if (sdlWindow == IntPtr.Zero)
        {
            return;
        }
        SdlSetWindowPosition? setPosition = LoadSdlSetWindowPosition();
        if (setPosition == null)
        {
            // The parker still moves the window on the first Update; only the pre-show move is lost.
            Console.Error.WriteLine("Gum automation: SDL_SetWindowPosition was not found, so the window " +
                "is parked from the first Update instead of before it is shown.");
            return;
        }
        setPosition(sdlWindow, -32000, -32000);
    }

    [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
    private delegate void SdlSetWindowPosition(IntPtr window, int x, int y);

    private static SdlSetWindowPosition? _sdlSetWindowPosition;

    // KNI's SDL binding is internal, so the SDL2 library KNI loaded is resolved by its platform name.
    private static SdlSetWindowPosition? LoadSdlSetWindowPosition()
    {
        if (_sdlSetWindowPosition != null)
        {
            return _sdlSetWindowPosition;
        }
        string[] names = OperatingSystem.IsWindows() ? new[] { "SDL2.dll", "SDL2" }
            : OperatingSystem.IsMacOS() ? new[] { "libSDL2-2.0.0.dylib", "libSDL2.dylib", "SDL2" }
            : new[] { "libSDL2-2.0.so.0", "libSDL2.so", "SDL2" };
        foreach (string name in names)
        {
            if (System.Runtime.InteropServices.NativeLibrary.TryLoad(name, typeof(GumAutomationHost).Assembly, null, out IntPtr library)
                && System.Runtime.InteropServices.NativeLibrary.TryGetExport(library, "SDL_SetWindowPosition", out IntPtr export))
            {
                _sdlSetWindowPosition = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<SdlSetWindowPosition>(export);
                return _sdlSetWindowPosition;
            }
        }
        return null;
    }

    private sealed class RunOnInitialize : GameComponent
    {
        private readonly Action _action;

        public RunOnInitialize(Game game, Action action) : base(game) => _action = action;

        public override void Initialize()
        {
            _action();
            base.Initialize();
        }
    }

#endif
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
#if KNISKIA
        // Gum draws into any SKCanvas, so the frame is rendered into an offscreen Skia surface. The
        // game's own canvas (or none, before the game's first Draw) is put back afterwards.
        global::RenderingLibrary.SystemManagers managers = global::RenderingLibrary.SystemManagers.Default;
        SkiaSharp.SKCanvas? previousCanvas = null;
        try
        {
            previousCanvas = managers.Canvas;
        }
        catch (InvalidOperationException)
        {
            // The getter throws until a canvas is assigned; the game has not drawn yet.
        }
        using SkiaSharp.SKSurface surface = SkiaSharp.SKSurface.Create(
            new SkiaSharp.SKImageInfo(parameters.BackBufferWidth, parameters.BackBufferHeight));
        surface.Canvas.Clear(SkiaSharp.SKColors.Black);
        try
        {
            GumService.Default.Draw(surface.Canvas);
        }
        finally
        {
            managers.Canvas = previousCanvas!;
        }
        CreateFolderOf(path);
        using SkiaSharp.SKImage image = surface.Snapshot();
        using SkiaSharp.SKData png = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        using FileStream stream = File.Create(path);
        png.SaveTo(stream);
#else
        using RenderTarget2D target = new RenderTarget2D(device, parameters.BackBufferWidth, parameters.BackBufferHeight);
        device.SetRenderTarget(target);
        device.Clear(Color.Black);
        GumService.Default.Draw();
        device.SetRenderTarget(null);
        CreateFolderOf(path);
        using FileStream stream = File.Create(path);
        target.SaveAsPng(stream, target.Width, target.Height);
#endif
    }

    private static void CreateFolderOf(string path)
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
        if (folder != null)
        {
            Directory.CreateDirectory(folder);
        }
    }

    private static string? ValueAfter(string[] args, string flag)
    {
        int index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
