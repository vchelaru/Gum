using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;

namespace FontPlayground.MonoGame;

/// <summary>
/// Thin MonoGame host for the dynamic-font playground. All UI and live-update logic lives in the
/// platform-neutral <see cref="FontPlaygroundScreen"/> (shared via a linked source file). This host
/// only bootstraps Gum, registers KernSmith for in-memory font generation, and pumps Update/Draw.
///
/// Fonts are generated in memory by KernSmith — there are no .fnt files shipped with this sample.
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;

    // Issue #4302 manual-test rig: mouse wheel zooms the camera, blurring both preview texts below
    // the playground the same way any zoomed Gum UI blurs today. Pressing R regenerates only
    // _oversampledZoomPreviewText's font at the current zoom (manually-triggered, per this issue's
    // scope), so it turns crisp again while _plainZoomPreviewText stays blurry for comparison.
    private TextRuntime _plainZoomPreviewText = null!;
    private TextRuntime _oversampledZoomPreviewText = null!;
    private int _previousScrollWheelValue;
    private bool _wasRKeyDown;

    // Issue #4304 manual-test rig: a single Text whose FontSize tracks the mouse wheel directly (NOT
    // camera.Zoom -- this text is drawn in normal, unzoomed screen space so the raster size KernSmith
    // bakes always matches this text's own on-screen footprint 1:1, with no compounding from the
    // camera transform above). Confirms KernSmith rebakes crisply at each new fractional size as it
    // changes continuously, and the label shows the live requested FontSize (with decimals) so the
    // number is readable while zooming.
    private TextRuntime _fractionalFontSizeText = null!;
    private const float FractionalFontSizeMin = 8f;
    private const float FractionalFontSizeMax = 96f;
    private float _fractionalFontSize = 24f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        GumService.Default.Initialize(this);

        // Wire up KernSmith so any TextRuntime can get a font for any (family, size, style)
        // without a .fnt file on disk. This is what makes the live preview re-render work.
        CustomSetPropertyOnRenderable.InMemoryFontCreator =
            new KernSmithFontCreator(GraphicsDevice);

        FontPlaygroundScreen.Build(GumService.Default.Root);

        BuildZoomOversamplingDemo();
        BuildFractionalFontSizeDemo();

        base.Initialize();
    }

    private void BuildFractionalFontSizeDemo()
    {
        _fractionalFontSizeText = new TextRuntime();
        _fractionalFontSizeText.Font = "Arial";
        _fractionalFontSizeText.X = 16;
        _fractionalFontSizeText.Y = 660;
        _fractionalFontSizeText.Red = 255;
        _fractionalFontSizeText.Green = 255;
        _fractionalFontSizeText.Blue = 255;
        _fractionalFontSizeText.Alpha = 255;
        GumService.Default.Root.Children.Add(_fractionalFontSizeText);

        ApplyFractionalFontSize();
    }

    private void ApplyFractionalFontSize()
    {
        _fractionalFontSizeText.FontSize = _fractionalFontSize;
        _fractionalFontSizeText.Text = $"Font size: {_fractionalFontSize:0.00}";
    }

    private void BuildZoomOversamplingDemo()
    {
        // Off by default project-wide (see TextRuntime.UseFontOversampling); this sample opts in
        // to demonstrate the feature.
        TextRuntime.UseFontOversampling = true;

        _plainZoomPreviewText = new TextRuntime();
        _plainZoomPreviewText.Text = "Scroll to zoom (always blurry)";
        _plainZoomPreviewText.FontSize = 24;
        _plainZoomPreviewText.X = 16;
        _plainZoomPreviewText.Y = 560;
        _plainZoomPreviewText.Red = 255;
        _plainZoomPreviewText.Green = 255;
        _plainZoomPreviewText.Blue = 255;
        _plainZoomPreviewText.Alpha = 255;
        GumService.Default.Root.Children.Add(_plainZoomPreviewText);

        _oversampledZoomPreviewText = new TextRuntime();
        _oversampledZoomPreviewText.Text = "Scroll to zoom, then press R (crisp)";
        _oversampledZoomPreviewText.FontSize = 24;
        _oversampledZoomPreviewText.X = 16;
        _oversampledZoomPreviewText.Y = 600;
        _oversampledZoomPreviewText.Red = 255;
        _oversampledZoomPreviewText.Green = 255;
        _oversampledZoomPreviewText.Blue = 255;
        _oversampledZoomPreviewText.Alpha = 255;
        GumService.Default.Root.Children.Add(_oversampledZoomPreviewText);

        _previousScrollWheelValue = Mouse.GetState().ScrollWheelValue;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        UpdateZoomOversamplingDemo();

        GumService.Default.Update(gameTime);

        base.Update(gameTime);
    }

    private void UpdateZoomOversamplingDemo()
    {
        MouseState mouseState = Mouse.GetState();
        int scrollWheelDelta = mouseState.ScrollWheelValue - _previousScrollWheelValue;
        _previousScrollWheelValue = mouseState.ScrollWheelValue;

        Camera camera = SystemManagers.Default.Renderer.Camera;
        if (scrollWheelDelta != 0)
        {
            camera.Zoom = System.Math.Clamp(camera.Zoom * (1 + scrollWheelDelta * 0.001f), 0.25f, 8f);

            _fractionalFontSize = System.Math.Clamp(
                _fractionalFontSize * (1 + scrollWheelDelta * 0.001f), FractionalFontSizeMin, FractionalFontSizeMax);
            ApplyFractionalFontSize();
        }

        bool isRKeyDown = Keyboard.GetState().IsKeyDown(Keys.R);
        if (isRKeyDown && !_wasRKeyDown)
        {
            _oversampledZoomPreviewText.RegenerateOversampledFont(camera.Zoom);
        }
        _wasRKeyDown = isRKeyDown;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 46));

        GumService.Default.Draw();

        base.Draw(gameTime);
    }
}
