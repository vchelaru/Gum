using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace FontResizeDemo.MonoGame;

/// <summary>
/// Manual-test rig for resize-driven zoom plus font oversampling (see
/// docs/code/files-and-fonts/font-oversampling.md). Drag the window edges (or maximize) to zoom
/// the whole canvas via GumService.EnableZoomToWindow (height-dominant): with oversampling on, the
/// 14px Arial preview re-rasterizes itself every frame and stays crisp; uncheck the checkbox to see
/// the same text blur/pixelate like any zoomed-up bitmap font. The Reset button restores the window
/// to its startup size, which brings the zoom back to 1x on the next frame.
/// </summary>
public class Game1 : Game
{
    private const int DefaultWidth = 1024;
    private const int DefaultHeight = 768;

    private readonly GraphicsDeviceManager _graphics;
    private TextRuntime _hudLabel = null!;
    private float _lastLoggedZoom = float.NaN;
    private int _framesSinceStart;
    private bool _hasDumpedAtlas;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = DefaultWidth;
        _graphics.PreferredBackBufferHeight = DefaultHeight;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Window.AllowUserResizing = true;

        GumService.Default.Initialize(this);

        // KernSmith builds fonts in memory, at whatever size oversampling asks for -- no .fnt
        // files are shipped with this sample.
        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(GraphicsDevice);
        TextRuntime.UseFontOversampling = true;
        // The default 1px regenerate threshold is a much bigger fraction of this demo's small 14px
        // preview than it would be of a large FontSize -- 0 means "regenerate on any change" so the
        // preview re-crisps immediately instead of staying stuck at the native raster.
        TextRuntime.OversamplingRegenerateThresholdPixels = 0f;

        // Reference resolution is the window size at this call (DefaultWidth x DefaultHeight).
        // Height drives the zoom by default (WindowZoomMode.HeightDominant).
        GumService.Default.EnableZoomToWindow();

        BuildUi();

        base.Initialize();
    }

    private void BuildUi()
    {
        InteractiveGue root = GumService.Default.Root;

        TextRuntime previewText = new TextRuntime();
        previewText.Font = "Arial";
        previewText.FontSize = 14;
        previewText.Text = "Resize the window to zoom me";
        previewText.Red = 255;
        previewText.Green = 255;
        previewText.Blue = 255;
        previewText.Alpha = 255;
        previewText.Anchor(Anchor.Center);
        previewText.Y = -20;
        root.Children.Add(previewText);

        // Repeated identical glyphs directly adjacent to each other -- the clearest way to visually
        // spot inconsistent sub-pixel positioning between "identical" characters (see the
        // SnapPositionEvenWhenScaled checkbox above).
        TextRuntime repeatedGlyphText = new TextRuntime();
        repeatedGlyphText.Font = "Arial";
        repeatedGlyphText.FontSize = 32;
        repeatedGlyphText.Text = "oooo)))) mmmm";
        repeatedGlyphText.Red = 255;
        repeatedGlyphText.Green = 255;
        repeatedGlyphText.Blue = 255;
        repeatedGlyphText.Alpha = 255;
        repeatedGlyphText.Anchor(Anchor.Center);
        repeatedGlyphText.Y = 20;
        root.Children.Add(repeatedGlyphText);

        StackPanel controlsPanel = new StackPanel();
        controlsPanel.Orientation = Orientation.Vertical;
        controlsPanel.Spacing = 6;
        controlsPanel.Visual.X = 16;
        controlsPanel.Visual.Y = 16;
        root.AddChild(controlsPanel);

        // UseFontOversampling is a single static switch for the whole game (see
        // docs/code/files-and-fonts/font-oversampling.md), not a per-Text property -- this checkbox
        // toggles that global switch so you can flip between crisp and blurry at the same zoom.
        CheckBox oversamplingCheckBox = new CheckBox();
        oversamplingCheckBox.Text = "Font Oversampling (crisp on zoom)";
        oversamplingCheckBox.Width = 260;
        oversamplingCheckBox.IsChecked = true;
        oversamplingCheckBox.Checked += (_, _) => TextRuntime.UseFontOversampling = true;
        oversamplingCheckBox.Unchecked += (_, _) => TextRuntime.UseFontOversampling = false;
        controlsPanel.AddChild(oversamplingCheckBox);

        // TEMPORARY debug toggle for the glyph-positioning fix (adjacent identical glyphs rendering
        // with inconsistent edges under fractional scale -- SpriteRenderer.SnapPositionEvenWhenScaled,
        // see its doc comment). Unchecking reproduces the pre-fix behavior for A/B comparison; remove
        // this checkbox once the fix is confirmed and no longer needs visual verification.
        CheckBox snapPositionCheckBox = new CheckBox();
        snapPositionCheckBox.Text = "Snap glyph position (fix for )) artifact)";
        snapPositionCheckBox.Width = 300;
        snapPositionCheckBox.IsChecked = true;
        snapPositionCheckBox.Checked += (_, _) => SpriteRenderer.SnapPositionEvenWhenScaled = true;
        snapPositionCheckBox.Unchecked += (_, _) => SpriteRenderer.SnapPositionEvenWhenScaled = false;
        controlsPanel.AddChild(snapPositionCheckBox);

        Button resetButton = new Button();
        resetButton.Text = "Reset to Default Size";
        resetButton.Width = 200;
        resetButton.Click += (_, _) =>
        {
            _graphics.PreferredBackBufferWidth = DefaultWidth;
            _graphics.PreferredBackBufferHeight = DefaultHeight;
            _graphics.ApplyChanges();
        };
        controlsPanel.AddChild(resetButton);

        _hudLabel = new TextRuntime();
        _hudLabel.Font = "Arial";
        _hudLabel.FontSize = 14;
        _hudLabel.Red = 255;
        _hudLabel.Green = 255;
        _hudLabel.Blue = 255;
        _hudLabel.Alpha = 255;
        controlsPanel.AddChild(_hudLabel);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        GumService.Default.Update(gameTime);

        float zoom = SystemManagers.Default.Renderer.Camera.Zoom;

        // TEMPORARY diagnostic (remove once the small-FontSize blur is root-caused). Re-derives the
        // exact threshold check TextRuntime.UpdateAutomaticFontOversampling uses internally --
        // regenerate only fires once the requested raster size has moved a full pixel from the last
        // rasterized size (assumed here to still be the native FontSize, i.e. before any regenerate
        // has happened yet -- true at startup, which is what we're checking). If FontSize=14's delta
        // is under 1px while FontSize=48's clears it at the same zoom, that confirms the hypothesis:
        // oversampling silently never kicks in for the small text.
        float oversampleRatio = System.MathF.Max(1f, zoom);
        float rasterSize14 = 14f * oversampleRatio;
        float rasterSize48 = 48f * oversampleRatio;
        float delta14 = System.MathF.Abs(rasterSize14 - 14f);
        float delta48 = System.MathF.Abs(rasterSize48 - 48f);

        if (float.IsNaN(_lastLoggedZoom) || System.MathF.Abs(zoom - _lastLoggedZoom) > 0.0001f)
        {
            _lastLoggedZoom = zoom;
            string logPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "oversampling-diagnostics.txt");
            System.IO.File.AppendAllText(logPath,
                $"PreferredBackBuffer: {_graphics.PreferredBackBufferWidth}x{_graphics.PreferredBackBufferHeight}  " +
                $"ActualBackBuffer: {GraphicsDevice.PresentationParameters.BackBufferWidth}x{GraphicsDevice.PresentationParameters.BackBufferHeight}\n" +
                $"Zoom (raw): {zoom:R}  OversampleRatio: {oversampleRatio:R}\n" +
                $"FontSize 14 -> rasterSize {rasterSize14:0.###}px, rasterDelta {delta14:0.###}px / 1px threshold (regenerates: {delta14 >= 1f})\n" +
                $"FontSize 48 -> rasterSize {rasterSize48:0.###}px, rasterDelta {delta48:0.###}px / 1px threshold (regenerates: {delta48 >= 1f})\n" +
                "---\n");
        }

        _hudLabel.Text =
            $"Zoom: {zoom:0.0000}x  Window: {GraphicsDevice.PresentationParameters.BackBufferWidth}x" +
            $"{GraphicsDevice.PresentationParameters.BackBufferHeight}\n" +
            $"14px -> raster {rasterSize14:0.##}px (delta {delta14:0.###}px)  " +
            $"48px -> raster {rasterSize48:0.##}px ((((((((delta {delta48:0.###}px))))))))";

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 46));

        GumService.Default.Draw();

        // TEMPORARY diagnostics (remove once the o/m stretching artifact is root-caused): a full-frame
        // screenshot (so the artifact can be inspected in context, at whatever zoom/resize state the
        // window is in) plus a one-time dump of the HUD label's actual font atlas texture (so a
        // bad-bake-in-the-atlas vs. bad-runtime-layout can be told apart). Both overwrite the same
        // path each time so the most recent state is always what's on disk.
        _framesSinceStart++;
        if (_framesSinceStart % 30 == 0)
        {
            SaveScreenshot();
        }
        if (!_hasDumpedAtlas && _framesSinceStart >= 5)
        {
            SaveFontAtlas();
        }

        base.Draw(gameTime);
    }

    private void SaveScreenshot()
    {
        int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
        Color[] pixels = new Color[width * height];
        GraphicsDevice.GetBackBufferData(pixels);

        using Texture2D texture = new Texture2D(GraphicsDevice, width, height);
        texture.SetData(pixels);

        string path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "screenshot.png");
        using System.IO.FileStream stream = System.IO.File.Create(path);
        texture.SaveAsPng(stream, width, height);
    }

    private void SaveFontAtlas()
    {
        Text text = (Text)_hudLabel.RenderableComponent;
        Texture2D atlas = text.BitmapFont?.Texture;
        if (atlas == null)
        {
            return;
        }

        _hasDumpedAtlas = true;
        string path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "font-atlas.png");
        using System.IO.FileStream stream = System.IO.File.Create(path);
        atlas.SaveAsPng(stream, atlas.Width, atlas.Height);
    }
}
