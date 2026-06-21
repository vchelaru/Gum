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
    public static void Main()
    {
        const int screenWidth = 1024;
        const int screenHeight = 768;

        GumService.Default.CanvasWidth = screenWidth;
        GumService.Default.CanvasHeight = screenHeight;

        SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(screenWidth, screenHeight, "Gum raylib - Dynamic Font Playground");

        GumService.Default.Initialize();
        GumService.Default.UseKeyboardDefaults();

        // KernSmith generates a font for any (family, size, style) on demand — this single line is
        // the only platform-specific difference from the MonoGame host's KernSmithFontCreator.
        CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();

        FontPlaygroundScreen.Build(GumService.Default.Root);

        while (!WindowShouldClose())
        {
            GumService.Default.Update(GetTime());

            BeginDrawing();
            ClearBackground(new Color(30, 30, 46, 255));

            GumService.Default.Draw();

            EndDrawing();
        }

        CloseWindow();
    }
}
