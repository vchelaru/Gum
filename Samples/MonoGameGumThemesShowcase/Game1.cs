using System;
using Gum.Themes.Bubblegum;
using Gum.Themes.DarkPro;
using Gum.Themes.Editor;
using Gum.Themes.ForestGlade;
using Gum.Themes.Hazard;
using Gum.Themes.Meadow;
using Gum.Themes.Neon;
using Gum.Themes.Retro95;
using Gum.Themes.Template;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Gum;
using MonoGameGumThemesShowcase.Screens;

namespace MonoGameGumThemesShowcase;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    GumService GumUI => GumService.Default;

    ShowcaseScreen _currentScreen;
    Func<ShowcaseScreen> _currentScreenFactory;
    ThemeOption[] _themes;
    int _currentThemeIndex;
    KeyboardState _previousKeyboard;
    Color _clearColor;

    // A selectable theme: its display name, its Apply entry point, and the
    // backdrop color the showcase should clear to while it is active.
    sealed class ThemeOption
    {
        public string Name { get; }
        public Action<GraphicsDevice> Apply { get; }
        public Color ClearColor { get; }

        public ThemeOption(string name, Action<GraphicsDevice> apply, Color clearColor)
        {
            Name = name;
            Apply = apply;
            ClearColor = clearColor;
        }
    }

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

        // Press 1-7 to swap themes; the active screen is rebuilt so its
        // controls pick up the newly-installed default templates. Editor has
        // no named Background color, so a sensible dark surround is used; the
        // Retro95 chrome is its Surface (battleship gray).
        _themes = new[]
        {
            new ThemeOption("Forest Glade", ForestGladeTheme.Apply, ForestGladeStyling.ActiveStyle.Colors.CanopyDeep),
            new ThemeOption("Neon", NeonTheme.Apply, NeonStyling.ActiveStyle.Colors.Background),
            new ThemeOption("Dark Pro", DarkProTheme.Apply, DarkProStyling.ActiveStyle.Colors.Background),
            new ThemeOption("Bubblegum", BubblegumTheme.Apply, BubblegumStyling.ActiveStyle.Colors.Background),
            new ThemeOption("Editor", EditorTheme.Apply, new Color(40, 40, 40)),
            new ThemeOption("Retro 95", Retro95Theme.Apply, Retro95Styling.ActiveStyle.Colors.Surface),
            new ThemeOption("Hazard", HazardTheme.Apply, HazardPalette.Background),
            new ThemeOption("Meadow", MeadowTheme.Apply, MeadowStyling.ActiveStyle.Colors.Cream),
            new ThemeOption("Template Theme", TemplateTheme.Apply, TemplatePalette.Background),
        };

        // F1: all controls. F2: screenshot panel.
        _currentScreenFactory = () => new AllControlsScreen();

        ApplyTheme(0);
        RebuildScreen();

        base.Initialize();
    }

    // Installs the theme's default templates / styling and updates the
    // backdrop. Callers must RebuildScreen afterward so existing controls
    // re-resolve their visuals from the new templates.
    void ApplyTheme(int index)
    {
        if (index < 0 || index >= _themes.Length)
        {
            return;
        }

        _currentThemeIndex = index;
        ThemeOption theme = _themes[index];
        theme.Apply(GraphicsDevice);
        _clearColor = theme.ClearColor;
        Window.Title = $"Gum Theme Showcase — {index + 1}. {theme.Name}  (1-{_themes.Length} swap theme, F1/F2 swap screen)";
    }

    void RebuildScreen()
    {
        SwitchScreen(_currentScreenFactory);
    }

    void SwitchScreen(Func<ShowcaseScreen> factory)
    {
        _currentScreenFactory = factory;
        _currentScreen?.Destroy();
        _currentScreen = factory();
        _currentScreen.Build();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (keyboard.IsKeyDown(Keys.F1) && _previousKeyboard.IsKeyUp(Keys.F1))
        {
            SwitchScreen(() => new AllControlsScreen());
        }
        else if (keyboard.IsKeyDown(Keys.F2) && _previousKeyboard.IsKeyUp(Keys.F2))
        {
            SwitchScreen(() => new ScreenshotScreen());
        }

        // Number keys 1-7 swap the active theme and rebuild the current screen.
        for (int i = 0; i < _themes.Length; i++)
        {
            Keys themeKey = Keys.D1 + i;
            if (keyboard.IsKeyDown(themeKey) && _previousKeyboard.IsKeyUp(themeKey))
            {
                if (i != _currentThemeIndex)
                {
                    ApplyTheme(i);
                    RebuildScreen();
                }
                break;
            }
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
