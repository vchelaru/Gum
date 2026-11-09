using Gum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using KniSkiaGumImmediateMode.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace KniSkiaGumImmediateMode;

/// <summary>
/// The KNI Skia version of MonoGameGumImmediateMode: GumBatch (immediate-mode rendering) drawn by SkiaGum
/// by GumService. A retained-mode nav strip at the top, built
/// with Gum Forms controls, switches between example screens; each screen shows a different way to use
/// GumBatch. Skia draws text with system fonts, so there are no font files.
/// </summary>
public class Game1 : Game
{
    GumService GumUI => GumService.Default;

    private readonly GraphicsDeviceManager _graphics;
    private GumBatch _gumBatch = null!;

    private StackPanel _navStrip = null!;
    private IImmediateModeScreen? _currentScreen;

    // Reused each frame so Root and the current screen's AdditionalUpdateRoots go through a
    // single GumUI.Update call - see IImmediateModeScreen.AdditionalUpdateRoots
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
        // GumService initialization sets up SystemManagers, Forms input, and everything needed for
        // retained-mode controls. We use it for the nav strip; the example screens use GumBatch in Draw.
        GumUI.Initialize(this);

        GumUI.UseKeyboardDefaults();

        GumUI.UseGamepadDefaults();

        _gumBatch = new GumBatch();

        // The current example screen does its immediate-mode draws first, then Gum renders the
        // retained-mode nav strip on top, so the buttons stay visible no matter what the screen drew.
        // GumService.Initialize turns off the Renderer's ClearsCanvas, so Gum's draw does not wipe them.
        GumUI.BeforeDraw += canvas => _currentScreen?.Draw(_gumBatch, canvas);

        BuildNavStrip();
        ShowScreen(new DrawStringScreen());

        base.Initialize();
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

        // Mirrors the Root canvas-fit that GumUI.Update(gameTime) would otherwise
        // perform before delegating to the roots overload below - needed here because Root and
        // the current screen's extra interactive roots must go through that overload together,
        // in one call (see IImmediateModeScreen.AdditionalUpdateRoots).
        GraphicalUiElement root = GumUI.Root;
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
        GumUI.Update(gameTime, _updateRoots);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _currentScreen?.Dispose();
        base.UnloadContent();
    }
}
