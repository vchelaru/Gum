using Gum;
using KernSmith.Gum;
using Raylib_cs;
using RaylibGum.Renderables;
using static Raylib_cs.Raylib;

namespace FontPlayground.Raylib;

/// <summary>
/// Thin raylib host for the dynamic-font playground. All UI and live-update logic lives in the
/// platform-neutral <see cref="FontPlaygroundScreen"/> (the same file the MonoGame host links). This
/// host only bootstraps raylib + Gum, registers KernSmith for in-memory font generation, and pumps
/// the raylib frame loop.
///
/// Fonts are generated in memory by KernSmith — no .fnt files are shipped with this sample, and both
/// the Forms controls and the live preview text rasterize through the one creator registration.
/// </summary>
public static class Program
{
    static GumService GumUI => GumService.Default;

    public static void Main()
    {
        const int screenWidth = 1024;
        const int screenHeight = 768;

        SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(screenWidth, screenHeight, "Gum raylib - Dynamic Font Playground");

        GumUI.Initialize();

        GumUI.UseKeyboardDefaults();

        GumUI.UseGamepadDefaults();

        // KernSmith generates a font for any (family, size, style) on demand.
        GumUI.UseKernSmith();

        FontPlaygroundScreen.Build(GumUI.Root);

        while (!WindowShouldClose())
        {
            GumUI.Update(GetTime());

            BeginDrawing();
            ClearBackground(new Color(30, 30, 46, 255));

            GumUI.Draw();

            EndDrawing();
        }

        CloseWindow();
    }
}
