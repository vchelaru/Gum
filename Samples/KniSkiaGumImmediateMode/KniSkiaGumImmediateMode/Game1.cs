using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using KniSkiaGumImmediateMode.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGameRendering;
using SkiaSharp;

namespace KniSkiaGumImmediateMode;

/// <summary>
/// The KNI Skia version of MonoGameGumImmediateMode: GumBatch (immediate-mode rendering) drawn by SkiaGum
/// into a SkiaGameRendering <see cref="SkiaRenderTarget2D"/>. A retained-mode nav strip at the top, built
/// with Gum Forms controls, switches between example screens; each screen shows a different way to use
/// GumBatch. Skia draws text with system fonts, so there are no font files.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SkiaRenderTarget2D? _canvas;
    private GumBatch _gumBatch = null!;

    private StackPanel _navStrip = null!;
    private IImmediateModeScreen? _currentScreen;

    // Reused each frame so Root and the current screen's AdditionalUpdateRoots go through a
    // single GumService.Default.Update call - see IImmediateModeScreen.AdditionalUpdateRoots
    // for why calling Update more than once per frame silently breaks push/click.
    private readonly List<GraphicalUiElement> _updateRoots = new List<GraphicalUiElement>();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1024,
            PreferredBackBufferHeight = 768,
        };
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // The graphics device exists once the base Initialize has run.
        base.Initialize();

        // GumService initialization sets up SystemManagers, Forms input, and everything needed for
        // retained-mode controls. We use it for the nav strip; the example screens use GumBatch in Draw.
        GumService.Default.Initialize(this);

        _gumBatch = new GumBatch();

        BuildNavStrip();
        ShowScreen(new DrawStringScreen());
    }

    private void BuildNavStrip()
    {
        _navStrip = new StackPanel();
        _navStrip.Orientation = Orientation.Horizontal;
        _navStrip.Spacing = 4;
        _navStrip.Visual.X = 4;
        _navStrip.Visual.Y = 4;
        _navStrip.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        _navStrip.Width = 0;
        _navStrip.Visual.WrapsChildren = true;
        _navStrip.AddToRoot();

        AddNavButton("DrawString", () => ShowScreen(new DrawStringScreen()));
        AddNavButton("TextRuntime", () => ShowScreen(new TextRuntimeScreen()));
        AddNavButton("Parent/Child", () => ShowScreen(new ParentChildScreen()));
        AddNavButton("RenderTarget", () => ShowScreen(new RenderTargetScreen()));
        AddNavButton("Mixed Batch", () => ShowScreen(new MixedBatchScreen()));
    }

    private void AddNavButton(string text, Action onClick)
    {
        Button button = new Button();
        button.Text = text;
        button.Click += (_, _) => onClick();
        _navStrip.AddChild(button);
    }

    private void ShowScreen(IImmediateModeScreen screen)
    {
        _currentScreen?.Dispose();
        _currentScreen = screen;
        _currentScreen.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        // Mirrors the Root canvas-fit that GumService.Default.Update(gameTime) would otherwise
        // perform before delegating to the roots overload below - needed here because Root and
        // the current screen's extra interactive roots must go through that overload together,
        // in one call (see IImmediateModeScreen.AdditionalUpdateRoots).
        GraphicalUiElement root = GumService.Default.Root;
        root.X = 0;
        root.Y = 0;
        root.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        root.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        root.Width = GraphicalUiElement.CanvasWidth;
        root.Height = GraphicalUiElement.CanvasHeight;

        _updateRoots.Clear();
        _updateRoots.Add(root);
        if (_currentScreen != null)
        {
            _updateRoots.AddRange(_currentScreen.AdditionalUpdateRoots);
        }
        GumService.Default.Update(gameTime, _updateRoots);

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

        // GumBatch draws onto SystemManagers.Default.Canvas, which GumService.Default.Draw(canvas) would
        // otherwise only set after the screen has drawn.
        SystemManagers.Default.Canvas = canvas;

        // The current example screen does its immediate-mode draws first...
        _currentScreen?.Draw(_gumBatch, canvas);

        // ...then GumService.Default.Draw renders the retained-mode nav strip on top, so the buttons
        // stay visible no matter what the screen drew. GumService.Initialize turns off the Renderer's
        // ClearsCanvas, so this does not wipe the screen's draws.
        GumService.Default.Draw(canvas);

        // Composites the target onto the back buffer at the origin, at native size.
        _canvas.End();
    }

    protected override void UnloadContent()
    {
        _currentScreen?.Dispose();
        _canvas?.Dispose();
        _canvas = null;
        SkiaRenderer.Dispose();
        base.UnloadContent();
    }
}
