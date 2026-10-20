using GameUiSamples.Screens;
using GameUiSamples.Services;
using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkiaGameRendering;
using SkiaSharp;

namespace GameUiSamples;

/// <summary>
/// The GameUiSamples game, with its UI drawn by SkiaGum instead of the XNA-family renderer.
/// SkiaGameRendering supplies a <see cref="SkiaRenderTarget2D"/> (a KNI texture with an SKCanvas);
/// Gum draws into that canvas and the target is composited onto the back buffer.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SkiaRenderTarget2D? _canvas;
    private Label _statusLabel;

    GumService GumUI => GumService.Default;

    public static GameServiceContainer ServiceContainer { get; private set; } = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1366;
        _graphics.PreferredBackBufferHeight = 768;
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        ServiceContainer = Services;

        Services.AddService<InventoryService>(new InventoryService());
        Services.AddService<HyTaleInventoryService>(new HyTaleInventoryService());

        // The KNI Skia GumService reads its size from the game, so it initializes here like the
        // MonoGame sample; the SKCanvas only needs to exist by the first Draw.
        GumUI.Initialize(this, "Content/GumProject/GameUiSamplesGumProject.gumx");

        // This allows the keyboard to control the game (tabbing)
        GumUI.UseKeyboardDefaults();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Gum.Forms.Input.Keys.Down });
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Gum.Forms.Input.Keys.Up });

        MainMenu startScreen = new MainMenu();
        startScreen.AddToRoot();

        // Pinned to the bottom-right corner of the PopupRoot, which screens do not clear. It shows the
        // service's GameTime advancing and the canvas size Gum laid out against; if the size were wrong
        // at startup the label would sit off screen or away from the corner.
        _statusLabel = new Label();
        _statusLabel.Visual.Anchor(Anchor.BottomRight);
        _statusLabel.Visual.X = -8;
        _statusLabel.Visual.Y = -8;
        GumUI.PopupRoot.Children.Add(_statusLabel.Visual);

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);

        foreach (var item in GumUI.Root.Children)
        {
            if (item is InteractiveGue asInteractiveGue)
            {
                (asInteractiveGue.FormsControlAsObject as IUpdateScreen)?.Update(gameTime);
            }
        }

        if (GumUI.Keyboard.KeyPushed(Gum.Forms.Input.Keys.F1))
        {
            GumUI.ExportSnapshot("MyTestSnapshot.gumx");
        }

        _statusLabel.Text =
            $"KNI Skia | GameTime {GumUI.GameTime.TotalGameTime.TotalSeconds:0.0}s | " +
            $"Gum canvas {GraphicalUiElement.CanvasWidth:0}x{GraphicalUiElement.CanvasHeight:0}";

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
        GraphicsDevice.Clear(Color.CornflowerBlue);

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
        bool hadCanvas = _canvas != null;
        bool resized = _canvas == null || _canvas.Texture.Width != width || _canvas.Texture.Height != height;
        if (resized)
        {
            _canvas?.Dispose();
            _canvas = new SkiaRenderTarget2D(GraphicsDevice, width, height);
        }

        _canvas!.Begin();
        SKCanvas canvas = _canvas.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (hadCanvas && resized)
        {
            GumUI.HandleResize(width, height);
        }

        GumUI.Draw(canvas);

        // Composites the target onto the back buffer at the origin, at native size.
        _canvas.End();
    }

    protected override void UnloadContent()
    {
        _canvas?.Dispose();
        _canvas = null;
        SkiaRenderer.Dispose();
        base.UnloadContent();
    }
}
