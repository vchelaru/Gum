using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework;
using SkiaSharp;

namespace KniSkiaGumSample;

/// <summary>
/// A KNI desktop game whose UI is Gum drawn by Skia. The game class is the MonoGameGum one: Gum initializes
/// from the game, so the first screen is built in <see cref="Initialize"/>, and <c>Update</c> and <c>Draw</c>
/// are one call each. The GumService owns the Skia render target and composites it onto the back buffer.
/// Mouse, keyboard and gamepad reach Gum through KNI, with no Apos.Shapes involved.
/// </summary>
public class Game1 : Game
{
    GumService GumUI => GumService.Default;

    private readonly GraphicsDeviceManager _graphics;
    private int _clicks;
    private Label? _sizeLabel;

    private readonly bool _benchmark;
    private readonly bool _hidden;
    private readonly string? _benchmarkPath;

    public Game1() : this(Array.Empty<string>())
    {
    }

    public Game1(string[] args)
    {
        int benchmarkIndex = Array.IndexOf(args, "--benchmark");
        _benchmark = benchmarkIndex >= 0;
        _hidden = args.Contains("--hidden");
        // Resolved against the launch directory, which is where the caller expects the file.
        _benchmarkPath = _benchmark && benchmarkIndex + 1 < args.Length && !args[benchmarkIndex + 1].StartsWith("--")
            ? Path.GetFullPath(args[benchmarkIndex + 1]) : Path.GetFullPath("benchmark-result.md"); // the exe is WinExe, so stdout is invisible: with no path the row goes to benchmark-result.md in the launch directory

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = _benchmark ? 1280 : 1024,
            PreferredBackBufferHeight = _benchmark ? 720 : 640,
            SynchronizeWithVerticalRetrace = !_benchmark,
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        GumUI.Initialize(this);

        GumUI.UseKeyboardDefaults();

        GumUI.UseGamepadDefaults();

        if (_benchmark)
        {
            RunBenchmark();
            Exit();
            return;
        }

        BuildUi();

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        if (_benchmark)
        {
            return; // the run finished inside Initialize; Exit() takes effect after the first loop pass
        }

        GumUI.Update(gameTime);

        // Gum's canvas size should always equal the back buffer size, including after a resize.
        _sizeLabel!.Text =
            $"Gum canvas: {GraphicalUiElement.CanvasWidth} x {GraphicalUiElement.CanvasHeight}\n" +
            $"Back buffer: {GraphicsDevice.PresentationParameters.BackBufferWidth} x {GraphicsDevice.PresentationParameters.BackBufferHeight}";

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_benchmark)
        {
            return;
        }

        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }

    private void RunBenchmark()
    {
        GumSamples.Benchmark.BenchmarkRunner.RequireRelease();
        if (_hidden)
        {
            GumSamples.Benchmark.BenchmarkHost.HideWindow(Window.Handle);
        }
        GumSamples.Benchmark.BenchmarkScreen screen = new GumSamples.Benchmark.BenchmarkScreen();
        screen.AddToRoot();
        Console.WriteLine("GL_RENDERER: " + GumSamples.Benchmark.BenchmarkHost.Renderer());
        GumSamples.Benchmark.BenchmarkRunner.Run("KNI Skia (DesktopGL)", screen,
            update: seconds => GumUI.Update(seconds),
            draw: () =>
            {
                GraphicsDevice.Clear(Color.CornflowerBlue);
                GumUI.Draw();
            },
            sync: () =>
            {
                GraphicsDevice.Present();
                GumSamples.Benchmark.BenchmarkHost.Finish();
            },
            _benchmarkPath);
    }

    private void BuildUi()
    {
        // Added first so it draws behind everything. It stretches to the root, so its edges sit on the
        // window edges when the canvas size is right.
        RectangleRuntime outline = new RectangleRuntime();
        outline.Width = 0;
        outline.Height = 0;
        outline.WidthUnits = DimensionUnitType.RelativeToParent;
        outline.HeightUnits = DimensionUnitType.RelativeToParent;
        outline.IsFilled = false;
        outline.StrokeWidth = 4;
        outline.StrokeColor = SKColors.Red;
        GumUI.Root.AddChild(outline);

        Button bottomRight = new Button();
        bottomRight.Text = "Bottom right";
        bottomRight.AddToRoot();
        bottomRight.Visual.XOrigin = HorizontalAlignment.Right;
        bottomRight.Visual.YOrigin = VerticalAlignment.Bottom;
        bottomRight.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        bottomRight.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        bottomRight.Visual.X = -10;
        bottomRight.Visual.Y = -10;

        Button center = new Button();
        center.Text = "Centered";
        center.AddToRoot();
        center.Visual.XOrigin = HorizontalAlignment.Center;
        center.Visual.YOrigin = VerticalAlignment.Center;
        center.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        center.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        _sizeLabel = new Label();
        _sizeLabel.AddToRoot();
        _sizeLabel.Visual.XOrigin = HorizontalAlignment.Right;
        _sizeLabel.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        _sizeLabel.Visual.X = -24;
        _sizeLabel.Visual.Y = 24;

        StackPanel panel = new StackPanel();
        panel.Spacing = 10;
        panel.AddToRoot();
        panel.Visual.X = 24;
        panel.Visual.Y = 24;

        Label title = new Label();
        title.Text = "Gum on KNI, drawn by Skia";
        panel.AddChild(title);

        Label clickLabel = new Label();
        clickLabel.Text = "No clicks yet";

        Button button = new Button();
        button.Text = "Click me";
        button.Click += (_, _) =>
        {
            _clicks++;
            clickLabel.Text = $"Clicked {_clicks} times";
        };
        panel.AddChild(button);
        panel.AddChild(clickLabel);

        TextBox textBox = new TextBox();
        textBox.Width = 260;
        textBox.Placeholder = "Type here";
        panel.AddChild(textBox);

        CheckBox checkBox = new CheckBox();
        checkBox.Text = "A check box";
        panel.AddChild(checkBox);

        ListBox listBox = new ListBox();
        listBox.Width = 260;
        listBox.Height = 120;
        for (int i = 1; i <= 8; i++)
        {
            listBox.Items!.Add($"Item {i}");
        }
        panel.AddChild(listBox);
    }
}
