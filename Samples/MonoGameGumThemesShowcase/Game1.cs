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
        // A delegate, not a snapshot Color — some customizations change the theme's
        // background-equivalent token (e.g. Meadow's Cream), and re-querying live is
        // what lets the showcase's clear color follow that change.
        public Func<Color> GetClearColor { get; }
        public Action<bool> SetCustomized { get; }

        public ThemeOption(string name, Action<GraphicsDevice> apply, Func<Color> getClearColor, Action<bool> setCustomized)
        {
            Name = name;
            Apply = apply;
            GetClearColor = getClearColor;
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
            new ThemeOption("Forest Glade", ForestGladeTheme.Apply, () => ForestGladeStyling.ActiveStyle.Colors.CanopyDeep, SetForestGladeCustomized),
            new ThemeOption("Neon", NeonTheme.Apply, () => NeonStyling.ActiveStyle.Colors.Background, SetNeonCustomized),
            new ThemeOption("Dark Pro", DarkProTheme.Apply, () => DarkProStyling.ActiveStyle.Colors.Background, SetDarkProCustomized),
            new ThemeOption("Bubblegum", BubblegumTheme.Apply, () => BubblegumStyling.ActiveStyle.Colors.Background, SetBubblegumCustomized),
            new ThemeOption("Editor", EditorTheme.Apply, () => new Color(40, 40, 40), SetEditorCustomized),
            new ThemeOption("Retro 95", Retro95Theme.Apply, () => Retro95Styling.ActiveStyle.Colors.Surface, SetRetro95Customized),
            new ThemeOption("Hazard", HazardTheme.Apply, () => HazardStyling.ActiveStyle.Colors.Background, SetHazardCustomized),
            new ThemeOption("Meadow", MeadowTheme.Apply, () => MeadowStyling.ActiveStyle.Colors.Cream, SetMeadowCustomized),
            new ThemeOption("Template Theme", TemplateTheme.Apply, () => TemplateStyling.ActiveStyle.Colors.Background, SetTemplateCustomized),
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
        _customizeCheckBox.Checked += (_, _) => ApplyCustomizationToggle(true);
        _customizeCheckBox.Unchecked += (_, _) => ApplyCustomizationToggle(false);
        _customizeCheckBox.AddToRoot();

        // F1: all controls. F2: screenshot panel.
        _currentScreenFactory = () => new AllControlsScreen();

        ApplyTheme(0);
        RebuildScreen();

        base.Initialize();
    }

    // Mutating a theme's Colors/Text alone isn't enough: font selection and the 4-token
    // guardrail that colors stock, un-restyled V3 visuals (Label, etc.) are only pushed into
    // Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle once, inside the theme's own Apply() —
    // see ConfigureStyling() in each XyzTheme.cs. So re-calling theme.Apply() after
    // SetCustomized() is what actually refreshes fonts and Label-style text, matching the
    // theme's own documented "mutate before Apply()" pattern.
    void ApplyCustomizationToggle(bool customized)
    {
        ThemeOption theme = _themes[_currentThemeIndex];
        theme.SetCustomized(customized);
        theme.Apply(GraphicsDevice);
        _clearColor = theme.GetClearColor();
        RebuildScreen();
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
        _clearColor = theme.GetClearColor();
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
            // Tints the whole gray chrome toward blue, not just the accent — Primary,
            // BorderHover/BorderPushed, and the panel/recessed backgrounds all feed into
            // the same neutral gray by default, so all four need to move together.
            colors.Accent = new Color(140, 190, 255);
            colors.TextPrimary = new Color(210, 220, 255);
            colors.TextMuted = new Color(100, 110, 140);
            colors.Primary = new Color(50, 60, 90);
            colors.BorderHover = new Color(120, 150, 210);
            colors.BorderPushed = new Color(220, 230, 255);
            colors.Selection = new Color(30, 80, 200);
            colors.PanelBackground = new Color(18, 20, 32);
            colors.RecessedBackground = new Color(8, 10, 20);
            text.FontFamily = "Consolas";
            text.FontSize = 19;
        }
        else
        {
            colors.Accent = new Color(192, 222, 255);
            colors.TextPrimary = new Color(180, 180, 180);
            colors.TextMuted = new Color(88, 88, 88);
            colors.Primary = new Color(60, 60, 60);
            colors.BorderHover = new Color(150, 150, 150);
            colors.BorderPushed = new Color(255, 255, 255);
            colors.Selection = new Color(0, 92, 128);
            colors.PanelBackground = new Color(27, 27, 27);
            colors.RecessedBackground = new Color(10, 10, 10);
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
            // Deliberately a serif, not another mono font — DM Mono vs. Consolas would
            // read as "basically the same," so this picks something unmissable to
            // demonstrate the font-swap mechanism clearly.
            text.FontFamily = "Georgia";
            text.FontSize = 20;
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
            // Surface1/Background/Border/Placeholder are what TextBox actually reads for
            // its fill/border/placeholder (see BubblegumTextInputDecoration) — Accent/Text
            // alone only moved the caret and typed-text color, not the box itself.
            colors.Accent = new Color(40, 190, 190);
            colors.Text = new Color(20, 30, 60);
            colors.Muted = new Color(110, 120, 150);
            colors.Surface1 = new Color(235, 250, 250);
            colors.Background = new Color(230, 250, 248);
            colors.Border = new Color(120, 200, 200);
            colors.Placeholder = new Color(140, 170, 180);
            text.FontFamily = "Consolas";
            text.FontSize = 18;
        }
        else
        {
            colors.Accent = new Color(255, 107, 157);
            colors.Text = new Color(61, 17, 85);
            colors.Muted = new Color(160, 123, 176);
            colors.Surface1 = new Color(255, 255, 255);
            colors.Background = new Color(255, 240, 249);
            colors.Border = new Color(240, 170, 204);
            colors.Placeholder = new Color(201, 168, 217);
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
            // Surface1 is what ListBoxVisual actually fills its background with; Surface2
            // is the ListBox hover-row tint (HoverRow aliases it) — both need to move
            // together or hovering a row looks like a mismatched leftover of the old theme.
            colors.Accent = new Color(255, 0, 200);
            colors.Text = new Color(255, 250, 200);
            colors.Muted = new Color(140, 110, 160);
            colors.Surface1 = new Color(40, 10, 40);
            colors.Surface2 = new Color(55, 15, 55);
            text.FontFamily = "Consolas";
            text.FontSize = 17;
        }
        else
        {
            colors.Accent = new Color(0, 229, 255);
            colors.Text = new Color(200, 250, 255);
            colors.Muted = new Color(74, 96, 128);
            colors.Surface1 = new Color(13, 13, 34);
            colors.Surface2 = new Color(19, 19, 48);
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
            // Surface also drives the showcase's own clear color (via GetClearColor), so
            // this is the one theme where "tint everything" includes the window backdrop.
            colors.Surface = new Color(0, 128, 128);
            colors.WhiteFill = new Color(220, 255, 255);
            colors.Selection = new Color(128, 0, 32);
            colors.HighlightInner = new Color(150, 220, 220);
            colors.HighlightOuter = new Color(200, 255, 255);
            colors.ShadowInner = new Color(0, 80, 80);
            colors.ShadowOuter = new Color(0, 50, 50);
            colors.DisabledText = new Color(110, 70, 70);
            text.FontFamily = "Consolas";
            text.FontSize = 15;
        }
        else
        {
            colors.Surface = new Color(192, 192, 192);
            colors.WhiteFill = new Color(255, 255, 255);
            colors.Selection = new Color(0, 0, 128);
            colors.HighlightInner = new Color(223, 223, 223);
            colors.HighlightOuter = new Color(255, 255, 255);
            colors.ShadowInner = new Color(128, 128, 128);
            colors.ShadowOuter = new Color(64, 64, 64);
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
            // Blue/BlueDark/BlueHover are the button's rest/pressed/hover fills — changing
            // only Blue left the shadow and hover states stuck on the old sky-blue gradient.
            // SageDark is the checkbox/radio checked color; PeachDark is the shared
            // border/outline token (ListBox, input fields, dashed panels, splitter).
            // Cream/Cream2 are the page and panel backgrounds.
            colors.Blue = new Color(230, 90, 70);
            colors.BlueDark = new Color(150, 45, 35);
            colors.BlueHover = new Color(245, 130, 105);
            colors.TealDark = new Color(90, 40, 80);
            colors.Muted = new Color(170, 130, 150);
            colors.SageDark = new Color(170, 90, 140);
            colors.PeachDark = new Color(200, 150, 175);
            colors.Cream = new Color(238, 222, 235);
            colors.Cream2 = new Color(245, 232, 242);
            text.FontFamily = "Consolas";
            text.FontSize = 17;
        }
        else
        {
            colors.Blue = new Color(70, 173, 230);
            colors.BlueDark = new Color(46, 147, 210);
            colors.BlueHover = new Color(94, 187, 240);
            colors.TealDark = new Color(30, 106, 91);
            colors.Muted = new Color(180, 156, 132);
            colors.SageDark = new Color(132, 194, 166);
            colors.PeachDark = new Color(239, 200, 160);
            colors.Cream = new Color(247, 237, 214);
            colors.Cream2 = new Color(252, 245, 230);
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
            // Selection is the ListBox/MenuItem selected-row fill and TextBright is its
            // hover-row text — both defaulted to the same hazard-yellow as Accent, so only
            // changing Accent left selected/hovered list rows still orange. AccentPressed is
            // a separate explicit token (not derived from Accent), so the Slider thumb's
            // pressed state was the same gap.
            colors.Accent = new Color(40, 140, 255);
            colors.Text = new Color(200, 230, 255);
            colors.Muted = new Color(90, 110, 140);
            colors.Selection = new Color(40, 140, 255);
            colors.TextBright = new Color(150, 200, 255);
            colors.AccentPressed = new Color(20, 100, 200);
            text.FontFamily = "Consolas";
            text.FontSize = 17;
        }
        else
        {
            colors.Accent = new Color(244, 200, 26);
            colors.Text = new Color(227, 181, 40);
            colors.Muted = new Color(120, 102, 38);
            colors.Selection = new Color(244, 200, 26);
            colors.TextBright = new Color(248, 212, 59);
            colors.AccentPressed = new Color(201, 163, 12);
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
