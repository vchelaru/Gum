using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using MonoGameGum;
using MonoGameGumShapesGallery.Screens;

namespace MonoGameGumShapesGallery;

// Visual smoke test + consumer-facing example for Gum.Shapes.MonoGame. The intent is that
// the code in this file is exactly what a consumer would write to get Apos-shape runtimes
// rendering on screen - no manual ShapeBatch.Begin/End, no direct Render calls, no
// PreRender calls. GumService.Default.Draw() drives the whole pipeline; each shape is a
// runtime instance configured via property setters and added to the visual tree once.
//
// The sample is split into pages, each a FrameworkElement-derived screen under Screens/.
// A horizontal nav strip of Forms Buttons across the top swaps the active page at
// runtime. Add a page by creating a new Screen and registering it in BuildNavStrip.
public class Game1 : Game
{
    private const int BackBufferWidth = 1280;
    private const int BackBufferHeight = 1000;
    private const float NavStripHeight = 40;

    private readonly GraphicsDeviceManager _graphics;

    private StackPanel? _navStrip;
    private FrameworkElement? _currentScreen;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = BackBufferWidth;
        _graphics.PreferredBackBufferHeight = BackBufferHeight;
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = "Gum Shapes Gallery";
    }

    protected override void Initialize()
    {
        // Standard Gum + Apos.Shapes setup. GumService brings up SystemManagers and the
        // renderer; ShapeRenderer wires up the Apos.Shapes batch so any shape runtime added
        // to the tree picks the right backend up automatically.
        GumService.Default.Initialize(this);
        ShapeRenderer.Self.Initialize();

        BuildNavStrip();
        ShowScreen(NewSurveyScreen);

        base.Initialize();
    }

    private void BuildNavStrip()
    {
        _navStrip = new StackPanel();
        _navStrip.Orientation = Orientation.Horizontal;
        _navStrip.Spacing = 4;
        _navStrip.Visual.X = 4;
        _navStrip.Visual.Y = 4;
        _navStrip.AddToRoot();

        AddNavButton("Shape survey", NewSurveyScreen);
        AddNavButton("Circles", () => new CirclesScreen());
        AddNavButton("Rectangles", () => new RectanglesScreen());
        AddNavButton("Arcs", () => new ArcsScreen());
        AddNavButton("Polygons", () => new PolygonsScreen());
        AddNavButton("Gradients", () => new GradientScreen());
    }

    private ShapeSurveyScreen NewSurveyScreen() =>
        new ShapeSurveyScreen(BackBufferWidth, BackBufferHeight - NavStripHeight);

    private void AddNavButton(string text, System.Func<FrameworkElement> factory)
    {
        Button button = new Button();
        button.Text = text;
        button.Click += (_, _) => ShowScreen(factory);
        _navStrip!.AddChild(button);
    }

    private void ShowScreen(System.Func<FrameworkElement> factory)
    {
        if (_currentScreen != null)
        {
            _currentScreen.RemoveFromRoot();
        }

        _currentScreen = factory();
        // Offset the screen so it doesn't sit underneath the nav strip. The screen's ctor
        // calls Dock(Fill); reset the vertical anchoring to top + shrink height by the nav
        // strip's footprint so the two never overlap.
        _currentScreen.Visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Top;
        _currentScreen.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        _currentScreen.Visual.Y = NavStripHeight;
        _currentScreen.Visual.Height = -NavStripHeight;
        _currentScreen.AddToRoot();
    }

    protected override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Matches the SilkNetGum clear color so the two galleries render shadows against the
        // same backdrop and visual diffs stay attributable to the shape code, not the page.
        GraphicsDevice.Clear(new Color(51, 76, 204));
        GumService.Default.Draw();
        base.Draw(gameTime);
    }
}
