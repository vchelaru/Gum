using Gum;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using SkiaGameRendering;
using SkiaSharp;

namespace KniSkiaGumFontLoading;

/// <summary>
/// The KNI Skia version of MonoGameGumFontLoading: texts that use a custom font file, plus one text drawn in
/// immediate mode. Skia renders .ttf/.otf files, so the fonts are TrueType rather than bmfont .fnt files.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly List<TextRuntime> _texts = new List<TextRuntime>();
    private SkiaRenderTarget2D? _canvas;

#if DEBUG
    // Plays an NDJSON script when launched with --gum-automation <script>; null otherwise.
    // Automation/smoke.ndjson has the launch command; GumAutomationHost documents the flags.
    private readonly Gum.Automation.GumAutomationHost? _automation;
#endif
    private TextRuntime _immediateText = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        IsMouseVisible = true;

#if DEBUG
        _automation = Gum.Automation.GumAutomationHost.FromArgs(Environment.GetCommandLineArgs(), this);
#endif
    }

    protected override void Initialize()
    {
        // The graphics device exists once the base Initialize has run.
        base.Initialize();

        GumService.Default.Initialize(this);

        AddText("Content/Fonts/CustomFont.ttf", "I use CustomFont", 0);
        AddText("Content/Fonts/Super Morning.ttf", "I use Super Morning", 30);
        AddText("Content/Fonts/Winter Selfie.ttf", "I use Winter Selfie", 60);

        _immediateText = new TextRuntime
        {
            UseCustomFont = true,
            CustomFontFile = "Content/Fonts/CustomFont.ttf",
            Text = "I am an immediate mode TextRuntime",
            X = 110,
            Y = 120
        };
    }

    private void AddText(string fontFile, string text, float yPosition)
    {
        TextRuntime textRuntime = new TextRuntime
        {
            UseCustomFont = true,
            CustomFontFile = fontFile,
            Text = text,
            Y = yPosition
        };
        textRuntime.AddToManagers(SystemManagers.Default, null);
        _texts.Add(textRuntime);
    }

    protected override void Update(GameTime gameTime)
    {
#if DEBUG
        _automation?.BeforeUpdate();
#endif
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

        Renderer renderer = SystemManagers.Default.Renderer;
        renderer.Begin();
        renderer.Draw(_immediateText);
        renderer.End();

        // Composites the target onto the back buffer at the origin, at native size.
        _canvas.End();
    }

    protected override void UnloadContent()
    {
        foreach (TextRuntime text in _texts)
        {
            text.RemoveFromManagers();
        }
        _texts.Clear();

        _canvas?.Dispose();
        _canvas = null;
        SkiaRenderer.Dispose();
        base.UnloadContent();
    }
}
