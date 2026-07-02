using System;
using Gum;
using Gum.Themes.Bubblegum;
using Gum.Themes.DarkPro;
using Gum.Themes.Editor;
using Gum.Themes.ForestGlade;
using Gum.Themes.Hazard;
using Gum.Themes.Meadow;
using Gum.Themes.Neon;
using Gum.Themes.Retro95;
using Gum.Themes.Template;
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

        public ThemeOption(string name, Action apply, Color clearColor)
        {
            Name = name;
            Apply = apply;
            ClearColor = clearColor;
        }
    }

    private static ThemeOption[] _themes;
    private static int _currentThemeIndex;
    private static Color _clearColor;
    private static ShowcaseScreen _currentScreen;
    private static Func<ShowcaseScreen> _currentScreenFactory;

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
            new ThemeOption("Editor", EditorTheme.Apply, new Color(40, 40, 40, 255)),
            new ThemeOption("Dark Pro", DarkProTheme.Apply, DarkProStyling.ActiveStyle.Colors.Background),
            new ThemeOption("Bubblegum", BubblegumTheme.Apply, BubblegumStyling.ActiveStyle.Colors.Background),
            new ThemeOption("Forest Glade", ForestGladeTheme.Apply, ForestGladeColors.CanopyDeep),
            new ThemeOption("Hazard", HazardTheme.Apply, HazardPalette.Background),
            new ThemeOption("Meadow", MeadowTheme.Apply, MeadowColors.Cream),
            new ThemeOption("Neon", NeonTheme.Apply, NeonColors.Background),
            new ThemeOption("Retro 95", Retro95Theme.Apply, Retro95Colors.Surface),
            new ThemeOption("Template", TemplateTheme.Apply, TemplatePalette.Background),
        };

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
                ApplyTheme(i);
                RebuildScreen();
                break;
            }
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
    }

    private static void SwitchScreen(Func<ShowcaseScreen> factory)
    {
        _currentScreenFactory = factory;
        _currentScreen?.Destroy();
        _currentScreen = factory();
        _currentScreen.Build();
    }
}
