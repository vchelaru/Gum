using System;
using Gum;
using Gum.Themes.Editor;
using MonoGameGumThemesShowcase.Screens;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace RaylibGumThemesShowcase;

/// <summary>
/// raylib host for the Gum themes showcase. It applies a theme and drives the same
/// <see cref="ShowcaseScreen"/>s the MonoGame host (<c>MonoGameGumThemesShowcase</c>) uses — the
/// screen sources are source-shared between the two projects so the two render identically and can
/// be compared side by side. The host is otherwise stock raylib + Gum.
///
/// F1: all-controls screen. F2: screenshot panel. Only the Editor theme has a raylib variant so
/// far; as more theme variants land, add them to <see cref="ApplyTheme"/> (and a number-key swap,
/// mirroring the MonoGame host).
/// </summary>
public static class Program
{
    private static ShowcaseScreen _currentScreen;

    public static void Main()
    {
        const int screenWidth = 1400;
        const int screenHeight = 900;

        GumService.Default.CanvasWidth = screenWidth;
        GumService.Default.CanvasHeight = screenHeight;

        SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(screenWidth, screenHeight, "Gum raylib - Editor Theme Showcase  (F1/F2 swap screen)");

        GumService.Default.Initialize();
        GumService.Default.UseKeyboardDefaults();

        ApplyTheme();

        SwitchScreen(() => new AllControlsScreen());

        while (!WindowShouldClose())
        {
            if (IsKeyPressed(KeyboardKey.F1))
            {
                SwitchScreen(() => new AllControlsScreen());
            }
            else if (IsKeyPressed(KeyboardKey.F2))
            {
                SwitchScreen(() => new ScreenshotScreen());
            }

            BeginDrawing();
            ClearBackground(new Color(40, 40, 40, 255));

            GumService.Default.Update(GetTime());
            GumService.Default.Draw();

            EndDrawing();
        }

        CloseWindow();
    }

    // Installs the theme's default templates / styling. One parameterless call applies the theme on
    // every backend: it wires KernSmith for in-memory font generation (no .fnt files shipped) and
    // installs the Editor visuals as the defaults.
    private static void ApplyTheme()
    {
        EditorTheme.Apply();
    }

    private static void SwitchScreen(Func<ShowcaseScreen> factory)
    {
        _currentScreen?.Destroy();
        _currentScreen = factory();
        _currentScreen.Build();
    }
}
