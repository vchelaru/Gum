using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;

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

    // Issue #4317 manual-test rig: mouse wheel zooms the shared camera, magnifying both preview texts
    // uniformly (same as any zoomed Gum UI). _oversampledZoomPreviewText re-rasterizes itself
    // automatically at render time (TextRuntime.UpdateAutomaticFontOversampling, wired to Text.OnPreRender)
    // and stays crisp at every zoom level with no input required, while _plainZoomPreviewText -- an
    // ordinary Text with no oversampling -- visibly blurs/pixelates under the same zoom, for comparison.
    // (#4302's original version of this rig required manually pressing R; that trigger no longer exists.)
    private TextRuntime _plainZoomPreviewText = null!;
    private TextRuntime _oversampledZoomPreviewText = null!;
    private int _previousScrollWheelValue;

    // Issue #4304 manual-test rig: a single Text whose FontSize tracks the mouse wheel directly (NOT
    // camera.Zoom -- this text is drawn in normal, unzoomed screen space so the raster size KernSmith
    // bakes always matches this text's own on-screen footprint 1:1, with no compounding from the
    // camera transform above). Confirms KernSmith rebakes crisply at each new fractional size as it
    // changes continuously, and the label shows the live requested FontSize (with decimals) so the
    // number is readable while zooming.
    //
    // This deliberately does NOT demonstrate crisp text under CAMERA zoom -- that's the separate
    // automatic-oversampling demo below (BuildZoomOversamplingDemo / issue #4317).
    private TextRuntime _fractionalFontSizeText = null!;
    private const float FractionalFontSizeMin = 8f;
    // Matches the full 8-96 slider range in FontPlaygroundScreen. This text sits at Y=4 in the
    // reserved top band (see FontPlaygroundScreen.BuildInternal's controlsPanel.Visual.Y=100
    // comment), so there's ~96px of headroom before its box reaches the controls panel below --
    // a taller font (bigger FontSize -> taller glyph box) can eat into that margin at the top of
    // the range. If that becomes visually cramped, lower this or move controlsPanel.Visual.Y down.
    private const float FractionalFontSizeMax = 96f;
    private float _fractionalFontSize = 24f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1024;
        // Taller than the 768 the rest of the page's content originally fit in, to give the
        // fractional-FontSize demo's reserved top band (see FontPlaygroundScreen's controlsPanel
        // comment) room without cramming the zoom-demo pair against the bottom edge.
        _graphics.PreferredBackBufferHeight = 820;
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

        BuildFractionalFontSizeDemo();
        BuildZoomOversamplingDemo();

        base.Initialize();
    }

    private void BuildFractionalFontSizeDemo()
    {
        _fractionalFontSizeText = new TextRuntime();
        _fractionalFontSizeText.Font = "Arial";
        _fractionalFontSizeText.X = 16;
        _fractionalFontSizeText.Y = 4;
        _fractionalFontSizeText.Red = 255;
        _fractionalFontSizeText.Green = 255;
        _fractionalFontSizeText.Blue = 255;
        _fractionalFontSizeText.Alpha = 255;

        // Everything else in this sample renders through the main camera, which the mouse wheel
        // also zooms (see UpdateZoomOversamplingDemo) -- if this text stayed on that same layer, its
        // own FontSize growth would compound with the camera's zoom on top of it (pixelation, drift
        // from the zoom pivot, size growing faster than the displayed number). A screen-space layer
        // (IsInScreenSpace) is ignored by the main camera entirely, so this text's on-screen size and
        // position are driven ONLY by its own FontSize/X/Y -- exactly what the demo needs to show.
        //
        // Registered as a TOP-LEVEL layer member via AddToManagers rather than parented under Root
        // (Root.Children.Add) + MoveToLayer -- MoveToLayer only re-homes a TOP-LEVEL renderable's
        // layer membership, it does not detach a nested child from its parent's own render-tree walk.
        // Parenting under Root and then calling MoveToLayer left this text rendered TWICE: once via
        // Root's default-layer subtree walk (still scaled by the main camera zoom) AND once via its
        // own top-level entry on the screen-space layer -- invisible at zoom 1 (both draws coincide)
        // but increasingly visible as two diverging, overlapping copies at any other zoom. Surfaced
        // manually testing #4317; tracked generally (MoveToLayer silently double-rendering a parented
        // element) in #4333.
        Layer screenSpaceLayer = SystemManagers.Default.Renderer.AddLayer();
        screenSpaceLayer.Name = "Fractional FontSize (screen space)";
        screenSpaceLayer.LayerCameraSettings = new LayerCameraSettings { IsInScreenSpace = true };
        _fractionalFontSizeText.AddToManagers(SystemManagers.Default, screenSpaceLayer);

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
        _plainZoomPreviewText.Y = 650;
        _plainZoomPreviewText.Red = 255;
        _plainZoomPreviewText.Green = 255;
        _plainZoomPreviewText.Blue = 255;
        _plainZoomPreviewText.Alpha = 255;
        GumService.Default.Root.Children.Add(_plainZoomPreviewText);

        _oversampledZoomPreviewText = new TextRuntime();
        _oversampledZoomPreviewText.Text = "Scroll to zoom (auto-crisp)";
        _oversampledZoomPreviewText.FontSize = 24;
        _oversampledZoomPreviewText.X = 16;
        _oversampledZoomPreviewText.Y = 690;
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
            // Multiplying by (1 + delta*k) for a scroll up and by (1 - delta*k) for the same-sized
            // scroll down is NOT an inverse pair -- (1+k)*(1-k) = 1-k^2 < 1, so scrolling up then down
            // the same amount nets a small loss every time. An exponential step (Pow(base, delta)) IS
            // its own exact inverse (Pow(b,d) * Pow(b,-d) = 1 always), so equal-and-opposite scrolling
            // returns to precisely the original value.
            const float zoomStepBase = 1.001f;
            float zoomMultiplier = System.MathF.Pow(zoomStepBase, scrollWheelDelta);

            camera.Zoom = System.Math.Clamp(camera.Zoom * zoomMultiplier, 0.25f, 8f);

            _fractionalFontSize = System.Math.Clamp(
                _fractionalFontSize * zoomMultiplier, FractionalFontSizeMin, FractionalFontSizeMax);
            ApplyFractionalFontSize();
        }

        // No manual trigger needed here -- _oversampledZoomPreviewText re-rasterizes itself every
        // frame via TextRuntime.UpdateAutomaticFontOversampling, wired to Text.OnPreRender, which
        // reads this same camera's Zoom (issue #4317).
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 46));

        GumService.Default.Draw();

        base.Draw(gameTime);
    }
}
