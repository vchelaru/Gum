using System;
using Gum;
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
using MonoGameGumThemesShowcase.Screens;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace RaylibGumThemesShowcase;

/// <summary>
/// raylib host for the Gum themes showcase. It drives the same <see cref="ShowcaseScreen"/>s the
/// MonoGame host (<c>MonoGameGumThemesShowcase</c>) uses — the screen sources are source-shared
/// between the two projects, so the two render identically and can be compared side by side.
///
/// Number keys swap the active theme (only themes with a raylib variant are listed); F1/F2 swap the
/// screen. Add a theme to <see cref="_themes"/> as each gains a <c>.Raylib</c> variant.
/// </summary>
public static class Program
{
    private sealed class ThemeOption
    {
        public string Name { get; }
        public Action Apply { get; }
        public Color ClearColor { get; }
        public Action<bool> SetCustomized { get; }

        public ThemeOption(string name, Action apply, Color clearColor, Action<bool> setCustomized)
        {
            Name = name;
            Apply = apply;
            ClearColor = clearColor;
            SetCustomized = setCustomized;
        }
    }

    private static ThemeOption[] _themes;
    private static int _currentThemeIndex;
    private static Color _clearColor;
    private static ShowcaseScreen _currentScreen;
    private static Func<ShowcaseScreen> _currentScreenFactory;

    // Authoring-only toggle (not part of the ShowcaseScreen root bookkeeping) that lets a
    // maintainer preview a hardcoded per-theme customization before manually capturing
    // screenshots. See the SetXyzCustomized methods below for the per-theme values.
    private static CheckBox _customizeCheckBox;

    public static void Main()
    {
        const int screenWidth = 1400;
        const int screenHeight = 900;

        GumService.Default.CanvasWidth = screenWidth;
        GumService.Default.CanvasHeight = screenHeight;

        SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(screenWidth, screenHeight, "Gum raylib - Themes Showcase");

        GumService.Default.Initialize();
        GumService.Default.UseKeyboardDefaults();

        // Each theme's parameterless Apply() wires KernSmith for in-memory font generation and
        // installs that theme's visuals as the defaults. Editor has no named background color, so a
        // sensible dark surround is used.
        _themes = new[]
        {
            new ThemeOption("Editor", EditorTheme.Apply, new Color(40, 40, 40, 255), SetEditorCustomized),
            new ThemeOption("Dark Pro", DarkProTheme.Apply, DarkProStyling.ActiveStyle.Colors.Background, SetDarkProCustomized),
            new ThemeOption("Bubblegum", BubblegumTheme.Apply, BubblegumStyling.ActiveStyle.Colors.Background, SetBubblegumCustomized),
            new ThemeOption("Forest Glade", ForestGladeTheme.Apply, ForestGladeStyling.ActiveStyle.Colors.CanopyDeep, SetForestGladeCustomized),
            new ThemeOption("Hazard", HazardTheme.Apply, HazardStyling.ActiveStyle.Colors.Background, SetHazardCustomized),
            new ThemeOption("Meadow", MeadowTheme.Apply, MeadowStyling.ActiveStyle.Colors.Cream, SetMeadowCustomized),
            new ThemeOption("Neon", NeonTheme.Apply, NeonStyling.ActiveStyle.Colors.Background, SetNeonCustomized),
            new ThemeOption("Retro 95", Retro95Theme.Apply, Retro95Styling.ActiveStyle.Colors.Surface, SetRetro95Customized),
            new ThemeOption("Template", TemplateTheme.Apply, TemplateStyling.ActiveStyle.Colors.Background, SetTemplateCustomized),
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

        _currentScreenFactory = () => new AllControlsScreen();
        ApplyTheme(0);
        RebuildScreen();

        while (!WindowShouldClose())
        {
            HandleInput();

            GumService.Default.Update(GetTime());

            BeginDrawing();
            ClearBackground(_clearColor);

            GumService.Default.Draw();

            EndDrawing();
        }

        CloseWindow();
    }

    private static void HandleInput()
    {
        if (IsKeyPressed(KeyboardKey.F1))
        {
            SwitchScreen(() => new AllControlsScreen());
        }
        else if (IsKeyPressed(KeyboardKey.F2))
        {
            SwitchScreen(() => new ScreenshotScreen());
        }

        // Number keys 1..N swap the active theme and rebuild the current screen so its controls
        // re-resolve their visuals from the newly-installed default templates.
        for (int i = 0; i < _themes.Length; i++)
        {
            if (IsKeyPressed(KeyboardKey.One + i) && i != _currentThemeIndex)
            {
                RevertCustomizationIfChecked();
                ApplyTheme(i);
                RebuildScreen();
                break;
            }
        }
    }

    // If Show Customized is checked when the user swaps themes, revert the previous theme's
    // customization and uncheck the box first — don't let a customization silently persist
    // onto/across a theme switch, and don't auto-apply the new theme's customization.
    private static void RevertCustomizationIfChecked()
    {
        if (_customizeCheckBox.IsChecked == true)
        {
            _customizeCheckBox.IsChecked = false;
        }
    }

    private static void ApplyTheme(int index)
    {
        _currentThemeIndex = index;
        ThemeOption theme = _themes[index];
        theme.Apply();
        _clearColor = theme.ClearColor;
        SetWindowTitle($"Gum raylib - Themes Showcase  -  {index + 1}. {theme.Name}  (1-{_themes.Length} theme, F1/F2 screen)");
    }

    private static void RebuildScreen()
    {
        SwitchScreen(_currentScreenFactory);

        // Root.Children.Add appends, and later children paint on top in Gum, so every
        // ShowcaseScreen rebuild re-adds fresh controls AFTER _customizeCheckBox in the
        // root's child list — burying it behind the new screen unless it's re-parented to
        // the end again here.
        _customizeCheckBox.RemoveFromRoot();
        _customizeCheckBox.AddToRoot();
    }

    private static void SwitchScreen(Func<ShowcaseScreen> factory)
    {
        _currentScreenFactory = factory;
        _currentScreen?.Destroy();
        _currentScreen = factory();
        _currentScreen.Build();
    }

    // --- Hardcoded per-theme "Show Customized" preview values --------------------
    // Authoring tooling only, used by the Show Customized checkbox above. These values are not a
    // shared source of truth with any documentation example — they may drift over time.

    private static void SetEditorCustomized(bool customized)
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

    private static void SetDarkProCustomized(bool customized)
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

    private static void SetBubblegumCustomized(bool customized)
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

    private static void SetForestGladeCustomized(bool customized)
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

    private static void SetNeonCustomized(bool customized)
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

    private static void SetRetro95Customized(bool customized)
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

    private static void SetMeadowCustomized(bool customized)
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

    private static void SetHazardCustomized(bool customized)
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

    private static void SetTemplateCustomized(bool customized)
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
