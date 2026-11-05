using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace Gum.RenderTarget.Benchmarks;

/// <summary>
/// Runs every scenario in <see cref="ScenarioRegistry"/> for every config, one config after another, drawing
/// the scene into an offscreen render target so the whole grid is on screen whatever the window size.
/// Rendering cost only: Gum Update is never called and the scene is static.
/// </summary>
public sealed class BenchGame : Game
{
    private enum Phase
    {
        Idle,
        Warmup,
        Measure,
        MetricsFrame,
    }

    private static readonly BenchConfig[] Configs =
    {
        new BenchConfig(100, 32),
        new BenchConfig(100, 64),
        new BenchConfig(1000, 32),
        new BenchConfig(1000, 64),
    };

    private readonly BenchOptions _options;
    private readonly GraphicsDeviceManager _graphics;
    private readonly Stopwatch _runClock = Stopwatch.StartNew();
    private readonly List<ResultRow> _results = new List<ResultRow>();
    private readonly Queue<(Func<IBenchScenario> Factory, BenchConfig Config)> _work = new Queue<(Func<IBenchScenario>, BenchConfig)>();
    private readonly string _logPath;

    private GumService _gum = null!;
    private Texture2D _spriteTexture = null!;
    private RenderTarget2D? _scene;

    private Phase _phase = Phase.Idle;
    private IBenchScenario? _scenario;
    private BenchConfig _config;
    private int _frame;
    private readonly List<long> _starts = new List<long>();
    private readonly List<double> _drawMs = new List<double>();
    private readonly List<int> _gen0PerFrame = new List<int>();
    private long _allocStart;
    private int _gen0Start;
    private int _gen2Start;
    private bool _aborted;
    private double _setupMs;

    public int ExitCode { get; private set; }

    public BenchGame(BenchOptions options)
    {
        _options = options;
        _logPath = Path.Combine(options.OutputFolder, "log.txt");
        File.WriteAllText(_logPath, "");

        _graphics = new GraphicsDeviceManager(this);
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        _graphics.PreferredBackBufferWidth = 320;
        _graphics.PreferredBackBufferHeight = 240;
        _graphics.SynchronizeWithVerticalRetrace = false;
        _graphics.PreparingDeviceSettings += (_, e) =>
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        };

        IsFixedTimeStep = false;
        // The default sleeps 20 ms per frame while the window is inactive, which an off-screen window is.
        InactiveSleepTime = TimeSpan.Zero;
        Window.IsBorderless = true;
    }

    private void Log(string message)
    {
        File.AppendAllText(_logPath, $"[{_runClock.Elapsed.TotalSeconds,7:0.0}s] {message}\n");
    }

    protected override void Initialize()
    {
        Window.Position = new Point(-30000, -30000);

        _gum = GumService.Default;
        _gum.Initialize(this);
        // The same registration a game makes to load Outline.slang through SourceShaderFile.
        _gum.UseShadowDusk();

        _spriteTexture = CreateSpriteTexture(GraphicsDevice);

        foreach (Func<IBenchScenario> factory in ScenarioRegistry.Factories)
        {
            foreach (BenchConfig config in Configs)
            {
                _work.Enqueue((factory, config));
            }
        }

        Log("Adapter: " + GraphicsDevice.Adapter.Description + " | profile " + GraphicsDevice.GraphicsProfile);
        base.Initialize();
    }

    /// <summary>A 16x16 blob with a transparent border, the same texture for every sprite.</summary>
    private static Texture2D CreateSpriteTexture(GraphicsDevice device)
    {
        const int size = 16;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                if ((dx * dx) + (dy * dy) <= 5.5f * 5.5f)
                {
                    pixels[(y * size) + x] = new Color(60 + (x * 12), 90 + (y * 8), 220 - (x * 6), 255);
                }
            }
        }

        Texture2D texture = new Texture2D(device, size, size);
        texture.SetData(pixels);
        return texture;
    }

    private bool BeginNext()
    {
        if (_work.Count == 0)
        {
            return false;
        }

        (Func<IBenchScenario> factory, BenchConfig config) = _work.Dequeue();
        _scenario = factory();
        _config = config;

        _scene?.Dispose();
        _scene = new RenderTarget2D(GraphicsDevice, config.SceneSize, config.SceneSize, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        Log($"Build {_scenario.Id} {config} (scene {config.SceneSize}px)");
        long buildStart = Stopwatch.GetTimestamp();
        _scenario.Build(_gum, config, _spriteTexture);
        _setupMs = ToMs(Stopwatch.GetTimestamp() - buildStart);

        _frame = 0;
        _starts.Clear();
        _drawMs.Clear();
        _gen0PerFrame.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        _phase = Phase.Warmup;
        return true;
    }

    protected override void Draw(GameTime gameTime)
    {
        long start = Stopwatch.GetTimestamp();

        if (_runClock.Elapsed.TotalSeconds > _options.TimeoutSeconds)
        {
            Log("TIMEOUT: stopping with " + _results.Count + " finished configs");
            _aborted = true;
            ExitCode = 2;
            Finish();
            return;
        }

        if (_phase == Phase.Idle && !BeginNext())
        {
            Finish();
            return;
        }

        IBenchScenario scenario = _scenario!;
        scenario.BeforeDraw(_frame);

        GraphicsDevice.SetRenderTarget(_scene);
        GraphicsDevice.Clear(new Color(40, 40, 40, 255));

        GraphicsMetrics metricsBefore = GraphicsDevice.Metrics;

        long drawStart = Stopwatch.GetTimestamp();
        _gum.Draw();
        long drawEnd = Stopwatch.GetTimestamp();

        GraphicsMetrics metricsAfter = GraphicsDevice.Metrics;
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        switch (_phase)
        {
            case Phase.Warmup:
                _frame++;
                if (_frame >= _options.WarmupFrames)
                {
                    _frame = 0;
                    _allocStart = GC.GetAllocatedBytesForCurrentThread();
                    _gen0Start = GC.CollectionCount(0);
                    _gen2Start = GC.CollectionCount(2);
                    _phase = Phase.Measure;
                }

                break;

            case Phase.Measure:
                _starts.Add(start);
                _drawMs.Add(ToMs(drawEnd - drawStart));
                _gen0PerFrame.Add(GC.CollectionCount(0));
                _frame++;
                if (_frame >= _options.MeasuredFrames)
                {
                    _phase = Phase.MetricsFrame;
                }

                break;

            case Phase.MetricsFrame:
                // Its start-of-Draw ends the last measured frame; it is not itself a sample.
                _starts.Add(start);
                RecordConfig(metricsBefore, metricsAfter);
                _phase = Phase.Idle;
                break;
        }

        base.Draw(gameTime);
    }

    private void RecordConfig(GraphicsMetrics before, GraphicsMetrics after)
    {
        long allocBytes = GC.GetAllocatedBytesForCurrentThread() - _allocStart;
        int gen0 = GC.CollectionCount(0) - _gen0Start;
        int gen2 = GC.CollectionCount(2) - _gen2Start;

        // Wait for the GPU: reading one pixel back cannot return until everything before it has run.
        Color[] pixel = new Color[1];
        _scene!.GetData(0, new Rectangle(0, 0, 1, 1), pixel, 0, 1);
        long drained = Stopwatch.GetTimestamp();

        List<double> frameMs = new List<double>();
        for (int i = 0; i < _options.MeasuredFrames; i++)
        {
            frameMs.Add(ToMs(_starts[i + 1] - _starts[i]));
        }

        Renderer renderer = _gum.Renderer;
        ResultRow row = new ResultRow
        {
            Scenario = _scenario!.Id,
            Config = _config,
            MeasuredFrames = _options.MeasuredFrames,
            DrawCpuMs = FrameStats.From(_drawMs),
            FrameMs = FrameStats.From(frameMs),
            DrainedWallMsPerFrame = ToMs(drained - _starts[0]) / (_options.MeasuredFrames + 1),
            GlDrawCalls = after.DrawCount - before.DrawCount,
            GlClears = after.ClearCount - before.ClearCount,
            GlTargetSwitches = after.TargetCount - before.TargetCount,
            GumDrawCallCount = renderer.RenderStateChangeStatistics.DrawCallCount,
            GumSpriteBatchBegins = renderer.SpriteRenderer.LastFrameDrawStates.Count(),
            GumShapeBatchBegins = renderer.RenderStateChangeStatistics.ShapeBatchBeginCount,
            AllocBytesPerFrame = (double)allocBytes / _options.MeasuredFrames,
            SetupMs = _setupMs,
            Gen0Collections = gen0,
            Gen2Collections = gen2,
        };
        _results.Add(row);

        // Per-frame samples, to see whether slow frames cluster or line up with garbage collections.
        string framesPath = Path.Combine(_options.OutputFolder, "frames.csv");
        if (!File.Exists(framesPath))
        {
            File.WriteAllText(framesPath, "scenario,config,frame,draw_ms,frame_ms,gen0_count\n");
        }

        File.AppendAllLines(framesPath, Enumerable.Range(0, _options.MeasuredFrames).Select(i =>
            $"{row.Scenario},{_config},{i},{_drawMs[i].ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}," +
            $"{frameMs[i].ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)},{_gen0PerFrame[i]}"));

        Log($"Done {row.Scenario} {_config}: draw median {row.DrawCpuMs.Median:0.000} ms, frame median {row.FrameMs.Median:0.000} ms, " +
            $"drained {row.DrainedWallMsPerFrame:0.000} ms, GL draws {row.GlDrawCalls}, targets {row.GlTargetSwitches}");

        if (_config.SpriteCount == 100)
        {
            string folder = Path.Combine(_options.OutputFolder, "screenshots");
            Directory.CreateDirectory(folder);
            using FileStream stream = File.Create(Path.Combine(folder, $"{row.Scenario}_{_config}.png"));
            _scene.SaveAsPng(stream, _scene.Width, _scene.Height);
        }

        _scenario.Teardown(_gum);
        _scenario = null;
    }

    private static double ToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    private void Finish()
    {
        string header =
            "# Outline render-target benchmark" + (_options.Label.Length > 0 ? " (" + _options.Label + ")" : "") + "\n\n" +
            "- Hardware: " + _options.Hardware + "\n" +
            "- GL adapter: " + GraphicsDevice.Adapter.Description + "\n" +
            "- OS: " + Environment.OSVersion + ", .NET " + Environment.Version + ", MonoGame DesktopGL, " +
            (Environment.GetEnvironmentVariable("CONFIGURATION") ?? "built configuration") + "\n" +
            "- vsync off, fixed time step off, off-screen borderless window; scene drawn into an offscreen render target\n" +
            "- " + _options.WarmupFrames + " warm-up + " + _options.MeasuredFrames + " measured frames per config; margin " +
            BenchConfig.Margin + " px, Outline Thickness 2\n" +
            "- Scenarios: " + string.Join("; ", ScenarioRegistry.Factories.Select(f => f()).Select(s => s.Id + " = " + s.Description)) + "\n" +
            (_aborted ? "- **Aborted by the overall timeout; partial results.**\n" : "");
        ResultsWriter.Write(_options.OutputFolder, _results, header);
        Log("Wrote results; exiting");
        Exit();
    }

    protected override void UnloadContent()
    {
        _scene?.Dispose();
        base.UnloadContent();
    }
}
