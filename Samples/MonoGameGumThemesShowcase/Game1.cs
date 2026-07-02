using System;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Themes.Bubblegum;
using Gum.Themes.DarkPro;
using Gum.Themes.Editor;
using Gum.Themes.ForestGlade;
using Gum.Themes.Hazard;
using Gum.Themes.Meadow;
using Gum.Themes.Neon;
using Gum.Themes.Retro95;
using Gum.Themes.Template;
using Gum.Wireframe;
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

    // Authoring-only toggle (not part of the ShowcaseScreen root bookkeeping) that lets a
    // maintainer preview a hardcoded per-theme customization before manually capturing
    // screenshots. See ThemeOption.SetCustomized below for the per-theme values.
    CheckBox _customizeCheckBox;

    // A selectable theme: its display name, its Apply entry point, the backdrop color the
    // showcase should clear to while it is active, and a hardcoded customize/revert action
    // used only by the "Show Customized" checkbox (authoring tooling, not part of the theme itself).
    sealed class ThemeOption
    {
        public string Name { get; }
        public Action<GraphicsDevice> Apply { get; }
        public Color ClearColor { get; }
        public Action<bool> SetCustomized { get; }

        public ThemeOption(string name, Action<GraphicsDevice> apply, Color clearColor, Action<bool> setCustomized)
        {
            Name = name;
            Apply = apply;
            ClearColor = clearColor;
            SetCustomized = setCustomized;
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
            new ThemeOption("Forest Glade", ForestGladeTheme.Apply, ForestGladeStyling.ActiveStyle.Colors.CanopyDeep, SetForestGladeCustomized),
            new ThemeOption("Neon", NeonTheme.Apply, NeonStyling.ActiveStyle.Colors.Background, SetNeonCustomized),
            new ThemeOption("Dark Pro", DarkProTheme.Apply, DarkProStyling.ActiveStyle.Colors.Background, SetDarkProCustomized),
            new ThemeOption("Bubblegum", BubblegumTheme.Apply, BubblegumStyling.ActiveStyle.Colors.Background, SetBubblegumCustomized),
            new ThemeOption("Editor", EditorTheme.Apply, new Color(40, 40, 40), SetEditorCustomized),
            new ThemeOption("Retro 95", Retro95Theme.Apply, Retro95Styling.ActiveStyle.Colors.Surface, SetRetro95Customized),
            new ThemeOption("Hazard", HazardTheme.Apply, HazardStyling.ActiveStyle.Colors.Background, SetHazardCustomized),
            new ThemeOption("Meadow", MeadowTheme.Apply, MeadowStyling.ActiveStyle.Colors.Cream, SetMeadowCustomized),
            new ThemeOption("Template Theme", TemplateTheme.Apply, TemplateStyling.ActiveStyle.Colors.Background, SetTemplateCustomized),
        };

        // Authoring-only "Show Customized" checkbox — survives F1/F2 screen swaps via its own
        // AddToRoot call, separate from ShowcaseScreen's root-element bookkeeping. Created
        // before the first RebuildScreen call below so RebuildScreen can always rely on it
        // existing (see the re-parent-to-top comment inside RebuildScreen).
        _customizeCheckBox = new CheckBox();
        _customizeCheckBox.Text = "Show Customized";
        _customizeCheckBox.Anchor(Anchor.TopRight);
        _customizeCheckBox.X = -20;
        _customizeCheckBox.Y = 10;
        _customizeCheckBox.Checked += (_, _) =>
        {
            _themes[_currentThemeIndex].SetCustomized(true);
            RebuildScreen();
        };
        _customizeCheckBox.Unchecked += (_, _) =>
        {
            _themes[_currentThemeIndex].SetCustomized(false);
            RebuildScreen();
        };
        _customizeCheckBox.AddToRoot();

        // F1: all controls. F2: screenshot panel.
        _currentScreenFactory = () => new AllControlsScreen();

        ApplyTheme(0);
        RebuildScreen();

        base.Initialize();
    }

    // If Show Customized is checked when the user swaps themes, revert the previous theme's
    // customization and uncheck the box first — don't let a customization silently persist
    // onto/across a theme switch, and don't auto-apply the new theme's customization.
    void RevertCustomizationIfChecked()
    {
        if (_customizeCheckBox.IsChecked == true)
        {
            _customizeCheckBox.IsChecked = false;
        }
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

        // Root.Children.Add appends, and later children paint on top in Gum, so every
        // ShowcaseScreen rebuild re-adds fresh controls AFTER _customizeCheckBox in the
        // root's child list — burying it behind the new screen unless it's re-parented to
        // the end again here.
        _customizeCheckBox.RemoveFromRoot();
        _customizeCheckBox.AddToRoot();
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
                    RevertCustomizationIfChecked();
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

    // --- Hardcoded per-theme "Show Customized" preview values --------------------
    // Authoring tooling only, used by the Show Customized checkbox above. These values are not a
    // shared source of truth with any documentation example — they may drift over time.

    static void SetEditorCustomized(bool customized)
    {
        var colors = EditorStyling.ActiveStyle.Colors;
        var text = EditorStyling.ActiveStyle.Text;
        if (customized)
        {
            colors.Accent = new Color(255, 140, 0);
            colors.TextPrimary = new Color(235, 235, 245);
            colors.TextMuted = new Color(140, 140, 160);
            text.FontFamily = "Consolas";
            text.FontSize = 19;
        }
        else
        {
            colors.Accent = new Color(192, 222, 255);
            colors.TextPrimary = new Color(180, 180, 180);
            colors.TextMuted = new Color(88, 88, 88);
            text.FontFamily = "Arial";
            text.FontSize = 15;
        }
    }

    static void SetDarkProCustomized(bool customized)
    {
        var colors = DarkProStyling.ActiveStyle.Colors;
        var text = DarkProStyling.ActiveStyle.Text;
        if (customized)
        {
            colors.Accent = new Color(214, 64, 214);
            colors.Text = new Color(245, 240, 235);
            colors.Muted = new Color(150, 140, 135);
            text.FontFamily = "Consolas";
            text.FontSize = 18;
        }
        else
        {
            colors.Accent = new Color(0, 122, 204);
            colors.Text = new Color(212, 212, 212);
            colors.Muted = new Color(136, 136, 136);
            text.FontFamily = "DM Mono"; // DarkProTheme.BundledFontFamily is internal; mirrors it
            text.FontSize = 14;
        }
    }

    static void SetBubblegumCustomized(bool customized)
    {
        var colors = BubblegumStyling.ActiveStyle.Colors;
        var text = BubblegumStyling.ActiveStyle.Text;
        if (customized)
        {
            colors.Accent = new Color(40, 190, 190);
            colors.Text = new Color(20, 30, 60);
            colors.Muted = new Color(110, 120, 150);
            text.FontFamily = "Consolas";
            text.FontSize = 18;
        }
        else
        {
            colors.Accent = new Color(255, 107, 157);
            colors.Text = new Color(61, 17, 85);
            colors.Muted = new Color(160, 123, 176);
            text.FontFamily = "Nunito"; // BubblegumTheme.BundledFontFamily is internal; mirrors it
            text.FontSize = 14;
        }
    }

    static void SetForestGladeCustomized(bool customized)
    {
        var colors = ForestGladeStyling.ActiveStyle.Colors;
        var text = ForestGladeStyling.ActiveStyle.Text;
        if (customized)
        {
            colors.LeafBright = new Color(255, 105, 180);
            colors.Text = new Color(255, 240, 245);
            colors.Muted = new Color(200, 150, 165);
            text.FontFamily = "Consolas";
            text.FontSize = 18;
        }
        else
        {
            colors.LeafBright = new Color(71, 246, 65);
            colors.Text = new Color(241, 255, 240);
            colors.Muted = new Color(155, 186, 163);
            text.FontFamily = "Nunito"; // ForestGladeTheme.BundledFontFamily is internal; mirrors it
            text.FontSize = 14;
        }
    }

    static void SetNeonCustomized(bool customized)
    {
        var colors = NeonStyling.ActiveStyle.Colors;
        var text = NeonStyling.ActiveStyle.Text;
        if (customized)
        {
            colors.Accent = new Color(255, 0, 200);
            colors.Text = new Color(255, 250, 200);
            colors.Muted = new Color(140, 110, 160);
            text.FontFamily = "Consolas";
            text.FontSize = 17;
        }
        else
        {
            colors.Accent = new Color(0, 229, 255);
            colors.Text = new Color(200, 250, 255);
            colors.Muted = new Color(74, 96, 128);
            text.FontFamily = "Share Tech Mono"; // NeonTheme.BundledFontFamily is internal; mirrors it
            text.FontSize = 13;
        }
    }

    static void SetRetro95Customized(bool customized)
    {
        var colors = Retro95Styling.ActiveStyle.Colors;
        var text = Retro95Styling.ActiveStyle.Text;
        if (customized)
        {
            colors.Selection = new Color(128, 0, 32);
            colors.Text = new Color(10, 10, 60);
            colors.DisabledText = new Color(110, 70, 70);
            text.FontFamily = "Consolas";
            text.FontSize = 15;
        }
        else
        {
            colors.Selection = new Color(0, 0, 128);
            colors.Text = new Color(0, 0, 0);
            colors.DisabledText = new Color(128, 128, 128);
            text.FontFamily = "Nunito"; // Retro95Theme.BundledFontFamily is internal; mirrors it
            text.FontSize = 13;
        }
    }

    static void SetMeadowCustomized(bool customized)
    {
        var colors = MeadowStyling.ActiveStyle.Colors;
        var text = MeadowStyling.ActiveStyle.Text;
        if (customized)
        {
            colors.Blue = new Color(230, 90, 70);
            colors.TealDark = new Color(90, 40, 80);
            colors.Muted = new Color(170, 130, 150);
            text.FontFamily = "Consolas";
            text.FontSize = 17;
        }
        else
        {
            colors.Blue = new Color(70, 173, 230);
            colors.TealDark = new Color(30, 106, 91);
            colors.Muted = new Color(180, 156, 132);
            text.FontFamily = "Baloo 2"; // MeadowTheme.BundledFontFamily is internal; mirrors it
            text.FontSize = 15;
        }
    }

    static void SetHazardCustomized(bool customized)
    {
        var colors = HazardStyling.ActiveStyle.Colors;
        var text = HazardStyling.ActiveStyle.Text;
        if (customized)
        {
            colors.Accent = new Color(40, 140, 255);
            colors.Text = new Color(200, 230, 255);
            colors.Muted = new Color(90, 110, 140);
            text.FontFamily = "Consolas";
            text.FontSize = 17;
        }
        else
        {
            colors.Accent = new Color(244, 200, 26);
            colors.Text = new Color(227, 181, 40);
            colors.Muted = new Color(120, 102, 38);
            text.FontFamily = "Saira Condensed"; // HazardTheme.BundledFontFamily is internal; mirrors it
            text.FontSize = 15;
        }
    }

    static void SetTemplateCustomized(bool customized)
    {
        var colors = TemplateStyling.ActiveStyle.Colors;
        var text = TemplateStyling.ActiveStyle.Text;
        if (customized)
        {
            colors.Accent = new Color(60, 200, 120);
            colors.Text = new Color(225, 245, 235);
            colors.Muted = new Color(130, 150, 135);
            text.FontFamily = "Consolas";
            text.FontSize = 17;
        }
        else
        {
            colors.Accent = new Color(0, 122, 204);
            colors.Text = new Color(212, 212, 212);
            colors.Muted = new Color(136, 136, 136);
            text.FontFamily = "DM Mono"; // TemplateTheme.BundledFontFamily is internal; mirrors it
            text.FontSize = 14;
        }
    }
}
