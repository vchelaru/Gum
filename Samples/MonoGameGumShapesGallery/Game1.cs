using System.Linq;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameAndGum.Renderables;
using MonoGameGum;
using MonoGameGumShapesGallery.Screens;
using RenderingLibrary;
using RenderingLibrary.Graphics;

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
    private const float NavStripHeight = 80;

    private readonly GraphicsDeviceManager _graphics;

    private StackPanel? _navStrip;
    private FrameworkElement? _currentScreen;
    private TextRuntime? _drawCountOverlay;
    private KeyboardState _previousKeyboardState;

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
        BuildDrawCountOverlay();
        ShowScreen(NewSurveyScreen);

        base.Initialize();
    }

    // Overlay showing the count of SpriteBatch.Begin calls from the previous frame
    // (sourced from SpriteRenderer.LastFrameDrawStates) and the active orderer. Lives
    // as the last child of the nav strip so it flows alongside the buttons and wraps
    // to a new row when there isn't horizontal room. Note: this count does NOT include
    // Apos.Shapes StartBatch — those go through ShapeBatch, which is invisible to
    // LastFrameDrawStates.
    private void BuildDrawCountOverlay()
    {
        _drawCountOverlay = new TextRuntime();
        _drawCountOverlay.Red = 255;
        _drawCountOverlay.Green = 230;
        _drawCountOverlay.Blue = 120;
        _drawCountOverlay.Text = "SpriteBatch.Begin: 0";
        _navStrip!.Visual.Children.Add(_drawCountOverlay);
    }

    private void BuildNavStrip()
    {
        _navStrip = new StackPanel();
        _navStrip.Orientation = Orientation.Horizontal;
        _navStrip.Spacing = 4;
        _navStrip.Visual.X = 4;
        _navStrip.Visual.Y = 4;
        // Full-width wrap container so the draw-count overlay added after the buttons
        // flows alongside them and reflows to a new row when there isn't horizontal room.
        _navStrip.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        _navStrip.Visual.Width = -8;
        _navStrip.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        _navStrip.Visual.Height = NavStripHeight - 8;
        _navStrip.Visual.WrapsChildren = true;
        _navStrip.AddToRoot();

        AddNavButton("Shape survey", NewSurveyScreen);
        AddNavButton("Circles", () => new CirclesScreen());
        AddNavButton("Rectangles", () => new RectanglesScreen());
        AddNavButton("Arcs", () => new ArcsScreen());
        AddNavButton("Polygons", () => new PolygonsScreen());
        AddNavButton("Gradients", () => new GradientScreen());
        AddNavButton("Clipping", () => new ClippingScreen());
        AddNavButton("Batch mix stress", () => new BatchMixStressScreen());
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
        UpdateOrdererToggle();
        UpdateDrawCountOverlay();
        base.Update(gameTime);
    }

    // Press B (for Batch) to toggle between HierarchicalOrderer (default) and
    // BatchKeyGroupedOrderer. Both produce pixel-identical output when the safety
    // constraints hold; the difference is visible in the overlay's SpriteBatch.Begin
    // count and in the per-frame batch count on the Batch mix stress screen.
    private void UpdateOrdererToggle()
    {
        KeyboardState current = Keyboard.GetState();
        if (current.IsKeyDown(Keys.B) && _previousKeyboardState.IsKeyUp(Keys.B))
        {
            Renderer.SiblingOrdering = Renderer.SiblingOrdering == BatchKeyGroupedOrderer.Instance
                ? (IRenderableOrderer)HierarchicalOrderer.Instance
                : BatchKeyGroupedOrderer.Instance;
        }
        _previousKeyboardState = current;
    }

    private void UpdateDrawCountOverlay()
    {
        if (_drawCountOverlay == null)
        {
            return;
        }

        int count = SystemManagers.Default.Renderer.SpriteRenderer.LastFrameDrawStates.Count();
        string ordererLabel = Renderer.SiblingOrdering == BatchKeyGroupedOrderer.Instance
            ? "Grouped"
            : "Hierarchical";
        _drawCountOverlay.Text = $"SpriteBatch.Begin: {count}  |  Orderer (B): {ordererLabel}";
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
