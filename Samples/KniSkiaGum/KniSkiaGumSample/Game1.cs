using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework;
using SkiaGameRendering;
using SkiaSharp;

namespace KniSkiaGumSample;

/// <summary>
/// A KNI desktop game whose UI is Gum drawn by Skia. SkiaGameRendering gives a <see cref="SkiaRenderTarget2D"/>:
/// a KNI texture with an SKCanvas. Gum draws into that canvas, then the target is composited onto the
/// back buffer. Gum initializes from the game, like MonoGameGum, so the first screen is built in
/// <see cref="Initialize"/>; the canvas arrives with the first <c>Draw</c>. Mouse, keyboard and gamepad
/// reach Gum through KNI, with no Apos.Shapes involved.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SkiaRenderTarget2D? _canvas;
    private int _clicks;
    private Label? _sizeLabel;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1024,
            PreferredBackBufferHeight = 640,
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        // The graphics device exists once the base Initialize has run.
        base.Initialize();

        GumService.Default.Initialize(this);
        GumService.Default.UseKeyboardDefaults();
        BuildUi();
    }

    protected override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);

        // Gum's canvas size should always equal the back buffer size, including after a resize.
        _sizeLabel!.Text =
            $"Gum canvas: {GraphicalUiElement.CanvasWidth} x {GraphicalUiElement.CanvasHeight}\n" +
            $"Back buffer: {GraphicsDevice.PresentationParameters.BackBufferWidth} x {GraphicsDevice.PresentationParameters.BackBufferHeight}";

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // SkiaRenderer attaches to the GraphicsDevice once KNI has created it.
        if (!SkiaRenderer.IsInitialized && SkiaRenderer.IsReady)
        {
            SkiaRenderer.Initialize(GraphicsDevice);
        }

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(new Color(30, 34, 40));

        if (SkiaRenderer.IsInitialized)
        {
            DrawGum();
        }

        base.Draw(gameTime);
    }

    private void DrawGum()
    {
        int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = GraphicsDevice.PresentationParameters.BackBufferHeight;

        // A SkiaRenderTarget2D has a fixed size, so a resized window needs a new one.
        bool resized = _canvas == null || _canvas.Texture.Width != width || _canvas.Texture.Height != height;
        if (resized)
        {
            _canvas?.Dispose();
            _canvas = new SkiaRenderTarget2D(GraphicsDevice, width, height);
        }

        _canvas!.Begin();
        SKCanvas canvas = _canvas.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (resized)
        {
            GumService.Default.HandleResize(width, height);
        }

        GumService.Default.Draw(canvas);

        // Composites the target onto the back buffer at the origin, at native size.
        _canvas.End();
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
        GumService.Default.Root.AddChild(outline);

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

    protected override void UnloadContent()
    {
        _canvas?.Dispose();
        _canvas = null;
        SkiaRenderer.Dispose();
        base.UnloadContent();
    }
}
