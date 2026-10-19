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
    private bool _gumInitialized;
    private GameTime _lastGameTime = new GameTime();

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

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        _lastGameTime = gameTime;

        // Gum needs the SKCanvas to initialize, and the canvas only exists inside a render pass
        // (see DrawGum), so nothing updates until the first frame has been drawn.
        if (_gumInitialized)
        {
            GumUI.Update(gameTime.TotalGameTime.TotalSeconds);

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
        }

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
        bool resized = _canvas == null || _canvas.Texture.Width != width || _canvas.Texture.Height != height;
        if (resized)
        {
            _canvas?.Dispose();
            _canvas = new SkiaRenderTarget2D(GraphicsDevice, width, height);
        }

        _canvas!.Begin();
        SKCanvas canvas = _canvas.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (!_gumInitialized)
        {
            GumUI.Initialize(this, canvas, width, height, "Content/GumProject/GameUiSamplesGumProject.gumx");

            // This allows the keyboard to control the game (tabbing)
            GumUI.UseKeyboardDefaults();
            FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Gum.Forms.Input.Keys.Down });
            FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Gum.Forms.Input.Keys.Up });

            var startScreen = new MainMenu();
            startScreen.AddToRoot();

            _gumInitialized = true;
        }
        else if (resized)
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
