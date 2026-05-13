using Gum.Themes.Bubblegum;
using Gum.Themes.DarkPro;
using Gum.Themes.Editor;
using Gum.Themes.ForestGlade;
using Gum.Themes.Neon;
using Gum.Themes.Retro95;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGumThemesShowcase.Screens;

namespace MonoGameGumThemesShowcase;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    GumService GumUI => GumService.Default;

    ShowcaseScreen _currentScreen;
    KeyboardState _previousKeyboard;
    Color _clearColor;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        // for screenshots:
        //_graphics.PreferredBackBufferWidth = 530;
        //_graphics.PreferredBackBufferHeight = 350;

        // for viewing all :
        _graphics.PreferredBackBufferWidth = 1400;
        _graphics.PreferredBackBufferHeight = 900;


        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {

        System.AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
        {
            if (e.Exception.GetType().FullName == "KernSmith.FontParsingException")
            {
                var liveStack = new System.Diagnostics.StackTrace(fNeedFileInfo: true);
                System.Diagnostics.Debug.WriteLine(
                    $"[KernSmith] {e.Exception.Message}\n--- Caller stack ---\n{liveStack}");
            }
        };
        GumUI.Initialize(this);
        GumUI.UseKeyboardDefaults();

        // ---- Choose ONE theme (uncomment exactly one block) ----

        ForestGladeTheme.Apply(GraphicsDevice);
        _clearColor = ForestGladeColors.CanopyDeep;

        //NeonTheme.Apply(GraphicsDevice);
        //_clearColor = NeonColors.Background;

        //DarkProTheme.Apply(GraphicsDevice);
        //_clearColor = DarkProColors.Background;

        //BubblegumTheme.Apply(GraphicsDevice);
        //_clearColor = BubblegumColors.Background;

        //EditorTheme.Apply(GraphicsDevice);
        //_clearColor = new Color(40, 40, 40); // Editor has no named Background; this is a sensible dark surround.

        //Retro95Theme.Apply(GraphicsDevice);
        //_clearColor = Retro95Colors.Surface; // Retro95 has no Background; Surface (battleship gray) is the chrome.

        // ---------------------------------------------------------

        // ---- Choose ONE screen ----

        SwitchScreen(new AllControlsScreen());
        //SwitchScreen(new ScreenshotScreen());

        // ----------------------------

        base.Initialize();
    }

    // F1: all controls. F2: screenshot panel.
    void SwitchScreen(ShowcaseScreen newScreen)
    {
        _currentScreen?.Destroy();
        _currentScreen = newScreen;
        _currentScreen.Build();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.F1) && _previousKeyboard.IsKeyUp(Keys.F1))
        {
            SwitchScreen(new AllControlsScreen());
        }
        else if (keyboard.IsKeyDown(Keys.F2) && _previousKeyboard.IsKeyUp(Keys.F2))
        {
            SwitchScreen(new ScreenshotScreen());
        }
        _previousKeyboard = keyboard;

        GumUI.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_clearColor);
        GumUI.Draw();
        base.Draw(gameTime);
    }
}
