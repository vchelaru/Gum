using Gum;
using Gum.Forms.Controls;
using KniSkiaGumCodeGeneration.Screens;
using Microsoft.Xna.Framework;
using SkiaGameRendering;
using SkiaSharp;

namespace KniSkiaGumCodeGeneration;

/// <summary>
/// The KNI Skia version of MonoGameGumCodeGeneration: shows the generated MainMenuFullGeneration screen,
/// drawn by SkiaGum into a SkiaGameRendering <see cref="SkiaRenderTarget2D"/>.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SkiaRenderTarget2D? _canvas;
    private MainMenuFullGeneration? _mainMenu;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720
        };
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // The graphics device exists once the base Initialize has run.
        base.Initialize();

        // Skia reads the project from disk. The csproj copies Content/GumProject to the output.
        GumService.Default.Initialize(this, "Content/GumProject/GumProject.gumx");

        _mainMenu = new MainMenuFullGeneration
        {
            Name = "MainMenu"
        };
        _mainMenu.AddToRoot();
    }

    protected override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
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
            GumService.Default.HandleResize(width, height);
        }

        GumService.Default.Draw(canvas);

        // Composites the target onto the back buffer at the origin, at native size.
        _canvas.End();
    }

    protected override void UnloadContent()
    {
        GumService.Default.Root.Children.Clear();
        _mainMenu = null;
        _canvas?.Dispose();
        _canvas = null;
        SkiaRenderer.Dispose();
        base.UnloadContent();
    }
}
