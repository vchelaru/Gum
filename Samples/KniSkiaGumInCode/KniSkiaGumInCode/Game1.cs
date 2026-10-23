using Gum;
using Gum.Forms.Controls;
using GumSamples.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using SilkNetGum.Screens;
using SkiaGameRendering;
using SkiaSharp;

namespace KniSkiaGumInCode;

/// <summary>
/// The KNI Skia version of MonoGameGumInCode: a nav strip of feature screens built in code, drawn by SkiaGum
/// into a SkiaGameRendering <see cref="SkiaRenderTarget2D"/>. The screens are the SKIA builds of the shared
/// screen files that SilkNetGum also shows.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SkiaRenderTarget2D? _canvas;

    private StackPanel _navStrip = null!;
    private FrameworkElement? _currentScreen;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1400,
            PreferredBackBufferHeight = 900,
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        // The graphics device exists once the base Initialize has run.
        base.Initialize();

        GumService.Default.Initialize(this);

        // The shared screens load their textures relative to the Gum project folder, as in SilkNetGum.
        ToolsUtilities.FileManager.RelativeDirectory = "Content/GumProject/";

        // Gum core ships no shader loader, so the app registers a resolver that turns a
        // ContainerRuntime.SourceShaderFile (.sksl path) into a compiled SKRuntimeEffect.
        SkiaGum.CustomSetPropertyOnRenderable.RenderTargetEffectResolver =
            RenderTargetShaderScreen.CompileEffectFromFile;

        GumService.Default.UseKeyboardDefaults();

        // Demo the auto-fit helpers. Flip via the Zoom/Expand buttons in the nav strip.
        GumService.Default.EnableZoomToWindow();

        BuildNavStrip();

        ShowScreen<FormsScreen>();
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

        AddNavButton("Forms", () => ShowScreen<FormsScreen>());
        AddNavButton("Circles", () => ShowScreen<CirclesScreen>());
        AddNavButton("Rectangles", () => ShowScreen<RectanglesScreen>());
        AddNavButton("Arcs", () => ShowScreen<ArcsScreen>());
        AddNavButton("Polygons", () => ShowScreen<PolygonsScreen>());
        AddNavButton("Text", () => ShowScreen<TextScreen>());
        AddNavButton("NineSlice", () => ShowScreen<NineSliceScreen>());
        AddNavButton("Sprite", () => ShowScreen<SpriteScreen>());
        AddNavButton("Svg", () => ShowScreen<SvgScreen>());
        AddNavButton("Render Target", () => ShowScreen<RenderTargetScreen>());
        AddNavButton("RT Shader", () => ShowScreen<RenderTargetShaderScreen>());
        AddNavButton("Zoom", () => ShowScreen<ZoomScreen>());
        AddNavButton("Layer Camera", () => ShowScreen<LayerCameraSettingsScreen>());

        AddFitModeRadio("Zoom", isChecked: true, () => GumService.Default.EnableZoomToWindow());
        AddFitModeRadio("Expand", isChecked: false, () => GumService.Default.EnableExpandToWindow());
    }

    private void AddFitModeRadio(string text, bool isChecked, Action onChecked)
    {
        RadioButton radio = new RadioButton();
        radio.GroupName = "FitMode";
        radio.Text = text;
        radio.IsChecked = isChecked;
        radio.Checked += (_, _) => onChecked();
        _navStrip.AddChild(radio);
    }

    private void AddNavButton(string text, Action onClick)
    {
        Button button = new Button();
        button.Text = text;
        button.Click += (_, _) => onClick();
        _navStrip.AddChild(button);
    }

    private void ShowScreen<T>() where T : FrameworkElement, new()
    {
        _currentScreen?.RemoveFromRoot();

        // ZoomScreen and LayerCameraSettingsScreen drive the shared main Camera directly, so reset it
        // here so leaving either screen zoomed or panned never leaks into the next screen.
        SystemManagers.Default.Renderer.Camera.Zoom = 1f;
        SystemManagers.Default.Renderer.Camera.X = 0f;
        SystemManagers.Default.Renderer.Camera.Y = 0f;

        _currentScreen = new T();
        // The screen's ctor calls Dock(Fill); anchor it to the top and shrink it by the nav strip's
        // height so the two never overlap.
        _currentScreen.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Top;
        _currentScreen.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        _currentScreen.Visual.Y = _navStrip.Visual.AbsoluteHeight;
        _currentScreen.Visual.Height = -_navStrip.Visual.AbsoluteHeight;
        _currentScreen.AddToRoot();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        GumService.Default.Update(gameTime);

        // FrameworkElement.Activity() is FRB-only, so screens that animate get a host-driven Tick.
        (_currentScreen as TextScreen)?.Tick(gameTime.ElapsedGameTime.TotalSeconds);

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
        _canvas?.Dispose();
        _canvas = null;
        SkiaRenderer.Dispose();
        base.UnloadContent();
    }
}
